using NUnit.Compatibility;
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
    [SerializeField] private TMP_Text statusLabel;            // “digitando...” (optional)


    // Configs
    [Header("OpenAI Settings")]
    [SerializeField] private string openAiApiKey = "";


    private OpenAIService _service;
    private readonly List<ChatMessageDto> _history = new();   //complete messages log

    private void Awake()
    {
        //load environment variabe field if the one in the code is empty
        if (string.IsNullOrEmpty(openAiApiKey))
            openAiApiKey = System.Environment.GetEnvironmentVariable("OPENAI_KEY");

        _service = new OpenAIService(openAiApiKey);

        // calls handler from send button click
        sendButton.onClick.AddListener(HandleSend);
        inputField.onSubmit.AddListener(_ => HandleSend());
    }


    //Main send handler
    private void HandleSend()
    {
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
        try
        {
            statusLabel.text = "Typing...";
            string answer = await _service.ChatAsync(_history);

            //saves in history
            _history.Add(new ChatMessageDto("assistant", answer));

            //creates UI message bubble
            CreateBubble(answer, isUser: false);
        }
        catch(System.Exception e)
        {
            CreateBubble($"⚠️ {e.Message}", isUser: false);
        }
        finally
        {
            statusLabel.text = "";
        }
    }



    private void CreateBubble(string text, bool isUser)
    {
        var prefab = isUser? userBubblePrefab : botBubblePrefab;
        var go = Instantiate(prefab, chatContent);
        go.GetComponentInChildren<TMP_Text>().text = text;


        //auto scroll to end
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }
}
