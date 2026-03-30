
using Raven.Client.Documents;

namespace AiAgentDemo;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var store = new DocumentStore
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

        builder.Services.AddSingleton(chatOrchestrator);

        // Telegram Chatbot Hosted Service
        builder.Services.AddHostedService<TelegramHostedService>();

        var app = builder.Build();

        // HTTP EndPoint
        app.MapPost("/api/chat", async (ChatRequest request, ChatOrchestrator orchestrator, CancellationToken ct) =>
        {
            var response = await orchestrator.HandleHttpPromptAsync(request.UserId, request.Prompt, ct);
            return Results.Ok(new ChatResponse(response));
        });

        Console.WriteLine("Web API is running...");
        await app.RunAsync();
    }
}
