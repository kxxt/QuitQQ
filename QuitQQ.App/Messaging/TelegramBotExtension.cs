using QuitQQ.App.Utils;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace QuitQQ.App.Messaging;

internal static class TelegramBotExtension
{
    public static async Task<Message?>
        SendMessageWithErrorHandlingAsync(this ITelegramBotClient bot,
            Func<ITelegramBotClient, Task<Message>> taskMaker, ChatId? eventChatId = null)
    {
        try
        {
            var result = await taskMaker(bot);
            return result;
        }
        catch (ApiRequestException e) when (e.ErrorCode == 429) // Too many requests
        {
            if (e.Parameters?.RetryAfter != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(e.Parameters.RetryAfter.Value));
                return await taskMaker(bot);
            }

            Console.WriteLine("ERROR: Encountered HTTP 429 without parameter `RetryAfter`.");
            return null;
        }
        catch (ApiRequestException e) when (e.ErrorCode == 400) // Bad request
        {
            var log = $@"[Bot Error]: 400 Bad Request
{e.FormatException()}";
            if (eventChatId is not null)
            {
                await bot.SendMessageWithErrorHandlingAsync(
                    pBot => pBot.SendTextMessageAsync(eventChatId, log),
                    eventChatId);
            }

            Console.WriteLine(log);
        }
        catch (Exception ex) // Unknown Error
        {
            var log = $@"[Bot Error]: Unknown Error
{ex.FormatException()}";
            if (eventChatId is not null)
                try
                {
                    // Do not make recursive calls here.
                    // Because we don't know the cause.
                    await bot.SendTextMessageAsync(eventChatId, log);
                }
                catch
                {

                }
            Console.WriteLine(log);
        }
        return null;
    }

    public static async Task<Message[]?>
        SendMessageWithErrorHandlingAsync(this ITelegramBotClient bot,
            Func<ITelegramBotClient, Task<Message[]>> taskMaker, ChatId? eventChatId = null)
    {
        try
        {
            var result = await taskMaker(bot);
            return result;
        }
        catch (ApiRequestException e) when (e.ErrorCode == 429) // Too many requests
        {
            if (e.Parameters?.RetryAfter != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(e.Parameters.RetryAfter.Value));
                return await taskMaker(bot);
            }

            Console.WriteLine("ERROR: Encountered HTTP 429 without parameter `RetryAfter`.");
            return null;
        }
        catch (ApiRequestException e) when (e.ErrorCode == 400) // Bad request
        {
            var log = $@"[Bot Error]: 400 Bad Request
{e.FormatException()}";
            if (eventChatId is not null)
            {
                await bot.SendMessageWithErrorHandlingAsync(
                    pBot => pBot.SendTextMessageAsync(eventChatId, log),
                    eventChatId);
            }

            Console.WriteLine(log);
        }
        catch (Exception ex) // Unknown Error
        {
            var log = $@"[Bot Error]: Unknown Error
{ex.FormatException()}";
            if (eventChatId is not null)
                try
                {
                    // Do not make recursive calls here.
                    // Because we don't know the cause.
                    await bot.SendTextMessageAsync(eventChatId, log);
                }
                catch
                {

                }
            Console.WriteLine(log);
        }
        return null;
    }
}

