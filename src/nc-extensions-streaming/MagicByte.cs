namespace nc.Extensions.Streaming;

public class MagicByte
{
	/// <summary>
	/// Bytes that identify the file type.
	/// </summary>
	public required byte[] Data { get; set; }

	/// <summary>
	/// MimeType associated with starting bytes of the file.
	/// </summary>
	public required string MimeType { get; set; }

	/// <summary>
	/// Gets or sets the offset to analyze for file types like mp4.
	/// </summary>
	public int Offset { get; set; } = 0;

	/// <summary>
	/// Determines whether the specified file header matches the expected data pattern at the given offset.
	/// </summary>
	/// <param name="fileHeader">The byte array representing the file header to compare against the expected data pattern. Cannot be null.</param>
	/// <returns>true if the file header contains the expected data pattern at the specified offset; otherwise, false.</returns>
	public bool Matches(byte[] fileHeader)
	{
		// Ensure the header is large enough to contain the data at the specified offset
		if (fileHeader.Length < (Offset + Data.Length))
			return false;

		for (int i = 0; i < Data.Length; i++)
		{
			// Compare header at (Offset + i) against Data at (i)
			if (fileHeader[Offset + i] != Data[i])
				return false;
		}
		return true;
	}

	public static readonly List<MagicByte> Defaults =
	[
        // Images
        new() { Data = [0xFF, 0xD8, 0xFF], MimeType = "image/jpeg" },
		new() { Data = [0x89, 0x50, 0x4E, 0x47], MimeType = "image/png" },
		new() { Data = [0x47, 0x49, 0x46, 0x38], MimeType = "image/gif" },
		new() { Data = [0x42, 0x4D], MimeType = "image/bmp" },
		new() { Data = [0x49, 0x49, 0x2A, 0x00], MimeType = "image/tiff" }, // Little Endian
        new() { Data = [0x4D, 0x4D, 0x00, 0x2A], MimeType = "image/tiff" }, // Big Endian
        
		// Zip
		new() { Data = [0x50, 0x4B, 0x03, 0x04], MimeType = "application/zip" },
		new() { Data = [0x1F, 0x8B], MimeType = "application/gzip" }, 

        // Documents
        new() { Data = [0x25, 0x50, 0x44, 0x46], MimeType = "application/pdf" },
        
        // Legacy Word
        new() { Data = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1], MimeType = "application/ms-office" },

		// Audio
        new() { Data = [0x49, 0x44, 0x33], MimeType = "audio/mpeg" }, // MP3
        new() { Data = [0x52, 0x49, 0x46, 0x46], MimeType = "audio/wav" }, // WAV (RIFF)
        new() { Data = [0x66, 0x4C, 0x61, 0x43], MimeType = "audio/flac" },
        
        // Video
        new() { Data = [0x66, 0x74, 0x79, 0x70], MimeType = "video/mp4", Offset = 4 }, // ftyp atom
        new() { Data = [0x1A, 0x45, 0xDF, 0xA3], MimeType = "video/x-matroska" }, // MKV
	];

}

