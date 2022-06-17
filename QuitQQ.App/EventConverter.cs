using Mirai.Net.Data.Events;
using Mirai.Net.Data.Events.Concretes.Bot;
using Mirai.Net.Data.Events.Concretes.Friend;
using Mirai.Net.Data.Events.Concretes.Group;
using Mirai.Net.Data.Events.Concretes.Message;
using Mirai.Net.Data.Events.Concretes.OtherClient;
using Mirai.Net.Data.Events.Concretes.Request;
using Mirai.Net.Data.Shared;
using Mirai.Net.Sessions.Http.Managers;
using QuitQQ.App.Utils;

namespace QuitQQ.App;

internal class EventConverter
{
    private HashSet<string> _excluded;

    public EventConverter(IEnumerable<string> excluded)
    {
        _excluded = new(excluded);
    }

    private bool IsExcluded<T>(GroupSettingChangedEventBase<T> e) => _excluded.Contains(e.Group.Id);
    private bool IsExcluded(GroupMessageRecalledEvent e) => _excluded.Contains(e.Group.Id);
    private bool IsExcluded(JoinedEvent e) => _excluded.Contains(e.Group.Id);
    private bool IsExcluded(KickedEvent e) => _excluded.Contains(e.Group.Id);
    private bool IsExcluded(LeftEvent e) => _excluded.Contains(e.Group.Id);
    private bool IsExcluded(FriendNickChangedEvent e) => _excluded.Contains(e.Friend.Id);
    private bool IsExcluded(FriendInputStatusChangedEvent e) => _excluded.Contains(e.Friend.Id);
    private bool IsExcluded(FriendRecalledEvent e) => _excluded.Contains(e.AuthorId);

    public string? ToPlainText(EventBase e)
    {
        switch (e)
        {
            case DroppedEvent:
                return "[Mirai Bot]\n机器人掉线";
            case ReconnectedEvent:
                return "[Mirai Bot]\n机器人重新连接";
            case FriendInputStatusChangedEvent:
                return null;
            case FriendNickChangedEvent fe:
                return !IsExcluded(fe)
                    ? $"QQ 好友 {fe.Friend.Id} (备注: {fe.Friend.Remark}) 的昵称从 {fe.Origin} 改为了 {fe.New}"
                    : null;
            case FriendRecalledEvent fre:
                return !IsExcluded(fre)
                    ? $"{DateTimeExtension.UnixTimeStampToBeijingDateTime(long.Parse(fre.Time))}\nQQ 好友 {fre.Operator} 撤回了一条消息"
                    : null;
            case GroupAllowedAnonymousChatEvent gace:
                return !IsExcluded(gace)
                    ? $"QQ 群 {gace.Group.Name} 已{(gace.Current ? "允许" : "禁止")}匿名聊天\n操作人：{gace.Operator.Name}"
                    : null;
            case GroupAllowedConfessTalkChanged gacte:
                return !IsExcluded(gacte)
                    ? $"QQ 群 {gacte.Group.Name} 已{(gacte.Current ? "允许" : "禁止")}坦白说\n操作人：{gacte.Operator.Name}"
                    : null;
            case GroupAllowedMemberInviteEvent gamie:
                try
                {
                    return !IsExcluded(gamie)
                    ? $"QQ 群 {gamie.Group.Name} 已{(gamie.Current ? "允许" : "禁止")}群成员邀请他人入群\n操作人：{gamie.Operator.Name}"
                    : null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.FormatException());
                    Console.WriteLine(gamie.ToString());
                }
                break;

            case GroupEntranceAnnouncementChangedEvent geace:
                return !IsExcluded(geace)
                    ? $"QQ 群 {geace.Group.Name}入群公告改变\nOld: {geace.Origin}\nNew: {geace.Current}\n操作人：{geace.Operator.Name}"
                    : null;
            case GroupMessageRecalledEvent gmre:
                return !IsExcluded(gmre)
                    ? $"QQ 群 {gmre.Group.Name} {gmre.Operator.Name} 撤回了一条消息 {gmre.MessageId}"
                    : null;
            case GroupMutedAllEvent gmae:
                return !IsExcluded(gmae)
                    ? $"QQ 群 {gmae.Group.Name} 已{(gmae.Current ? "开启" : "关闭")}全员禁言\n操作人：{gmae.Operator.Name}"
                    : null;
            case GroupNameChangedEvent gnce:
                return !IsExcluded(gnce)
                    ? $"QQ 群 {gnce.Group.Id} 的名称已从 {gnce.Origin} 改变为 {gnce.Current}\n操作人: {gnce.Operator.Name}"
                    : null;
            case JoinedEvent je:
                return !IsExcluded(je) ? $"Bot 加入群 {je.Group.Name} ({je.Group.Id})" : null;
            case KickedEvent ke:
                return !IsExcluded(ke) ? $"Bot 被踢出群 {ke.Group.Name} ({ke.Group.Id})" : null;
            case LeftEvent le:
                return !IsExcluded(le) ? $"Bot 退出群聊 {le.Group.Name} ({le.Group.Id})" : null;
            case MemberCardChangedEvent memberCardChangedEvent:
                return null;
            case MemberHonorChangedEvent memberHonorChangedEvent:
                return null;
            case MemberJoinedEvent memberJoinedEvent:
                return null;
            case MemberKickedEvent memberKickedEvent:
                return null;
            case MemberLeftEvent memberLeftEvent:
                return null;
            case MemberMutedEvent memberMutedEvent:
                return null;
            case MemberPermissionChangedEvent memberPermissionChangedEvent:
                return null;
            case MemberTitleChangedEvent memberTitleChangedEvent:
                return null;
            case MemberUnmutedEvent memberUnmutedEvent:
                return null;
            case MutedEvent me:
                return $"Bot 被{me.Operator.Name}禁言{me.Period}s";
            case PermissionChangedEvent pce:
                return null;
            case UnmutedEvent ue:
                return $"Bot 被{ue.Operator.Name}取消禁言";
            case AtEvent atEvent:
                return null;
            case NudgeEvent ne:
                return $"[戳一戳] {ne.FromId} {ne.Action} {ne.Target} {ne.Suffix}";
            case OtherClientOfflineEvent ocoffe:
                return $"其他客户端下线: {ocoffe.Client.Platform}";
            case OtherClientOnlineEvent ocoe:
                return $"其他客户端上线: {ocoe.Client.Platform}";
            case NewFriendRequestedEvent nfre:
                return $"[QQ 好友申请]\n{nfre.FromId}\n{nfre.Nick}\n{nfre.Message}\n来源群组: {nfre.GroupId}";
            case NewInvitationRequestedEvent nire:
                RequestManager.HandleNewInvitationRequestedAsync(nire, NewInvitationRequestHandlers.Approve,
                    "[自动回复]好友添加成功");
                return $"[入群申请(已同意)]\n邀请人: {nire.Nick} ({nire.FromId})\n群: \n{nire.GroupId}\n信息: {nire.Message}";
            case NewMemberRequestedEvent nmre:
                return null;
            case RequestedEventBase requestedEventBase:
                return null;
            case OnlineEvent:
                return "[Mirai Bot]\n机器人登陆成功";
            case OfflineEvent:
                return "[Mirai Bot]\n机器人主动离线";
            case OfflineForceEvent:
                return "[Mirai Bot]\n机器人被挤下线！";
            default:
                return "[Mirai Bot]\n其他未知事件";
        }
    }
}
