using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;


/// <summary>
/// Service responsible for encapsulating communication with the OpenAI API.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> Centralize the HTTP request logic.</para>
/// <para><b>Benefits:</b></para>
/// <list type="bullet">
///   <item><description>Isolates the external dependency in a single place.</description></item>
///   <item><description>Makes it easy to switch providers without impacting the UI.</description></item>
///   <item><description>Enables simpler unit testing through mocking.</description></item>
/// </list>
/// </remarks>


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
    [SerializeField] private string openAiProjectId = "proj_bywB3ulxCAXyjsMVTJH4lcja";
    

    //------------------------------------------------------------------
    // Dependencies
    //------------------------------------------------------------------
    private readonly string _apiKey;

    public OpenAIService(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be empty");

        _apiKey = apiKey
                    .Replace("\r", "")   // remove CR
                    .Replace("\n", "")   // remove LF
                    .Replace(" ", "")    // remove espaços internos
                    .Trim();

        // se ainda tiver “Bearer”, tira:
        _apiKey = _apiKey.Replace("Bearer", "", StringComparison.OrdinalIgnoreCase);
    }



    /// <summary>
    /// Send history and return AI repsonse
    /// </summary>
    /// <param name="history">Complete list of messages.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>AI response text.</returns>
    /// <summary>
    /// Envia o histórico para a OpenAI e devolve a resposta do modelo.
    /// </summary>
    public async Task<string> ChatAsync(List<ChatMessageDto> history,
                                        CancellationToken ct = default)
    {
        //------------------------------------------------------------
        // 1) Monta o payload JSON
        //------------------------------------------------------------
        var requestBody = new OpenAIRequest(DefaultModel, history);
        string json = JsonUtility.ToJson(requestBody);

        //------------------------------------------------------------
        // 2) Prepara o UnityWebRequest
        //------------------------------------------------------------
        using var uwr = new UnityWebRequest(Endpoint, "POST");
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        uwr.downloadHandler = new DownloadHandlerBuffer();


        //openAiProjectId = openAiProjectId
        //            .Replace("\r", "")
        //            .Replace("\n", "")
        //            .Trim();

        //string h1 = "Bearer " + _apiKey;
        //DebugDump("AUTH", h1);
        //string h2 = openAiProjectId;
        //DebugDump("PROJ", h2);


        // --- headers -------------------------------------------------
        string auth = "Bearer " + _apiKey;
        string proj = openAiProjectId;

        Debug.Log($"[AUTH] {auth}");
        Debug.Log($"[PROJ] {proj}");

        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Authorization", auth);
        uwr.SetRequestHeader("OpenAI-Project", proj);

        //------------------------------------------------------------
        // 4) Dispara a requisição e aguarda
        //------------------------------------------------------------
        var op = uwr.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested)
            {
                uwr.Abort();
                ct.ThrowIfCancellationRequested();
            }
            await Task.Yield();                        // evita travar a UI
        }

        //------------------------------------------------------------
        // 5) Trata erros HTTP
        //------------------------------------------------------------
        if (uwr.result != UnityWebRequest.Result.Success)
            throw new Exception($"Erro OpenAI ({uwr.responseCode}): {uwr.error}");

        //------------------------------------------------------------
        // 6) Desserializa resposta
        //------------------------------------------------------------
        var response = JsonUtility.FromJson<OpenAIResponse>(uwr.downloadHandler.text);
        if (response?.choices == null || response.choices.Length == 0)
            throw new Exception("Resposta vazia da OpenAI");

        return response.choices[0].message.content.Trim();
    }


    private static void DebugDump(string tag, string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        Debug.Log($"[DUMP {tag}] len={bytes.Length} : {BitConverter.ToString(bytes)}");
    }


}
