using System.Collections.Generic;
using UnityEngine;

public class PoseMapper
{
    Animator _anim;

    // Low-pass per key
    Dictionary<string, JointLowPass> _lp = new Dictionary<string, JointLowPass>();

    // Cached transforms
    readonly Dictionary<HumanBodyBones, Transform> _t = new Dictionary<HumanBodyBones, Transform>();

    // Local aim axis per bone (auto-detected from bind pose)
    readonly Dictionary<HumanBodyBones, Vector3> _aimAxis = new Dictionary<HumanBodyBones, Vector3>();

    public void Bind(Animator anim)
    {
        _anim = anim;
        _t.Clear(); _aimAxis.Clear(); _lp.Clear();

        // Cache bones
        Cache(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm);
        Cache(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        Cache(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
        Cache(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);

        Cache(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg);
        Cache(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
        Cache(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg);
        Cache(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

        Cache(HumanBodyBones.Spine);
        Cache(HumanBodyBones.Chest);
        Cache(HumanBodyBones.Neck);
        Cache(HumanBodyBones.Hips);
    }

    void Cache(HumanBodyBones bone, HumanBodyBones childGuess = HumanBodyBones.LastBone)
    {
        var tr = _anim.GetBoneTransform(bone);
        if (!tr) return;
        _t[bone] = tr;

        // Auto-detect limb aim axis from bind pose using child direction
        if (childGuess != HumanBodyBones.LastBone)
        {
            var ch = _anim.GetBoneTransform(childGuess);
            if (ch)
            {
                var dirWS = (ch.position - tr.position).normalized;
                var dirLS = tr.InverseTransformDirection(dirWS).normalized;

                // Pick the closest principal axis ±X/±Y/±Z
                Vector3[] axes = { Vector3.right, -Vector3.right, Vector3.up, -Vector3.up, Vector3.forward, -Vector3.forward };
                float best = -1f; Vector3 bestAxis = Vector3.right;
                foreach (var ax in axes)
                {
                    float d = Vector3.Dot(dirLS, ax);
                    if (d > best) { best = d; bestAxis = ax; }
                }
                _aimAxis[bone] = bestAxis;
            }
        }
    }

    Vector3 GetVec(Dictionary<string, float[]> bones, string key)
    {
        if (bones != null && bones.TryGetValue(key, out var v) && v != null && v.Length >= 3)
            return new Vector3(v[0], v[1], v[2]);
        return Vector3.zero;
    }

    Vector3 LP(string key, Vector3 v)
    {
        if (!_lp.ContainsKey(key)) _lp[key] = new JointLowPass(0.8f);
        return _lp[key].Step(v);
    }

    static void AimToward(Transform tr, Vector3 worldDir, Vector3 localAimAxis, float slerp = 0.9f)
    {
        if (!tr) return;
        var d = worldDir;
        if (d.sqrMagnitude < 1e-8f) return;

        // Rotate so that the bone’s localAimAxis (in world) points to worldDir
        var currentAxisWS = tr.TransformDirection(localAimAxis);
        var delta = Quaternion.FromToRotation(currentAxisWS, d.normalized);
        var target = delta * tr.rotation;
        tr.rotation = Quaternion.Slerp(tr.rotation, target, slerp);
    }

    public void Apply(Dictionary<string, float[]> bones, Transform root)
    {
        if (_anim == null) return;

        // ---------- Torso ----------
        // Use ChestForward/ChestUp if present; otherwise keep animation
        var chestFwd = GetVec(bones, "ChestForward");
        var chestUp = GetVec(bones, "ChestUp");
        if (_t.TryGetValue(HumanBodyBones.Chest, out var chest))
        {
            var f = LP("ChestFwd", chestFwd);
            var u = LP("ChestUp", chestUp);
            if (f != Vector3.zero)
            {
                // Build world rotation from forward/up
                var target = Quaternion.LookRotation(f.normalized, (u == Vector3.zero ? Vector3.up : u.normalized));
                chest.rotation = Quaternion.Slerp(chest.rotation, target, 0.75f);
            }
        }

        // ---------- Limbs ----------
        void AimIf(HumanBodyBones b, string key)
        {
            if (!_t.TryGetValue(b, out var tr)) return;
            var dir = LP(key, GetVec(bones, key));
            if (dir == Vector3.zero) return;

            var aim = _aimAxis.TryGetValue(b, out var ax) ? ax : Vector3.right; // fallback
            AimToward(tr, dir, aim, 0.9f);
        }

        // Arms
        AimIf(HumanBodyBones.LeftUpperArm, "LeftUpperArm");
        AimIf(HumanBodyBones.LeftLowerArm, "LeftLowerArm");
        AimIf(HumanBodyBones.RightUpperArm, "RightUpperArm");
        AimIf(HumanBodyBones.RightLowerArm, "RightLowerArm");

        // Legs
        AimIf(HumanBodyBones.LeftUpperLeg, "LeftUpperLeg");
        AimIf(HumanBodyBones.LeftLowerLeg, "LeftLowerLeg");
        AimIf(HumanBodyBones.RightUpperLeg, "RightUpperLeg");
        AimIf(HumanBodyBones.RightLowerLeg, "RightLowerLeg");
    }
}
