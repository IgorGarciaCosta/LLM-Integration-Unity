using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstraction for a chat-capable Large Language Model (LLM).
/// Implementations (e.g., OpenAIService, GeminiService) must accept a
/// full history and return a single assistant response.
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Sends the full chat history to the provider and returns the assistant's text.
    /// </summary>
    /// <param name="history">List of messages in chronological order.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> ChatAsync(List<ChatMessageDto> history, CancellationToken ct = default);
}
