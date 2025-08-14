namespace nc.Hub;

public class Solution
{
    public string Name { get; set; }

    public string Version { get; set; }

    public IEnumerable<string> Models { get; set; }

    public IEnumerable<string> DataStorage { get; set; }
    public IEnumerable<string> FileStorage { get; set; }

    public IEnumerable<string> CloudTenants { get; set; }
}
