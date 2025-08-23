from deepface import DeepFace
import cv2

class GenderClassifier:
    def __init__(self):
        self.detector_backend = 'retinaface'

    def predict(self, bgr_frame):
        rgb = cv2.cvtColor(bgr_frame, cv2.COLOR_BGR2RGB)
        try:
            objs = DeepFace.analyze(
                rgb, actions=['gender'], detector_backend=self.detector_backend,
                enforce_detection=False
            )
            if isinstance(objs, list) and len(objs) > 0:
                obj = objs[0]
            else:
                obj = objs
            gender = obj.get('gender')
            if isinstance(gender, dict):
                label = 'female' if (gender.get('Woman', 0) >= gender.get('Man', 0)) else 'male'
            else:
                label = 'unknown'
            return label
        except Exception:
            return 'unknown'
