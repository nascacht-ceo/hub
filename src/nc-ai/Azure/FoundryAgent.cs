using Azure.AI.Inference;
using Azure.Core;
using Azure.Identity;
using nc.Ai.Interfaces;

namespace nc.Ai.Azure;

/// <summary>
/// Configuration record for an Azure AI Foundry agent.
/// Supports API-key auth, workload identity federation, client assertion, certificate,
/// managed identity, client secret, and <c>DefaultAzureCredential</c> as a fallback.
/// </summary>
public record FoundryAgent : IAgent
{
	/// <summary>Gets or sets the Azure AI Foundry deployment name.</summary>
	public string Model { get; set; } = string.Empty;

	/// <summary>Gets or sets the Azure AI Foundry endpoint URI.</summary>
	public string? Endpoint { get; set; }

	/// <summary>Gets or sets an API key for key-based authentication. When set, takes priority over token credentials.</summary>
	public string? ApiKey { get; set; }

	/// <summary>Gets or sets the Azure AI Inference API version override.</summary>
	public string? ApiVersion { get; set; }

	/// <summary>Gets or sets the Azure AD tenant ID for token-based authentication.</summary>
	public string? TenantId { get; set; }

	/// <summary>Gets or sets the Azure AD application client ID.</summary>
	public string? ClientId { get; set; }

	/// <summary>Gets or sets the client secret for client-secret credential auth.</summary>
	public string? ClientSecret { get; set; }

	#region Certificate Authentication
	/// <summary>Gets or sets the path to a PFX/PEM certificate file for certificate-based auth.</summary>
	public string? CertificatePath { get; set; }

	/// <summary>Gets or sets the certificate thumbprint (reserved for future use).</summary>
	public string? CertificateThumbprint { get; set; }

	/// <summary>Gets or sets the certificate password for encrypted PFX files.</summary>
	public string? CertificatePassword { get; set; }
	#endregion

	#region Workload Identity Federation
	/// <summary>Gets or sets the path to a federated token file (e.g. the Kubernetes projected service-account token).</summary>
	public string? FederatedTokenFile { get; set; }

	/// <summary>Gets or sets the raw federated token string, written to a temp file when <see cref="FederatedTokenFile"/> is not set.</summary>
	public string? FederatedToken { get; set; }

	/// <summary>Gets or sets a client assertion JWT for client-assertion credential auth.</summary>
	public string? ClientAssertion { get; set; }
	#endregion

	#region Managed Identity
	/// <summary>Gets or sets whether to use Managed Identity. When <c>true</c>, uses system-assigned MI, or user-assigned MI if <see cref="ClientId"/> is also set.</summary>
	public bool UseManagedIdentity { get; set; }
	#endregion

	/// <summary>
	/// Converts to a <see cref="TokenCredential"/> using the same priority chain as <c>AzureTenant</c>:
	/// workload identity → client assertion → certificate → managed identity → client secret → DefaultAzureCredential.
	/// </summary>
	public static implicit operator TokenCredential(FoundryAgent agent)
	{
		if (!string.IsNullOrEmpty(agent.ClientId) && !string.IsNullOrEmpty(agent.TenantId))
		{
			// Priority 1: Workload Identity Federation
			if (!string.IsNullOrEmpty(agent.FederatedTokenFile) || !string.IsNullOrEmpty(agent.FederatedToken))
			{
				var tokenFile = agent.FederatedTokenFile ?? WriteTokenToTempFile(agent.FederatedToken!);
				return new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
				{
					TenantId = agent.TenantId,
					ClientId = agent.ClientId,
					TokenFilePath = tokenFile
				});
			}

			// Priority 2: Client Assertion
			if (!string.IsNullOrEmpty(agent.ClientAssertion))
				return new ClientAssertionCredential(agent.TenantId, agent.ClientId, () => agent.ClientAssertion);

			// Priority 3: Certificate-based
			if (!string.IsNullOrEmpty(agent.CertificatePath))
				return new ClientCertificateCredential(agent.TenantId, agent.ClientId, agent.CertificatePath);
		}

		// Priority 4: Managed Identity (system-assigned, or user-assigned by ClientId)
		if (agent.UseManagedIdentity)
		{
			return !string.IsNullOrEmpty(agent.ClientId)
				? new ManagedIdentityCredential(agent.ClientId)
				: new ManagedIdentityCredential();
		}

		// Priority 5: Client Secret
		if (!string.IsNullOrEmpty(agent.ClientId) &&
			!string.IsNullOrEmpty(agent.ClientSecret) &&
			!string.IsNullOrEmpty(agent.TenantId))
			return new ClientSecretCredential(agent.TenantId, agent.ClientId, agent.ClientSecret);

		// Priority 6: DefaultAzureCredential
		return new DefaultAzureCredential();
	}

	/// <summary>Implicitly converts a <see cref="FoundryAgent"/> to <see cref="AzureAIInferenceClientOptions"/>.</summary>
	public static implicit operator AzureAIInferenceClientOptions(FoundryAgent agent) => new();

	private static string WriteTokenToTempFile(string token)
	{
		var path = Path.GetTempFileName();
		File.WriteAllText(path, token);
		return path;
	}
}
