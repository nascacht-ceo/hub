using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace nc.Soap;


/// <summary>
/// Specifies the version of the SOAP protocol to be used in a SOAP message.
/// </summary>
/// <remarks>This enumeration is used to indicate whether the SOAP 1.1 or SOAP 1.2 protocol should be applied. It
/// is commonly used in scenarios where SOAP-based communication is required, such as web services.</remarks>
public enum SoapVersion
{
	Soap11,
	Soap12
}

/// <summary>
/// Represents the configuration details for a SOAP service specification, including its name, address, version,  and
/// optional authentication and operation-to-action mappings.
/// </summary>
/// <remarks>This class is used to define the necessary parameters for interacting with a SOAP service. It
/// includes  properties for specifying the service's name, endpoint address, SOAP version, and optional basic
/// authentication  credentials. Additionally, it allows mapping operations to default SOAPAction headers when they are
/// not explicitly  provided by the caller.</remarks>
public sealed class SoapSpecification
{

	public SoapSpecification()
	{ }

	[SetsRequiredMembers]
	public SoapSpecification(string specUrl)
		: this(new Uri(specUrl))
	{ }

	[SetsRequiredMembers]
	public SoapSpecification(Uri specUrl)
	{
		SpecUrl = specUrl;
	}

	/// <summary>
	/// Gets the URI address associated with Soap Specification (WSDL).
	/// </summary>
	public required Uri SpecUrl { get; init; }

	/// <summary>
	/// Gets the SOAP version used by the service.
	/// Defaults to <see cref="SoapVersion.Soap11"/>.
	/// </summary>
	public SoapVersion Version { get; init; } = SoapVersion.Soap11;

	/// <summary>
	/// Gets the mapping of operation names to their corresponding SOAP actions.
	/// </summary>
	public Dictionary<string, string>? OperationToAction { get; init; } // key = operation name, value = SOAPAction
}
