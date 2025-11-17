using nc.Hub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Data.Tests;

public class SolutionFacts
{
	public class Walkthrough
	{
		[Fact]
		public async Task WalksThroughSolution()
		{
			// Create a solution, and add the AventureWorks database endpoint.
			var solution = new Solution() { Name = "Data.Walkthrough" };
			solution.AddDatabase("AdventureWorks", "Server=(localdb)\\mssqllocaldb;Database=AdventureWorks;Trusted_Connection=True;");

			// Explore the models available in the database endpoint.
			var endpoint = solution.GetEndpoint<DatabaseEndpoint>("AdventureWorks");
			var models = await endpoint.GetModelsAsync("Person", "%").ToListAsync();
			Assert.NotEmpty(models);
			var tableNames = models.Select(m => m.ModelName).ToList();
			Assert.Contains("Person_Person", tableNames);
			Assert.Contains("Person_Address", tableNames);

			// Add a DbContext for these models to the solution.
			// solution.AddDbContext("People", models);


		}
	}
}
