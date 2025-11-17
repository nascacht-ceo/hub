using Amazon.Extensions.NETCore.Setup;
using nc.Cloud;
using System.ComponentModel.DataAnnotations;
using System.Security;

namespace nc.Aws;

public class AmazonTenant : ITenant
{
	public string? TenantId { get;set; }
	[Key]
	public string Name { get; set; }
	public string? ServiceUrl { get; set; }

	public string? AccessKey { get; set; }

	public string? SecretKey { get; set; }

	public string? Profile { get; set; }

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
