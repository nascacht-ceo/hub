namespace nc.Google;

/// <summary>
/// Provides configuration options for the Google tenant manager.
/// </summary>
public class GoogleTenantManagerOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether to throw an exception when a tenant is not found.
	/// Default is true.
	/// </summary>
	public bool ThrowOnNotFound { get; set; } = true;

	/// <summary>
	/// Gets or sets the default GCP project ID to use when no tenant is specified.
	/// </summary>
	public string? DefaultProjectId { get; set; }
}
