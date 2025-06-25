using Microsoft.Kiota.Abstractions;
using static Microsoft.Graph.Groups.Item.Events.EventsRequestBuilder;

public class CalendarQuery : IQuery<CalendarEvent>
{
    public string? Subject { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string[]? OrderBy { get; set; }
    public int? Top { get; set; }

    /// <summary>
    /// Generates the OData filter string based on the query properties.
    /// </summary>
    /// <returns>OData filter string.</returns>
    public string ToFilter()
    {
        var filters = new List<string>();

        if (!string.IsNullOrEmpty(Subject))
        {
            filters.Add($"contains(subject, '{Subject}')");
        }

        if (StartDate.HasValue)
        {
            filters.Add($"start/dateTime ge '{StartDate.Value.UtcDateTime:o}'");
        }

        if (EndDate.HasValue)
        {
            filters.Add($"end/dateTime le '{EndDate.Value.UtcDateTime:o}'");
        }

        return string.Join(" and ", filters);
    }

    /// <summary>
    /// Populates the query parameters for the Microsoft Graph API request.
    /// </summary>
    /// <param name="requestConfiguration">Request configuration for the Graph API.</param>
    public void ApplyQueryParameters(RequestConfiguration<EventsRequestBuilderGetQueryParameters> requestConfiguration)
    {
        var filter = ToFilter();
        if (!string.IsNullOrEmpty(filter))
        {
            requestConfiguration.QueryParameters.Filter = filter;
        }

        if (OrderBy != null)
        {
            requestConfiguration.QueryParameters.Orderby = OrderBy;
        }

        if (Top.HasValue)
        {
            requestConfiguration.QueryParameters.Top = Top;
        }
    }

    /// <summary>
    /// Provides a client-side predicate for filtering results after they are retrieved.
    /// </summary>
    public Func<CalendarEvent, bool> Criteria =>
        instance =>
        {
            if (!string.IsNullOrEmpty(Subject) && !instance.Subject.Contains(Subject, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (StartDate.HasValue && instance.Start < StartDate)
            {
                return false;
            }

            if (EndDate.HasValue && instance.End > EndDate)
            {
                return false;
            }

            return true;
        };
}
