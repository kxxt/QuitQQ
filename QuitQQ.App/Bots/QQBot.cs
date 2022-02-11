namespace QuitQQ.App.Bots;

using Mirai.Net.Sessions;

internal class QQBot
{
    private QQConfig _config;
    private MiraiBot _bot;
    public QQBot(QQConfig config)
    {
        _config = config;
        _bot = new MiraiBot()
        {
            Address = config.Mirai.Address,
            QQ = config.Number,
            VerifyKey = config.Mirai.VerifyKey
        };
    }
}

