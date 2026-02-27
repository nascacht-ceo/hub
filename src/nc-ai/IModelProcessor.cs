/// <summary>
/// Abstracts a multimodal AI model capable of processing text, image, audio, and video inputs.
/// </summary>
public interface IModelProcessor
{
	/// <summary>Processes a text input and returns the model's text response.</summary>
	/// <param name="input">The text to process.</param>
	Task<string> ProcessTextAsync(string input);

	/// <summary>Processes an image and returns the model's output as raw bytes.</summary>
	/// <param name="imageStream">A stream containing the image data.</param>
	Task<byte[]> ProcessImageAsync(Stream imageStream);

	/// <summary>Processes an audio stream and returns a text transcription or response.</summary>
	/// <param name="audioStream">A stream containing the audio data.</param>
	Task<string> ProcessAudioAsync(Stream audioStream);

	/// <summary>Processes a video stream and returns a text response.</summary>
	/// <param name="videoStream">A stream containing the video data.</param>
	Task<string> ProcessVideoAsync(Stream videoStream);
}
