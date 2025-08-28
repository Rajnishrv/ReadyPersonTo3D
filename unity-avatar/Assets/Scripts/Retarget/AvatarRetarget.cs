using System.Collections.Generic;
using UnityEngine;

public class AvatarRetarget : MonoBehaviour
{
    public Animator animator;
    PoseMapper mapper = new PoseMapper();

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator) mapper.Bind(animator);
    }

    public void ApplyBones(Dictionary<string, float[]> bones)
    {
        mapper.Apply(bones, transform);
    }
}
