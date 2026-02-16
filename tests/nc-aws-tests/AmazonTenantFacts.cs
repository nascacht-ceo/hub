using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;

namespace nc.Aws.Tests;

public class AmazonTenantFacts
{
	public class Constructor : AmazonTenantFacts
	{
		[Fact]
		public void SetsDefaultName()
		{
			var tenant = new AmazonTenant();
			Assert.Equal("Default", tenant.Name);
		}

		[Fact]
		public void SetsDefaultTenantId()
		{
			var tenant = new AmazonTenant();
			Assert.False(string.IsNullOrEmpty(tenant.TenantId));
			Assert.True(Guid.TryParse(tenant.TenantId, out _));
		}

		[Fact]
		public void CredentialsAreNullByDefault()
		{
			var tenant = new AmazonTenant();
			Assert.Null(tenant.AccessKey);
			Assert.Null(tenant.SecretKey);
			Assert.Null(tenant.SessionToken);
		}
	}

	public class ImplicitConversionToAWSOptions : AmazonTenantFacts
	{
		[Fact]
		public void CreatesBasicAWSCredentials()
		{
			var tenant = new AmazonTenant
			{
				AccessKey = "AKIAIOSFODNN7EXAMPLE",
				SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
			};

			AWSOptions options = tenant;

			Assert.NotNull(options.Credentials);
			Assert.IsType<BasicAWSCredentials>(options.Credentials);
			var creds = options.Credentials.GetCredentials();
			Assert.Equal("AKIAIOSFODNN7EXAMPLE", creds.AccessKey);
			Assert.Equal("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", creds.SecretKey);
			Assert.True(string.IsNullOrEmpty(creds.Token));
		}

		[Fact]
		public void CreatesSessionAWSCredentials()
		{
			var tenant = new AmazonTenant
			{
				AccessKey = "ASIAIOSFODNN7EXAMPLE",
				SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
				SessionToken = "FwoGZXIvYXdzEBYaD..."
			};

			AWSOptions options = tenant;

			Assert.NotNull(options.Credentials);
			Assert.IsType<SessionAWSCredentials>(options.Credentials);
			var creds = options.Credentials.GetCredentials();
			Assert.Equal("ASIAIOSFODNN7EXAMPLE", creds.AccessKey);
			Assert.Equal("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", creds.SecretKey);
			Assert.Equal("FwoGZXIvYXdzEBYaD...", creds.Token);
		}

		[Fact]
		public void CreatesAssumeRoleWithWebIdentityCredentialsFromFile()
		{
			// Create a temporary token file
			var tokenFile = Path.GetTempFileName();
			File.WriteAllText(tokenFile, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...");

			try
			{
				var tenant = new AmazonTenant
				{
					RoleArn = "arn:aws:iam::123456789012:role/github-actions-role",
					WebIdentityTokenFile = tokenFile,
					RoleSessionName = "test-session"
				};

				AWSOptions options = tenant;

				Assert.NotNull(options.Credentials);
				Assert.IsType<AssumeRoleWithWebIdentityCredentials>(options.Credentials);
			}
			finally
			{
				File.Delete(tokenFile);
			}
		}

		[Fact]
		public void CreatesAssumeRoleWithWebIdentityCredentialsFromToken()
		{
			var tenant = new AmazonTenant
			{
				RoleArn = "arn:aws:iam::123456789012:role/github-actions-role",
				WebIdentityToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
			};

			AWSOptions options = tenant;

			Assert.NotNull(options.Credentials);
			Assert.IsType<AssumeRoleWithWebIdentityCredentials>(options.Credentials);
		}

		[Fact]
		public void UsesDefaultRoleSessionName()
		{
			var tenant = new AmazonTenant
			{
				RoleArn = "arn:aws:iam::123456789012:role/github-actions-role",
				WebIdentityToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
				// RoleSessionName not set - should default to "nc-session"
			};

			AWSOptions options = tenant;

			Assert.NotNull(options.Credentials);
			Assert.IsType<AssumeRoleWithWebIdentityCredentials>(options.Credentials);
		}

		[Fact]
		public void CredentialsAreNullByDefault()
		{
			var tenant = new AmazonTenant();

			AWSOptions options = tenant;

			Assert.Null(options.Credentials);
		}

		[Fact]
		public void SetsServiceUrl()
		{
			var tenant = new AmazonTenant
			{
				ServiceUrl = "http://localhost:4566/"
			};

			AWSOptions options = tenant;

			Assert.Equal("http://localhost:4566/", options.DefaultClientConfig.ServiceURL);
		}

		[Fact]
		public void SetsProfile()
		{
			var tenant = new AmazonTenant
			{
				Profile = "my-profile"
			};

			AWSOptions options = tenant;

			Assert.Equal("my-profile", options.Profile);
		}

		[Fact]
		public void WebIdentityTakesPriorityOverBasicCredentials()
		{
			var tenant = new AmazonTenant
			{
				// Both OIDC and basic credentials set - OIDC should win
				RoleArn = "arn:aws:iam::123456789012:role/github-actions-role",
				WebIdentityToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
				AccessKey = "AKIAIOSFODNN7EXAMPLE",
				SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
			};

			AWSOptions options = tenant;

			Assert.NotNull(options.Credentials);
			Assert.IsType<AssumeRoleWithWebIdentityCredentials>(options.Credentials);
		}

		[Fact]
		public void RoleArnWithoutToken_FallsBackToBasicCredentials()
		{
			var tenant = new AmazonTenant
			{
				// RoleArn set but no token - should fall back to basic credentials
				RoleArn = "arn:aws:iam::123456789012:role/github-actions-role",
				AccessKey = "AKIAIOSFODNN7EXAMPLE",
				SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
			};

			AWSOptions options = tenant;

			Assert.NotNull(options.Credentials);
			Assert.IsType<BasicAWSCredentials>(options.Credentials);
		}
	}

	public class GetService : AmazonTenantFacts
	{
		[Fact]
		public void ThrowsForNonAmazonServiceType()
		{
			var tenant = new AmazonTenant
			{
				AccessKey = "test",
				SecretKey = "test"
			};

			Assert.Throws<ArgumentException>(() => tenant.GetService<string>());
		}
	}
}
