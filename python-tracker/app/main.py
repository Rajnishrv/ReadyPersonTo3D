# main.py (snappy / non-blocking)
import cv2, threading, time
from mediapipe_pose import MPose
from feature_utils import normalize_pose
from smoothing import BoneSmoother
from pose_sender import PoseSender
from gender import GenderClassifier
from person_gate import PersonGate

GENDER_COOLDOWN_SEC = 1.5  # run gender at most this often

gender_cache = "unknown"
gender_lock = threading.Lock()
gender_busy = threading.Event()     # indicates a gender job is running
last_gender_ts = 0.0

def _quick_center_crop(frame, target_w=320):
    h, w = frame.shape[:2]
    # light, safe center crop; you can replace with a real face ROI if available
    side = int(min(h, w) * 0.6)
    cy, cx = h // 2, w // 2
    y0, y1 = max(0, cy - side//2), min(h, cy + side//2)
    x0, x1 = max(0, cx - side//2), min(w, cx + side//2)
    crop = frame[y0:y1, x0:x1]
    if crop.shape[1] > 0:
        scale = target_w / float(crop.shape[1])
        crop = cv2.resize(crop, (target_w, int(crop.shape[0]*scale)), interpolation=cv2.INTER_AREA)
    return crop

def gender_worker(gc, frame_bgr):
    global gender_cache, last_gender_ts
    try:
        crop = _quick_center_crop(frame_bgr, 320)
        label = gc.predict(crop)  # non-blocking for the main loop
        with gender_lock:
            gender_cache = label
            last_gender_ts = time.time()
    finally:
        gender_busy.clear()

def main():
    global last_gender_ts

    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
    # cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    # cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)

    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 580)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 320)
    # Reduce capture latency (most backends ignore this, but cheap when supported)
    cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)

    mp_pose = MPose()                          # no kwargs → matches your wrapper
    smoother = BoneSmoother(beta=0.7)
    sender = PoseSender(port=5056)
    gc = GenderClassifier()                    # can swap backend inside for speed
    gate = PersonGate(stable_frames=2, absent_timeout=0.5)  # faster enter/leave

    frame_i = 0

    while True:
        ok, frame = cap.read()
        if not ok:
            continue

        frame_i += 1

        # Pose (fast) — must not block.
        landmarks = mp_pose.process(frame)
        detected = landmarks is not None

        event = gate.step(detected)

        # Start gender job when needed, but never block the loop
        now = time.time()
        should_kickoff = (
            (event == "entered") or                 # on entry
            (now - last_gender_ts >= GENDER_COOLDOWN_SEC)
        )
        if should_kickoff and not gender_busy.is_set():
            gender_busy.set()
            threading.Thread(target=gender_worker, args=(gc, frame.copy()), daemon=True).start()

        # Use the latest available gender instantly
        with gender_lock:
            current_gender = gender_cache

        if detected:
            bones = normalize_pose(landmarks)
            bones = smoother.smooth_dict(bones)
            sender.send(current_gender, event, bones)

            cv2.putText(frame, f"Gender: {current_gender}", (20, 40),
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 2)
        else:
            if event == "left":
                sender.send("unknown", event, {})

        cv2.imshow("Tracker - Press Q to quit", frame)
        if (cv2.waitKey(1) & 0xFF) in (ord("q"), 27):
            break

    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    main()