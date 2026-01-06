using Microsoft.Extensions.Options;

namespace nc.Extensions.Streaming;

/// <summary>
/// Provides methods and configuration for comparing fingerprints to determine their degree of similarity.
/// </summary>
/// <remarks>The FingerprintComparison class supports multiple comparison strategies, including direct hash
/// comparison and analysis of cryptographic, visual, and semantic fingerprint components. Threshold values can be
/// customized to adjust the sensitivity of duplicate and similarity detection. This class is suitable for scenarios
/// where identifying exact, duplicate, similar, or different entities based on fingerprint data is required.</remarks>
public class FingerprintComparison
{
	private readonly FingerprintComparisonOptions _options;

	/// <summary>
	/// Initializes a new instance of the FingerprintComparison class with the specified comparison options.
	/// </summary>
	/// <param name="options">The options to use for fingerprint comparison. If null, default options are used.</param>
	public FingerprintComparison(FingerprintComparisonOptions? options = null)
	{
		_options = options ?? new FingerprintComparisonOptions();
	}

	/// <summary>
	/// Initializes a new instance of the FingerprintComparison class with the specified configuration options provided from configuration.
	/// </summary>
	/// <param name="options">The options to use for fingerprint comparison. If null, default options are used.</param>
	public FingerprintComparison(IOptions<FingerprintComparisonOptions> options)
	{
		_options = options.Value ?? new FingerprintComparisonOptions();
	}

	/// <summary>
	/// Compares two fingerprint hashes and determines the degree of similarity between them.
	/// </summary>
	/// <remarks>The comparison is based on the Hamming distance between the two hashes. The thresholds for
	/// determining duplicate and similar matches are defined by the ThresholdLow and ThresholdHigh values. This method
	/// does not modify the input values.</remarks>
	/// <param name="hashA">The first fingerprint hash to compare.</param>
	/// <param name="hashB">The second fingerprint hash to compare.</param>
	/// <returns>A value indicating whether the fingerprints are exact matches, duplicates, similar, or different.</returns>
	public FingerprintMatch Compare(ulong? hashA, ulong? hashB)
	{
		if (!hashA.HasValue || !hashB.HasValue)
			return FingerprintMatch.Different;
		int distance = System.Numerics.BitOperations.PopCount(hashA.Value ^ hashB.Value);
		if (distance == 0)
			return FingerprintMatch.Exact;
		else if (distance <= _options.ThresholdLow)
			return FingerprintMatch.Duplicate;
		else if (distance <= _options.ThresholdHigh)
			return FingerprintMatch.Similar;
		else
			return FingerprintMatch.Different;
	}

	/// <summary>
	/// Compares two fingerprint hashes and classifies their similarity based on the number of differing bits.
	/// </summary>
	/// <remarks>The comparison uses the Hamming distance between the two hashes to determine similarity. If the
	/// distance is zero, the fingerprints are considered exact matches. Adjusting the threshold parameters allows
	/// customization of what is considered a duplicate or similar match.</remarks>
	/// <param name="hashA">The first 64-bit fingerprint hash to compare.</param>
	/// <param name="hashB">The second 64-bit fingerprint hash to compare.</param>
	/// <param name="thresholdLow">The maximum Hamming distance at which the fingerprints are considered duplicates. Must be less than or equal to
	/// <paramref name="thresholdHigh"/>. Defaults to <see cref="DefaultThresholdLow"/>.</param>
	/// <param name="thresholdHigh">The maximum Hamming distance at which the fingerprints are considered similar. Must be greater than or equal to
	/// <paramref name="thresholdLow"/>. Defaults to <see cref="DefaultThresholdHigh"/>.</param>
	/// <returns>A <see cref="FingerprintMatch"/> value indicating whether the fingerprints are exact matches, duplicates, similar,
	/// or different.</returns>
	public static FingerprintMatch Compare(ulong hashA, ulong hashB, int? thresholdLow = null, int? thresholdHigh = null)
	{
		thresholdLow ??= new FingerprintComparisonOptions().ThresholdLow;
		thresholdHigh ??= new FingerprintComparisonOptions().ThresholdHigh;
		int distance = System.Numerics.BitOperations.PopCount(hashA ^ hashB);
		if (distance == 0)
			return FingerprintMatch.Exact;
		else if (distance <= thresholdLow)
			return FingerprintMatch.Duplicate;
		else if (distance <= thresholdHigh)
			return FingerprintMatch.Similar;
		else
			return FingerprintMatch.Different;
	}

	/// <summary>
	/// Compares two fingerprint instances and determines their degree of similarity.
	/// </summary>
	/// <remarks>This method evaluates multiple aspects of the fingerprints, including cryptographic, visual, and
	/// semantic hashes, to determine the most accurate match category. Use this method to assess whether two fingerprints
	/// represent the same or similar entities.</remarks>
	/// <param name="source">The fingerprint to use as the source for the comparison.</param>
	/// <param name="target">The fingerprint to compare against the source fingerprint.</param>
	/// <returns>A value of the FingerprintMatch enumeration indicating whether the fingerprints are exact matches, duplicates,
	/// similar, or different.</returns>
	public FingerprintMatch Compare(Fingerprint source, Fingerprint target)
	{
		if (source.CryptographicHash == target.CryptographicHash)
			return FingerprintMatch.Exact;

		if (Compare(source.VisualHash, target.VisualHash) == FingerprintMatch.Exact)
			return FingerprintMatch.Duplicate;
		if (Compare(source.SemanticHash, target.SemanticHash) == FingerprintMatch.Exact)
			return FingerprintMatch.Duplicate;

		if (Compare(source.VisualHash, target.VisualHash) == FingerprintMatch.Duplicate)
			return FingerprintMatch.Duplicate;
		if (Compare(source.SemanticHash, target.SemanticHash) == FingerprintMatch.Duplicate)
			return FingerprintMatch.Duplicate;

		if (Compare(source.VisualHash, target.VisualHash) == FingerprintMatch.Similar)
			return FingerprintMatch.Similar;
		if (Compare(source.SemanticHash, target.SemanticHash) == FingerprintMatch.Similar)
			return FingerprintMatch.Similar;

		return FingerprintMatch.Different;
	}

	/// <summary>
	/// Compares two streams and determines the degree of similarity between their contents using fingerprint analysis.
	/// </summary>
	/// <remarks>The comparison considers cryptographic, visual, and semantic fingerprints to assess similarity.
	/// Both streams are read from their current positions; ensure streams are positioned appropriately before calling this
	/// method. The method does not modify the position of the streams after completion.</remarks>
	/// <param name="source">The source stream to compare. The stream must be readable and positioned at the beginning of the content to
	/// analyze.</param>
	/// <param name="target">The target stream to compare against. The stream must be readable and positioned at the beginning of the content to
	/// analyze.</param>
	/// <returns>A value of the FingerprintMatch enumeration indicating whether the streams are exact matches, duplicates, similar,
	/// or different.</returns>
	public FingerprintMatch Compare(Stream source, Stream target)
	{
		var sourceFingerprint = source.ToFingerprint();
		var targetFingerprint = target.ToFingerprint();
		if (sourceFingerprint.CryptographicHash == targetFingerprint.CryptographicHash)
			return FingerprintMatch.Exact;
		if (Compare(sourceFingerprint.VisualHash, targetFingerprint.VisualHash) == FingerprintMatch.Duplicate)
			return FingerprintMatch.Duplicate;
		if (Compare(sourceFingerprint.SemanticHash, targetFingerprint.SemanticHash) == FingerprintMatch.Duplicate)
			return FingerprintMatch.Duplicate;
		if (Compare(sourceFingerprint.VisualHash, targetFingerprint.VisualHash) == FingerprintMatch.Similar)
			return FingerprintMatch.Similar;
		if (Compare(sourceFingerprint.SemanticHash, targetFingerprint.SemanticHash) == FingerprintMatch.Similar)
			return FingerprintMatch.Similar;
		return FingerprintMatch.Different;
	}
}

/// <summary>
/// Represents configuration options for fingerprint comparison operations, including threshold values used to determine
/// match sensitivity.
/// </summary>
public record FingerprintComparisonOptions
{
	/// <summary>
	/// Gets or sets the lower threshold value used for calculating a <see cref="FingerprintMatch"/>.
	/// Default is 3.
	/// </summary>
	public int ThresholdLow { get; init; } = 3;
	/// <summary>
	/// Gets or sets the high threshold value used for calculating a <see cref="FingerprintMatch"/>.
	/// Default is 10.
	/// </summary>
	public int ThresholdHigh { get; init; } = 10;
}