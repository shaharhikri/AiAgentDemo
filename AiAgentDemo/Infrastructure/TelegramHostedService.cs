namespace AiAgentDemo;

internal class TelegramHostedService(ChatOrchestrator chatOrchestrator) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botRunner = new TelegramBotRunner(chatOrchestrator.HandleTelegramPromptAsync);
        Console.WriteLine("Telegram bot is running...");
        await botRunner.RunAsync(stoppingToken);
    }
}
