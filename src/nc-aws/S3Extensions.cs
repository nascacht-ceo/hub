using Amazon.S3.Model;

namespace nc.Aws;

public static class S3Extensions
{
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
