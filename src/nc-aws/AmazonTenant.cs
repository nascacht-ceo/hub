using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using nc.Cloud;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
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
	public string TenantId { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Gets or sets the unique name associated with the entity.
	/// </summary>
	[Key]
	public string Name { get; set; } = "Default";

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
	/// Gets or sets the session token for temporary security credentials.
	/// </summary>
	public string? SessionToken { get; set; }

	/// <summary>
	/// Gets or sets the user profile associated with the current context.
	/// </summary>
	public string? Profile { get; set; }

	#region OIDC / Web Identity (e.g., GitHub Actions)
	/// <summary>
	/// Gets or sets the ARN of the IAM role to assume using web identity.
	/// </summary>
	public string? RoleArn { get; set; }

	/// <summary>
	/// Gets or sets the path to the web identity token file (OIDC token).
	/// Used by GitHub Actions via AWS_WEB_IDENTITY_TOKEN_FILE environment variable.
	/// </summary>
	public string? WebIdentityTokenFile { get; set; }

	/// <summary>
	/// Gets or sets the web identity token directly (alternative to file path).
	/// </summary>
	public string? WebIdentityToken { get; set; }

	/// <summary>
	/// Gets or sets the session name for the assumed role. Defaults to "nc-session" if not specified.
	/// </summary>
	public string? RoleSessionName { get; set; }
	#endregion

	private static MethodInfo serviceFactory = typeof(AWSOptions).GetMethod(nameof(AWSOptions.CreateServiceClient))!;

	/// <summary>
	/// Creates an AWS service client of the specified type using this tenant's credentials.
	/// </summary>
	/// <typeparam name="T">The type of AWS service client to create. Must implement <see cref="IAmazonService"/>.</typeparam>
	/// <returns>An instance of the requested AWS service client configured with this tenant's credentials.</returns>
	/// <exception cref="ArgumentException">Thrown when T does not implement <see cref="IAmazonService"/>.</exception>
	public T GetService<T>()
	{
		if (!typeof(IAmazonService).IsAssignableFrom(typeof(T)))
			throw new ArgumentException($"Type {typeof(T).Name} must implement IAmazonService.", nameof(T));

		AWSOptions options = this;
		var method = serviceFactory.MakeGenericMethod(typeof(T));
		return (T)method.Invoke(options, null)!;
	}

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

		// Priority 1: Web Identity / OIDC (e.g., GitHub Actions)
		if (!string.IsNullOrEmpty(tenant.RoleArn) &&
			(!string.IsNullOrEmpty(tenant.WebIdentityTokenFile) || !string.IsNullOrEmpty(tenant.WebIdentityToken)))
		{
			var tokenFile = tenant.WebIdentityTokenFile ?? WriteTokenToTempFile(tenant.WebIdentityToken!);
			options.Credentials = new AssumeRoleWithWebIdentityCredentials(
				tokenFile,
				tenant.RoleArn,
				tenant.RoleSessionName ?? "nc-session"
			);
		}
		// Priority 2: Session credentials (temporary, from STS)
		else if (tenant.AccessKey != null && tenant.SecretKey != null && !string.IsNullOrEmpty(tenant.SessionToken))
		{
			options.Credentials = new SessionAWSCredentials(tenant.AccessKey, tenant.SecretKey, tenant.SessionToken);
		}
		// Priority 3: Basic credentials (long-term)
		else if (tenant.AccessKey != null && tenant.SecretKey != null)
		{
			options.Credentials = new BasicAWSCredentials(tenant.AccessKey, tenant.SecretKey);
		}
		// Priority 4: Fall back to profile or default credential chain

		options.DefaultClientConfig.ServiceURL = tenant.ServiceUrl;
		return options;
	}

	private static string WriteTokenToTempFile(string token)
	{
		var path = Path.GetTempFileName();
		File.WriteAllText(path, token);
		return path;
	}
}
