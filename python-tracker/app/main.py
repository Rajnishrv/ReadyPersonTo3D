# main.py — low-latency pose + robust, non-blocking gender
import cv2, threading, time, collections
from mediapipe_pose import MPose
from feature_utils import normalize_pose
from smoothing import BoneSmoother
from pose_sender import PoseSender
from person_gate import PersonGate
from gender import GenderClassifier

# --------- Tuning ---------
MODEL_COMPLEXITY = 0          # fast & stable
SMOOTH_BETA = 0.65
GENDER_INTERVAL_SEC = 3.0     # run gender every N seconds (background)
CONSENSUS_WINDOW = 7          # frames of gender votes to consider
CONSENSUS_MIN_AGREE = 4       # votes needed to flip label

# --------- Background gender worker ---------
class GenderWorker:
    def __init__(self):
        self.gc = GenderClassifier()
        self.raw = "unknown"
        self._busy = threading.Event()
        self._last_ts = 0.0

    def maybe_kick(self, frame_bgr, landmarks, interval_sec=GENDER_INTERVAL_SEC):
        now = time.time()
        if self._busy.is_set(): return
        if now - self._last_ts < interval_sec: return
        self._busy.set()
        threading.Thread(target=self._run, args=(frame_bgr.copy(), landmarks), daemon=True).start()

    def _run(self, frame_bgr, landmarks):
        try:
            self.raw = self.gc.predict(frame_bgr, landmarks=landmarks) or "unknown"
            self._last_ts = time.time()
        except Exception:
            self.raw = "unknown"
        finally:
            self._busy.clear()

class GenderConsensus:
    def __init__(self, window=CONSENSUS_WINDOW, min_agree=CONSENSUS_MIN_AGREE):
        self.buf = collections.deque(maxlen=window)
        self.min_agree = int(min_agree)
        self.current = "unknown"
    def update(self, raw_label: str) -> str:
        lab = (raw_label or "unknown").lower()
        self.buf.append(lab)
        male = sum(1 for x in self.buf if x == "male")
        fem  = sum(1 for x in self.buf if x == "female")
        cand = None
        if male >= self.min_agree: cand = "male"
        if fem  >= self.min_agree and fem >= male: cand = "female"
        if cand and cand != self.current:
            self.current = cand
        return self.current

def main():
    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
    cap.set(cv2.CAP_PROP_FPS, 30)
    # Uncomment for even lower latency on many webcams:
    # cap.set(cv2.CAP_PROP_FOURCC, cv2.VideoWriter_fourcc(*"MJPG"))
    # cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)

    mp_pose = MPose(model_complexity=MODEL_COMPLEXITY)
    smoother = BoneSmoother(beta=SMOOTH_BETA)
    sender   = PoseSender(port=5056)
    gate     = PersonGate(stable_frames=6, absent_timeout=0.6)

    g_worker = GenderWorker()
    g_cons   = GenderConsensus()

    print("[INFO] Streaming… press Q to quit")
    while True:
        ok, frame = cap.read()
        if not ok: continue

        lms = mp_pose.process(frame)            # (33,4) or None
        detected = lms is not None
        event = gate.step(detected)

        if detected:
            # Non-blocking gender update
            g_worker.maybe_kick(frame, lms, GENDER_INTERVAL_SEC)
            current_gender = g_cons.update(g_worker.raw)

            # Build/send bones (your existing mapping)
            bones = normalize_pose(lms)
            bones = smoother.smooth_dict(bones)
            sender.send(current_gender, event, bones)

            # Debug overlay
            cv2.putText(frame, f"Gender raw: {g_worker.raw}  ->  final: {current_gender}",
                        (12, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0,255,0), 2)
        else:
            if event == "left":
                sender.send(g_cons.current, event, {})

        cv2.imshow("Tracker", frame)
        if (cv2.waitKey(1) & 0xFF) in (ord('q'), 27):
            break

    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
