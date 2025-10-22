using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Scaling.Tests;

public class Walkthrough
{

	public Task<OtherPoco> SampleTransform(SomePoco some)
	{
		return Task.FromResult(new OtherPoco() { Name = $"Other {some.Name}" } );
	}

	public class SomePoco
	{
		public string Name { get; set; }
	}

	public class OtherPoco
	{
		public string Name { get; set; }
	}
}
