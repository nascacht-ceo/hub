using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Aws;

/// <summary>
/// Represents configuration options for an encryption key store, including key prefix and recovery window settings.
/// </summary>
public class EncryptionStoreOptions
{
	/// <summary>
	/// Gets or sets the prefix used for key storage paths.
	/// </summary>
	public string KeyPrefix { get; set; } = "/pki/keypairs/";

	/// <summary>
	/// Gets or sets the number of days during which deleted data can be recovered.
	/// </summary>
	/// <remarks>A longer recovery window allows more time to restore deleted data before it is permanently removed.
	/// The value must be a non-negative integer.</remarks>
	public int RecoveryWindowInDays { get; set; } = 7;
}
