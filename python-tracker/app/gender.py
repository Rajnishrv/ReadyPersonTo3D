# gender.py — fast gender (OpenCV Haar + Tiny CNN), auto-download & input-shape aware
import os
import cv2
import numpy as np

# Quiet TF logs
os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")

MODEL_FILENAME = "gender_net.h5"
MODEL_URL = (
    "https://github.com/oarriaga/face_classification/"
    "raw/master/trained_models/gender_models/simple_CNN.81-0.96.hdf5"
)

def _download_model_if_needed(path: str) -> bool:
    if os.path.isfile(path):
        return True
    try:
        import urllib.request
        os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
        print(f"[gender] Downloading model to {path} ...")
        urllib.request.urlretrieve(MODEL_URL, path)
        print("[gender] Download complete.")
        return True
    except Exception as e:
        print(f"[gender] Model download failed: {e}")
        return False

class _TinyCNN:
    """Lazy-load Keras model and adapt preprocessing to model.input_shape."""
    def __init__(self, model_path: str):
        self.model_path = model_path
        self.model = None
        self.labels = ["female", "male"]
        self.target_h = 48
        self.target_w = 48
        self.target_c = 3  # default; will be overridden by model.input_shape

    def _ensure_loaded(self) -> bool:
        if self.model is not None:
            return True
        if not _download_model_if_needed(self.model_path):
            return False
        try:
            from tensorflow.keras.models import load_model
            self.model = load_model(self.model_path)
            # input_shape like (None, H, W, C) or (None, H, W)
            ish = getattr(self.model, "input_shape", None)
            if isinstance(ish, (list, tuple)) and len(ish) >= 2:
                shp = ish[1]  # (H, W, C) or (H, W)
                if isinstance(shp, (list, tuple)):
                    if len(shp) == 3:
                        self.target_h, self.target_w, self.target_c = int(shp[0]), int(shp[1]), int(shp[2])
                    elif len(shp) == 2:
                        self.target_h, self.target_w = int(shp[0]), int(shp[1])
                        self.target_c = 1
            return True
        except Exception as e:
            print(f"[gender] Failed to load Keras model: {e}")
            return False

    def _prep(self, face_bgr: np.ndarray) -> np.ndarray:
        """
        Convert BGR crop to model's expected input:
        - resize to (target_w, target_h)
        - if target_c == 1 -> grayscale [H,W,1]
        - if target_c == 3 -> RGB [H,W,3]
        - normalize to [0,1]
        - add batch dim -> [1,H,W,C]
        """
        h, w = self.target_h, self.target_w
        c = self.target_c
        if c == 1:
            gray = cv2.cvtColor(face_bgr, cv2.COLOR_BGR2GRAY)
            img = cv2.resize(gray, (w, h)).astype(np.float32) / 255.0
            img = np.expand_dims(img, axis=-1)  # [H,W,1]
        else:
            # model expects 3 channels; convert to RGB
            rgb = cv2.cvtColor(face_bgr, cv2.COLOR_BGR2RGB)
            img = cv2.resize(rgb, (w, h)).astype(np.float32) / 255.0
        img = np.expand_dims(img, axis=0)  # [1,H,W,C] or [1,H,W,1]
        return img

    def predict_label(self, face_bgr: np.ndarray) -> str:
        if not self._ensure_loaded():
            return "unknown"
        try:
            x = self._prep(face_bgr)
            pred = self.model.predict(x, verbose=0)[0]
            idx = int(np.argmax(pred))
            return self.labels[idx] if 0 <= idx < len(self.labels) else "unknown"
        except Exception as e:
            print(f"[gender] Inference error: {e}")
            return "unknown"

class GenderClassifier:
    """
    CPU-only, fast:
      - Detects face with OpenCV Haar (very fast)
      - Classifies with tiny CNN (auto-downloads once)
      - Adapts to model input size (fixes 48x48 vs 64x64 mismatch)
      - On any failure, returns 'unknown' (never crashes)
    Works with your existing threaded GenderWorker.
    """
    def __init__(self, model_path: str = MODEL_FILENAME):
        cascade_path = cv2.data.haarcascades + "haarcascade_frontalface_default.xml"
        self.face_cascade = cv2.CascadeClassifier(cascade_path)
        if self.face_cascade.empty():
            print("[gender] WARNING: haarcascade not found; check opencv-python install.")
        self.cnn = _TinyCNN(model_path)

    def predict(self, frame_bgr, landmarks=None) -> str:
        if frame_bgr is None or getattr(frame_bgr, "size", 0) == 0:
            return "unknown"

        try:
            gray = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2GRAY)
        except Exception:
            return "unknown"

        faces = self.face_cascade.detectMultiScale(
            gray, scaleFactor=1.2, minNeighbors=5, minSize=(60, 60)
        )
        if len(faces) == 0:
            return "unknown"

        # largest face
        x, y, w, h = max(faces, key=lambda b: b[2] * b[3])

        # small margin for crop
        H, W = frame_bgr.shape[:2]
        m = int(0.08 * max(w, h))
        x0 = max(0, x - m); y0 = max(0, y - m)
        x1 = min(W, x + w + m); y1 = min(H, y + h + m)

        face = frame_bgr[y0:y1, x0:x1]
        if face.size == 0:
            return "unknown"

        return self.cnn.predict_label(face)
