using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class UdpPoseReceiver : MonoBehaviour
{
    public int port = 5056;
    UdpClient _client;
    Thread _thread;
    private object _lock = new object();
    private List<string> _msgs = new List<string>();
    public RandomAvatarSpawner spawner;

    string _lastGender = "unknown";

    void Start()
    {
        spawner = FindObjectOfType<RandomAvatarSpawner>();
        _client = new UdpClient(port);
        _thread = new Thread(Listen) { IsBackground = true };
        _thread.Start();
    }

    void Listen()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            try
            {
                var data = _client.Receive(ref ep);
                var json = Encoding.UTF8.GetString(data);
                lock (_lock) _msgs.Add(json);
            }
            catch { }
        }
    }

    void Update()
    {
        string msg = null;
        lock (_lock)
        {
            if (_msgs.Count > 0)
            {
                msg = _msgs[0];
                _msgs.RemoveAt(0);
            }
        }
        if (msg != null) ProcessJson(msg);
    }

    static string NormalizeGender(string g)
    {
        if (string.IsNullOrEmpty(g)) return "unknown";
        g = g.Trim().ToLowerInvariant();
        if (g == "female" || g == "f" || g == "woman" || g == "girl") return "female";
        if (g == "male" || g == "m" || g == "man" || g == "boy") return "male";
        return "unknown";
    }

    void ProcessJson(string json)
    {
        // parse gender & event
        string gender = null;
        string evt = null;
        var mG = Regex.Match(json, "\"gender\"\\s*:\\s*\"(?<g>[^\"]+)\"", RegexOptions.IgnoreCase);
        if (mG.Success) gender = mG.Groups["g"].Value;

        var mE = Regex.Match(json, "\"event\"\\s*:\\s*\"(?<e>[^\"]+)\"", RegexOptions.IgnoreCase);
        if (mE.Success) evt = mE.Groups["e"].Value;

        // parse bones
        var bones = new Dictionary<string, float[]>();
        var ix = json.IndexOf("\"bones\"", System.StringComparison.OrdinalIgnoreCase);
        if (ix >= 0)
        {
            var bStart = json.IndexOf('{', ix);
            if (bStart >= 0)
            {
                int depth = 0;
                int i;
                for (i = bStart; i < json.Length; ++i)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') { depth--; if (depth == 0) break; }
                }
                if (i < json.Length)
                {
                    var bonesJson = json.Substring(bStart, i - bStart + 1);
                    var re = new Regex("\"(?<name>[A-Za-z0-9_]+)\"\\s*:\\s*\\[(?<vals>[^\\]]+)\\]");
                    var m = re.Matches(bonesJson);
                    foreach (Match mm in m)
                    {
                        var name = mm.Groups["name"].Value;
                        var vals = mm.Groups["vals"].Value.Split(',');
                        var arr = new List<float>();
                        foreach (var s in vals)
                        {
                            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
                                arr.Add(f);
                        }
                        if (arr.Count >= 3)
                            bones[name] = new float[] { arr[0], arr[1], arr[2] };
                    }
                }
            }
        }

        var gNorm = NormalizeGender(gender);
        var eNorm = (evt ?? "none").Trim().ToLowerInvariant();

        // Spawn rules:
        // 1) On "entered" -> spawn for current gender
        // 2) If gender changed (male<->female) -> respawn immediately
        if (spawner == null) spawner = FindObjectOfType<RandomAvatarSpawner>();
        if (spawner == null) return;

        bool genderChanged = (gNorm != "unknown" && gNorm != _lastGender);
        if (eNorm == "entered" || genderChanged)
        {
            if (gNorm == "male" || gNorm == "female")
            {
                spawner.SpawnNew(gNorm);
                _lastGender = gNorm;
            }
        }
        else if (eNorm == "left")
        {
            spawner.Despawn();
            _lastGender = "unknown";
        }

        if (bones.Count > 0) spawner.ApplyBones(bones);
    }

    void OnDestroy()
    {
        try { _client?.Close(); } catch { }
        try { _thread?.Abort(); } catch { }
    }
}
