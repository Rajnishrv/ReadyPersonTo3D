using System.Collections.Generic;
using UnityEngine;

public class RandomAvatarSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public Transform SpawnRoot;
    public Vector3 SpawnPosition = Vector3.zero;
    public Vector3 SpawnEuler = new Vector3(0, 0, 0); // was 180 — can hide avatar behind camera
    public float Scale = 1.0f;

    [Header("Prefabs (Resources paths)")]
    public string MaleFolder = "Avatars/Male";
    public string FemaleFolder = "Avatars/Female";

    GameObject[] malePrefabs;
    GameObject[] femalePrefabs;

    GameObject _current;
    AvatarRetarget _retarget;

    public bool HasAvatar => _current != null;

    void Awake()
    {
        if (!SpawnRoot) SpawnRoot = this.transform;

        malePrefabs = Resources.LoadAll<GameObject>(MaleFolder);
        femalePrefabs = Resources.LoadAll<GameObject>(FemaleFolder);

        Debug.Log($"[Spawner] Loaded prefabs -> male:{malePrefabs.Length} female:{femalePrefabs.Length}");
        if (malePrefabs.Length == 0 && femalePrefabs.Length == 0)
            Debug.LogWarning($"[Spawner] No prefabs found. Put your prefabs under Resources/{MaleFolder} or Resources/{FemaleFolder}");
    }

    public void SpawnNew(string gender)
    {
        Despawn();

        var list = (gender != null && gender.ToLower().StartsWith("f")) ? femalePrefabs : malePrefabs;
        if (list == null || list.Length == 0)
        {
            Debug.LogWarning($"[Spawner] No prefabs available for gender '{gender}'.");
            return;
        }

        var prefab = list[Random.Range(0, list.Length)];
        _current = Instantiate(prefab, SpawnRoot);
        _current.transform.localPosition = SpawnPosition;
        _current.transform.localRotation = Quaternion.Euler(SpawnEuler);
        _current.transform.localScale = Vector3.one * Scale;

        _retarget = _current.GetComponentInChildren<AvatarRetarget>();
        if (!_retarget) _retarget = _current.AddComponent<AvatarRetarget>();

        // Make sure Animator is Humanoid & exists
        var anim = _current.GetComponentInChildren<Animator>();
        if (!anim) Debug.LogWarning("[Spawner] Animator not found on avatar!");
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
