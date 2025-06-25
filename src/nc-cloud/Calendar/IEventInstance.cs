public interface IEvent
{
    public string Id { get; set; }
    public string Subject { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string TimeZone { get; set; }
    public List<string> Attendees { get; set; }
    public string Body { get; set; }
}
