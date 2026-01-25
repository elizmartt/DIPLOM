using System;
using System.Collections.Concurrent;
using System.Threading;
using ApiGateway.Ports;
using Microsoft.Extensions.Configuration;

namespace ApiGateway.Adapters.Outbound.Http
{
    // Այս դասը իրականացնում է ILoadBalancer ինտերֆեյսը՝ օգտագործելով Round Robin տրամաբանությունը։
    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        // Cluster ներքին դասը պահում է միկրոծառայության բոլոր հասցեները
        private class Cluster
        {
            public string[] Endpoints { get; }

            // Index-ը պետք է լինի հանրային (public) և փոխանցվի Interlocked-ին 
            // Thread-safe ինկրեմենտալ հաշվարկի համար:
            public int Index;

            public Cluster(string[] endpoints)
            {
                Endpoints = endpoints;
            }
        }

        // ConcurrentDictionary-ն ապահովում է Thread Safety կլաստերների ցուցակը կարդալիս։
        private readonly ConcurrentDictionary<string, Cluster> _clusters = new();

        public RoundRobinLoadBalancer(IConfiguration config)
        {
            // Կարդում ենք ServiceClusters բաժինը appsettings.json-ից
            var section = config.GetSection("ServiceClusters");

            foreach (var child in section.GetChildren())
            {
                // Get<string[]>()-ը վերլուծում է JSON զանգվածը string զանգվածի:
                var urls = child.Get<string[]>();
                if (urls != null && urls.Length > 0)
                {
                    // Յուրաքանչյուր ծառայության անունը (child.Key) դառնում է կլաստերի բանալին:
                    _clusters[child.Key] = new Cluster(urls);
                }
            }
        }

        /// <summary>
        /// Ընտրում է հաջորդ հասցեն Round Robin սկզբունքով։
        /// </summary>
        /// <param name="serviceName">Միկրոծառայության անունը (օրինակ՝ DiagnosisService)։</param>
        /// <returns>Հաջորդ հասանելի URL-ը։</returns>
        public string ResolveEndpoint(string serviceName)
        {
            if (!_clusters.TryGetValue(serviceName, out var cluster))
                throw new InvalidOperationException($"Service '{serviceName}' is not configured.");

            // 1. Thread-safe ինկրեմենտացիա: Interlocked-ը ապահովում է, որ 
            // բազմաթիվ թրեդներ չփոխեն Index-ը միաժամանակ։
            // Այն վերադարձնում է ՆՈՐ արժեքը։
            int nextIndex = Interlocked.Increment(ref cluster.Index);

            // 2. Round Robin Տրամաբանություն (Modulo Թվաբանություն)
            // nextIndex % Endpoints.Length բանաձևը ապահովում է, որ ինդեքսը 
            // մնա 0-ից մինչև N-1 տիրույթում և ցիկլային կերպով հերթագայվի:
            // Օրինակ՝ 1 % 2 = 1, 2 % 2 = 0, 3 % 2 = 1, ...
            int selectedIndex = nextIndex % cluster.Endpoints.Length;

            // 3. Վերադարձնում ենք ընտրված հասցեն։
            return cluster.Endpoints[selectedIndex];
        }
    }
}