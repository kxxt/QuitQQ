namespace QuitQQ.App.Messaging;

internal record ForwardedMessages(string Text, List<IMessage> Messages) : IMessage
{
    public string Text { get; set; } = Text;

    public void Deconstruct(out string text, out List<IMessage> messages)
    {
        text = this.Text;
        messages = this.Messages;
    }
}
