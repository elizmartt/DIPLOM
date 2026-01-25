namespace ApiGateway.Ports
{
    public interface ILoadBalancer
    {
        string ResolveEndpoint(string serviceName);
    }
}
