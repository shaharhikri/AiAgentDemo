
using Raven.Client.Documents;

namespace AiAgentDemo;

public class Program
{
    public static async Task Main()
    {
        using var store = new DocumentStore()
        {
            Database = "SportsDB",
            Urls = new[] { "http://localhost:8080" },
        }.Initialize();

        var setup = new RavenSetup(store);
        await setup.EnsureDatabaseExistsAsync();
        await setup.SeedSampleDataAsync();
        var agentIdentifiers = await setup.EnsureAgentsAsync();

        var aiClient = new RavenAiClient(store);
        var userProfileResolver = new RavenUserProfileResolver(store);
        var chatOrchestrator = new ChatOrchestrator(agentIdentifiers, aiClient, userProfileResolver);
        var botRunner = new TelegramBotRunner(chatOrchestrator.HandleTelegramPromptAsync);

        Console.WriteLine("Telegram bot is running...");
        await botRunner.RunAsync();
    }
}
