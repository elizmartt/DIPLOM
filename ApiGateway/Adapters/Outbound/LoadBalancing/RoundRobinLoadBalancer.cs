
using System.Collections.Concurrent;

using ApiGateway.Ports;


namespace ApiGateway.Adapters.Outbound.Http
{
   
    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        
        private class Cluster
        {
            public string[] Endpoints { get; }

            
            public int Index;

            public Cluster(string[] endpoints)
            {
                Endpoints = endpoints;
            }
        }

       
        private readonly ConcurrentDictionary<string, Cluster> _clusters = new();

        public RoundRobinLoadBalancer(IConfiguration config)
        {
            
            var section = config.GetSection("ServiceClusters");

            foreach (var child in section.GetChildren())
            {
            
                var urls = child.Get<string[]>();
                if (urls != null && urls.Length > 0)
                {
           
                    _clusters[child.Key] = new Cluster(urls);
                }
            }
        }


        public string ResolveEndpoint(string serviceName)
        {
            if (!_clusters.TryGetValue(serviceName, out var cluster))
                throw new InvalidOperationException($"Service '{serviceName}' is not configured.");


            int nextIndex = Interlocked.Increment(ref cluster.Index);


            int selectedIndex = nextIndex % cluster.Endpoints.Length;
            
            return cluster.Endpoints[selectedIndex];
        }
    }
}