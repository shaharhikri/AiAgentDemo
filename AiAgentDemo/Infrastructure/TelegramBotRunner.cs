using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiAgentDemo;

internal class TelegramBotRunner(Func<long, string, CancellationToken, Task<string>> getAnswer)
{
    public const string EnvironmentVariable = "TELEGRAM_BOT_DEM_TOKEN";
    public string? BotName { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var botToken = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(botToken))
            throw new InvalidOperationException($"Please set the {EnvironmentVariable} environment variable with the Telegram bot token.");

        var bot = new TelegramBotClient(botToken);
        BotName = (await bot.GetMe(cancellationToken)).Username;

        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            },
            cancellationToken: cancellationToken
        );

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is null)
            return;

        if (message.From is null)
            return;

        var answer = await getAnswer.Invoke(message.From.Id, message.Text, ct);

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: answer,
            cancellationToken: ct
        );
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine(ex);
        return Task.CompletedTask;
    }
}
