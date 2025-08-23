using UnityEngine;

public class RandomAvatarSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public Transform SpawnRoot;
    public Vector3 SpawnPosition = new Vector3(0, 0, 0);
    public Vector3 SpawnEuler = new Vector3(0, 0, 0);
    public float Scale = 1.0f;

    [Header("Prefabs (Resources paths)")]
    public string MaleFolder = "Avatars/Male";
    public string FemaleFolder = "Avatars/Female";
    public string FallbackFolder = "Avatars"; // optional

    GameObject[] malePrefabs;
    GameObject[] femalePrefabs;
    GameObject[] fallbackPrefabs;

    GameObject _current;
    AvatarRetarget _retarget;

    public bool HasAvatar => _current != null;

    void Awake()
    {
        if (!SpawnRoot) SpawnRoot = this.transform;

        malePrefabs = Resources.LoadAll<GameObject>(MaleFolder);
        femalePrefabs = Resources.LoadAll<GameObject>(FemaleFolder);
        fallbackPrefabs = Resources.LoadAll<GameObject>(FallbackFolder);

        Debug.Log($"[Spawner] Prefabs -> male:{malePrefabs.Length} female:{femalePrefabs.Length} fallback:{fallbackPrefabs.Length}");
    }

    public void SpawnNew(string gender)
    {
        Despawn();

        var list = PickList(gender);
        if (list == null || list.Length == 0)
        {
            Debug.LogWarning($"[Spawner] No prefabs for gender '{gender}'. Put prefabs under Resources/{MaleFolder} or /{FemaleFolder}.");
            return;
        }

        var prefab = list[Random.Range(0, list.Length)];
        _current = Instantiate(prefab, SpawnRoot);
        _current.transform.localPosition = new Vector3(SpawnPosition.x, Mathf.Max(0f, SpawnPosition.y), SpawnPosition.z);
        _current.transform.localRotation = Quaternion.Euler(SpawnEuler);
        _current.transform.localScale = Vector3.one * Scale;

        // Ensure Animator exists and is Humanoid
        var anim = _current.GetComponentInChildren<Animator>();
        if (!anim) Debug.LogWarning("[Spawner] Animator not found on avatar!");
        _retarget = _current.GetComponentInChildren<AvatarRetarget>();
        if (!_retarget) _retarget = _current.AddComponent<AvatarRetarget>();
    }

    GameObject[] PickList(string gender)
    {
        var g = (gender ?? "unknown").Trim().ToLowerInvariant();
        if (g.StartsWith("f") && femalePrefabs.Length > 0) return femalePrefabs;
        if (g.StartsWith("m") && malePrefabs.Length > 0) return malePrefabs;
        if (malePrefabs.Length > 0) return malePrefabs;
        if (femalePrefabs.Length > 0) return femalePrefabs;
        return fallbackPrefabs;
    }

    public void Despawn()
    {
        if (_current) Destroy(_current);
        _current = null;
        _retarget = null;
    }

    public void ApplyBones(System.Collections.Generic.Dictionary<string, float[]> bones)
    {
        if (_retarget != null && bones != null && bones.Count > 0)
            _retarget.ApplyBones(bones);
    }
}
