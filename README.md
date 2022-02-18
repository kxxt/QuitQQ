# QuitQQ

[![GitHub issues](https://img.shields.io/github/issues/kxxt/QuitQQ)](https://github.com/kxxt/QuitQQ/issues)
[![GitHub forks](https://img.shields.io/github/forks/kxxt/QuitQQ)](https://github.com/kxxt/QuitQQ/network)
[![GitHub stars](https://img.shields.io/github/stars/kxxt/QuitQQ)](https://github.com/kxxt/QuitQQ/stargazers)
[![LICENSE](https://img.shields.io/badge/license-AGPL-yellowgreen)](LICENSE)
![.Net](https://img.shields.io/badge/.net-6.0-purple)
![C#](https://img.shields.io/badge/C%23-10.0-brightgreen)
![F#](https://img.shields.io/badge/F%23-6.0-purple)

一个减少 QQ 使用的服务程序。

很惭愧，写了一手屎山，两天时间整出来的。

## 功能

### 消息转发

将部分重要的 QQ 通知群的消息转发到电报，支持常见的消息类型（文本、图片、文件）。对于不支持的消息类型，将转换为纯文本。

### 自动回复

自动回复好友消息，告知好友你的其他联系方式。

### 事件转发

自动接受入群邀请并在电报进行提示。很多其他类型的事件也会在电报进行提示。

## Build

Create a config file `config.yaml` in the repo.

```yaml
Telegram:
  Token: # Your telegram bot token

QQ:
  Number: # Your QQ Number
  Mirai:
    Address: # Mirai http API address, e.g. 'localhost:8000'
    VerifyKey: # Mirai http API verify key

System:
  MaxFileDownloadSize: # Max size for file to be forwarded to telegram, e.g. 2000000000 (in bytes)
  EventMessageTarget: # Telegram chat id to receive event messages
  ReplyToFriendsDelayDays: # Delay before sending another auto reply to your friend
  ReplyToFriends: 【来自 kxxt 的自动回复】 # Message for auto reply, use a blank line to represent a new line.

    您好，感谢您联系 kxxt, 此 QQ 账号已不再使用，您在此处发送的消息我无法收到。

    若您有事务想要与我商讨, 请使用电子邮件联系 rsworktech@outlook.com, 我建议您使用 PGP 加密电子邮件, 您可以在 Ubuntu Key Server 上找到我的 PGP 公钥。

    再次感谢您联系 kxxt，此自动回复在一段时间内不会再次触发，祝您身体健康，工作顺利。

Forwardings: # Config for message forwardings
  - Sources: ['342342342349', '691934563437'] # Message sources (QQ Number)
    Target: -1003240230500  # Chat id to receive messages
  - Sources: ['383234232388']
    Target: -1231489234592
```

Now you can run `dotnet build` to build the project.

## Credits

Powered by [Mirai.Net](https://github.com/SinoAHpx/Mirai.Net) and [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot).
