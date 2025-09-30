using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Data Transfer Object representing a single chat message in the
/// OpenAI-compatible format.
/// </summary>
/// <remarks>
/// role: "system", "user" or "assistant"
/// content: free text
/// </remarks>
[Serializable]
public class ChatMessageDto
{
    public string role;
    public string content;

    public ChatMessageDto(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

/// <summary>
/// Request body structure for OpenAI's Chat Completions API.
/// </summary>
[Serializable]
public class OpenAIRequest
{
    public string model;
    public List<ChatMessageDto> messages;
    public int max_tokens;
    public float temperature;

    public OpenAIRequest(string model, List<ChatMessageDto> messages, int max_tokens = 256, float temperature = 0.7f)
    {
        this.model = model;
        this.messages = messages;
        this.max_tokens = max_tokens;
        this.temperature = temperature;
    }
}

/// <summary>
/// Response DTOs for OpenAI's Chat Completions API.
/// </summary>
[Serializable]
public class OpenAIResponse
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public ChatMessageDto message;
}

/// <summary>
/// Service responsible for encapsulating communication with the OpenAI API.
/// </summary>
/// <remarks>
/// Purpose: centralize HTTP logic and keep UI free of vendor specifics.
/// Benefits:
/// - Easier to swap providers without touching UI code.
/// - Enables simple unit testing by mocking this service.
/// - Single place for logging/retry/rate-limit handling if needed.
/// </remarks>
public sealed class OpenAIService : ILLMService
{
    // ----------------------------
    // Constants
    // ----------------------------
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private const string DefaultModel = "gpt-4o-mini";

    // ----------------------------
    // Dependencies/State
    // ----------------------------
    private readonly string _apiKey;
    private readonly string _projectId;

    /// <summary>
    /// Creates the OpenAI service.
    /// </summary>
    /// <param name="apiKey">API key from OpenAI.</param>
    /// <param name="projectId">Project ID from OpenAI dashboard.</param>
    /// <exception cref="ArgumentException">If arguments are null or whitespace.</exception>
    public OpenAIService(string apiKey, string projectId)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key cannot be empty");
        if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("Project ID cannot be empty");

        // Normalize API key: remove line breaks/spaces and any accidental "Bearer" prefix.
        _apiKey = apiKey.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim()
                        .Replace("Bearer", "", StringComparison.OrdinalIgnoreCase);

        // Normalize project ID as well.
        _projectId = projectId.Replace("\r", "").Replace("\n", "").Trim();
    }

    /// <summary>
    /// Sends the full chat history and returns the assistant's answer.
    /// </summary>
    /// <param name="history">Complete chat history (system/user/assistant).</param>
    /// <param name="ct">Cancellation token to abort the request from the caller.</param>
    /// <returns>Assistant text answer.</returns>
    public async Task<string> ChatAsync(List<ChatMessageDto> history, CancellationToken ct = default)
    {
        // 1) Serialize payload
        var requestBody = new OpenAIRequest(DefaultModel, history);
        string json = JsonUtility.ToJson(requestBody);

        // 2) Prepare UnityWebRequest (POST with JSON body)
        using var uwr = new UnityWebRequest(Endpoint, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 45 // Safety timeout to avoid hanging requests
        };

        // 3) Headers required by OpenAI
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Authorization", "Bearer " + _apiKey);
        uwr.SetRequestHeader("OpenAI-Project", _projectId);

        // 4) Send and await without blocking the main thread
        var op = uwr.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested)
            {
                uwr.Abort();
                ct.ThrowIfCancellationRequested();
            }
            await Task.Yield(); // Keep UI responsive
        }

        // 5) Error handling
        if (uwr.result != UnityWebRequest.Result.Success)
            throw new Exception($"Erro OpenAI ({uwr.responseCode}): {uwr.error} | {uwr.downloadHandler.text}");

        // 6) Deserialize and extract first candidate text
        var response = JsonUtility.FromJson<OpenAIResponse>(uwr.downloadHandler.text);
        if (response?.choices == null || response.choices.Length == 0)
            throw new Exception("Resposta vazia da OpenAI");

        return response.choices[0].message.content?.Trim() ?? "";
    }
}
