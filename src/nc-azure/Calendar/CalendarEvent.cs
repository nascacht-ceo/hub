using Microsoft.Graph.Models;

public class CalendarEvent: IEvent
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public List<string> Attendees { get; set; } = new();
    public ItemBody ItemBody { get; set; } = new ItemBody();

    public string Body 
    {   get => ItemBody.Content ?? string.Empty;
        set 
        {
            ItemBody.Content = value;
            ItemBody.ContentType = BodyType.Text;
        } 
    }
}
