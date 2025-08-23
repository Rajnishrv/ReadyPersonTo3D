# mediapipe_pose.py
import mediapipe as mp
import numpy as np
import cv2

class MPose:
    def __init__(self, **pose_kwargs):
        # sensible defaults; caller can override
        defaults = dict(
            static_image_mode=False,
            model_complexity=1,
            smooth_landmarks=True,
            enable_segmentation=False,
            min_detection_confidence=0.6,
            min_tracking_confidence=0.6,
        )
        defaults.update(pose_kwargs)
        self._mp = mp.solutions.pose
        self._pose = self._mp.Pose(**defaults)
        self._drawer = mp.solutions.drawing_utils
        self._style = mp.solutions.drawing_styles

    def process(self, frame_bgr):
        rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        res = self._pose.process(rgb)
        if not res.pose_landmarks:
            return None
        lms = res.pose_landmarks.landmark
        arr = np.array([[lm.x, lm.y, lm.z, lm.visibility] for lm in lms], dtype=np.float32)
        return arr

    def draw_landmarks(self, frame_bgr, arr_or_none):
        if arr_or_none is None:
            return
        # Rebuild a LandmarkList for drawing
        lm_list = [self._mp.PoseLandmark(i) for i in range(len(self._mp.PoseLandmark))]
        # Use MediaPipe utilities directly when you already have results to draw.
        # If you need drawing, call Pose once and keep its results, or adapt this to your pipeline.
        # (Optional: skip drawing to save time.)
