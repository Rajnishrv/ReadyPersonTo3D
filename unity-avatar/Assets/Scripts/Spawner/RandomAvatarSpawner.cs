using System.Collections.Generic;
using UnityEngine;

public class RandomAvatarSpawner : MonoBehaviour
{
    [Header("Spawn Parent/Transform")]
    public Transform SpawnRoot;
    public Vector3 SpawnPosition = Vector3.zero;
    public Vector3 SpawnEuler = new Vector3(0, 180, 0);
    public float Scale = 1.0f;

    [Header("Avatar Sources")]
    public bool useResources = true;
    [Tooltip("Used when useResources = false")]
    public GameObject[] malePrefabs;
    [Tooltip("Used when useResources = false")]
    public GameObject[] femalePrefabs;

    GameObject[] malePool;
    GameObject[] femalePool;

    GameObject _current;
    AvatarRetarget _retarget;

    void Start()
    {
        if (useResources)
        {
            malePool = Resources.LoadAll<GameObject>("Avatars/Male");
            femalePool = Resources.LoadAll<GameObject>("Avatars/Female");
        }
        else
        {
            malePool = malePrefabs;
            femalePool = femalePrefabs;
        }

        Debug.Log($"[Spawner] Male prefabs: {(malePool != null ? malePool.Length : 0)} | Female prefabs: {(femalePool != null ? femalePool.Length : 0)}");
    }

    public void SpawnNew(string gender)
    {
        Despawn();

        var g = (gender ?? "unknown").Trim().ToLowerInvariant();
        GameObject[] pool = null;
        if (g == "female") pool = femalePool;
        else if (g == "male") pool = malePool;

        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning($"[Spawner] No avatars found for gender: {g}. Check Resources/Avatars/{(g == "female" ? "Female" : "Male")} or assign arrays in inspector.");
            return;
        }

        var prefab = pool[Random.Range(0, pool.Length)];
        var parent = SpawnRoot ? SpawnRoot : transform;
        _current = Instantiate(prefab, parent);
        _current.transform.localPosition = SpawnPosition;
        _current.transform.localEulerAngles = SpawnEuler;
        _current.transform.localScale = Vector3.one * Scale;

        _retarget = _current.GetComponentInChildren<AvatarRetarget>();
        if (_retarget == null) _retarget = _current.AddComponent<AvatarRetarget>();
    }

    public void Despawn()
    {
        if (_current) Destroy(_current);
        _current = null; _retarget = null;
    }

    public void ApplyBones(Dictionary<string, float[]> bones)
    {
        if (_retarget != null && bones != null && bones.Count > 0)
            _retarget.ApplyBones(bones);
    }
}
