using Amazon.S3.Model;
using FluentStorage.Blobs;
using nc.Google;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace nc.Storage.Tests;

public class StorageServiceFacts
{
	private readonly StorageServiceOptions _storageServiceOptions;
	private readonly StorageService _storageService;

	public StorageServiceFacts()
	{
		_storageServiceOptions = new StorageServiceOptions()
		{
			AwsRegionDefault = "us-east-1",
		};
		_storageServiceOptions
			.CredentialCache
			.Add(new Uri("aws.s3://secure"), _storageServiceOptions.AuthenticationType, new NetworkCredential("awsUser", "awsPassword", "us-south-2"));

		_storageServiceOptions
			.GcpIdentities
			.Add("secure", new GoogleTenant() { Name = "secure", TenantId = "secure", ProjectId = "secure", PrivateKey = "Fake", ClientEmail = "me@gmail.com" });
		_storageService = new StorageService(_storageServiceOptions);
	}

	public class GetConnectionString : StorageServiceFacts
	{
		[Theory]
		[InlineData("aws.s3://some/bucket/file.txt", false)]
		[InlineData("aws.s3://secure/bucket/file.txt", true)]
		public void MatchesCredentials(string url, bool expectedCredentials)
		{
			var uri = new Uri(url);
			var credential = _storageServiceOptions.CredentialCache.GetCredential(uri, _storageServiceOptions.AuthenticationType);
			if (expectedCredentials)
				Assert.NotNull(credential);
			else
				Assert.Null(credential);
		}

		[Theory]
		[InlineData("aws://some/bucket/file.txt", "aws.s3://bucket=some;region=us-east-1")]
		[InlineData("s3://some/bucket/file.txt", "aws.s3://bucket=some;region=us-east-1")]
		[InlineData("aws.s3://some/bucket/file.txt", "aws.s3://bucket=some;region=us-east-1")]
		[InlineData("aws.s3://secure/bucket/file.txt", "aws.s3://key=awsUser;secret=awsPassword;region=us-south-2;bucket=secure")]
		[InlineData("gcp://some/bucket/file.txt", "google.storage://bucket=some")]
		[InlineData("google://some/bucket/file.txt", "google.storage://bucket=some")]
		[InlineData("google.gcp://some/bucket/file.txt", "google.storage://bucket=some")]
		public void NormalizesUri(string url, string expected)
		{
			var uri = new Uri(url);
			var connectionString = _storageService.GetConnectionString(uri);
			Assert.NotNull(connectionString);
			Assert.Equal(expected, connectionString);
		}

		[Theory]
		[InlineData("aws.s3://secure/bucket/file.txt", "aws.s3://key=awsUser;secret=awsPassword;region=us-south-2;bucket=secure")]
		[InlineData("google.gcp://secure/bucket/file.txt", "google.storage://project=secure;cred=eyJUZW5hbnRJZCI6InNlY3VyZSIsIk5hbWUiOiJzZWN1cmUiLCJ0eXBlIjoic2VydmljZV9hY2NvdW50IiwicHJvamVjdF9pZCI6InNlY3VyZSIsInByaXZhdGVfa2V5IjoiRmFrZSIsImNsaWVudF9lbWFpbCI6Im1lQGdtYWlsLmNvbSIsImF1dGhfdXJpIjoiaHR0cHM6Ly9hY2NvdW50cy5nb29nbGUuY29tL28vb2F1dGgyL2F1dGgiLCJ0b2tlbl91cmkiOiJodHRwczovL29hdXRoMi5nb29nbGVhcGlzLmNvbS90b2tlbiIsImF1dGhfcHJvdmlkZXJfeDUwOV9jZXJ0X3VybCI6Imh0dHBzOi8vd3d3Lmdvb2dsZWFwaXMuY29tL29hdXRoMi92MS9jZXJ0cyJ9")]
		public void InjectsCredential(string url, string expected)
		{
			var uri = new Uri(url);
			var connectionString = _storageService.GetConnectionString(uri);
			Assert.NotNull(connectionString);
			Assert.Equal(expected, connectionString);
		}

	}

	public class GetBlobStorage : StorageServiceFacts
	{
		[Theory]
		[InlineData("aws.s3://nascacht-io-tests/bookmark.pdf")]
		public async Task ReadsFiles(string url)
		{
			var uri = new Uri(url);
			using var blobStorage = _storageService.GetBlobStorage(uri);
			Assert.NotNull(blobStorage);
			using var stream = await blobStorage.OpenReadAsync(uri.PathAndQuery);
			Assert.NotNull(stream);

		}
	}

	public class OpenReadAsync : StorageServiceFacts
	{
		[Theory]
		[InlineData("aws.s3://nascacht-io-tests/bookmark.pdf")]
		[InlineData("google.storage://nascacht-io-tests/bookmark.pdf")]
		public async Task ReturnsStream(string url)
		{
			using var stream = await _storageService.OpenReadAsync(url);
			Assert.NotNull(stream);
		}

		[Theory]
		[InlineData("aws.s3://nascacht-io-invalid/bookmark.pdf")]
		[InlineData("aws.s3://nascacht-io-invalid/invalid.xxx")]
		public async Task HandlesInvalidUri(string url)
		{
			await Assert.ThrowsAnyAsync<Exception>(async () => await _storageService.OpenReadAsync(url));
		}
	}

	public class GetBlobsAsync : StorageServiceFacts
	{
		[Theory]
		[InlineData("aws.s3://nascacht-io-tests/bookmark.pdf")]
		[InlineData("aws.s3://nascacht-io-tests/sample.477x640.jpg")]
		[InlineData("aws.s3://nascacht-io-tests/sample.flac")]
		[InlineData("aws.s3://nascacht-io-tests/sample.jpeg")]
		[InlineData("aws.s3://nascacht-io-tests/sample.moving.gif")]
		[InlineData("aws.s3://nascacht-io-tests/speech.mp3")]
		[InlineData("aws.s3://nascacht-io-tests/speech.wav")]
		[InlineData("google.gcp://nascacht-io-tests/bookmark.pdf")]
		[InlineData("disk://./nc-storage-tests.deps.json")]

		public async Task FindsFile(string url)
		{
			var blob = await _storageService.GetBlobsAsync([url]).FirstOrDefaultAsync();
			Assert.NotNull(blob);
			Assert.NotEqual(0, blob.Size);
			Assert.Contains(BlobExtensions.Uri, blob.Metadata);
		}


	}

	public class ListAsync : StorageServiceFacts
	{
		[Theory]
		[InlineData("google.gcp://nascacht-io-tests/", 8)]
		[InlineData("google.gcp://nascacht-io-tests/*", 8)]
		[InlineData("google.gcp://nascacht-io-tests/**", 8)]
		[InlineData("google.gcp://nascacht-io-tests/sample*", 4)]
		[InlineData("google.gcp://nascacht-io-tests/*.wav", 1)]
		public async Task ReturnsFiles(string prefix, int expectedFiles)
		{
			var blobs = await _storageService.ListAsync(prefix);
			Assert.NotNull(blobs);
			Assert.NotEmpty(blobs);
			Assert.Equal(expectedFiles, blobs.Count());
			foreach (var blob in blobs)
				Assert.Contains(BlobExtensions.Uri, blob.Metadata);
		}
	}

	public class WriteAsync : StorageServiceFacts
	{
		[Fact]
		public async Task WritesFiles()
		{
			using var stream = new MemoryStream();
			stream.Write(Encoding.UTF8.GetBytes("Hello, World!"));
			stream.Position = 0;
			var guid = Guid.NewGuid();
			await _storageService.WriteAsync($"disk://./{guid}", stream);
			Assert.True(File.Exists($"./{guid}"));
			var info = new FileInfo($"./{guid}");
			Assert.NotNull(info);
			Assert.Equal(13, info.Length);
		}
	}
}
