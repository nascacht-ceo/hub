public interface IModelProcessor
{
    Task<string> ProcessTextAsync(string input);
    Task<byte[]> ProcessImageAsync(Stream imageStream);
    Task<string> ProcessAudioAsync(Stream audioStream);
    Task<string> ProcessVideoAsync(Stream videoStream);
}
