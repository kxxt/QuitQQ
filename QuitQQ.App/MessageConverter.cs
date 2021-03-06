using Mirai.Net.Sessions.Http.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuitQQ.App.Messaging;
using QuitQQ.App.Utils;
using System.Text;

namespace QuitQQ.App;

using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;

internal static class MessageConverter
{
    public static string ToRawMessage(MessageBase msg)
    {
        switch (msg)
        {
            case PlainMessage pm:
                return pm.Text;
            case AppMessage am:
                return $"AppMessage: {am.Content}";
            case FlashImageMessage flash:
                return $"FlashImage: {flash.Path}\n{flash.Url}\n{flash.ImageId}";
            case ImageMessage image:
                return $"ImageMessage:\nurl: {image.Url}\nid: {image.ImageId}";
            case FileMessage file:
                return $"FileMessage: \nname: {file.Name}\nid: {file.FileId}\nsize: {file.Size}";
            case AtAllMessage:
                return "@全体成员";
            case AtMessage at:
                return $"AtMessage:\ntarget: \n{at.Target}";
            case DiceMessage dice:
                return $"Dice: {dice.Value}";
            case FaceMessage face:
                return $"【表情：{face.Name}】";
            case ForwardMessage forward:
                return $"Forward: {forward.NodeList}";
            case PokeMessage poke:
                return $"【戳一戳：{poke.Name}】";
            case MusicShareMessage m:
                return $"MusicShare: {m.Title}\n{m.Brief}\n{m.JumpUrl}\n{m.Summary}\n{m.Title}\n{m.PictureUrl}";
            case JsonMessage j:
                return $"Json: {j.Json}";
            case XmlMessage xml:
                return $"Xml: {xml.Xml}";
            case VoiceMessage v:
                return $"VoiceMessage:\nurl: {v.Url}\nid: {v.VoiceId}";
            case SourceMessage source:
                return $"SourceMessage:\ntime: {source.Time}\nid: {source.MessageId}";
            case QuoteMessage q:
                return $"QuoteMessage:\ngroup: {q.GroupId}\ntarget: {q.TargetId}\nsender: {q.SenderId}";
            case MarketFaceMessage m:
                return $"【商城表情:{m.Name}】";
            case MiraiCodeMessage mc:
                return $"[MiraiCode:{mc.Code}]";
            default:
                return "Unknown Message";
        }
    }

    public static async Task<IMessage> ToTelegramMessages(GroupMessageReceiver r)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[{r.GroupName}]");
        try
        {
            var profile = await r.Sender.GetMemberProfileAsync();
            sb.AppendLine($"{r.Sender.Name} ({profile.NickName})");
        }
        catch // Failure may arise when getting user profile.
        {
            sb.AppendLine($"{r.Sender.Name} ({r.Sender.Id})");
        }

        string prefix = sb.ToString();
        IMessage result = ReduceMessageChain(r.MessageChain, r.GroupId);
        result.Text = prefix + result.Text;
        return result;
    }

    public static IMessage ToTelegramMessages(ForwardMessage.ForwardNode node)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[转发的消息]");
        sb.AppendLine($"{node.SenderName} ({node.SenderId})");
        string prefix = sb.ToString();
        IMessage result = ReduceMessageChain(node.MessageChain, null);
        result.Text = prefix + result.Text;
        return result;
    }

    private static IMessage ReduceMessageChain(IEnumerable<MessageBase> chain, string? groupId)
    {
        StringBuilder parsingStatus = new();
        StringBuilder sb = new();
        var msgs = new Queue<MessageBase>();
        foreach (var message in chain) msgs.Enqueue(message);
        CompositeMessage result = new();
        // Check for empty message
        if (msgs.Count == 0)
        {
            parsingStatus.AppendLine("Parser: 收到空消息");
            result.Text = parsingStatus.ToString();
            return result;
        }
        // Check for SourceMessage, usually the first one.
        if (msgs.Peek() is SourceMessage src)
        {
            sb.AppendLine($"时间：{DateTimeExtension.UnixTimeStampToBeijingDateTime(long.Parse(src.Time))}");
            msgs.Dequeue();
        }
        else
        {
            if (groupId != null) // 转发的消息没有 SourceMessage 和 groupId
                parsingStatus.AppendLine("Parser: 警告！首条消息非 SourceMessage!");
        }
        // Check for QuoteMessage, usually the second one.
        if (msgs.Count > 0 && msgs.Peek() is QuoteMessage q)
        {
            sb.AppendLine($"引用：{q.SenderId} 的消息");
            var origin = q.Origin;
            foreach (var omsg in origin)
            {
                sb.AppendLine(ToRawMessage(omsg));
            }
            sb.AppendLine("引用结束");
            msgs.Dequeue();
        }
        // Check for forwarded messages
        if (msgs.Count > 0 && msgs.Peek() is ForwardMessage fmsg)
        {
            sb.AppendLine("[转发的消息记录(见下)]");
            ForwardedMessages fResult = new(sb.ToString() + '\n' + parsingStatus, new List<IMessage>());
            foreach (var node in fmsg.NodeList)
            {
                fResult.Messages.Add(ToTelegramMessages(node));
            }
            return fResult;
        }
        // Reduce messages
        int picNum = 0;
        while (msgs.Count > 0)
        {
            var msg = msgs.Dequeue();
            if (CanBeConvertToTextMessage(msg.Type))
            {
                sb.Append(ConvertToTextMessage(msg));
            }
            else switch (msg)
                {
                    // FlashImageMessage is also ImageMessage
                    case ImageMessage im:
                        result.Images.Add(im.Url);
                        sb.Append($"[图片{++picNum}]");
                        break;
                    case FileMessage fm when groupId != null:
                        result.Files.Add(new Messaging.FileMessage(fm.FileId, groupId, fm.Name, fm.Size));
                        break;
                    case FileMessage fm:
                        parsingStatus.AppendLine($"来自未知群组的文件(无法转发到电报)：\n文件名：{fm.Name}\n文件ID:{fm.FileId}\n文件大小{fm.Size}");
                        break;
                    default:
                        sb.AppendLine(ToRawMessage(msg));
                        break;
                }
        }

        result.Text = sb.ToString() + '\n' + parsingStatus;
        return result;
    }

    private static readonly HashSet<Messages> TextConvertibleMessageTypes =
        new()
        {
            Messages.Plain,
            Messages.Poke,
            Messages.Dice,
            Messages.AtAll,
            Messages.Face,
            Messages.At,
            Messages.MarketFace,
            Messages.MiraiCode,
            Messages.App
        };


    private static bool CanBeConvertToTextMessage(Messages t)
    {
        return TextConvertibleMessageTypes.Contains(t);
    }

    private static string GetDescFromAppMessageDetail(JToken detail) => detail["desc"]!.Value<string>()!;
    private static string GetUrlFromAppMessageDetail(JToken detail) => detail["qqdocurl"]!.Value<string>()!;

    private static string? GetHostFromAppMessageDetail(JToken detail)
    {
        if (detail["host"] is { } host)
            return $"来源：{host["nick"]!.Value<string>()} ({host["uin"]})";
        return null;
    }
    private static string ConvertAppMessage(AppMessage am)
    {
        var sb = new StringBuilder();
        try
        {
            var json = JsonConvert.DeserializeObject<JObject>(am.Content)!;
            if (json["prompt"]?.Type == JTokenType.String)
                sb.AppendLine(json["prompt"]!.Value<string>());
            var detail = json["meta"]?["detail_1"];
            if (detail != null)
            {
                var title = detail["title"]!.Value<string>()!;
                switch (title)
                {
                    case "哔哩哔哩":
                        sb.AppendLine(GetDescFromAppMessageDetail(detail));
                        sb.AppendLine(GetUrlFromAppMessageDetail(detail));
                        sb.AppendLine(GetHostFromAppMessageDetail(detail));
                        return sb.ToString();
                    case "腾讯文档":
                        sb.AppendLine(GetDescFromAppMessageDetail(detail));
                        sb.AppendLine(GetUrlFromAppMessageDetail(detail));
                        sb.AppendLine(GetHostFromAppMessageDetail(detail));
                        return sb.ToString();
                    default:
                        return am.Content;
                }
            }

            // WeiXin
            var news = json["meta"]?["news"]!;
            var uin = news["uin"]!.Value<long>()!;
            var url = news["jumpUrl"]!.Value<string>()!;
            sb.AppendLine($"uin: {uin}");
            sb.AppendLine(url);
            return sb.ToString();
        }
        catch
        {
            return am.Content;
        }
    }

    private static string ConvertToTextMessage(MessageBase msg)
    {
        return msg switch
        {
            PlainMessage p => p.Text,
            PokeMessage p => $"[戳一戳:{p.Name}]",
            DiceMessage d => $"[骰子:{d.Value}]",
            AtAllMessage a => "@全体成员",
            AtMessage a => $"@{a.Target}",
            FaceMessage f => $"[表情:{f.Name}]",
            MarketFaceMessage m => $"[商城表情:{m.Name}]",
            MiraiCodeMessage mc => $"[MiraiCode:{mc.Code}]",
            AppMessage am => ConvertAppMessage(am),
            _ => throw new ArgumentException("msg not convertible to text.", nameof(msg))
        };
    }
}

