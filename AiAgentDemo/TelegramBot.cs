using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiAgentDemo;
internal class TelegramBot(Func<long, string, Task<string>> getAnswer)
{
    public const string EnvironmentVariable = "TELEGRAM_BOT_DEM_TOKEN";
    public string Name { get; private set; }

    public async Task Initialize()
    {
        var botToken = Environment.GetEnvironmentVariable(EnvironmentVariable);

        var bot = new TelegramBotClient(botToken);
        Name = (await bot.GetMe()).Username!;

        using var cts = new CancellationTokenSource();

        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            },
            cancellationToken: cts.Token
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is null)
            return;

        if (message.From is null)
            return;

        var answer = await getAnswer.Invoke(message.From.Id, message.Text);

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

