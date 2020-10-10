using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nuka.SDK.Cosmos.App.Models;
using Nuka.SDK.Cosmos.App.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Nuka.SDK.Cosmos.App.Controllers
{
    [ExcludeFromCodeCoverage]
    [ApiController]
    [Route("api/[controller]")]
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
        [ProducesResponseType(typeof(NukaExampleExternalModel), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAsync(string group, string id)
        {
            var result = await _exampleService.GetAsync(@group, id);

            if (result != null)
            {
                return Ok(new NukaExampleExternalModel
                {
                    Id = result.Id,
                    Value = result.Value
                });
            }

            _logger.LogDebug($"No item with id {id} was found.");
            return NotFound();
        }

        [HttpGet("{group}/values", Name = "GerAllResources")]
        [ProducesResponseType(typeof(IEnumerable<NukaExampleExternalModel>), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllAsync(string group)
        {
            var result = await _exampleService.GetAllAsync(group);

            if (result != null)
                return Ok(result.Select(r => new NukaExampleExternalModel {Id = r.Id, Value = r.Value}));

            _logger.LogDebug($"No item with id {group} was found.");
            return NotFound();
        }

        [HttpGet("{group}/values/limit/{limit?}", Name = "GetResourcesToLimit")]
        [ProducesResponseType(typeof(IEnumerable<NukaExampleExternalModel>), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public IActionResult GetToLimitAsync(string group, int? limit)
        {
            try
            {
                var result = _exampleService.GetToLimit(group, limit);

                if (result != null)
                {
                    var items = result.ToList<NukaExampleInternalModel>();
                    var extItems = items.Select(r => new NukaExampleExternalModel {Id = r.Id, Value = r.Value});
                    return Ok(extItems);
                }

                _logger.LogDebug($"No item with id {@group} was found.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"GetAsync: Exception message: {ex.Message}");
                return StatusCode((int) HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("{group}/list/values/{ids}", Name = "GetResourcesByList")]
        [ProducesResponseType(typeof(IEnumerable<NukaExampleExternalModel>), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetListAsync(string group, string ids)
        {
            try
            {
                var idList = ids.Split(',');
                var result = await _exampleService.GetByIdsAsync(@group, idList);

                if (result != null)
                    return Ok(result.Select(r => new NukaExampleExternalModel {Id = r.Id, Value = r.Value}));

                _logger.LogDebug($"No item with id {group} was found.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"GetAsync: Exception message: {ex.Message}");
                return StatusCode((int) HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost("{group}/values", Name = "PostResource")]
        [ProducesResponseType(typeof(NukaExampleExternalModel), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
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

        [HttpPut("{group}/values/{id}", Name = "PutResourceById")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutAsync(string group, string id, [FromBody] NukaExampleExternalModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogDebug($"PutAsync: Model state is not valid");
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _exampleService.PutAsync(@group, id,
                    new NukaExampleInternalModel
                    {
                        Grouping = @group,
                        Id = model.Id,
                        Value = model.Value
                    });

                if (result != null)
                    return NoContent();

                _logger.LogDebug($"No item with id {id} was found.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"PutAsync: Exception message: {ex.Message}");
                return StatusCode((int) HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete("{group}/values/{id}", Name = "DeleteResourceById")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteAsync(string group, string id)
        {
            try
            {
                var result = await _exampleService.GetAsync(group, id);

                if (result == null)
                {
                    _logger.LogDebug($"No item with id {id} was found.");
                    return NotFound();
                }

                var resultDeleted = await _exampleService.DeleteAsync(@group, id);

                if (resultDeleted) 
                    return NoContent();
                
                _logger.LogDebug($"Item with id {id} was not successfully deleted.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Exception message: {ex.Message}");
                return StatusCode((int) HttpStatusCode.InternalServerError);
            }
        }
    }
}