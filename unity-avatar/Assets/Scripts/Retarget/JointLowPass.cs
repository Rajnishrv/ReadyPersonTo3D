using UnityEngine;

public class JointLowPass
{
    readonly float alpha;
    Vector3 y;
    bool has;

    public JointLowPass(float alpha = 0.5f) { this.alpha = Mathf.Clamp01(alpha); }

    public Vector3 Step(Vector3 x)
    {
        if (!has) { y = x; has = true; return y; }
        y = (1f - alpha) * y + alpha * x;
        return y;
    }
}
