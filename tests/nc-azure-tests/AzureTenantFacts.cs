using Azure.Core;
using Azure.Identity;

namespace nc.Azure.Tests;

public class AzureTenantFacts
{
	public class Constructor : AzureTenantFacts
	{
		[Fact]
		public void SetsDefaultName()
		{
			var tenant = new AzureTenant();
			Assert.Equal("Default", tenant.Name);
		}

		[Fact]
		public void SetsDefaultTenantId()
		{
			var tenant = new AzureTenant();
			Assert.False(string.IsNullOrEmpty(tenant.TenantId));
			Assert.True(Guid.TryParse(tenant.TenantId, out _));
		}

		[Fact]
		public void CredentialsAreNullByDefault()
		{
			var tenant = new AzureTenant();
			Assert.Null(tenant.ClientId);
			Assert.Null(tenant.ClientSecret);
			Assert.Null(tenant.TenantDomain);
		}

		[Fact]
		public void CertificatePropertiesAreNullByDefault()
		{
			var tenant = new AzureTenant();
			Assert.Null(tenant.CertificatePath);
			Assert.Null(tenant.CertificateThumbprint);
			Assert.Null(tenant.CertificatePassword);
		}

		[Fact]
		public void WorkloadIdentityPropertiesAreNullByDefault()
		{
			var tenant = new AzureTenant();
			Assert.Null(tenant.FederatedTokenFile);
			Assert.Null(tenant.FederatedToken);
			Assert.Null(tenant.ClientAssertion);
		}

		[Fact]
		public void ManagedIdentityIsDisabledByDefault()
		{
			var tenant = new AzureTenant();
			Assert.False(tenant.UseManagedIdentity);
			Assert.Null(tenant.ManagedIdentityResourceId);
		}
	}

	public class ImplicitConversionToTokenCredential : AzureTenantFacts
	{
		[Fact]
		public void CreatesClientSecretCredential()
		{
			var tenant = new AzureTenant
			{
				ClientId = "00000000-0000-0000-0000-000000000001",
				ClientSecret = "super-secret-value",
				TenantDomain = "contoso.onmicrosoft.com"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<ClientSecretCredential>(credential);
		}

		[Fact]
		public void CreatesWorkloadIdentityCredentialFromFile()
		{
			// Create a temporary token file
			var tokenFile = Path.GetTempFileName();
			File.WriteAllText(tokenFile, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...");

			try
			{
				var tenant = new AzureTenant
				{
					ClientId = "00000000-0000-0000-0000-000000000001",
					TenantDomain = "contoso.onmicrosoft.com",
					FederatedTokenFile = tokenFile
				};

				TokenCredential credential = tenant;

				Assert.NotNull(credential);
				Assert.IsType<WorkloadIdentityCredential>(credential);
			}
			finally
			{
				File.Delete(tokenFile);
			}
		}

		[Fact]
		public void CreatesWorkloadIdentityCredentialFromToken()
		{
			var tenant = new AzureTenant
			{
				ClientId = "00000000-0000-0000-0000-000000000001",
				TenantDomain = "contoso.onmicrosoft.com",
				FederatedToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<WorkloadIdentityCredential>(credential);
		}

		[Fact]
		public void CreatesClientAssertionCredential()
		{
			var tenant = new AzureTenant
			{
				ClientId = "00000000-0000-0000-0000-000000000001",
				TenantDomain = "contoso.onmicrosoft.com",
				ClientAssertion = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<ClientAssertionCredential>(credential);
		}

		[Fact]
		public void CreatesManagedIdentityCredential_SystemAssigned()
		{
			var tenant = new AzureTenant
			{
				UseManagedIdentity = true
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<ManagedIdentityCredential>(credential);
		}

		[Fact]
		public void CreatesManagedIdentityCredential_UserAssignedByClientId()
		{
			var tenant = new AzureTenant
			{
				UseManagedIdentity = true,
				ClientId = "00000000-0000-0000-0000-000000000001"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<ManagedIdentityCredential>(credential);
		}

		[Fact]
		public void CreatesManagedIdentityCredential_UserAssignedByResourceId()
		{
			var tenant = new AzureTenant
			{
				UseManagedIdentity = true,
				ManagedIdentityResourceId = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/myRg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/myIdentity"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<ManagedIdentityCredential>(credential);
		}

		[Fact]
		public void CreatesDefaultAzureCredentialWhenNoCredentialsSet()
		{
			var tenant = new AzureTenant();

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<DefaultAzureCredential>(credential);
		}

		[Fact]
		public void WorkloadIdentityTakesPriorityOverClientSecret()
		{
			var tenant = new AzureTenant
			{
				// Both workload identity and client secret set - workload identity should win
				ClientId = "00000000-0000-0000-0000-000000000001",
				TenantDomain = "contoso.onmicrosoft.com",
				FederatedToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
				ClientSecret = "super-secret-value"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<WorkloadIdentityCredential>(credential);
		}

		[Fact]
		public void ClientAssertionTakesPriorityOverCertificate()
		{
			// Create a dummy certificate file path (doesn't need to exist for this test)
			var tenant = new AzureTenant
			{
				ClientId = "00000000-0000-0000-0000-000000000001",
				TenantDomain = "contoso.onmicrosoft.com",
				ClientAssertion = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
				CertificatePath = "/path/to/cert.pfx"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<ClientAssertionCredential>(credential);
		}

		[Fact]
		public void ManagedIdentityTakesPriorityOverClientSecret()
		{
			var tenant = new AzureTenant
			{
				UseManagedIdentity = true,
				ClientId = "00000000-0000-0000-0000-000000000001",
				ClientSecret = "super-secret-value",
				TenantDomain = "contoso.onmicrosoft.com"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			// Managed identity by client ID takes priority
			Assert.IsType<ManagedIdentityCredential>(credential);
		}

		[Fact]
		public void ClientIdWithoutTenantDomain_FallsBackToDefaultCredential()
		{
			var tenant = new AzureTenant
			{
				// ClientId set but no TenantDomain - should fall back to default
				ClientId = "00000000-0000-0000-0000-000000000001",
				ClientSecret = "super-secret-value"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<DefaultAzureCredential>(credential);
		}

		[Fact]
		public void ClientSecretWithoutClientId_FallsBackToDefaultCredential()
		{
			var tenant = new AzureTenant
			{
				// ClientSecret set but no ClientId - should fall back to default
				ClientSecret = "super-secret-value",
				TenantDomain = "contoso.onmicrosoft.com"
			};

			TokenCredential credential = tenant;

			Assert.NotNull(credential);
			Assert.IsType<DefaultAzureCredential>(credential);
		}
	}

	public class GetService : AzureTenantFacts
	{
		[Fact]
		public void ThrowsForUnsupportedServiceType()
		{
			var tenant = new AzureTenant
			{
				ServiceUrl = "https://example.blob.core.windows.net",
				ClientId = "test",
				ClientSecret = "test",
				TenantDomain = "test"
			};

			Assert.Throws<NotSupportedException>(() => tenant.GetService<string>());
		}

		[Fact]
		public void ThrowsWithHelpfulMessage()
		{
			var tenant = new AzureTenant
			{
				ServiceUrl = "https://example.blob.core.windows.net"
			};

			var ex = Assert.Throws<NotSupportedException>(() => tenant.GetService<string>());
			Assert.Contains("String", ex.Message);
			Assert.Contains("BlobServiceClient", ex.Message);
			Assert.Contains("SecretClient", ex.Message);
			Assert.Contains("ArmClient", ex.Message);
		}
	}
}
