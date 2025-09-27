using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EnvLoader
{
    private static Dictionary<string, string> _env = new Dictionary<string, string>();

    public static void Load(string path = ".env")
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($".env file not found at {path}");
            return;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                _env[key] = value;
            }
        }
    }

    public static string Get(string key)
    {
        return _env.ContainsKey(key) ? _env[key] : null;
    }
}
