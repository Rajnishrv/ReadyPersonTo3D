import numpy as np

KEYS = {
    "LShoulder": 11, "RShoulder": 12, "LElbow": 13, "RElbow": 14, "LWrist": 15, "RWrist": 16,
    "LHip": 23, "RHip": 24, "LKnee": 25, "RKnee": 26, "LAnkle": 27, "RAnkle": 28
}

PAIRS = [
    ("Spine", 23, 11),
    ("Spine2", 24, 12),
    ("Neck", 11, 12),
    ("LeftUpperArm", KEYS["LShoulder"], KEYS["LElbow"]),
    ("LeftLowerArm", KEYS["LElbow"], KEYS["LWrist"]),
    ("RightUpperArm", KEYS["RShoulder"], KEYS["RElbow"]),
    ("RightLowerArm", KEYS["RElbow"], KEYS["RWrist"]),
    ("LeftUpperLeg", KEYS["LHip"], KEYS["LKnee"]),
    ("LeftLowerLeg", KEYS["LKnee"], KEYS["LAnkle"]),
    ("RightUpperLeg", KEYS["RHip"], KEYS["RKnee"]),
    ("RightLowerLeg", KEYS["RKnee"], KEYS["RAnkle"]),
]

def unit(v):
    n = np.linalg.norm(v)
    if n < 1e-6: return np.zeros_like(v)
    return v / n

def normalize_pose(landmarks, vis_thresh=0.6):
    """
    landmarks: (33,4) -> x,y,z,visibility (image space)
    Returns dict of bone unit directions (0 vectors if not confidently visible).
    """
    pts = landmarks[:, :3].astype(np.float32).copy()
    vis = landmarks[:, 3] if landmarks.shape[1] > 3 else np.ones((pts.shape[0],), np.float32)

    # Flip Y so +Y is up in Unity terms
    pts[:, 1] = 1.0 - pts[:, 1]

    lhip, rhip = pts[23], pts[24]
    lsh,  rsh  = pts[11], pts[12]
    pelvis = 0.5 * (lhip + rhip)
    shoulders_center = 0.5 * (lsh + rsh)
    shoulder_span = np.linalg.norm(rsh - lsh)
    scale = shoulder_span if shoulder_span > 1e-6 else 1.0
    pts = (pts - pelvis) / scale

    bones = {}
    for name, a, b in PAIRS:
        ok = (vis[a] >= vis_thresh) and (vis[b] >= vis_thresh)
        va, vb = pts[a], pts[b]
        bones[name] = unit(vb - va) if ok else np.zeros(3, dtype=np.float32)

    chest_ok = (vis[11] >= vis_thresh) and (vis[12] >= vis_thresh) and (vis[23] >= vis_thresh) and (vis[24] >= vis_thresh)
    if chest_ok:
        chest_right = unit(rsh - lsh)
        chest_up    = unit(shoulders_center - pelvis)
        chest_fwd   = unit(np.cross(chest_right, chest_up))
    else:
        chest_right = chest_up = chest_fwd = np.zeros(3, dtype=np.float32)

    bones["ChestForward"] = chest_fwd
    bones["ChestUp"]      = chest_up
    bones["ChestRight"]   = chest_right
    return bones
