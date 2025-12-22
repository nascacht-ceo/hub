using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace nc_extensions_streaming;

/// <summary>
/// Provides extension methods for computing cryptographic and perceptual hashes on streams.
/// </summary>
/// <remarks>The StreamExtensions class includes methods for generating cryptographic hash values using various
/// algorithms, as well as perceptual and content-based hashes for streams containing images or text. These methods are
/// designed to simplify common hashing scenarios, such as verifying file integrity or comparing content similarity. All
/// methods are implemented as static extension methods and require a readable stream as input.</remarks>
public static class StreamExtensions
{
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

	/// <summary>
	/// Computes the cryptographic hash of the entire content of the specified stream using the given hashing algorithm.
	/// </summary>
	/// <remarks>The method reads the entire stream from the beginning to compute the hash. The caller is
	/// responsible for disposing the stream if necessary. The position of the stream after the operation is at the end of
	/// the stream if it is seekable.</remarks>
	/// <param name="stream">The input stream whose contents are to be hashed. The stream must be readable. If the stream supports seeking, its
	/// position is reset to the beginning before hashing.</param>
	/// <param name="algorithm">The cryptographic hashing algorithm to use. If null, SHA256 is used by default.</param>
	/// <returns>A byte array containing the computed hash value of the stream's content.</returns>
	/// <exception cref="NotSupportedException">Thrown if the specified algorithm is not supported.</exception>
	public static byte[] CryptographicHash(this Stream stream, CryptographicHashingAlgorighm? algorithm = CryptographicHashingAlgorighm.SHA256) 
	{
		ArgumentNullException.ThrowIfNull(stream);

		// Reset stream position if possible to ensure we hash the whole file
		if (stream.CanSeek)
		{
			stream.Position = 0;
		}
		switch (algorithm)
		{
			case CryptographicHashingAlgorighm.SHA256:
				using (var hasher = SHA256.Create())
					return hasher.ComputeHash(stream);
			case CryptographicHashingAlgorighm.SHA384:
				using (var hasher = SHA384.Create())
					return hasher.ComputeHash(stream);
			case CryptographicHashingAlgorighm.SHA512:
				using (var hasher = SHA512.Create())
					return hasher.ComputeHash(stream);
			case CryptographicHashingAlgorighm.SHA3_256:
				using (var hasher = SHA3_256.Create())
					return hasher.ComputeHash(stream);
			case CryptographicHashingAlgorighm.SHA3_384:
				using (var hasher = SHA3_384.Create())
					return hasher.ComputeHash(stream);
			case CryptographicHashingAlgorighm.SHA3_512:
				using (var hasher = SHA3_512.Create())
					return hasher.ComputeHash(stream);
			case CryptographicHashingAlgorighm.SHA1:
				using (var hasher = SHA1.Create())
					return hasher.ComputeHash(stream);
			case CryptographicHashingAlgorighm.MD5:
				using (var hasher = MD5.Create())
					return hasher.ComputeHash(stream);
			default:
				throw new NotSupportedException();
		}

	}

	/// <summary>
	/// Computes a perceptual difference hash (dHash) for the image data in the specified stream, enabling fast visual
	/// similarity comparisons between images.
	/// </summary>
	/// <remarks>The returned hash encodes the relative brightness differences of adjacent pixels in a downscaled
	/// grayscale version of the image. Images that are visually similar will produce similar hash values, even if their
	/// binary representations differ. This method resets the stream position to the beginning if the stream supports
	/// seeking.</remarks>
	/// <param name="stream">The stream containing image data to hash. The stream must be readable and positioned at the start of the image
	/// data.</param>
	/// <returns>A 64-bit unsigned integer representing the visual hash of the image. Returns 0 if the image cannot be decoded.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the stream parameter is null.</exception>
	public static ulong VisualHash(this Stream stream)
	{
		if (stream == null) throw new ArgumentNullException(nameof(stream));
		if (stream.CanSeek) stream.Position = 0;

		// 1. Initialize the Codec
		using var codec = SKCodec.Create(stream);
		if (codec == null) return 0;

		// 2. Set modern Sampling (Linear is best for hashing)
		var sampling = new SKSamplingOptions(SKFilterMode.Linear);

		// 3. Prepare a 9x8 "Comparison Grid"
		// We use Gray8 (1 byte per pixel) to save memory and ignore color noise
		var info = new SKImageInfo(9, 8, SKColorType.Gray8, SKAlphaType.Opaque);
		using var targetBitmap = new SKBitmap(info);

		// 4. Decode and Resize
		using (var canvas = new SKCanvas(targetBitmap))
		{
			using var original = SKBitmap.Decode(codec);
			using var image = SKImage.FromBitmap(original);

			// DrawImage is required for SKSamplingOptions
			canvas.DrawImage(image, new SKRect(0, 0, 9, 8), sampling, null);
		}

		// 5. Generate the Difference Hash (dHash)
		ulong hash = 0;
		int bit = 0;
		ReadOnlySpan<byte> pixels = targetBitmap.GetPixelSpan();

		for (int y = 0; y < 8; y++) // 8 rows
		{
			for (int x = 0; x < 8; x++) // 8 horizontal comparisons per row
			{
				int current = (y * 9) + x;
				int next = (y * 9) + (x + 1);

				// If the left pixel is brighter than the right, set the bit to 1
				if (pixels[current] > pixels[next])
				{
					hash |= (1UL << bit);
				}
				bit++;
			}
		}

		return hash;
	}

	/// <summary>
	/// Generates a 64-bit SimHash (Semantic Hash) for a stream of text.
	/// Resists small changes, typos, and reordering of sentences.
	/// </summary>
	public static ulong SemanticHash(this Stream stream)
	{
		if (stream == null) throw new ArgumentNullException(nameof(stream));
		if (stream.CanSeek) stream.Position = 0;

		using var reader = new StreamReader(stream, Encoding.UTF8);
		string text = reader.ReadToEnd();

		// 1. Tokenize (Clean text and split into words)
		var words = text.ToLowerInvariant()
						.Split(new[] { ' ', '\r', '\n', '\t', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

		// 2. Initialize a 64-bit weight vector
		int[] v = new int[64];

		foreach (var word in words)
		{
			// 3. Hash each word to 64 bits (using a simple non-crypto hash like FNV-1a or MD5)
			ulong wordHash = GetKnuthHash(word);

			for (int i = 0; i < 64; i++)
			{
				// 4. If the bit is 1, add weight; if 0, subtract weight
				if (((wordHash >> i) & 1UL) == 1UL)
					v[i]++;
				else
					v[i]--;
			}
		}

		// 5. Fingerprint: If weight > 0, set bit to 1, else 0
		ulong fingerprint = 0;
		for (int i = 0; i < 64; i++)
		{
			if (v[i] > 0)
				fingerprint |= (1UL << i);
		}

		return fingerprint;
	}

	/// <summary>
	/// Computes a 64-bit hash value for the specified string using a variant of Knuth's multiplicative hashing algorithm.
	/// </summary>
	/// <remarks>This method provides a fast, non-cryptographic hash suitable for hash tables and general-purpose
	/// use. The hash is case-sensitive and depends on the exact sequence of characters in the input string. Do not use
	/// this hash for security-sensitive purposes.</remarks>
	/// <param name="read">The input string to hash. Cannot be null.</param>
	/// <returns>A 64-bit unsigned integer representing the hash value of the input string.</returns>
	private static ulong GetKnuthHash(string read)
	{
		ulong hashedValue = 3074457345618258791ul;
		for (int i = 0; i < read.Length; i++)
		{
			hashedValue += read[i];
			hashedValue *= 3074457345618258799ul;
		}
		return hashedValue;
	}
}
