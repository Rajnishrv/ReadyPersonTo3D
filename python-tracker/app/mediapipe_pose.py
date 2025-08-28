import mediapipe as mp
import numpy as np
import cv2

class MPose:
    def __init__(self,
                 static_image_mode=False,
                 model_complexity=1,
                 smooth_landmarks=True,
                 min_detection_confidence=0.6,
                 min_tracking_confidence=0.6):
        self.pose = mp.solutions.pose.Pose(
            static_image_mode=static_image_mode,
            model_complexity=model_complexity,
            smooth_landmarks=smooth_landmarks,
            enable_segmentation=False,
            min_detection_confidence=min_detection_confidence,
            min_tracking_confidence=min_tracking_confidence
        )

    def process(self, frame_bgr):
        rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        res = self.pose.process(rgb)
        if not res.pose_landmarks:
            return None
        lms = res.pose_landmarks.landmark
        # [x,y,z,visibility] in image space; y flipped later in feature_utils
        arr = np.array([[lm.x, lm.y, lm.z, lm.visibility] for lm in lms], dtype=np.float32)
        return arr
