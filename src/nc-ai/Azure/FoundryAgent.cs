using Azure.AI.Inference;
using Azure.Core;
using Azure.Identity;
using nc.Ai.Interfaces;

namespace nc.Ai.Azure;

public record FoundryAgent : IAgent
{
	/// <summary>Deployment name</summary>
	public string Model { get; set; } = string.Empty;
	public string? Endpoint { get; set; }
	public string? ApiKey { get; set; }
	public string? ApiVersion { get; set; }

	public string? TenantId { get; set; }
	public string? ClientId { get; set; }
	public string? ClientSecret { get; set; }

	#region Certificate Authentication
	public string? CertificatePath { get; set; }
	public string? CertificateThumbprint { get; set; }
	public string? CertificatePassword { get; set; }
	#endregion

	#region Workload Identity Federation
	public string? FederatedTokenFile { get; set; }
	public string? FederatedToken { get; set; }
	public string? ClientAssertion { get; set; }
	#endregion

	#region Managed Identity
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

	public static implicit operator AzureAIInferenceClientOptions(FoundryAgent agent) => new();

	private static string WriteTokenToTempFile(string token)
	{
		var path = Path.GetTempFileName();
		File.WriteAllText(path, token);
		return path;
	}
}
