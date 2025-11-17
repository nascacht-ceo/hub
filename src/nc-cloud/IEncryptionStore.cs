using nc.Cloud.Models.Encryption;

namespace nc.Cloud;

/// <summary>
/// Defines the contract for retrieving cryptographic key data (PKI pairs) for named brokers.
/// This abstraction allows the application to be cloud-agnostic regarding key storage.
/// </summary>
public interface IEncryptionStore
{
	/// <summary>
	/// Retrieves the public and private key material associated with a specific ID.
	/// </summary>
	/// <param name="id">The unique identifier for the keypair (which corresponds to the Secret name/ID).</param>
	/// <returns>A KeyDataModel containing the public and private key strings, or null if not found.</returns>
	Task<KeyPair?> GetKeyPairAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves or updates the public and private key material associated with a specific ID.
	/// </summary>
	/// <param name="id">The unique identifier for the keypair (which corresponds to the Secret name/ID).</param>
	/// <param name="keyData">The KeyDataModel containing the key pair to be saved.</param>
	Task SetKeyPairAsync(string id, KeyPair keyData, CancellationToken cancellationToken = default);

	/// <summary>
	/// Schedules the deletion of the public and private key material associated with a specific broker ID.
	/// AWS Secrets Manager uses a recovery window for safety (default 30 days if not specified).
	/// </summary>
	/// <param name="id">The unique identifier for the broker.</param>
	Task DeleteKeyPairAsync(string id, CancellationToken cancellationToken = default);
}
