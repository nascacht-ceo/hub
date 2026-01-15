using System.Text.Json.Serialization;

namespace nc.License;

/// <summary>
/// Represents a software license, including subject, organization type, and expiration date information.
/// </summary>
/// <param name="Subject">The subject associated with the license. Typically identifies the licensed user or entity.</param>
/// <param name="OrganizationType">The type of organization to which the license applies, such as 'Commercial' or 'Nonprofit'.</param>
/// <param name="ExpirationDate">The expiration date of the license, or a value indicating that the license does not expire.</param>
public record License(
	[property: JsonPropertyName("sub")] string Subject, 
	[property: JsonPropertyName("org")] string OrganizationType,
	[property: JsonPropertyName("exp")] DateOnly? ExpirationDate
)
{
	/// <summary>
	/// Converts an <see cref="ApiKeyDetails"/> instance to a <see cref="License"/> object using standard mapping rules.
	/// </summary>
	/// <remarks>The conversion sets the subject to "API Customer", the organization type to "Commercial", and the
	/// expiration date to the value of <paramref name="apiEntry"/>.ExpiresAt, or "Never" if it is null.</remarks>
	/// <param name="apiEntry">The API key details to convert. Cannot be null.</param>
	public static implicit operator License?(ApiKeyDetails apiEntry)
	{
		if (!DateOnly.TryParse(apiEntry.ExpiresAt, out var expirationDate))
			return null;
		return new License(
			Subject: apiEntry.Key,
			OrganizationType: apiEntry.Status, // Online purchases are standard commercial
			ExpirationDate: expirationDate
		);
	}
}

/// <summary>
/// Internal record representing the root response from Lemon Squeezy API.
/// </summary>
public record ApiValidationResponse(
	[property: JsonPropertyName("valid")] bool Valid,
	[property: JsonPropertyName("error")] string? Error,
	[property: JsonPropertyName("license_key")] ApiKeyDetails? LicenseKey
);

/// <summary>
/// Internal record representing the specific license details from the API.
/// </summary>
public record ApiKeyDetails(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("status")] string Status,
	[property: JsonPropertyName("key")] string Key,
	[property: JsonPropertyName("expires_at")] string? ExpiresAt,
	[property: JsonPropertyName("test_mode")] bool TestMode
);