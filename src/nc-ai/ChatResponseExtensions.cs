using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Xml.Linq;

namespace nc.Ai;

public static class ChatResponseExtensions
{
	/// <summary>
	/// Returns the response text with any surrounding markdown code fence stripped.
	/// If <paramref name="language"/> is provided, only strips fences whose language
	/// tag matches (e.g. "json", "xml"); otherwise strips any fence.
	/// Returns the original text unchanged if no fence is detected.
	/// </summary>
	public static string? ExtractCode(this ChatResponse response, string? language = null)
	{
		var text = response.Text?.Trim();
		if (string.IsNullOrEmpty(text))
			return null;
		if (!text.StartsWith("```"))
			return text;

		var eol = text.IndexOf('\n');
		if (eol < 0)
			return text;

		var tag = text[3..eol].Trim(); // e.g. "json", "xml", or ""
		if (language is not null && !tag.Equals(language, StringComparison.OrdinalIgnoreCase))
			return text;

		var closing = text.LastIndexOf("\n```");
		if (closing <= eol)
			return text;

		return text[(eol + 1)..closing].Trim();
	}

	/// <summary>
	/// Strips any markdown code fence and deserializes the response text as JSON.
	/// Tries <c>```json</c> first, then any unlabelled fence, then the raw text.
	/// </summary>
	public static T? Deserialize<T>(this ChatResponse response, JsonSerializerOptions? options = null)
	{
		var json = response.ExtractCode("json")
			?? response.ExtractCode()
			?? response.Text;

		return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, options);
	}

	/// <summary>
	/// Strips any markdown code fence and parses the response text as XML.
	/// </summary>
	public static XDocument ToXDocument(this ChatResponse response)
	{
		var xml = response.ExtractCode("xml")
			?? response.ExtractCode()
			?? response.Text
			?? string.Empty;

		return XDocument.Parse(xml);
	}
}
