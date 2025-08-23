using System.Collections.Generic;
using UnityEngine;

public class PoseMapper
{
    Animator _anim;
    readonly Dictionary<string, JointLowPass> _lp = new Dictionary<string, JointLowPass>();
    readonly Dictionary<HumanBodyBones, Transform> _t = new Dictionary<HumanBodyBones, Transform>();

    public void Bind(Animator anim)
    {
        _anim = anim;
        Cache(HumanBodyBones.LeftUpperArm);
        Cache(HumanBodyBones.LeftLowerArm);
        Cache(HumanBodyBones.RightUpperArm);
        Cache(HumanBodyBones.RightLowerArm);
        Cache(HumanBodyBones.LeftUpperLeg);
        Cache(HumanBodyBones.LeftLowerLeg);
        Cache(HumanBodyBones.RightUpperLeg);
        Cache(HumanBodyBones.RightLowerLeg);
        Cache(HumanBodyBones.Spine);
        Cache(HumanBodyBones.Chest);
        Cache(HumanBodyBones.Neck);
        Cache(HumanBodyBones.Hips);
    }

    void Cache(HumanBodyBones b)
    {
        var tr = _anim.GetBoneTransform(b);
        if (tr) _t[b] = tr;
    }

    Vector3 LP(string key, Vector3 v)
    {
        if (!_lp.ContainsKey(key)) _lp[key] = new JointLowPass(0.5f);
        return _lp[key].Step(v);
    }

    static void RotateTowardWorldDir(Transform tr, Vector3 worldDir, float slerp = 0.75f, Vector3 localAxis = default)
    {
        if (!tr) return;
        if (worldDir.sqrMagnitude < 1e-6f) return;

        if (localAxis == default) localAxis = Vector3.right; // <- MIXAMO/RPM use +X
        var currentAxisWS = tr.TransformDirection(localAxis);
        var delta = Quaternion.FromToRotation(currentAxisWS, worldDir.normalized);
        var targetWorld = delta * tr.rotation;
        tr.rotation = Quaternion.Slerp(tr.rotation, targetWorld, slerp);
    }

    Vector3 GetVec(Dictionary<string, float[]> bones, string key)
    {
        if (bones.TryGetValue(key, out var v))
            return new Vector3(v[0], v[1], v[2]);
        return Vector3.zero;
    }

    public void Apply(Dictionary<string, float[]> bones, Transform root = null)
    {
        if (_anim == null) return;

        var chestFwd = LP("ChestFwd", GetVec(bones, "ChestForward"));
        var chestUp = LP("ChestUp", GetVec(bones, "ChestUp"));

        if (_t.TryGetValue(HumanBodyBones.Chest, out var chest))
        {
            if (chestFwd != Vector3.zero)
            {
                var up = (chestUp != Vector3.zero) ? chestUp.normalized : Vector3.up;
                var target = Quaternion.LookRotation(chestFwd.normalized, up);
                chest.rotation = Quaternion.Slerp(chest.rotation, target, 0.65f);
            }
        }

        void Aim(HumanBodyBones bone, string key)
        {
            if (_t.TryGetValue(bone, out var tr))
            {
                var dir = LP(bone.ToString(), GetVec(bones, key));
                if (dir != Vector3.zero)
                    RotateTowardWorldDir(tr, dir, 0.8f, Vector3.right);
            }
        }

        // Arms
        Aim(HumanBodyBones.LeftUpperArm, "LeftUpperArm");
        Aim(HumanBodyBones.LeftLowerArm, "LeftLowerArm");
        Aim(HumanBodyBones.RightUpperArm, "RightUpperArm");
        Aim(HumanBodyBones.RightLowerArm, "RightLowerArm");

        // Legs
        Aim(HumanBodyBones.LeftUpperLeg, "LeftUpperLeg");
        Aim(HumanBodyBones.LeftLowerLeg, "LeftLowerLeg");
        Aim(HumanBodyBones.RightUpperLeg, "RightUpperLeg");
        Aim(HumanBodyBones.RightLowerLeg, "RightLowerLeg");
    }
}
