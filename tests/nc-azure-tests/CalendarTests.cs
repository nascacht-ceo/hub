
using Microsoft.Extensions.Configuration;

public class CalendarTests
{
    public class RepositoryTests: CalendarTests
    {
        [Fact]
        public async Task Walkthrough()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("artifacts/azure.json")
                .Build();
            var options = new AzureServiceOptions();
            config.GetSection("Azure").Bind(options);
            Assert.NotNull(options.CalendarSources);
            Assert.NotEmpty(options.CalendarSources);
            Assert.Contains("default", options.CalendarSources.Keys);
            // Connect to a "computer": an Azure Calendar account.
            var calendarSource = new CalendarSource(options.CalendarSources["default"]);

            var calendars = calendarSource.SearchAsync(new CalendarRepositoryOptions() { Name = "*" });

            // Create a "drive": a calendar in the Calendar account.
            var name = $"{Guid.NewGuid()}";
            var repository = await calendarSource.CreateAsync(new CalendarRepositoryOptions() { Name = name, UserEmail = "epatrick@quandis.com" });
            Assert.NotNull(repository);
            // Create an "event" in the "drive".
            var results = await repository.SaveAsync(new CalendarEvent()
            {
                Subject = "Meeting",
                Start = DateTimeOffset.Now,
                End = DateTimeOffset.Now.AddHours(1),
                TimeZone = "Eastern Standard Time",
                Attendees = new List<string> { "epatrick@quandis.com" }
            });
        }
    }
}

