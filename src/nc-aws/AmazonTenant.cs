using Amazon.Extensions.NETCore.Setup;
using nc.Cloud;
using System.ComponentModel.DataAnnotations;
using System.Security;

namespace nc.Aws;

/// <summary>
/// Represents configuration settings for an Amazon Web Services (AWS) tenant, including credentials and connection
/// details.
/// </summary>
/// <remarks>This class is typically used to encapsulate the information required to connect to AWS services on
/// behalf of a specific tenant. It supports conversion to an AWSOptions instance for use with AWS SDK clients. The
/// class includes properties for specifying access keys, secret keys, profile names, and service endpoints. When both
/// AccessKey and SecretKey are provided, they are used to create explicit AWS credentials; otherwise, the specified
/// Profile is used. The Name property uniquely identifies the tenant within the application.</remarks>
public class AmazonTenant : ITenant
{
	/// <summary>
	/// Gets or sets the unique identifier of the tenant associated with the current context.
	/// </summary>
	public required string TenantId { get;set; }

	/// <summary>
	/// Gets or sets the unique name associated with the entity.
	/// </summary>
	[Key]
	public required string Name { get; set; }

	/// <summary>
	/// Gets or sets the base URL of the remote service endpoint.
	/// </summary>
	public string? ServiceUrl { get; set; }

	/// <summary>
	/// Gets or sets the access key used to authenticate requests.
	/// </summary>
	public string? AccessKey { get; set; }

	/// <summary>
	/// Gets or sets the secret key used for authentication or encryption purposes.
	/// </summary>
	public string? SecretKey { get; set; }

	/// <summary>
	/// Gets or sets the user profile associated with the current context.
	/// </summary>
	public string? Profile { get; set; }

	/// <summary>
	/// Defines an implicit conversion from an <see cref="AmazonTenant"/> instance to an <see cref="AWSOptions"/> object,
	/// allowing seamless use of tenant configuration as AWS SDK options.
	/// </summary>
	/// <remarks>This operator enables direct assignment of an <see cref="AmazonTenant"/> to an <see
	/// cref="AWSOptions"/> variable, simplifying integration with AWS SDK clients. If both <c>AccessKey</c> and
	/// <c>SecretKey</c> are provided, the resulting <see cref="AWSOptions"/> will include explicit credentials; otherwise,
	/// it will use the specified profile. The <c>ServiceUrl</c> property is also mapped to the options.</remarks>
	/// <param name="tenant">The <see cref="AmazonTenant"/> instance containing AWS profile and credential information to convert.</param>
	public static implicit operator AWSOptions(AmazonTenant tenant)
	{
		var options = new AWSOptions()
		{
			Profile = tenant.Profile
		};
		if (tenant.AccessKey != null && tenant.SecretKey != null)
			options.Credentials = new Amazon.Runtime.BasicAWSCredentials(tenant.AccessKey, tenant.SecretKey);
		options.DefaultClientConfig.ServiceURL = tenant.ServiceUrl;
		return options;
	}
}
