using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

public static class SimpleSpriteCache
{
    static Dictionary<string, Sprite> _cache = new();

    public static async Task<Sprite> Load(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (_cache.TryGetValue(url, out var s)) return s;

        using var req = UnityWebRequestTexture.GetTexture(url);
        var op = req.SendWebRequest();
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();
#if UNITY_2020_3_OR_NEWER
    if (req.result != UnityWebRequest.Result.Success) return null;
#else
        if (req.isNetworkError || req.isHttpError) return null;
#endif
        var tex = DownloadHandlerTexture.GetContent(req);
        var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        _cache[url] = spr;
        return spr;
    }
}