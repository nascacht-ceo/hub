namespace nc.Cloud;


/// <summary>
/// Represents a cloud tenant.
/// </summary>
/// <typeparam name="TCredential">Credential type used by the tenant.</typeparam>
public interface ITenant
{
	/// <summary>
	/// Unique identifier of the tenant.
	/// </summary>
	public string TenantId { get; set; }

	/// <summary>
	/// Name of the tenant.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets a credential associated with the tenant.
	/// </summary>
	public T GetService<T>();
}
