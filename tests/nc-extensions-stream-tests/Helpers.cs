namespace nc.Extensions.Stream.Tests;

using SkiaSharp;
using System.Text;

public static class Helpers
{
	/// <summary>
	/// Creates a slightly modified version of an image to test VisualHash thresholds.
	/// It changes a few pixels and modifies JPEG quality.
	/// </summary>
	public static void CreateImageNearDuplicate(string sourcePath, string outputPath)
	{
		using var input = File.OpenRead(sourcePath);
		using var bitmap = SKBitmap.Decode(input) ?? throw new InvalidOperationException($"Failed to decode image from {sourcePath}.");

		// 1. Alter a single pixel in the corner (invisible to humans)
		bitmap.SetPixel(0, 0, new SKColor(254, 254, 254));

		// 2. Save with a slightly different compression level
		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85); // Change from original
		using var stream = File.OpenWrite(outputPath);
		data.SaveTo(stream);
	}

	/// <summary>
	/// Creates a slightly modified version of a text file to test ContentHash (SimHash).
	/// </summary>
	public static void CreateTextNearDuplicate(string sourcePath, string outputPath)
	{
		string text = File.ReadAllText(sourcePath);

		// 1. Change one word or add a typo (e.g., "The" -> "Teh")
		// This should result in a very low Hamming Distance (1-3 bits)
		string modifiedText = text.Replace(" the ", " teh ").Replace(" ", "  ");

		File.WriteAllText(outputPath, modifiedText, Encoding.UTF8);
	}
}