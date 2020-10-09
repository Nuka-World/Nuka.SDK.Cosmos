using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nuka.SDK.Cosmos.App.Models;
using Nuka.SDK.Cosmos.Repositories.Document;

namespace Nuka.SDK.Cosmos.App.Services
{
    public class NukaExampleService : INukaExampleService
    {
        private readonly IDocumentRepository<NukaExampleInternalModel> _repository;

        public NukaExampleService(IDocumentRepository<NukaExampleInternalModel> repository)
        {
            _repository = repository;
        }

        public async Task<NukaExampleInternalModel> GetAsync(string group, string id)
        {
            return await _repository.GetDocumentAsync(group, id);
        }

        public async Task<IEnumerable<NukaExampleInternalModel>> GetAllAsync(string group)
        {
            return await _repository.GetDocumentsAsync(group);
        }

        public async Task<IEnumerable<NukaExampleInternalModel>> GetByIdsAsync(string group, string[] ids)
        {
            return await _repository.GetDocumentsByIdsAsync(group, ids);
        }

        public IQueryable<NukaExampleInternalModel> GetToLimit(string group, int? maxLimit)
        {
            return _repository.GetDocumentQuery(group, maxLimit);
        }

        public async Task<NukaExampleInternalModel> PostAsync(string group, NukaExampleInternalModel model)
        {
            await _repository.SetDocumentAsync(group, model);
            return model;
        }

        public async Task<NukaExampleInternalModel> PutAsync(string group, string id, NukaExampleInternalModel model)
        {
            model.Id = id;
            await _repository.SetDocumentAsync(group, model);
            return model;
        }

        public async Task<bool> DeleteAsync(string group, string id)
        {
            await _repository.DeleteDocumentAsync(group, id);
            return true;
        }
    }
}