namespace nc.Models;

/// <summary>
/// Represents a vCard or JSContact entry for a person or organization.
/// <see href="https://datatracker.ietf.org/doc/html/rfc6350">RFC 6350 – vCard 4.0</see>
/// <see href="https://datatracker.ietf.org/doc/html/rfc9553">RFC 9553 – JSContact</see>
/// </summary>
public class Contact
{
	public string? FullName { get; set; }
	public string? GivenName { get; set; }
	public string? FamilyName { get; set; }
	public IEnumerable<Email>? Emails { get; set; }
	public IEnumerable<Telephone>? Telephones { get; set; }
	public IEnumerable<Address>? Addresses { get; set; }
}

/// <summary>
/// Postal or delivery address for a Contact.
/// <see href="https://datatracker.ietf.org/doc/html/rfc6350#section-6.3.1">vCard ADR property</see>
/// </summary>
public class Address
{
	public string? Street { get; set; }
	public string? City { get; set; }
	public string? Region { get; set; }
	public string? PostalCode { get; set; }
	public string? Country { get; set; }
}

/// <summary>
/// Email address element.
/// <see href="https://datatracker.ietf.org/doc/html/rfc6350#section-6.4.2">vCard EMAIL property</see>
/// <see href="https://datatracker.ietf.org/doc/html/rfc6068">RFC 6068 – mailto URI scheme</see>
/// </summary>
public class Email
{
	public string? Address { get; set; }
	public string? Type { get; set; }  // home, work, etc.
}

/// <summary>
/// Telephone number element.
/// <see href="https://datatracker.ietf.org/doc/html/rfc6350#section-6.4.1">vCard TEL property</see>
/// <see href="https://datatracker.ietf.org/doc/html/rfc3966">RFC 3966 – tel URI scheme</see>
/// </summary>
public class Telephone
{
	public string? Number { get; set; }
	public string? Type { get; set; }
}

/// <summary>
/// Calendar event.
/// <see href="https://datatracker.ietf.org/doc/html/rfc5545">RFC 5545 – iCalendar</see>
/// <see href="https://datatracker.ietf.org/doc/html/rfc8984">RFC 8984 – JSCalendar</see>
/// </summary>
public class Event
{
	public string? Uid { get; set; }
	public string? Summary { get; set; }
	public string? Description { get; set; }
	public DateTimeOffset? Start { get; set; }
	public DateTimeOffset? End { get; set; }
	public RecurrenceRule? Recurrence { get; set; }
	public IEnumerable<Alarm>? Alarms { get; set; }
	public Location? Location { get; set; }
}

/// <summary>
/// Recurrence rule defining repeating events.
/// <see href="https://datatracker.ietf.org/doc/html/rfc5545#section-3.3.10">iCalendar RRULE</see>
/// </summary>
public class RecurrenceRule
{
	public string? Frequency { get; set; } // DAILY, WEEKLY, etc.
	public int? Interval { get; set; }
	public DateTimeOffset? Until { get; set; }
	public IEnumerable<DayOfWeek>? ByDay { get; set; }
}

/// <summary>
/// Alarm/notification associated with a calendar component.
/// <see href="https://datatracker.ietf.org/doc/html/rfc5545#section-3.6.6">iCalendar VALARM</see>
/// </summary>
public class Alarm
{
	public string? Action { get; set; }    // DISPLAY, EMAIL, etc.
	public string? Description { get; set; }
	public TimeSpan? Trigger { get; set; } // relative or absolute
}

/// <summary>
/// Physical or virtual location reference.
/// <see href="https://datatracker.ietf.org/doc/html/rfc5545#section-3.8.1.7">iCalendar LOCATION property</see>
/// <see href="https://datatracker.ietf.org/doc/html/rfc5870">RFC 5870 – geo URI scheme</see>
/// <see href="https://datatracker.ietf.org/doc/html/rfc7946">RFC 7946 – GeoJSON</see>
/// </summary>
public class Location
{
	public string? Name { get; set; }
	public Address? Address { get; set; }
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
}