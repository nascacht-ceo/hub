namespace nc.Extensions.Streaming;

public class MagicByte
{
	public required byte[] Data { get; set; } 
	public required string MimeType { get; set; }

	public int Offset { get; set; } = 0;

	public bool Matches(byte[] fileHeader)
	{
		if (fileHeader.Length < Data.Length) return false;

		for (int i = 0; i < Data.Length; i++)
		{
			if (fileHeader[i] != Data[i]) return false;
		}
		return true;
	}

	public static readonly List<MagicByte> Defaults = new()
	{
        // Images
        new() { Data = new byte[] { 0xFF, 0xD8, 0xFF }, MimeType = "image/jpeg" },
		new() { Data = new byte[] { 0x89, 0x50, 0x4E, 0x47 }, MimeType = "image/png" },
		new() { Data = new byte[] { 0x47, 0x49, 0x46, 0x38 }, MimeType = "image/gif" },
		new() { Data = new byte[] { 0x42, 0x4D }, MimeType = "image/bmp" },
		new() { Data = new byte[] { 0x49, 0x49, 0x2A, 0x00 }, MimeType = "image/tiff" }, // Little Endian
        new() { Data = new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, MimeType = "image/tiff" }, // Big Endian
        
		// Zip
		new() { Data = new byte[] { 0x50, 0x4B, 0x03, 0x04 }, MimeType = "application/zip" },
		new() { Data = new byte[] { 0x1F, 0x8B }, MimeType = "application/gzip" }, 

        // Documents
        new() { Data = new byte[] { 0x25, 0x50, 0x44, 0x46 }, MimeType = "application/pdf" },
        
        // Zip-based Office formats (DOCX, XLSX, etc.)
        new() { Data = new byte[] { 0x50, 0x4B, 0x03, 0x04 }, MimeType = "application/vnd.openxmlformats-officedocument" },
        
        // Legacy Word
        new() { Data = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, MimeType = "application/msword" },

		// --- Audio ---
        new() { Data = new byte[] { 0x49, 0x44, 0x33 }, MimeType = "audio/mpeg" }, // MP3
        new() { Data = new byte[] { 0x52, 0x49, 0x46, 0x46 }, MimeType = "audio/wav" }, // WAV (RIFF)
        new() { Data = new byte[] { 0x66, 0x4C, 0x61, 0x43 }, MimeType = "audio/flac" },
        
        // --- Video ---
        new() { Data = new byte[] { 0x66, 0x74, 0x79, 0x70 }, MimeType = "video/mp4", Offset = 4 }, // ftyp atom
        new() { Data = new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, MimeType = "video/x-matroska" }, // MKV
	};

}

