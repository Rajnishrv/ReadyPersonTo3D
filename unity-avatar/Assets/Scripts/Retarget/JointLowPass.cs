using UnityEngine;

public class JointLowPass
{
    readonly float _alpha;
    Vector3 _state;
    bool _has;

    public JointLowPass(float alpha = 0.8f) { _alpha = alpha; _has = false; }

    public Vector3 Step(Vector3 v)
    {
        if (v == Vector3.zero) { _has = false; return Vector3.zero; }
        if (!_has) { _state = v; _has = true; return _state; }
        _state = _alpha * _state + (1f - _alpha) * v;
        return _state;
    }
}
