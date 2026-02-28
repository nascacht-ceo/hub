using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Ai.Tests;

public static class Functions
{
	[Description("Gets the weather")]
	public static string GetWeather(string location)
	{
		return Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
	}
}

