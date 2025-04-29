namespace MauiApp1234.Models;

public class ChatMessage
{
    public string Content { get; set; }
    public bool IsUser { get; set; }
    public bool IsNotUser => !IsUser;
}
