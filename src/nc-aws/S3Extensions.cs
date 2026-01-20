using Amazon.S3.Model;

namespace nc.Aws;

/// <summary>
/// Extension methods for working with Amazon S3 MetadataCollection objects.
/// </summary>
public static class S3Extensions
{
	/// <summary>
	/// Converts a MetadataCollection to a standard dictionary.
	/// </summary>
	public static IDictionary<string, string?> ToDictionary(this MetadataCollection collection, StringComparer? comparer = null)
	{
		var dict = new Dictionary<string, string?>(comparer ?? StringComparer.OrdinalIgnoreCase);
		foreach (var key in collection.Keys)
		{
			dict[key] = collection[key];
		}
		return dict;
	}
}
