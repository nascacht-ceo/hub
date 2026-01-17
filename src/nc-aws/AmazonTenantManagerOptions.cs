namespace nc.Aws;

/// <summary>
/// Provides configuration options for the Amazon tenant manager.
/// </summary>
public class AmazonTenantManagerOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether to throw an exception when a tenant is not found.
	/// Default is true.
	/// </summary>
	public bool ThrowOnNotFound { get; set; } = true;
}