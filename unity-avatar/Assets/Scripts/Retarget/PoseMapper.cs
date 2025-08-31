using System.Collections.Generic;
using UnityEngine;

public class PoseMapper
{
    Animator _anim;

    Dictionary<string, JointLowPass> _lp = new Dictionary<string, JointLowPass>();
    readonly Dictionary<HumanBodyBones, Transform> _t = new Dictionary<HumanBodyBones, Transform>();
    readonly Dictionary<HumanBodyBones, Vector3> _aimAxis = new Dictionary<HumanBodyBones, Vector3>();

    // --- NEW: keep previous flat heading to prevent 180° flips when user turns ---
    Vector3 _prevHeading = Vector3.forward;
    bool _hasPrevHeading = false;

    public void Bind(Animator anim)
    {
        _anim = anim;
        _t.Clear(); _aimAxis.Clear(); _lp.Clear();
        _hasPrevHeading = false;

        // Cache limbs (child hints let us auto-detect local aim axis in bind pose)
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

        // Auto-detect limb local aim axis from bind pose using child direction
        if (childGuess != HumanBodyBones.LastBone)
        {
            var ch = _anim.GetBoneTransform(childGuess);
            if (ch)
            {
                var dirWS = (ch.position - tr.position).normalized;
                var dirLS = tr.InverseTransformDirection(dirWS).normalized;

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

    Vector3 LP(string key, Vector3 v, float a = 0.8f)
    {
        if (!_lp.ContainsKey(key)) _lp[key] = new JointLowPass(a);
        return _lp[key].Step(v);
    }

    static void AimToward(Transform tr, Vector3 worldDir, Vector3 localAimAxis, float slerp = 0.9f)
    {
        if (!tr) return;
        var d = worldDir;
        if (d.sqrMagnitude < 1e-8f) return;

        var currentAxisWS = tr.TransformDirection(localAimAxis);
        var delta = Quaternion.FromToRotation(currentAxisWS, d.normalized);
        var target = delta * tr.rotation;
        tr.rotation = Quaternion.Slerp(tr.rotation, target, slerp);
    }

    // --- NEW: robust heading from any forward vector, yaw-only and continuous ---
    Vector3 SolveFlatHeading(Vector3 forwardWS)
    {
        var up = Vector3.up;
        var flat = Vector3.ProjectOnPlane(forwardWS, up).normalized;
        if (flat == Vector3.zero)
            flat = _hasPrevHeading ? _prevHeading : Vector3.forward;

        if (_hasPrevHeading)
        {
            // Pick orientation (flat vs -flat) that keeps continuity with previous heading
            if (Vector3.Dot(flat, _prevHeading) < 0f)
                flat = -flat;
            // Smooth a little to avoid jitter
            flat = Vector3.Slerp(_prevHeading, flat, 0.6f).normalized;
        }

        _prevHeading = flat;
        _hasPrevHeading = true;
        return flat;
    }

    // --- NEW: apply only yaw to a transform, preserving its roll/pitch ---
    static void ApplyYawOnly(Transform tr, Vector3 targetHeading, float lerp = 0.8f)
    {
        if (!tr) return;
        var up = Vector3.up;

        // Current yaw-only orientation of the bone
        var curFwdFlat = Vector3.ProjectOnPlane(tr.forward, up);
        if (curFwdFlat.sqrMagnitude < 1e-8f) curFwdFlat = tr.forward;
        var curYaw = Quaternion.LookRotation(curFwdFlat.normalized, up);

        // Desired yaw-only orientation from target heading
        var tgtFwdFlat = Vector3.ProjectOnPlane(targetHeading, up);
        if (tgtFwdFlat.sqrMagnitude < 1e-8f) tgtFwdFlat = targetHeading;
        var tgtYaw = Quaternion.LookRotation(tgtFwdFlat.normalized, up);

        // Delta that changes only yaw; then apply on top of current full rotation
        var delta = Quaternion.Inverse(curYaw) * tgtYaw;
        tr.rotation = Quaternion.Slerp(tr.rotation, delta * tr.rotation, lerp);
    }

    public void Apply(Dictionary<string, float[]> bones, Transform root)
    {
        if (_anim == null) return;

        // ---------- 0) Compute scene heading (from chest or body forward), yaw-only & continuous ----------
        // Prefer a body/chest forward vector from sender; fall back to chest forward if provided; else keep last
        Vector3 headingIn = GetVec(bones, "BodyForward");
        if (headingIn == Vector3.zero)
            headingIn = GetVec(bones, "ChestForward");
        if (headingIn == Vector3.zero && _hasPrevHeading)
            headingIn = _prevHeading;

        var flatHeading = SolveFlatHeading(headingIn == Vector3.zero ? Vector3.forward : headingIn);

        // Apply yaw only to Hips (root of humanoid). This prevents lower-body flipping when user turns around.
        if (_t.TryGetValue(HumanBodyBones.Hips, out var hips))
        {
            ApplyYawOnly(hips, flatHeading, 0.9f);
        }

        // ---------- 1) Torso orientation (still use forward/up if available) ----------
        if (_t.TryGetValue(HumanBodyBones.Chest, out var chest))
        {
            var chestFwd = LP("ChestFwd", GetVec(bones, "ChestForward"), 0.7f);
            var chestUp = LP("ChestUp", GetVec(bones, "ChestUp"), 0.7f);

            if (chestFwd != Vector3.zero)
            {
                var up = (chestUp == Vector3.zero) ? Vector3.up : chestUp.normalized;
                var target = Quaternion.LookRotation(chestFwd.normalized, up);
                chest.rotation = Quaternion.Slerp(chest.rotation, target, 0.75f);
            }
        }

        // ---------- 2) Limbs (unchanged aim logic) ----------
        void AimIf(HumanBodyBones b, string key)
        {
            if (!_t.TryGetValue(b, out var tr)) return;
            var dir = LP(key, GetVec(bones, key), 0.8f);
            if (dir == Vector3.zero) return;

            var aim = _aimAxis.TryGetValue(b, out var ax) ? ax : Vector3.right;
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
