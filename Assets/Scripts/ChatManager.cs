using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;

/// <summary>
/// High-level chat coordinator for the client UI.
/// - Owns references to UI widgets (input, buttons, scroll, labels).
/// - Holds the chat history and forwards it to the selected LLM service.
/// - Handles provider switching (OpenAI/Gemini) via dropdown.
/// - Creates chat bubbles and auto-scrolls the ScrollRect.
/// </summary>
public class ChatManager : MonoBehaviour
{
    // ----------------------------
    // UI References (assigned in Inspector)
    // ----------------------------
    [Header("UI Elements")]
    [SerializeField] private Transform chatContent;            // Content container inside the ScrollRect
    [SerializeField] private GameObject userBubblePrefab;      // Prefab for user message bubble
    [SerializeField] private GameObject botBubblePrefab;       // Prefab for LLM message bubble
    [SerializeField] private TMP_InputField inputField;        // User input field
    [SerializeField] private Button sendButton;                // Send button
    [SerializeField] private Button exportHistoryBtn;          // Button to export chat log
    [SerializeField] private ScrollRect scrollRect;            // Scroll view
    [SerializeField] private TMP_Text statusLabel;             // "Typing…" label
    [SerializeField] private TMP_Dropdown providerDropdown;    // Provider selector (Gemini/OpenAI)
    [SerializeField] private GameObject notificationPopup;     // Popup if no provider is configured
    [SerializeField] private GameObject exportHistoryPopup;    // Popup for export flow
    [SerializeField] private ExportNotificationManager notifyManager; // Small toast/notification manager

    // ----------------------------
    // State
    // ----------------------------
    private bool _waitingResponse;                             // Prevents spamming the service

    // ----------------------------
    // Configuration (loaded from .env)
    // ----------------------------
    [Header("OpenAI Settings")]
    private string openAiApiKey;
    private string openAiProjectId;
    private string geminiKey;
    private string geminiModel;

    // ----------------------------
    // Services
    // ----------------------------
    private ILLMService _service;          // Currently selected provider
    private OpenAIService _openAIService;  // OpenAI implementation
    private GeminiService _geminiService;  // Gemini implementation

    // Full chat history shared for the session
    private readonly List<ChatMessageDto> _history = new();

    // Dropdown indexes used when removing options dynamically
    private int OpenAIDropdownIndex = 0;
    private int GeminiDropdownIndex = 1;

    // Label for the currently selected provider (for bubble prefix)
    private string currentLLM = "Gemini";

    /// <summary>
    /// MonoBehaviour initialization.
    /// Loads environment variables, initializes services, wires UI events,
    /// and selects the initial provider.
    /// </summary>
    private void Awake()
    {
        // Ensure popups start hidden
        notificationPopup.SetActive(false);
        exportHistoryPopup.SetActive(false);

        // Load .env at project root (see EnvLoader for details)
        EnvLoader.Load();

        // Pull configuration from EnvLoader
        openAiApiKey = GetEnv("OPENAI_KEY");
        openAiProjectId = GetEnv("OPENAI_PROJECT_ID");
        geminiKey = GetEnv("GEMINI_KEY");
        geminiModel = GetEnv("GEMINI_MODEL", "gemini-1.5-flash");

        bool missingOpenAI = string.IsNullOrEmpty(openAiApiKey) || string.IsNullOrEmpty(openAiProjectId);
        bool missingGemini = string.IsNullOrEmpty(geminiKey);

        // If a provider is not configured, remove it from the dropdown
        if (missingOpenAI)
        {
            Debug.LogWarning("OPENAI_KEY or OPENAI_PROJECT_ID inexistent on .env");
            RemoveDropdownOption(providerDropdown, OpenAIDropdownIndex);
        }

        if (missingGemini)
        {
            Debug.LogWarning("GEMINI_KEY inexistent on .env");
            RemoveDropdownOption(providerDropdown, GeminiDropdownIndex);
        }

        // If none are configured, show a notification popup
        if (missingOpenAI && missingGemini)
            notificationPopup.SetActive(true);

        if (statusLabel != null) statusLabel.text = ""; // Hide typing label initially

        // Instantiate providers only if keys are available
        if (!string.IsNullOrEmpty(openAiApiKey))
            _openAIService = new OpenAIService(openAiApiKey, openAiProjectId);

        if (!string.IsNullOrEmpty(geminiKey))
            _geminiService = new GeminiService(geminiKey, geminiModel);

        // Select initial provider (based on current dropdown value, or 0 if null)
        SetProvider(providerDropdown != null ? providerDropdown.value : 0);

        // Wire UI events
        sendButton.onClick.AddListener(HandleSend);
        inputField.onSubmit.AddListener(_ => HandleSend());
        if (providerDropdown != null)
            providerDropdown.onValueChanged.AddListener(SetProvider);
    }

    /// <summary>
    /// Switches the current LLM provider based on dropdown index.
    /// 0 = Gemini, 1 = OpenAI (as configured in the scene).
    /// </summary>
    private void SetProvider(int index)
    {
        switch (index)
        {
            case 0: // Gemini
                if (_geminiService != null) { _service = _geminiService; currentLLM = "Gemini"; }
                else notifyManager.ShowNotification("Gemini indisponível (GEMINI_KEY ausente).");
                break;

            case 1: // OpenAI
                if (_openAIService != null) { _service = _openAIService; currentLLM = "ChatGPT"; }
                else notifyManager.ShowNotification("OpenAI indisponível (OPENAI_KEY/PROJECT_ID ausentes).");
                break;
        }
        Debug.Log("Current LLM: " + currentLLM);
    }

    /// <summary>
    /// Safely removes a dropdown option by index (used when a provider is missing).
    /// </summary>
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

    /// <summary>
    /// Helper to read environment variables with a default fallback.
    /// </summary>
    private string GetEnv(string key, string fallback = "")
    {
        return (EnvLoader.Get(key) ?? fallback).Trim();
    }

    /// <summary>
    /// Main send handler: validates input, creates user bubble,
    /// appends to history, clears the input, and triggers async request.
    /// </summary>
    private void HandleSend()
    {
        // Avoid spamming or sending while no provider is selected
        if (_waitingResponse || _service == null) return;

        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Create user bubble
        CreateBubble("User: " + text, isUser: true);

        // Add to in-memory history
        _history.Add(new ChatMessageDto("user", text));

        // Clear input and keep focus
        inputField.text = "";
        inputField.ActivateInputField();

        // Fire async round-trip to the current provider
        _ = ProcessBotResponseAsync();
    }

    /// <summary>
    /// Orchestrates the async call to the provider, updates UI and history,
    /// and ensures the state flags/labels are restored in finally.
    /// </summary>
    private async Task ProcessBotResponseAsync()
    {
        _waitingResponse = true;
        try
        {
            if (statusLabel != null) statusLabel.text = "Typing…";

            // Ask the selected provider using the full history
            string answer = await _service.ChatAsync(_history);

            // Append provider message to history
            _history.Add(new ChatMessageDto("assistant", answer));

            // Show provider bubble
            CreateBubble(currentLLM + ": " + answer, isUser: false);
        }
        catch (System.Exception e)
        {
            // Show error in a bubble (keeps UI feedback consistent)
            CreateBubble($"Error: {e.Message}", isUser: false);
        }
        finally
        {
            if (statusLabel != null) statusLabel.text = "";
            _waitingResponse = false;
        }
    }

    /// <summary>
    /// Exports the current chat history to a .txt file.
    /// Uses Assets folder in Editor and persistentDataPath in builds.
    /// </summary>
    public void ExportHistoryToTxt()
    {
        var history = _history;
        if (history == null || history.Count == 0)
        {
            notifyManager.ShowNotification("No chat history to export.");
            return;
        }

        // Choose base path depending on environment
        string basePath = Application.isEditor ? Application.dataPath : Application.persistentDataPath;
        Directory.CreateDirectory(basePath);

        // Create a unique filename
        string nameNoExt = "chat_history";
        string ext = ".txt";
        string filePath = Path.Combine(basePath, nameNoExt + ext);

        int i = 1;
        while (File.Exists(filePath))
        {
            filePath = Path.Combine(basePath, $"{nameNoExt}({i}){ext}");
            i++;
        }

        // Write the log
        try
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            foreach (var m in history)
                writer.WriteLine($"[{m.role}]: {m.content}");

            notifyManager.ShowNotification("Chat history successfully exported");
            Debug.Log($"Exported to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export: {e}");
            notifyManager.ShowNotification("Failed to export");
        }

        exportHistoryPopup.SetActive(false);
    }

    /// <summary>
    /// Instantiates the proper bubble prefab, assigns text, rebuilds the layout
    /// so the ContentSizeFitters/Layouts update immediately, and auto-scrolls down.
    /// </summary>
    private void CreateBubble(string text, bool isUser)
    {
        var prefab = isUser ? userBubblePrefab : botBubblePrefab;
        var go = Instantiate(prefab, chatContent);

        // Assign message text
        go.GetComponentInChildren<TMP_Text>().text = text;

        // Force layout calculations right away to avoid visual "pop" or clipping
        var rtBubble = go.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rtBubble);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chatContent);

        // Auto-scroll to the end
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0;
    }
}
