using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

/// <summary>
/// Minimal DTOs for Gemini's generateContent API.
/// </summary>
[Serializable] public class GeminiPart { public string text; }
[Serializable] public class GeminiContent { public string role; public GeminiPart[] parts; }
[Serializable] public class GeminiRequest { public GeminiContent[] contents; }
[Serializable] public class GeminiCandidate { public GeminiContent content; public string finishReason; }
[Serializable] public class GeminiResponse { public GeminiCandidate[] candidates; }

/// <summary>
/// Service that talks to Google's Gemini API (AI Studio) using REST.
/// Authentication is passed through the "x-goog-api-key" header.
/// </summary>
public sealed class GeminiService : ILLMService
{
    private readonly string _apiKey;
    private readonly string _model;

    /// <summary>
    /// Creates the Gemini service.
    /// </summary>
    /// <param name="apiKey">Gemini API key obtained from AI Studio.</param>
    /// <param name="model">Model name, e.g., "gemini-2.5-flash" or "gemini-1.5-flash".</param>
    public GeminiService(string apiKey, string model = "gemini-2.5-flash")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Gemini API key cannot be empty");
        _apiKey = apiKey;
        _model = model;
    }

    /// <summary>
    /// Sends the full chat history mapped to Gemini's "contents/parts/text" format
    /// and returns the assistant's response text.
    /// </summary>
    public async Task<string> ChatAsync(List<ChatMessageDto> history, CancellationToken ct = default)
    {
        // 1) Map chat history to Gemini format (assistant -> model, user -> user)
        var contents = new List<GeminiContent>();
        foreach (var m in history)
        {
            var role = m.role == "assistant" ? "model" : "user";
            contents.Add(new GeminiContent
            {
                role = role,
                parts = new[] { new GeminiPart { text = m.content ?? "" } }
            });
        }

        // 2) Serialize request
        var req = new GeminiRequest { contents = contents.ToArray() };
        string json = JsonUtility.ToJson(req);

        // 3) Build request (key via header "x-goog-api-key")
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
        using var uwr = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 30
        };
        // Note: The two lines below reassign the same handlers again; kept to preserve your logic exactly.
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("x-goog-api-key", _apiKey);
        uwr.timeout = 30;

        // 4) Send without blocking the main thread
        var op = uwr.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested)
            {
                uwr.Abort();
                ct.ThrowIfCancellationRequested();
            }
            await Task.Yield();
        }

        // 5) Handle HTTP errors
        if (uwr.result != UnityWebRequest.Result.Success)
            throw new Exception($"Erro Gemini ({uwr.responseCode}): {uwr.error} | {uwr.downloadHandler.text}");

        // 6) Parse response and return first candidate
        var resp = JsonUtility.FromJson<GeminiResponse>(uwr.downloadHandler.text);
        if (resp?.candidates == null || resp.candidates.Length == 0 ||
            resp.candidates[0]?.content?.parts == null || resp.candidates[0].content.parts.Length == 0)
            throw new Exception("Resposta vazia do Gemini");

        return resp.candidates[0].content.parts[0].text?.Trim() ?? "";
    }
}
