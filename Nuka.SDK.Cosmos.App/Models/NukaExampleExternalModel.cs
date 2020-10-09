using System.ComponentModel.DataAnnotations;

namespace Nuka.SDK.Cosmos.App.Models
{
    public class NukaExampleExternalModel
    {
        [Required]
        public string Id { get; set; }
        public string Value { get; set; }
    }
}