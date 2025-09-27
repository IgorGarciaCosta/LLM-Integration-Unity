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

    private bool _waitingResponse;


    // Configs
    [Header("OpenAI Settings")]
    private string openAiApiKey;


    private OpenAIService _service;
    private readonly List<ChatMessageDto> _history = new();   //complete messages log

    private void Awake()
    {
        // carrega .env
        EnvLoader.Load();

        if (string.IsNullOrEmpty(openAiApiKey))
        {
            openAiApiKey = EnvLoader.Get("OPENAI_KEY");
            Debug.Log("Key: " + openAiApiKey);
        }

        if (string.IsNullOrEmpty(openAiApiKey))
        {
            Debug.LogError("OPENAI_KEY não encontrada!");
        }

        _service = new OpenAIService(openAiApiKey.Trim());

        if (statusLabel != null) statusLabel.text = "";//hide status label at the beginning

        // calls handler from send button click
        sendButton.onClick.AddListener(HandleSend);
        inputField.onSubmit.AddListener(_ => HandleSend());
    }


    //Main send handler
    private void HandleSend()
    {
        if (_waitingResponse) return;        // avoids flood

        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        //creates user  text bubble
        CreateBubble(text, isUser:true);

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
            CreateBubble(answer, isUser: false);
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
