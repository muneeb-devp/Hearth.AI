using Hearth.Blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace Hearth.Blazor.Components;

public partial class HearthChat : IAsyncDisposable
{
    [Parameter] public string? SystemPrompt { get; set; }
    [Parameter] public string Placeholder { get; set; } = "Ask me anything…";
    [Parameter] public bool EnableMarkdown { get; set; } = true;
    [Parameter] public HearthChatTheme Theme { get; set; } = HearthChatTheme.Default;
    [Parameter] public IList<AITool>? Tools { get; set; }
    [Parameter] public ChatOptions? ChatOptions { get; set; }
    [Parameter] public RenderFragment? EmptyStateContent { get; set; }
    [Parameter] public RenderFragment<ChatEntry>? MessageTemplate { get; set; }
    [Parameter] public EventCallback<string> OnMessageSent { get; set; }
    [Parameter] public EventCallback<ChatEntry> OnResponseReceived { get; set; }
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    private readonly List<ChatMessage> _history = [];
    private readonly List<ChatEntry> _entries = [];
    private bool _isStreaming;
    private ElementReference _messagesContainer;
    private CancellationTokenSource? _cts;
    private IJSObjectReference? _module;

    private async Task HandleSendAsync(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText) || _isStreaming)
        {
            return;
        }

        var userEntry = new ChatEntry { Id = Guid.NewGuid().ToString(), Role = ChatRole.User, Content = userText };
        _entries.Add(userEntry);
        _history.Add(new ChatMessage(ChatRole.User, userText));

        var assistantEntry = new ChatEntry { Id = Guid.NewGuid().ToString(), Role = ChatRole.Assistant, IsStreaming = true };
        _entries.Add(assistantEntry);

        _isStreaming = true;
        _cts = new CancellationTokenSource();
        await OnMessageSent.InvokeAsync(userText);
        StateHasChanged();

        var messages = BuildMessages();
        var opts = BuildEffectiveChatOptions();

        try
        {
            await foreach (var update in ChatClient.GetStreamingResponseAsync(messages, opts, _cts.Token))
            {
                assistantEntry.Content += update.Text;
                StateHasChanged();
                await Task.Yield();
            }
        }
        catch (OperationCanceledException)
        {
            // user cancelled — content stays as-is
        }
        catch (Exception ex)
        {
            assistantEntry.Content = "An error occurred. Please try again.";
            await OnError.InvokeAsync(ex);
        }
        finally
        {
            assistantEntry.IsStreaming = false;
            _isStreaming = false;
            _history.Add(new ChatMessage(ChatRole.Assistant, assistantEntry.Content));
            _cts?.Dispose();
            _cts = null;
            await OnResponseReceived.InvokeAsync(assistantEntry);
            StateHasChanged();
        }
    }

    private ChatOptions? BuildEffectiveChatOptions()
    {
        if (Tools is not { Count: > 0 })
        {
            return ChatOptions;
        }

        var merged = new List<AITool>(ChatOptions?.Tools ?? []);
        merged.AddRange(Tools);

        return new ChatOptions
        {
            ModelId = ChatOptions?.ModelId,
            Temperature = ChatOptions?.Temperature,
            MaxOutputTokens = ChatOptions?.MaxOutputTokens,
            TopP = ChatOptions?.TopP,
            TopK = ChatOptions?.TopK,
            FrequencyPenalty = ChatOptions?.FrequencyPenalty,
            PresencePenalty = ChatOptions?.PresencePenalty,
            Seed = ChatOptions?.Seed,
            StopSequences = ChatOptions?.StopSequences,
            ToolMode = ChatOptions?.ToolMode,
            ResponseFormat = ChatOptions?.ResponseFormat,
            Tools = merged,
        };
    }

    private List<ChatMessage> BuildMessages()
    {
        var all = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(SystemPrompt))
        {
            all.Add(new ChatMessage(ChatRole.System, SystemPrompt));
        }

        all.AddRange(_history);
        return all;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Hearth.AI.Blazor/hearth-chat.js");

            if (_entries.Count > 0)
            {
                await _module.InvokeVoidAsync("scrollToBottom", _messagesContainer);
            }
        }
        catch (JSException)
        {
            // JS interop not available (e.g. prerender or SSR)
        }
    }

    public void Cancel() => _cts?.Cancel();

    public void ClearHistory()
    {
        _history.Clear();
        _entries.Clear();
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
