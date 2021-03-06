using Microsoft.Extensions.DependencyInjection;
using Mirai.Net.Data.Events;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using QuitQQ.App.Collections;
using QuitQQ.App.Messaging;
using QuitQQ.App.Utils;
using QuitQQ.Configuration;
using System.Net;
using System.Reactive.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace QuitQQ.App;

internal class MessageBridge : IDisposable
{
    private readonly MiraiBot _qqBot;
    private readonly ITelegramBotClient _tgBot;
    private readonly IList<ForwardingConfig> _forwardingConfigs;
    private readonly ChatId _eventMessageTarget;
    private readonly HttpClient _httpClient = new();
    private readonly long _maxFileDownloadSize;
    private readonly string _savedReplyToFriends;
    private readonly string _savedReplyToStrangers;

    /// <summary>
    /// A TimeoutHashSet for QQ friends that the bot talked to recently.
    /// </summary>
    private readonly TimeoutHashSet<string> _recentlyContactedFriends;

    private readonly EventConverter _eventConverter;

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1.6),
            retryCount: 6);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(delay);
    }

    public MessageBridge(AppConfig config)
    {
        _qqBot = new MiraiBot
        {
            Address = config.QQ.Mirai.Address,
            QQ = config.QQ.Number,
            VerifyKey = config.QQ.Mirai.VerifyKey
        };

        _savedReplyToFriends = config.System.ReplyToFriends;
        _savedReplyToStrangers = config.System.ReplyToStrangers;
        _recentlyContactedFriends = new(
            TimeSpan.FromDays(double.Parse(config.System.ReplyToFriendsDelayDays)));

        // Telegram Bot Setup
        var services = new ServiceCollection();
        services.AddHttpClient("telegram")
            .AddTypedClient<ITelegramBotClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.telegram.org");
                return new TelegramBotClient(config.Telegram.Token, client);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy());

        using var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetService<IServiceScopeFactory>()!;
        using var scope = scopeFactory.CreateScope();

        _tgBot = scope.ServiceProvider.GetService<ITelegramBotClient>()!;

        _forwardingConfigs = config.Forwardings;
        _eventConverter = new(config.System.EventExcludedChats);
        _maxFileDownloadSize = long.Parse(config.System.MaxFileDownloadSize);
        _eventMessageTarget = new(config.System.EventMessageTarget);
    }

    public async Task StartAsync()
    {
        // Register Exception Handlers
        AppDomain.CurrentDomain.UnhandledException += ProcessUnhandledException;
        TaskScheduler.UnobservedTaskException += ProcessUnobservedException;
        // Initialize Bots
        await _qqBot.LaunchAsync();
        // Apply forwarding configs
        foreach (var config in _forwardingConfigs)
        {
            _qqBot.MessageReceived
                .OfType<GroupMessageReceiver>()
                .Where(r => config.Sources.Contains(r.GroupId))
                .Subscribe(async r =>
                {
                    var tgMsgs = await MessageConverter.ToTelegramMessages(r);
                    await SendTelegramMessagesAsync(tgMsgs, config.Target); // TODO
                });
        }

        _qqBot.EventReceived.Subscribe(SendEventMessageToTelegramAsync);
        _qqBot.MessageReceived
            .OfType<FriendMessageReceiver>()
            .Where(r => !_recentlyContactedFriends.Contains(r.Sender.Id))
            .Subscribe(ProcessQQFriendMessage);
        _qqBot.MessageReceived
            .OfType<StrangerMessageReceiver>()
            .Where(r => !_recentlyContactedFriends.Contains(r.StrangerId))
            .Subscribe(ProcessQQStrangerMessage);
    }

    #region Exception Handling

    private async void ProcessUnobservedException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        await ProcessException(sender, e.Exception);
        e.SetObserved();
    }

    private async void ProcessUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        await ProcessException(sender, (Exception)e.ExceptionObject);
    }

    private async Task ProcessException(object? sender, Exception e)
    {
        if (e is ApiRequestException are)
        {
            if (are.ErrorCode == 429 && are.Parameters?.RetryAfter != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(are.Parameters.RetryAfter.Value + 1));
                try
                {
                    await _tgBot.SendTextMessageAsync(_eventMessageTarget, $@"TG??????????????????
???????????????{are.Parameters.RetryAfter}s ????????????");
                }
                catch (Exception exn)
                {
                    Console.WriteLine($"Error: {exn.Message}\n{exn.StackTrace}");
                }
            }
        }

        Console.WriteLine($"Error: {e.Message}\n{e.StackTrace}");
    }

    #endregion

    #region Process QQ PM

    private async void ProcessQQFriendMessage(FriendMessageReceiver r)
    {
        _recentlyContactedFriends.Add(r.Sender.Id);
        await MessageManager.SendFriendMessageAsync(r.Sender.Id, _savedReplyToFriends);
        await _tgBot.SendMessageWithErrorHandlingAsync(
            bot => bot.SendTextMessageAsync(_eventMessageTarget,
            $"??????????????? {r.Sender.NickName} (?????????{r.Sender.Remark}) ???????????????????????????")
        );
    }

    private async void ProcessQQStrangerMessage(StrangerMessageReceiver r)
    {
        _recentlyContactedFriends.Add(r.Sender.Id);
        await MessageManager.SendFriendMessageAsync(r.Sender.Id, _savedReplyToStrangers);
        await _tgBot.SendMessageWithErrorHandlingAsync(
            bot => bot.SendTextMessageAsync(_eventMessageTarget,
            $"?????????????????? {r.Sender.NickName} (?????????Name???{r.StrangerName}, Id: {r.StrangerId}) ???????????????????????????")
        );
    }

    #endregion

    #region Send QQ Events To Telegram

    private async void SendEventMessageToTelegramAsync(EventBase e)
    {
        var converted = _eventConverter.ToPlainText(e);
        if (converted != null)
            await _tgBot.SendMessageWithErrorHandlingAsync(
                bot => bot.SendTextMessageAsync(_eventMessageTarget, converted)
            );
    }

    #endregion

    #region Send QQ Messages To Telegram

    private Task SendTelegramMessagesAsync(IMessage msg, string chatId)
    {
        var chatIdInstance = new ChatId(chatId);
        if (msg is CompositeMessage cMsg) return SendCompositeMessageToTelegramAsync(cMsg, chatIdInstance, null);
        return SendForwardedMessagesToTelegramAsync((ForwardedMessages)msg, chatIdInstance);
    }

    private Task SendTelegramMessagesAsync(IMessage msg, ChatId chatId, int? replyTo)
    {
        if (msg is CompositeMessage cMsg) return SendCompositeMessageToTelegramAsync(cMsg, chatId, replyTo);
        return SendForwardedMessagesToTelegramAsync((ForwardedMessages)msg, chatId);
    }

    private Task SendForwardedMessagesToTelegramAsync(ForwardedMessages msgs, string chatId)
    {
        var chatIdInstance = new ChatId(chatId);
        return SendForwardedMessagesToTelegramAsync(msgs, chatIdInstance);
    }

    private async Task SendForwardedMessagesToTelegramAsync(ForwardedMessages msgs, ChatId chatIdInstance)
    {
        var firstMessage = await _tgBot.SendMessageWithErrorHandlingAsync(
            bot => bot.SendTextMessageAsync(chatIdInstance, msgs.Text));
        if (firstMessage == null)
        {
            return;
        }
        foreach (var msg in msgs.Messages)
        {
            await SendTelegramMessagesAsync(msg, chatIdInstance, firstMessage.MessageId);
        }
    }

    private async Task SendCompositeMessageToTelegramAsync(CompositeMessage cMsg, ChatId chatIdInstance, int? replyTo)
    {
        var (text, images, files) = cMsg;

        Message? firstMessage; // The message to be replied to.
        switch (images.Count)
        {
            case 1:
                // One photo with caption
                firstMessage = await _tgBot.SendMessageWithErrorHandlingAsync(
                    bot => bot.SendPhotoAsync(chatIdInstance, new InputOnlineFile(images.First()), text,
                        replyToMessageId: replyTo)
                );
                break;
            case > 1:
                {
                    // A text message with another album message
                    firstMessage = await _tgBot.SendMessageWithErrorHandlingAsync(
                        bot => bot.SendTextMessageAsync(chatIdInstance, text, replyToMessageId: replyTo)
                    );
                    if (firstMessage != null)
                        await _tgBot.SendMessageWithErrorHandlingAsync(
                            bot => bot.SendMediaGroupAsync(chatIdInstance,
                                from url in images select new InputMediaPhoto(url),
                                replyToMessageId: firstMessage.MessageId
                            ));
                    break;
                }
            default:
                {
                    if (files.Count == 1)
                    {
                        // One file message
                        var (fileId, groupId, fileName, fileSize) = files.First();
                        try
                        {
                            var file = await FileManagerExtension.GetFileInfoWithRetriesAsync(groupId, fileId);
                            if (fileSize > _maxFileDownloadSize)
                            {
                                await _tgBot.SendMessageWithErrorHandlingAsync(
                                    bot => bot.SendTextMessageAsync(
                                        chatIdInstance,
                                        $@"{text}
???????????????????????????????????????????????????????????????
?????????: {fileName}
???????????????{file.DownloadInfo.Url}
MD5: {file.DownloadInfo.Md5}
SHA1: {file.DownloadInfo.Sha1}
??????:  {file.Path}")
                                );
                                return;
                            }
                            await using var stream = await _httpClient.GetStreamAsync(file.DownloadInfo.Url);
                            await _tgBot.SendMessageWithErrorHandlingAsync(
                                bot => bot.SendDocumentAsync(chatIdInstance,
                                    new InputOnlineFile(stream, fileName),
                                    caption: text, replyToMessageId: replyTo));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.FormatException());
                            await SendFileFailureMessageToTelegramAsync(chatIdInstance, fileName, fileId, fileSize, groupId, text);
                        }
                        return;
                    }
                    else
                    {
                        firstMessage = await _tgBot.SendMessageWithErrorHandlingAsync(
                            bot => bot.SendTextMessageAsync(chatIdInstance, text, replyToMessageId: replyTo)
                        );
                    }

                    break;
                }
        }
        if (firstMessage != null)
            foreach (var (fileId, groupId, fileName, fileSize) in
                     files.Where(f => f.Size <= _maxFileDownloadSize))
            {
                // ???????????? QQ ????????????????????????????????????????????????????????????
                try
                {
                    var file = await FileManager.GetFileAsync(groupId, fileId, true);
                    await using var stream = await _httpClient.GetStreamAsync(file.DownloadInfo.Url);
                    await _tgBot.SendMessageWithErrorHandlingAsync(
                        bot => bot.SendDocumentAsync(chatIdInstance, new InputOnlineFile(stream, fileName),
                            replyToMessageId: firstMessage.MessageId)
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.FormatException());
                    await SendFileFailureMessageToTelegramAsync(chatIdInstance, fileName, fileId, fileSize, groupId);
                }

            }
    }

    private Task SendFileFailureMessageToTelegramAsync(ChatId chatIdInstance, string fileName, string fileId, long fileSize, string groupId, string text = "")
    {
        return _tgBot.SendMessageWithErrorHandlingAsync(
            bot => bot.SendTextMessageAsync(
                chatIdInstance,
                $@"{text}
??????????????????????????????
?????????: {fileName}
????????? {groupId}
??????ID: {fileId}
???????????????{fileSize}
"));
    }

    private Task SendCompositeMessageToTelegramAsync(CompositeMessage cMsg, string chatId)
    {
        var chatIdInstance = new ChatId(long.Parse(chatId));
        return SendCompositeMessageToTelegramAsync(cMsg, chatIdInstance, null);
    }

    #endregion

    public void Dispose()
    {
        _qqBot.Dispose();
        _httpClient.Dispose();
    }
}
