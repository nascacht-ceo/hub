using System.Threading.Tasks.Dataflow;

namespace nc.Scaling;

/// <summary>
/// Provides configuration options for scaling the execution of a dataflow block,  including support for implicit
/// conversions to and from <see cref="CancellationToken"/>  and <see cref="int"/> for convenience.
/// </summary>
/// <remarks>This class extends <see cref="ExecutionDataflowBlockOptions"/> and allows for  simplified
/// configuration of scaling-related properties. It supports implicit  conversions to and from <see
/// cref="CancellationToken"/> and <see cref="int"/>: <list type="bullet"> <item> <description>Implicit conversion from
/// <see cref="CancellationToken"/> creates a new instance with the specified cancellation token.</description> </item>
/// <item> <description>Implicit conversion from <see cref="int"/> sets the maximum degree of
/// parallelism.</description> </item> <item> <description>Implicit conversion to <see cref="CancellationToken"/>
/// retrieves the cancellation token associated with the instance.</description> </item> </list></remarks>
public class TplScalingOptions: ExecutionDataflowBlockOptions, IScalingOptions<TplPipeline>
{
	/// <summary>
	/// <inheritdoc cref="GroupingDataflowBlockOptions.Greedy"/>
	/// </summary>
	public bool Greedy { get; set; } = true;

	/// <summary>
	/// <inheritdoc cref="GroupingDataflowBlockOptions.MaxNumberOfGroups"/>
	/// </summary>
	public int MaxNumberOfGroups { get; set; } = DataflowBlockOptions.Unbounded;

	/// <summary>
	/// Implicitly converts a <see cref="CancellationToken"/> to a <see cref="TplScalingOptions"/>.
	/// </summary>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for the conversion.</param>
	public static implicit operator TplScalingOptions(CancellationToken cancellationToken)
		=> new TplScalingOptions { CancellationToken = cancellationToken };

	/// <summary>
	/// Implicitly converts an integer value to a <see cref="TplScalingOptions"/> instance.
	/// </summary>
	/// <param name="degreesOfParallelism">The maximum degree of parallelism to be used, represented as an integer.</param>
	public static implicit operator TplScalingOptions(int degreesOfParallelism)
		=> new TplScalingOptions { MaxDegreeOfParallelism = degreesOfParallelism };

	/// <summary>
	/// Implicitly converts a <see cref="TplScalingOptions"/> instance to a <see cref="CancellationToken"/>.
	/// </summary>
	/// <param name="options">The <see cref="TplScalingOptions"/> instance to convert. Must not be <c>null</c>.</param>
	public static implicit operator CancellationToken(TplScalingOptions options)
		=> options.CancellationToken;

	/// <summary>
	/// Defines an implicit conversion from a string to a <see cref="TplScalingOptions"/> instance.
	/// </summary>
	/// <param name="nameFormat">The name format string to initialize the <see cref="TplScalingOptions"/> instance.</param>
	public static implicit operator TplScalingOptions(string nameFormat)
		=> new TplScalingOptions { NameFormat = nameFormat };

	/// <summary>
	/// Implicitly converts a <see cref="TplScalingOptions"/> instance to a <see cref="GroupingDataflowBlockOptions"/>
	/// instance.
	/// </summary>
	/// <param name="options">The <see cref="TplScalingOptions"/> instance to convert. Cannot be <see langword="null"/>.</param>
	public static implicit operator GroupingDataflowBlockOptions(TplScalingOptions options)
		=> new GroupingDataflowBlockOptions
		{
			Greedy = options.Greedy,
			BoundedCapacity = options.BoundedCapacity,
			EnsureOrdered = options.EnsureOrdered,
			MaxNumberOfGroups = options.MaxNumberOfGroups,
			MaxMessagesPerTask = options.MaxMessagesPerTask,
			NameFormat = options.NameFormat,
			CancellationToken = options.CancellationToken,
		};
}