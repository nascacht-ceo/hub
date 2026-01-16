using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nc.Ai.Gemini;
using System;
using System.Threading;

namespace nc.Ai.Tests;

public class GeminiFileServiceTests
{
	[Fact(Skip = "work in progress")]
	public async Task UploadsFiles()
	{
		var config = new ConfigurationBuilder().AddUserSecrets("nc-hub").Build().GetSection("tests:nc-ai-tests:gemini");
		// Arrange: configure your real GeminiFileServiceOptions here
		var options = Options.Create(new GeminiFileServiceOptions
		{
			ApiKey = config["apikey"]
		});

		// Use the default HttpClientFactory for integration
		var httpClientFactory = new DefaultHttpClientFactory();

		var service = new GeminiFileService(httpClientFactory, options);

		// Use a real, publicly accessible file URI for testing
		var testFileUri = new Uri("https://nascacht-io-sample.s3.us-east-1.amazonaws.com/financial/w2.pdf");
		var mediaType = "application/pdf";


		using var httpClient = new HttpClient();
		using var response = await httpClient.GetAsync(testFileUri, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();
		using var stream = await response.Content.ReadAsStreamAsync();


		// Act
		var fileId = await service.UploadAsync(stream, mediaType, default);

		// Assert
		Assert.False(string.IsNullOrWhiteSpace(fileId));
		// Optionally, print the fileId for manual verification
		Console.WriteLine($"Uploaded file ID: {fileId}");
	}

	// Minimal HttpClientFactory for integration testing
	private class DefaultHttpClientFactory : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => new();
	}
}
