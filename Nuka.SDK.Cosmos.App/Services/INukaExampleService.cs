using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuka.SDK.Cosmos.App.Models;

namespace Nuka.SDK.Cosmos.App.Services
{
    public interface INukaExampleService
    {
        Task<NukaExampleInternalModel> GetAsync(string group, string id);
        Task<IEnumerable<NukaExampleInternalModel>> GetAllAsync(string group);
        Task<IEnumerable<NukaExampleInternalModel>> GetByIdsAsync(string group, string[] ids);
        IQueryable<NukaExampleInternalModel> GetToLimit(string group, int? maxLimit);
        Task<NukaExampleInternalModel> PostAsync(string group, NukaExampleInternalModel model);
        Task<NukaExampleInternalModel> PutAsync(string group, string id, NukaExampleInternalModel model);
        Task<bool> DeleteAsync(string group, string id);
    }
}

    