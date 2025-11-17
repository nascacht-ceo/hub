using System.IO.Pipelines;

namespace nc.Ai;

public interface IAiFileService<TService, TReturn> where TService : IAiFileService<TService, TReturn>
{
	Task<TReturn> UploadAsync(PipeReader reader, string mediaType, CancellationToken cancellationToken);
}


public static class Extensions
{
	public static async Task<TReturn> UploadAsync<TService, TReturn>(this IAiFileService<TService, TReturn> service, Stream stream, string mediaType, CancellationToken cancellationToken)
		where TService : IAiFileService<TService, TReturn>
	{
		var pipe = new Pipe();
		_ = Task.Run(async () =>
		{
			await stream.CopyToAsync(pipe.Writer, cancellationToken);
			await pipe.Writer.CompleteAsync();
		}, cancellationToken);

		return await service.UploadAsync(pipe.Reader, mediaType, cancellationToken);
	}

	public static async Task<TReturn> UploadUriAsync<TService, TReturn>(this IAiFileService<TService, TReturn> service, Uri uri, string mediaType, CancellationToken cancellationToken)
		where TService : IAiFileService<TService, TReturn>
	{
		ArgumentNullException.ThrowIfNull(uri);
		using var httpClient = new HttpClient();
		using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		response.EnsureSuccessStatusCode();
		using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

		//using var target = new FileStream("w2.pdf", FileMode.Create, FileAccess.Write);
		//await stream.CopyToAsync(target, cancellationToken);
		//target.Flush();
		//target.Dispose();
		//stream.Position = 0;
		var pipe = new Pipe();

		var copy = Task.Run(async () =>
		{
			try
			{
				await stream.CopyToAsync(pipe.Writer, cancellationToken);
			}
			finally
			{
				await pipe.Writer.CompleteAsync(); 
			}
		}, cancellationToken).ContinueWith(t =>
		{
			if (t.Exception != null)
				throw t.Exception;
		}, TaskContinuationOptions.OnlyOnFaulted);

		// Start copying from the HTTP stream to the pipe writer in the background
		//var copyTask = Task.Run(async () =>
		//{
		//	await stream.CopyToAsync(pipe.Writer, cancellationToken);
		//	await pipe.Writer.CompleteAsync();
		//}, cancellationToken);

		//var copyTask = stream
		//	.CopyToAsync(pipe.Writer, cancellationToken)
		//	.ContinueWith(t => pipe.Writer.CompleteAsync(t.Exception), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);


		try
		{
			// 3. Pass the PipeReader directly to the service
			// The service's PipeReader.AsStream() will now pull data as the copyTask pushes it.
			var result = await service.UploadAsync(pipe.Reader, mediaType, cancellationToken);

			// 4. Await the copy task here to ensure all data was written and to propagate any exceptions
			await copy;

			return result;
		}
		finally
		{
			// Ensure the PipeReader is completed/disposed if something failed before the copyTask completed
			await pipe.Reader.CompleteAsync();
		}
	}
}
