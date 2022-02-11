using QuitQQ.App.Bots;
using QuitQQ.Configuration;

namespace QuitQQ.App;

internal class MessageBridge
{
    private QQBot _qqBot;
    private TelegramBot _tgBot;
    private IList<ForwardingConfig> _forwardingConfigs;
    public MessageBridge(AppConfig config)
    {
        _qqBot = new QQBot(config.QQ);
        _tgBot = new TelegramBot(config.Telegram);
        _forwardingConfigs = config.Forwardings;
    }

    public async Task StartAsync()
    {
        // Initialize Bots
        await _qqBot.StartAsync();

    }
}

