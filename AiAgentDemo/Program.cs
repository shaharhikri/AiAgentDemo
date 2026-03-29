
using AiAgentDemo.Classes;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.AI;
using System.Text;

namespace AiAgentDemo;

public class Program
{
    public static async Task Main()
    {
        // ==== Database Init =====
        using var store = new DocumentStore()
        {
            Database = "SportsDB",
            Urls = new[] { "http://localhost:8080" },
        }.Initialize();
        await DatabaseHelper.TryCreateDatabaseAsync(store, store.Database);
        await DatabaseHelper.ConfigureAgentsAsync(store);
        await DatabaseHelper.CreateSampleDataAsync(store);

        var agents = await DatabaseHelper.ConfigureAgentsAsync(store);
        var preGuard = new PromptGuard(store, agents.PreGuardId);
        var postGuard = new PromptGuard(store, agents.PostGuardId);
        var agentIdentifier = agents.AgentId;

        // ==== Bot Run =====
        var bot = new TelegramBot((telegramUserId, userPrompt) => GetAnswer(telegramUserId, userPrompt));
        await bot.Initialize();

        Console.WriteLine("Telegram-Bot - " + bot.Name + Environment.NewLine + "Bot is running...");
        await Task.Delay(-1);

        async Task<string> GetAnswer(long telegramUserId, string userPrompt)
        {
            var classification = await preGuard.ClassifyAsync(userPrompt);
            PrintPrompt("User " + telegramUserId, userPrompt, ConsoleColor.DarkGreen);
            if (ConversationHelper.IsBlocked(classification, userPrompt, out var blockingMessage))
            {
                PrintPrompt("Agent", "User", userPrompt, blockingMessage, classification, ConsoleColor.Blue);
                return blockingMessage;
            }

            var answer = await AgentAnswer(store, agentIdentifier, "Chats/" + telegramUserId, userPrompt);
            classification = await postGuard.ClassifyAsync(answer);
            if (ConversationHelper.IsBlocked(classification, answer, out blockingMessage))
            {
                PrintPrompt("Agent", "Agent", answer, blockingMessage, classification, ConsoleColor.Blue);
                return blockingMessage;
            }
            PrintPrompt("Agent", answer, ConsoleColor.Blue);
            return answer;
        }
    }

    private static int c = 0;
    private static async Task<string> AgentAnswer(IDocumentStore store, string agentIdentifier, string chatId, string userPrompt, CancellationToken token = default)
    {
        // if (++c % 3 == 0) return "תמות!";
        //
        // return "סבבה";

        var chat = store.AI.Conversation(agentIdentifier, chatId, new AiConversationCreationOptions()
        {
            ExpirationInSec = -1
        }.AddParameter("userId", "Users/1"));
        var safePrompt = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(userPrompt));
        chat.SetUserPrompt(safePrompt);
        var r = await chat.RunAsync<CoachResponse>(token);
        if (r?.Answer?.Answer == null)
            return string.Empty;

        return r.Answer.Answer;
    }

    private static void PrintPrompt(string role, string text, ConsoleColor color)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(role + ": ");
        Console.ForegroundColor = color;
        Console.Write(NormalizeForConsole(text));
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
    }


    private static void PrintPrompt(string role, string blockedRole, string oldPrompt, string prompt, Classification? classification, ConsoleColor color)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(role + ": ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{blockedRole} said '{NormalizeForConsole(oldPrompt)}'");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write($" [{classification}]");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(NormalizeForConsole(" => "));
        Console.ForegroundColor = color;
        Console.Write(NormalizeForConsole(prompt));
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
    }

    static string NormalizeForConsole(string text)
    {
        var lang = ConversationHelper.DetectLanguage(text);
        return lang switch
        {
            ConversationLanguage.Hebrew => Reverse(text),
            ConversationLanguage.Arabic => Reverse(text),
            _ => text
        };
    }

    static string Reverse(string text)
    {
        var chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
