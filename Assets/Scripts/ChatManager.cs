using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



//SUMMARY
//Responsible for managing the chat  for the client
//SUMMARY
public class ChatManager: MonoBehaviour
{
    //  UI References (exposed on Inspector)
    [Header("UI Elements")]
    [SerializeField] private Transform chatContent;           
    [SerializeField] private GameObject userBubblePrefab;     
    [SerializeField] private GameObject botBubblePrefab;      
    [SerializeField] private TMP_InputField inputField;      
    [SerializeField] private Button sendButton;               
    [SerializeField] private ScrollRect scrollRect;           
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Dropdown providerDropdown;
    [SerializeField] private GameObject notificationPopup;

    private bool _waitingResponse;


    // Configs
    [Header("OpenAI Settings")]
    // Keys (via .env)
    private string openAiApiKey;
    private string openAiProjectId;
    private string geminiKey;
    private string geminiModel;


    private ILLMService _service;
    private OpenAIService _openAIService;
    private GeminiService _geminiService;
    private readonly List<ChatMessageDto> _history = new();   //complete messages history


    private int OpenAIDropdownIndex = 0;
    private int GeminiDropdownIndex = 1;
    private void Awake()
    {
        notificationPopup.SetActive(false);

        // carrega .env
        EnvLoader.Load();

        openAiApiKey = GetEnv("OPENAI_KEY");
        openAiProjectId = GetEnv("OPENAI_PROJECT_ID");
        geminiKey = GetEnv("GEMINI_KEY");
        geminiModel = GetEnv("GEMINI_MODEL", "gemini-1.5-flash");

        bool missingOpenAI = string.IsNullOrEmpty(openAiApiKey) || string.IsNullOrEmpty(openAiProjectId);
        bool missingGemini = string.IsNullOrEmpty(geminiKey);

        if (missingOpenAI)
        {
            Debug.LogWarning("OPENAI_KEY or OPENAI_PROJECT_ID inexistent on .env");
            RemoveDropdownOption(providerDropdown, OpenAIDropdownIndex);
        }

        if (missingGemini)
        {
            Debug.LogWarning("GEMINI_KEY iexistent on .env");
            RemoveDropdownOption(providerDropdown, GeminiDropdownIndex);
        }

        // Se nenhum provider estiver configurado, mostra popup
        if (missingOpenAI && missingGemini)
            notificationPopup.SetActive(true);




        if (statusLabel != null) statusLabel.text = "";//hide status label at the beginning

        // Instancia serviços (apenas os que têm chave)
        if (!string.IsNullOrEmpty(openAiApiKey))
            _openAIService = new OpenAIService(openAiApiKey); // seu OpenAIService já usa o Project ID interno
        if (!string.IsNullOrEmpty(geminiKey))
            _geminiService = new GeminiService(geminiKey, geminiModel);

        //Initial Provider
        SetProvider(providerDropdown != null ? providerDropdown.value : 0);


        // calls handler from send button click
        sendButton.onClick.AddListener(HandleSend);
        inputField.onSubmit.AddListener(_ => HandleSend());
        if(providerDropdown != null)
        {
            providerDropdown.onValueChanged.AddListener(SetProvider);
        }
    }

    private void SetProvider(int index)
    {
        if (index == 0 && _geminiService != null)
        {
            _service = _geminiService;
            Debug.Log("LLM atual: ChatGPT");
        }
        else
        {
            _service = _openAIService;
            Debug.Log("LLM atual: Gemini");
        }
    }

    private void RemoveDropdownOption(TMP_Dropdown dropdown, int index)
    {
        if (index < 0 || index >= dropdown.options.Count)
        {
            Debug.LogWarning($"Índice {index} inválido para remover opção do dropdown {dropdown.name}");
            return;
        }

        dropdown.options.RemoveAt(index);
        dropdown.RefreshShownValue();
    }

    private string GetEnv(string key, string fallback = "")
    {
        return (EnvLoader.Get(key) ?? fallback).Trim();
    }


    //Main send handler
    private void HandleSend()
    {
        if (_waitingResponse || _service == null) return;        // avoids flood

        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        //creates user  text bubble
        CreateBubble("User: " +text, isUser:true);

        //saves in history
        _history.Add(new ChatMessageDto("user", text));

        //clear field
        inputField.text = "";
        inputField.ActivateInputField();//add focus

        //fires request to LLM
        _ = ProcessBotResponseAsync();
    }


    //LLM connection async cycle
    private async Task ProcessBotResponseAsync()
    {
        _waitingResponse = true;
        try
        {
            if (statusLabel != null) statusLabel.text = "Typing…";
            string answer = await _service.ChatAsync(_history);

            //saves in history
            _history.Add(new ChatMessageDto("assistant", answer));

            //creates UI message bubble
            CreateBubble("LLM: "+answer, isUser: false);
        }
        catch(System.Exception e)
        {
            CreateBubble($"Error: {e.Message}", isUser: false);
        }
        finally
        {
            if (statusLabel != null) statusLabel.text = "";
            _waitingResponse = false;
        }
    }



    private void CreateBubble(string text, bool isUser)
    {
        var prefab = isUser? userBubblePrefab : botBubblePrefab;
        var go = Instantiate(prefab, chatContent);
        go.GetComponentInChildren<TMP_Text>().text = text;


        //auto scroll to end
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0;
    }
}
