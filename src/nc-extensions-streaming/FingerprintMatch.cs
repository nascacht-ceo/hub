namespace nc.Extensions.Streaming;

/// <summary>
/// Specifies the result of comparing two fingerprints for similarity or duplication.
/// </summary>
/// <remarks>Use this enumeration to interpret the outcome of a fingerprint comparison operation. The values
/// indicate whether the fingerprints are identical, likely duplicates, similar, or different, typically based on the
/// computed Hamming distance between them.</remarks>
public enum FingerprintMatch
{
	/// <summary>
	/// Exact fingerprint match
	/// </summary>
	Exact,
	/// <summary>
	/// Highly likely a duplicate based on Low Hamming Distance
	/// Hamm
	/// </summary>
	Duplicate,
	/// <summary>
	/// Possilbe similar content based on Medium Hamming Distance
	/// </summary>
	Similar,
	/// <summary>
	/// Different content based on High Hamming Distance
	/// </summary>
	Different
}
