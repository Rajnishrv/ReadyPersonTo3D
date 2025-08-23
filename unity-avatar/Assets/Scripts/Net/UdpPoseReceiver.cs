using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpPoseReceiver : MonoBehaviour
{
    [Header("Network")]
    public int listenPort = 5056;

    [Header("Scene")]
    public RandomAvatarSpawner spawner;

    [Header("Incoming Basis Offset (deg)")]
    [Tooltip("Final correction after CV->Unity. Adjust if the avatar looks pitched/yawed/rolled.")]
    public Vector3 rotationOffsetEuler = new Vector3(-90f, 0f, 0f); // <- fixes 'head down'

    UdpClient _client;
    Thread _thread;
    volatile bool _running;

    readonly Queue<string> _queue = new Queue<string>();
    readonly object _lock = new object();

    string _lastGender = "unknown";
    string _spawnedGender = null;

    void Start()
    {
        if (!spawner) spawner = FindObjectOfType<RandomAvatarSpawner>();

        _client = new UdpClient(listenPort);
        _running = true;
        _thread = new Thread(ListenLoop) { IsBackground = true };
        _thread.Start();
        Debug.Log($"[UDP] Listening on port {listenPort}");
    }

    void OnDestroy()
    {
        _running = false;
        try { _client?.Close(); } catch { }
        try { _thread?.Join(100); } catch { }
    }

    void ListenLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenPort);
        while (_running)
        {
            try
            {
                var data = _client.Receive(ref ep);
                var txt = Encoding.UTF8.GetString(data);
                lock (_lock) _queue.Enqueue(txt);
            }
            catch { }
        }
    }

    void Update()
    {
        while (true)
        {
            string msg = null;
            lock (_lock) { if (_queue.Count > 0) msg = _queue.Dequeue(); }
            if (msg == null) break;

            try { ProcessJson(msg); }
            catch (Exception e) { Debug.LogWarning($"[UDP] Parse error: {e.Message}\n{msg}"); }
        }
    }

    // --- Helpers ---

    static Vector3 CvToUnity(Vector3 v) => new Vector3(v.x, -v.y, -v.z);

    Dictionary<string, float[]> ConvertAll(Dictionary<string, float[]> src)
    {
        var rot = Quaternion.Euler(rotationOffsetEuler);
        var dst = new Dictionary<string, float[]>(src.Count);
        foreach (var kv in src)
        {
            var a = kv.Value;
            var u = CvToUnity(new Vector3(a[0], a[1], a[2]));
            u = rot * u; // apply final basis correction
            dst[kv.Key] = new float[] { u.x, u.y, u.z };
        }
        return dst;
    }

    void ProcessJson(string json)
    {
        var root = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
        if (root == null) throw new Exception("Root JSON is not an object");

        string gender = root.ContainsKey("gender") ? (root["gender"]?.ToString() ?? "unknown") : "unknown";
        _lastGender = string.IsNullOrEmpty(gender) ? "unknown" : gender;

        string e = root.ContainsKey("event") ? (root["event"]?.ToString() ?? "") : "";

        // Bones
        var bones = new Dictionary<string, float[]>();
        if (root.ContainsKey("bones") && root["bones"] is Dictionary<string, object> jb)
        {
            foreach (var kv in jb)
            {
                if (kv.Value is List<object> arr && arr.Count >= 3)
                {
                    float x = Convert.ToSingle(arr[0]);
                    float y = Convert.ToSingle(arr[1]);
                    float z = Convert.ToSingle(arr[2]);
                    bones[kv.Key] = new float[] { x, y, z };
                }
            }
        }

        // Spawn/despawn from event
        if (!string.IsNullOrEmpty(e))
        {
            if (e == "enter" || e == "entered") SpawnIfNeeded(_lastGender, force: true);
            else if (e == "exit" || e == "left") { spawner.Despawn(); _spawnedGender = null; }
        }

        // Safety: spawn on first bones if nothing spawned
        if (bones.Count > 0) SpawnIfNeeded(_lastGender, force: false);

        if (bones.Count > 0)
            spawner.ApplyBones(ConvertAll(bones));
    }

    void SpawnIfNeeded(string gender, bool force)
    {
        // respawn if gender changed
        if (!spawner.HasAvatar || force || (_spawnedGender != null && _spawnedGender != GenderKey(gender)))
        {
            spawner.SpawnNew(gender);
            _spawnedGender = GenderKey(gender);
        }
    }

    static string GenderKey(string g)
    {
        g = (g ?? "unknown").Trim().ToLowerInvariant();
        if (g.StartsWith("f")) return "female";
        if (g.StartsWith("m")) return "male";
        return "unknown";
    }

    // -------- MiniJSON (tiny, no dependencies) --------
    static class MiniJSON
    {
        public static class Json
        {
            public static object Deserialize(string json) { return Parser.Parse(json); }

            sealed class Parser : IDisposable
            {
                const string WORD_BREAK = "{}[],:\"";
                StringReader json;
                Parser(string s) { json = new StringReader(s); }
                public static object Parse(string s) { using (var p = new Parser(s)) return p.ParseValue(); }
                public void Dispose() { json.Dispose(); }

                Dictionary<string, object> ParseObject()
                {
                    var table = new Dictionary<string, object>();
                    json.Read();
                    while (true)
                    {
                        var t = NextToken;
                        if (t == TOKEN.CURLY_CLOSE) { json.Read(); return table; }
                        if (t == TOKEN.NONE) return null;
                        var name = ParseString();
                        if (NextToken != TOKEN.COLON) return null; json.Read();
                        table[name] = ParseValue();
                        switch (NextToken) { case TOKEN.COMMA: json.Read(); continue; case TOKEN.CURLY_CLOSE: json.Read(); return table; default: return null; }
                    }
                }

                List<object> ParseArray()
                {
                    var a = new List<object>(); json.Read();
                    while (true)
                    {
                        var t = NextToken;
                        switch (t) { case TOKEN.NONE: return null; case TOKEN.SQUARE_CLOSE: json.Read(); return a; default: a.Add(ParseValue()); break; }
                        switch (NextToken) { case TOKEN.COMMA: json.Read(); break; case TOKEN.SQUARE_CLOSE: json.Read(); return a; default: return null; }
                    }
                }

                object ParseValue()
                {
                    switch (NextToken)
                    {
                        case TOKEN.STRING: return ParseString();
                        case TOKEN.NUMBER: return ParseNumber();
                        case TOKEN.CURLY_OPEN: return ParseObject();
                        case TOKEN.SQUARE_OPEN: return ParseArray();
                        case TOKEN.TRUE: json.Read(); json.Read(); json.Read(); json.Read(); return true;
                        case TOKEN.FALSE: json.Read(); json.Read(); json.Read(); json.Read(); json.Read(); return false;
                        case TOKEN.NULL: json.Read(); json.Read(); json.Read(); json.Read(); return null;
                        default: return null;
                    }
                }

                string ParseString()
                {
                    var s = new System.Text.StringBuilder(); json.Read();
                    while (true)
                    {
                        if (json.Peek() == -1) break; var c = (char)json.Read();
                        if (c == '"') break;
                        if (c == '\\')
                        {
                            if (json.Peek() == -1) break; c = (char)json.Read();
                            switch (c)
                            {
                                case '"': case '\\': case '/': s.Append(c); break;
                                case 'b': s.Append('\b'); break;
                                case 'f': s.Append('\f'); break;
                                case 'n': s.Append('\n'); break;
                                case 'r': s.Append('\r'); break;
                                case 't': s.Append('\t'); break;
                                case 'u': var hex = new char[4]; json.Read(hex, 0, 4); s.Append((char)Convert.ToInt32(new string(hex), 16)); break;
                            }
                        }
                        else s.Append(c);
                    }
                    return s.ToString();
                }

                object ParseNumber()
                {
                    var n = NextWord;
                    if (n.IndexOf('.') == -1 && n.IndexOf('e') == -1 && n.IndexOf('E') == -1)
                        return long.Parse(n, System.Globalization.CultureInfo.InvariantCulture);
                    return double.Parse(n, System.Globalization.CultureInfo.InvariantCulture);
                }

                void EatWS() { while (json.Peek() != -1 && char.IsWhiteSpace((char)json.Peek())) json.Read(); }
                char PeekChar { get { return Convert.ToChar(json.Peek()); } }
                char NextChar { get { return Convert.ToChar(json.Read()); } }
                string NextWord { get { var sb = new System.Text.StringBuilder(); while (json.Peek() != -1 && "{}[],:\"".IndexOf(PeekChar) == -1 && !char.IsWhiteSpace(PeekChar)) sb.Append(NextChar); return sb.ToString(); } }
                TOKEN NextToken
                {
                    get
                    {
                        EatWS(); if (json.Peek() == -1) return TOKEN.NONE;
                        switch (PeekChar)
                        { case '{': return TOKEN.CURLY_OPEN; case '}': return TOKEN.CURLY_CLOSE; case '[': return TOKEN.SQUARE_OPEN; case ']': return TOKEN.SQUARE_CLOSE; case ',': return TOKEN.COMMA; case '"': return TOKEN.STRING; case ':': return TOKEN.COLON; case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9': case '-': return TOKEN.NUMBER; }
                        var w = NextWord; switch (w) { case "true": return TOKEN.TRUE; case "false": return TOKEN.FALSE; case "null": return TOKEN.NULL; }
                        return TOKEN.NONE;
                    }
                }
                enum TOKEN { NONE, CURLY_OPEN, CURLY_CLOSE, SQUARE_OPEN, SQUARE_CLOSE, COLON, COMMA, STRING, NUMBER, TRUE, FALSE, NULL }
            }

            sealed class StringReader : IDisposable
            {
                readonly string s; int i;
                public StringReader(string str) { s = str; i = 0; }
                public int Peek() { return (i < s.Length) ? s[i] : -1; }
                public int Read() { return (i < s.Length) ? s[i++] : -1; }
                public void Read(char[] buf, int off, int len) { for (int k = 0; k < len; k++) buf[off + k] = (char)Read(); }
                public void Dispose() { }
            }
        }
    }
}
