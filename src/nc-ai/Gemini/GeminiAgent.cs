using Google.Apis.Auth.OAuth2;
using Google.GenAI;
using Google.GenAI.Types;
using nc.Ai.Interfaces;

namespace nc.Ai.Gemini;

public record GeminiAgent: IAgent
{
	public string Model { get; set; } = string.Empty;
	public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);
	public bool? VertexAI { get; set; }
	public string? ApiKey { get; set; }
	public ICredential? Credential { get; set; }
	public string? Project { get; set; }
	public string? Location { get; set; }
	public HttpOptions? HttpOptions { get; set; }
}
