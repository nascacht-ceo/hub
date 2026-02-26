using Microsoft.Extensions.AI;
using nc.Ai;
using System.Text.Json;
using System.Xml;

namespace nc.Ai.Tests;

public class ChatResponseExtensionsFacts
{
	private static ChatResponse Response(string? text) =>
		text is null
			? new ChatResponse([])
			: new ChatResponse(new ChatMessage(ChatRole.Assistant, text));

	public class ExtractCodeFacts : ChatResponseExtensionsFacts
	{
		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void NullOrEmptyText_ReturnsNull(string? text)
		{
			Assert.Null(Response(text).ExtractCode());
		}

		[Fact]
		public void NoFence_ReturnsTextUnchanged()
		{
			Assert.Equal("hello", Response("hello").ExtractCode());
		}

		[Fact]
		public void FenceWithoutNewline_ReturnsTextUnchanged()
		{
			// ``` with no \n — can't identify where the content starts
			Assert.Equal("```json", Response("```json").ExtractCode());
		}

		[Fact]
		public void LanguageMismatch_ReturnsOriginal()
		{
			var raw = "```xml\n<root/>\n```";
			Assert.Equal(raw, Response(raw).ExtractCode("json"));
		}

		[Fact]
		public void NoClosingFence_ReturnsOriginal()
		{
			var raw = "```json\n{\"a\":1}";
			Assert.Equal(raw, Response(raw).ExtractCode("json"));
		}

		[Fact]
		public void ClosingFenceAtEol_ReturnsOriginal()
		{
			// closing == eol means the \n``` is the opening newline itself — no content
			var raw = "```\n```";
			Assert.Equal(raw, Response(raw).ExtractCode());
		}

		[Fact]
		public void UnlabelledFence_NullLanguage_StripsContent()
		{
			Assert.Equal("{}", Response("```\n{}\n```").ExtractCode());
		}

		[Fact]
		public void LabelledFence_MatchingLanguage_StripsContent()
		{
			Assert.Equal("{}", Response("```json\n{}\n```").ExtractCode("json"));
		}

		[Fact]
		public void LanguageMatch_IsCaseInsensitive()
		{
			Assert.Equal("{}", Response("```JSON\n{}\n```").ExtractCode("json"));
		}

		[Fact]
		public void LabelledFence_NullLanguage_StripsAnyFence()
		{
			Assert.Equal("{}", Response("```json\n{}\n```").ExtractCode(null));
		}

		[Fact]
		public void MultilineContent_PreservedInsideFence()
		{
			var content = "line one\nline two";
			Assert.Equal(content, Response($"```\n{content}\n```").ExtractCode());
		}
	}

	public class DeserializeFacts : ChatResponseExtensionsFacts
	{
		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void NullOrEmptyText_ReturnsDefault(string? text)
		{
			Assert.Null(Response(text).Deserialize<int[]>());
		}

		[Fact]
		public void JsonFence_Deserializes()
		{
			var result = Response("```json\n[1,2,3]\n```").Deserialize<int[]>();
			Assert.NotNull(result);
			Assert.Equal(new[] { 1, 2, 3 }, result);
		}

		[Fact]
		public void PlainJson_Deserializes()
		{
			var result = Response("[1,2,3]").Deserialize<int[]>();
			Assert.NotNull(result);
			Assert.Equal(new[] { 1, 2, 3 }, result);
		}

		[Fact]
		public void CustomOptions_Applied()
		{
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			// "value" (lowercase) should bind to Value property only with case-insensitive options
			var result = Response("""{"value":42}""").Deserialize<Wrapper>(options);
			Assert.Equal(42, result?.Value);
		}

		private record Wrapper(int Value);
	}

	public class ToXDocumentFacts : ChatResponseExtensionsFacts
	{
		[Fact]
		public void XmlFence_ParsesDocument()
		{
			var doc = Response("```xml\n<root><child/></root>\n```").ToXDocument();
			Assert.Equal("root", doc.Root?.Name.LocalName);
			Assert.Single(doc.Root!.Elements());
		}

		[Fact]
		public void PlainXml_ParsesDocument()
		{
			var doc = Response("<root/>").ToXDocument();
			Assert.Equal("root", doc.Root?.Name.LocalName);
		}

		[Fact]
		public void NullText_ThrowsXmlException()
		{
			Assert.Throws<XmlException>(() => Response(null).ToXDocument());
		}
	}
}
