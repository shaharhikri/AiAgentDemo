using Raven.Client.Documents;
using Raven.Client.Documents.AI;
using System.Text;

namespace AiAgentDemo;

internal class RavenAiClient(IDocumentStore store)
{
    public async Task<Classification> ClassifyAsync(string agentIdentifier, string conversationId, string prompt, CancellationToken token = default)
    {
        var chat = store.AI.Conversation(agentIdentifier, conversationId, new AiConversationCreationOptions { ExpirationInSec = 600 });
        chat.SetUserPrompt($"""
                            the prompt:
                            "{Encode(prompt)}"
                            """);
        var result = await chat.RunAsync<GuardReply>(token);
        return result?.Answer?.Classification ?? Classification.Unknown;
    }

    public async Task<string> GetCoachResponseAsync(string agentIdentifier, string conversationId, string userId, string userPrompt, CancellationToken token = default)
    {
        var chat = store.AI.Conversation(agentIdentifier, conversationId, new AiConversationCreationOptions
        {
            ExpirationInSec = -1
        }.AddParameter("userId", userId));
        chat.SetUserPrompt(Encode(userPrompt));
        var result = await chat.RunAsync<CoachResponse>(token);
        return result?.Answer?.Answer ?? string.Empty;
    }

    private static string Encode(string text) => Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text));
}
