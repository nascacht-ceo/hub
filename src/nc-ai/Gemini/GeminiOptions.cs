using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Ai.Gemini;

public class GeminiOptions
{
	public GeminiFileServiceOptions FileService { get; set; } = new GeminiFileServiceOptions();
}

public class GeminiFileServiceOptions
{
	public string? ApiKey { get; set; }

	public string DownloadClientName { get; set; } = "GeminiDownload";

	public string UploadClientName { get; set; } = "GeminiUpload";

	public string UploadClientUrl { get; set; } = "https://generativelanguage.googleapis.com/upload/v1beta/files";
}
