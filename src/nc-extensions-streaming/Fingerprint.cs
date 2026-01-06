using System;
using System.Collections.Generic;
using System.Text;

namespace nc.Extensions.Streaming;

public class Fingerprint
{
	public required byte[] CryptographicHash { get; set; }
	public ulong? VisualHash { get; set; }
	public ulong? SemanticHash { get; set; }
}
