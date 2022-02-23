namespace QuitQQ.App.Messaging;

internal class CompositeMessage : IMessage
{
    public string Text { get; set; } = String.Empty;
    public List<string> Images { get; set; } = new();
    public List<FileMessage> Files { get; set; } = new();

    public void Deconstruct(out string text, out List<string> images, out List<FileMessage> files)
    {
        text = Text;
        images = Images;
        files = Files;
    }

}
