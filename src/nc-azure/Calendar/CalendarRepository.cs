using Microsoft.Graph.Models;
using Microsoft.Graph;
using System.Runtime.CompilerServices;

public class CalendarRepository : IRepository<CalendarEvent>
{
    private readonly GraphServiceClient _graphClient;
    private readonly CalendarRepositoryOptions _options;
    private readonly Calendar _calendar;

    public CalendarRepository(GraphServiceClient graphClient, CalendarRepositoryOptions options, Calendar calendar)
    {
        _graphClient = graphClient;
        _options = options;
        _calendar = calendar;
    }

    public async IAsyncEnumerable<CalendarEvent> SearchAsync(IQuery<CalendarEvent> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var events = await _graphClient.Users[_options.UserEmail].Calendars[_calendar.Id].Events
            .GetAsync(cancellationToken: cancellationToken);
        var eventQuery = query as CalendarQuery;
        if (events?.Value != null)
        {
            foreach (var calendarEvent in events.Value)
            {
                var eventInstance = new CalendarEvent
                {
                    Id = calendarEvent.Id,
                    Subject = calendarEvent.Subject,
                    Start = DateTimeOffset.Parse(calendarEvent.Start.DateTime),
                    End = DateTimeOffset.Parse(calendarEvent.End.DateTime),
                    Body = calendarEvent.Body?.Content ?? string.Empty,
                    Attendees = calendarEvent.Attendees?.Select(a => a.EmailAddress.Address).ToList() ?? new List<string>()
                };

                if (eventQuery?.Criteria(eventInstance) ?? false)
                {
                    yield return eventInstance;
                }
            }
        }
    }

    public async Task<CalendarEvent> SaveAsync(CalendarEvent instance, CancellationToken cancellationToken = default)
    {
        var graphEvent = new Event
        {
            Subject = instance.Subject,
            Start = new DateTimeTimeZone { DateTime = instance.Start.ToString("o"), TimeZone = instance.TimeZone },
            End = new DateTimeTimeZone { DateTime = instance.End.ToString("o"), TimeZone = instance.TimeZone },
            Body = new ItemBody { Content = instance.Body, ContentType = BodyType.Text },
            Attendees = instance.Attendees.Select(a => new Attendee
            {
                EmailAddress = new EmailAddress { Address = a },
                Type = AttendeeType.Required
            }).ToList()
        };

        if (string.IsNullOrEmpty(instance.Id))
        {
            // Create a new event
            var createdEvent = await _graphClient.Users[_options.UserEmail].Calendars[_calendar.Id].Events
                .PostAsync(graphEvent, cancellationToken: cancellationToken);

            instance.Id = createdEvent.Id;
        }
        else
        {
            // Update an existing event
            await _graphClient.Users[_options.UserEmail].Calendars[_calendar.Id].Events[instance.Id]
                .PatchAsync(graphEvent, cancellationToken: cancellationToken);
        }

        return instance;
    }

    public async Task<CalendarEvent> DeleteAsync(CalendarEvent instance, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(instance.Id))
        {
            await _graphClient.Users[_options.UserEmail].Calendars[_calendar.Id].Events[instance.Id]
                .DeleteAsync(cancellationToken: cancellationToken);
        }

        return instance;
    }
}
