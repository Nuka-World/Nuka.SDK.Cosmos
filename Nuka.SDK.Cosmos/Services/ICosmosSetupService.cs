using System.Threading;
using System.Threading.Tasks;

namespace Nuka.SDK.Cosmos.Services
{
    public interface ICosmosSetupService
    {
        Task InitializeCosmosDbAsync(CancellationToken cancellationToken);
    }
}