using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Soap;

public class SoapServiceOptions : IValidatableObject
{
	public const string ConfigurtaionPath = "nc:soap";

	private IDictionary<string, SoapSpecification> _specifications
		= new Dictionary<string, SoapSpecification>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Gets or sets the collection of OpenAPI specifications.
	/// </summary>
	public IDictionary<string, SoapSpecification> Specifications
	{
		get
		{
			if (_specifications.Count == 0)
				_specifications.Add("Sample", new SoapSpecification(Resources.Defaults.SampleWsdl));
			return _specifications;
		}
		set { _specifications = value; }
	}

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		throw new NotImplementedException();
	}
}
