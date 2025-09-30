using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Minimal .env loader for Unity projects.
/// - Reads KEY=VALUE pairs, ignoring blank lines and comments.
/// - Supports inline comments after a '#' and trimming of surrounding quotes.
/// - Stores values in memory for quick access via Get().
/// </summary>
public static class EnvLoader
{
    // Internal in-memory store for variables
    private static Dictionary<string, string> _env = new Dictionary<string, string>();

    /// <summary>
    /// Loads the .env file at the given path (default = ".env" at project root).
    /// Existing entries are cleared before loading.
    /// </summary>
    public static void Load(string path = ".env")
    {
        _env.Clear();

        if (!File.Exists(path))
        {
            Debug.LogWarning($".env file not found at {path}");
            return;
        }

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw?.Trim();

            // Skip empty lines and full-line comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            // Remove inline comments: KEY=VALUE # comment
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash).Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Split only by the first '='
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();

            // Trim whitespace and surrounding double quotes for the value
            var value = parts[1].Trim().Trim('"');

            _env[key] = value;
        }
    }

    /// <summary>
    /// Gets a variable by key. Returns null if not present.
    /// </summary>
    public static string Get(string key)
    {
        return _env.ContainsKey(key) ? _env[key] : null;
    }
}
