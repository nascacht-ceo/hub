namespace nc.Scaling
{
	public class TplScalingServiceOptions
	{
		public int BoundedCapacity { get; set; } = -1;
		public bool EnsureOrdered { get; set; } = true;
		public int MaxDegreeOfParallelism { get; set; } = 1;
		public int MaxMessagesPerTask { get; set; } = -1;
		public string NameFormat { get; set; } = "{0} Id={1}";
		public bool SingleProducerConstrained { get; set; } = false;

		public static implicit operator TplScalingOptions(TplScalingServiceOptions options)
			=> new TplScalingOptions
			{
				BoundedCapacity = options.BoundedCapacity,
				EnsureOrdered = options.EnsureOrdered,
				MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
				MaxMessagesPerTask = options.MaxMessagesPerTask,
				NameFormat = options.NameFormat,
				SingleProducerConstrained = options.SingleProducerConstrained
			};

		public TplScalingOptions ToScalingOptions(CancellationTokenSource cancellationTokenSource)
		{
			return new TplScalingOptions
			{
				BoundedCapacity = BoundedCapacity,
				EnsureOrdered = EnsureOrdered,
				MaxDegreeOfParallelism = MaxDegreeOfParallelism,
				MaxMessagesPerTask = MaxMessagesPerTask,
				NameFormat = NameFormat,
				SingleProducerConstrained = SingleProducerConstrained,
				CancellationToken = cancellationTokenSource.Token
			};
		}
	}
}