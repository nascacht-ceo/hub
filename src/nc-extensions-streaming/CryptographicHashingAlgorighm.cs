namespace nc.Extensions.Streaming;

/// <summary>
/// Specifies the cryptographic hashing algorithm to use for computing hash values.
/// </summary>
/// <remarks>This enumeration includes commonly used secure hash algorithms, as well as legacy algorithms such
/// as MD5 and SHA1. When selecting an algorithm, consider the security requirements of your application. Some
/// algorithms, such as MD5 and SHA1, are considered insecure for most cryptographic purposes and should be avoided in
/// new applications.</remarks>
public enum CryptographicHashingAlgorighm
{
	SHA256,
	SHA384,
	SHA512,
	MD5,
	SHA1,
	RIPEMD160,
	SHA3_256,
	SHA3_384,
	SHA3_512
}
