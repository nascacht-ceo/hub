using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Scaling;

public interface IScalingOptions
{
	public CancellationToken CancellationToken { get; set; }

	public string NameFormat { get; set; }
}

public interface IScalingOptions<TImplementation>: IScalingOptions where TImplementation : IPipeline<TImplementation>
{
}
