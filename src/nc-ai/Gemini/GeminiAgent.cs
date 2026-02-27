using Google.Apis.Auth.OAuth2;
using Google.GenAI;
using Google.GenAI.Types;
using nc.Ai.Interfaces;

namespace nc.Ai.Gemini;

/// <summary>
/// Configuration record for a Google Gemini agent.
/// Supports both API-key authentication and Vertex AI with ADC or explicit credentials.
/// </summary>
public record GeminiAgent: IAgent
{
	/// <summary>Gets or sets the Gemini model name (e.g. <c>gemini-2.0-flash</c>).</summary>
	public string Model { get; set; } = string.Empty;

	/// <summary>Gets or sets the TTL for server-side cached instructions. Defaults to 1 hour.</summary>
	public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);

	/// <summary>Gets or sets the HTTP request timeout. When set, overrides <see cref="HttpOptions"/>.</summary>
	public TimeSpan? Timeout { get; set; }

	/// <summary>Gets or sets the number of retry attempts on transient failure. Defaults to 0 (no retry).</summary>
	public int RetryCount { get; set; } = 0;

	/// <summary>Gets or sets whether to use Vertex AI instead of the Google AI Studio endpoint. <c>null</c> uses the SDK default.</summary>
	public bool? VertexAI { get; set; }

	/// <summary>Gets or sets the Google AI Studio API key. Not used when <see cref="VertexAI"/> is <c>true</c>.</summary>
	public string? ApiKey { get; set; }

	/// <summary>Gets or sets an explicit Google credential for Vertex AI. Falls back to ADC when <c>null</c>.</summary>
	public ICredential? Credential { get; set; }

	/// <summary>Gets or sets the Google Cloud project ID for Vertex AI.</summary>
	public string? Project { get; set; }

	/// <summary>Gets or sets the Vertex AI region (e.g. <c>us-central1</c>).</summary>
	public string? Location { get; set; }

	/// <summary>Gets or sets advanced HTTP options passed to the underlying Google GenAI client.</summary>
	public HttpOptions? HttpOptions { get; set; }
}
