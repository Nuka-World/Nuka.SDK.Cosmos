using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Nuka.SDK.Cosmos.App.Models;
using Nuka.SDK.Cosmos.App.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Nuka.SDK.Cosmos.App.Controllers.v1
{
    [ExcludeFromCodeCoverage]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [SwaggerTag("Create, read, update and delete Products")]
    public class NukaExampleController : Controller
    {
        private readonly INukaExampleService _exampleService;
        private readonly ILogger<NukaExampleController> _logger;

        public NukaExampleController(
            INukaExampleService exampleService, 
            ILogger<NukaExampleController> logger)
        {
            _exampleService = exampleService;
            _logger = logger;
        }

        [HttpGet("{group}/values/{id}", Name = "GetResourceById")]
        public async Task<IActionResult> GetAsync(string group, string id)
        {
            var result = await _exampleService.GetAsync(@group, id);
            if (result == null)
            {
                _logger.LogDebug($"No item with id {id} was found.");
                return NotFound();
            }

            return Ok(new NukaExampleExternalModel
            {
                Id = result.Id,
                Value = result.Value
            });
        }

        [HttpPost("{group}/values", Name = "PostResource")]
        public async Task<IActionResult> PostAsync(
            string group, 
            [FromBody] NukaExampleExternalModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogDebug($"PostAsync: Model state is not valid");
                return BadRequest(ModelState);
            }

            try
            {
                var result = 
                    await _exampleService.PostAsync(group, new NukaExampleInternalModel
                    {
                        Grouping = @group,
                        Id = model.Id,
                        Value = model.Value
                    });
                return Ok(new NukaExampleExternalModel
                {
                    Id = result.Id,
                    Value = result.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"PostAsync: Exception message: {ex.Message}");
                return StatusCode((int) HttpStatusCode.InternalServerError);
            }
        }
    }
}