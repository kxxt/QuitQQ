namespace QuitQQ.App.Bots;
using Telegram.Bot;

internal class TelegramBot
{
    private TelegramConfig _config;
    private TelegramBotClient _bot;

    public TelegramBot(TelegramConfig config)
    {
        _config = config;
        _bot = new TelegramBotClient(config.Token);
    }
}
