using Raven.Client.Documents;
using Raven.Client.Documents.AI;
using System.Text;
using AiAgentDemo.Classes;

namespace AiAgentDemo;

internal class PromptGuard(IDocumentStore store, string agentIdentifier)
{
    public async Task<Classification> ClassifyAsync(string prompt, CancellationToken token = default)
    {
        var chat = store.AI.Conversation(agentIdentifier, "Classifications/", new AiConversationCreationOptions()
        {
            ExpirationInSec = -1
        });
        var safePrompt = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(prompt));
        chat.SetUserPrompt($"""
                            the prompt:
                            "{safePrompt}"
                            """);
        var r = await chat.RunAsync<GuardReply>(token);
        if (r?.Answer?.Classification == null)
            return Classification.Unknown;

        return r.Answer.Classification;
    }
}




