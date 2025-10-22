using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace nc.Hub.Tests;

public class SolutionFacts
{
	public class Constructor: SolutionFacts
	{
		[Fact]
		public void SetsVersion()
		{
			var solution = new Solution() { Name = "SolutionFacts.Constructor.SetsVersion" };
			Assert.NotNull(solution.Version);
		}

		
	}

	public class ServiceProvider: SolutionFacts 
	{ }



	public class AddModel: SolutionFacts 
	{ 
		[Fact]
		public void AddsByType()
		{
			var solution = new Solution() { Name = "SolutionFacts.AddModel" };
			solution.AddModel<ModelA>();
			var modelDefinition = solution.GetModelDefinition(typeof(ModelA));
			Assert.NotNull(modelDefinition);
			Assert.Equal(typeof(ModelA), solution.GetModelType(modelDefinition));
		}
	}

	public class GetModelType : SolutionFacts { }

	public class GetModelDefinition : SolutionFacts { }

	public class AddEndpoint : SolutionFacts { }

	public class GetEndpoint: SolutionFacts
	{
		
	}

	public class  GetCredential: SolutionFacts { }
}

public class  ModelA
{
	public int Id { get; set; }
	public required string AName { get; set; } 
}

public record class ModelB
{
	public long Id { get; set; }
	public required string BName { get; set; }
}

public class ModelC
{
	public Guid Id { get; set; }
	public required string CName { get; set; }

	public ModelA? A { get; set; } 
}
