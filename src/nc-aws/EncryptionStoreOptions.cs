using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Aws;

public class EncryptionStoreOptions
{
	public string KeyPrefix { get; set; } = "/pki/keypairs/";

	public int RecoveryWindowInDays { get; set; } = 7;
}
