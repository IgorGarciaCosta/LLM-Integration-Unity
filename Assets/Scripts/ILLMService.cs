using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ILLMService
{
    Task<string> ChatAsync(List<ChatMessageDto> history, CancellationToken ct = default);
}
