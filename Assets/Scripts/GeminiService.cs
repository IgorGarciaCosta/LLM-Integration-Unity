using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;


// DTOs mínimos
[Serializable] public class GeminiPart { public string text; }
[Serializable] public class GeminiContent { public string role; public GeminiPart[] parts; }
[Serializable] public class GeminiRequest { public GeminiContent[] contents; }
[Serializable] public class GeminiCandidate { public GeminiContent content; public string finishReason; }
[Serializable] public class GeminiResponse { public GeminiCandidate[] candidates; }


public sealed class GeminiService : ILLMService
{
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(string apiKey, string model = "gemini-2.5-flash")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Gemini API key cannot be empty");
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<string> ChatAsync(List<ChatMessageDto> history, CancellationToken ct = default)
    {
        //maps history to the Gemini format
        var contents = new List<GeminiContent>();
        foreach(var m in history)
        {
            var role = m.role == "assistant" ? "model" : "user";
            contents.Add(new GeminiContent
            {
                role = role,
                parts = new[] { new GeminiPart { text = m.content??""} }
            });

        }

        var req = new GeminiRequest { contents = contents.ToArray() };
        string json = JsonUtility.ToJson(req);

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
        using var uwr = new UnityWebRequest(url, "POST");
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("x-goog-api-key", _apiKey);
        uwr.timeout = 30;

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

        if (uwr.result != UnityWebRequest.Result.Success)
            throw new Exception($"Erro Gemini ({uwr.responseCode}): {uwr.error} | {uwr.downloadHandler.text}");

        var resp = JsonUtility.FromJson<GeminiResponse>(uwr.downloadHandler.text);
        if (resp?.candidates == null || resp.candidates.Length == 0 ||
            resp.candidates[0]?.content?.parts == null || resp.candidates[0].content.parts.Length == 0)
            throw new Exception("Resposta vazia do Gemini");

        return resp.candidates[0].content.parts[0].text?.Trim() ?? "";
    }
}
