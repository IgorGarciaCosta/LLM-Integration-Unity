using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.MPE;
using UnityEngine;
using UnityEngine.Networking;

//SUMMARY
//Structures that represents a message 
//in the standard OpenAI Chat API pattern
// role  : "system", "user" or "assistant".
// content : free text.
//SUMMARY
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


/// <summary> structure
//Request body
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
// Structures to Unserialize JSON responses
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
// Open AI API comminucation service
/// </summary>
public sealed class OpenAIService
{
    //------------------------------------------------------------------
    // Consts
    //------------------------------------------------------------------
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private const string DefaultModel = "gpt-4o-mini";//cheaper model currently

    //------------------------------------------------------------------
    // Dependencies
    //------------------------------------------------------------------
    private readonly string _apiKey;

    public OpenAIService(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty");
        }

        _apiKey = apiKey;
    }



    /// <summary>
    /// Send history and return AI repsonse
    /// </summary>
    /// <param name="history">Complete list of messages.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>AI response text.</returns>
    public async Task<string> ChatAsync(List<ChatMessageDto> history,
                                        CancellationToken ct = default)
    {
        // 1) Monta o payload ------------------------------------------
        var requestBody = new OpenAIRequest(DefaultModel, history);

        string json = JsonUtility.ToJson(requestBody);

        // 2) Setup UnityWebRequest -----------------------------------
        using var uwr = new UnityWebRequest(Endpoint, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Authorization", "Bearer " + _apiKey);

        // 3) FIres the request and waits ----------------------------
        var op = uwr.SendWebRequest();

        // Converts AsyncOperation to Task to allow using wait
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested)     // calcels if theres an external cancelling request
            {
                uwr.Abort();
                ct.ThrowIfCancellationRequested();
            }
            await Task.Yield();                 // Avoid freezing main thread
        }

        // 4)  HTTP erros handling-----------------------------------------
        if (uwr.result != UnityWebRequest.Result.Success)
        {
            throw new Exception($"Error OpenAI ({uwr.responseCode}): {uwr.error}");
        }

        // 5) Unserialize response ------------------------------------
        var response = JsonUtility.FromJson<OpenAIResponse>(uwr.downloadHandler.text);

        // checks if renponse is valid
        if (response?.choices == null || response.choices.Length == 0)
            throw new Exception("Resposta vazia da OpenAI");

        return response.choices[0].message.content.Trim();
    }


}
