using System.Collections.Generic;
using System.Linq;
public class ServiceClusterConfiguration : Dictionary<string, List<string>>
{

    public List<string> GetServiceNames()
    {
        return this.Keys.ToList();
    }
}