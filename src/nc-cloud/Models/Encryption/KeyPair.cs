namespace nc.Cloud.Models.Encryption;

/// <summary>
/// Represents an encryption key pair, including both public and private key material.
/// </summary>
/// <remarks>This model is typically used to store and manage RSA or ECC key pairs. The <see cref="PrivateKey"/>
/// contains the secret key material, while the <see cref="PublicKey"/> contains the corresponding public key.</remarks>
public record KeyPair
{
	/// <summary>
	/// The unique ID of the this key pair.
	/// </summary>
	public string? Id { get; set; }

	/// <summary>
	/// The RSA/ECC Private Key material (often PEM or PKCS#8 encoded). This is the secret.
	/// </summary>
	public string? PrivateKey { get; set; } 

	/// <summary>
	/// The RSA/ECC Public Key material (often PEM encoded).
	/// </summary>
	public string? PublicKey { get; set; } 

	/// <summary>
	/// Creates a new RSA key pair with the specified key size and an optional identifier.
	/// </summary>
	/// <param name="id">An optional identifier for the key pair. If <see langword="null"/>, a new GUID will be generated and used as the
	/// identifier.</param>
	/// <param name="keySize">The size of the RSA key, in bits. The default value is 3072. Must be a valid RSA key size supported by the
	/// platform.</param>
	/// <returns>A <see cref="KeyPair"/> object containing the generated RSA public and private keys, along with the specified or
	/// generated identifier.</returns>
	public static KeyPair Create(string? id = null, int keySize = 3072)
	{
		id ??= Guid.NewGuid().ToString();
		using var rsa = System.Security.Cryptography.RSA.Create(keySize);
		return new KeyPair
		{
			Id = id,
			PrivateKey = rsa.ExportPkcs8PrivateKeyPem(),
			PublicKey = rsa.ExportSubjectPublicKeyInfoPem()
		};
	}
}
