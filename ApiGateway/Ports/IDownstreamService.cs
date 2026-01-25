using System.Threading.Tasks;

namespace ApiGateway.Ports
{
    public interface IDownstreamService
    {
        Task<ApiResponse> ForwardRequestAsync(ApiRequest request);
    }
}
