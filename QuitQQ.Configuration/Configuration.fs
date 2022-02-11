namespace QuitQQ.Configuration

open FSharp.Configuration

type public AppConfig = YamlConfig<"../config.yaml", false, "", false>

// Unfortunately， F#'s type abbreviation won't work in C#

type public ForwardingConfig = AppConfig.Forwardings_Item_Type
type public QQConfig = AppConfig.QQ_Type
type public TelegramConfig = AppConfig.Telegram_Type

module public ConfigManager =
    let public ReadConfig() = 
        let conf = AppConfig()
        conf.Load("config.yaml")
        conf
