using Google.Apis.Auth.OAuth2;
using Google.GenAI;
using Google.GenAI.Types;

namespace nc.Ai.Gemini;

public record GeminiChatClientOptions
{
	public required string Model { get; init; }
	public TimeSpan CacheTtl { get; init; } = TimeSpan.FromHours(1);
	public bool? VertexAI { get; init; }
	public string? ApiKey { get; init; }
	public ICredential? Credential { get; init; }
	public string? Project { get; init; }
	public string? Location { get; init; }
	public HttpOptions? HttpOptions { get; init; }
}
