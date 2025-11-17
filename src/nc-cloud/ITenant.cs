using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Cloud;

public interface ITenant
{
	public string TenantId { get; set; }
	public string Name { get; set; }
}
