using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Nuka.SDK.Cosmos.Repositories.Document;

namespace Nuka.SDK.Cosmos.App.Models
{
 [DataContract(Name = "TestDocument")]
    public class NukaExampleInternalModel: IDocument
    {
        [JsonProperty(PropertyName = "grouping")]
        public string Grouping { get; set; }
        [JsonProperty(PropertyName = "id")] 
        public string Id { get; set; }
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}