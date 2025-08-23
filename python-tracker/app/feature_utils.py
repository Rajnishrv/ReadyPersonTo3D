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

def normalize_pose(landmarks):
    pts = landmarks[:, :3].copy()
    pts[:,1] = 1.0 - pts[:,1]  # invert y => up positive

    lhip = pts[23]; rhip = pts[24]
    lsh  = pts[11]; rsh  = pts[12]

    pelvis = 0.5*(lhip + rhip)
    shoulders_center = 0.5*(lsh + rsh)
    shoulder_span = np.linalg.norm(rsh - lsh)
    scale = shoulder_span if shoulder_span>1e-6 else 1.0

    pts = (pts - pelvis) / scale

    bones = {}
    for name, a, b in PAIRS:
        va = pts[a]; vb = pts[b]
        dirv = unit(vb - va)
        bones[name] = dirv
    chest_right = unit(rsh - lsh)
    chest_up    = unit(shoulders_center - pelvis)
    chest_fwd   = unit(np.cross(chest_right, chest_up))
    bones["ChestForward"] = chest_fwd
    bones["ChestUp"] = chest_up
    bones["ChestRight"] = chest_right
    return bones
