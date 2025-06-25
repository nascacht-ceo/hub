using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Runtime.CompilerServices;

public class CalendarSource : ISource<CalendarEvent>
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<CalendarSource>? _logger;
    private readonly CalendarSourceOptions? _options;

    public CalendarSource(CalendarSourceOptions options, ILogger<CalendarSource>? logger = null)
    {
        _logger = logger;
        _options = options;
        var clientSecretCredential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);

        // Create GraphServiceClient
        _graphClient = new GraphServiceClient(clientSecretCredential);
    }

    public CalendarSource(GraphServiceClient graphClient, ILogger<CalendarSource>? logger = null)
    {
        _graphClient = graphClient;
        _logger = logger;
    }

    public async IAsyncEnumerable<IRepository<CalendarEvent>> SearchAsync(IRepositoryOptions<CalendarEvent> options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var calendars = await _graphClient.Me.Calendars
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = $"startswith(name, '{options.Name}')";
            }, cancellationToken);

        if (calendars.Value != null)
        {
            foreach (var calendar in calendars.Value)
            {
                yield return new CalendarRepository(_graphClient, options as CalendarRepositoryOptions, calendar);
            }
        }
    }

    public async Task<IRepository<CalendarEvent>> CreateAsync(IRepositoryOptions<CalendarEvent> options, CancellationToken cancellationToken = default)
    {
        var calendar = new Calendar
        {
            Name = options.Name
        };
        var calendarOptions = options as CalendarRepositoryOptions;

        var users = await _graphClient.Users.GetAsync();


        var createdCalendar = await _graphClient.Users[calendarOptions.UserEmail].Calendars
            .PostAsync(calendar, cancellationToken: cancellationToken);

        return new CalendarRepository(_graphClient, calendarOptions, createdCalendar);
    }

    public async Task DeleteAsync(IRepositoryOptions<CalendarEvent> options, CancellationToken cancellationToken = default)
    {
        var calendarOptions = options as CalendarRepositoryOptions;

        var calendars = await _graphClient.Users[calendarOptions.UserEmail].Calendars
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = $"name eq '{calendarOptions.Name}'";
            }, cancellationToken);

        if (calendars.Value != null)
        {
            foreach (var calendar in calendars.Value)
            {
                await _graphClient.Me.Calendars[calendar.Id]
                    .DeleteAsync(cancellationToken: cancellationToken);
            }
        }
    }
}
