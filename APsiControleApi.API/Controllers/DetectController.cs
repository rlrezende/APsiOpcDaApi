using System;
using System.Collections.Generic;
using APsiControleApi.Application.DTOs;
using APsiControleApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APsiControleApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DetectController : ControllerBase
    {
        private readonly IDetectService _detectService;

        public DetectController(IDetectService detectService)
        {
            _detectService = detectService;
        }

        [HttpGet("groups")]
        public async Task<ActionResult<IEnumerable<DetectGroupDto>>> GetGroups()
        {
            var result = await _detectService.GetGroupsAsync();
            return Ok(result);
        }

        [HttpGet("tags")]
        public async Task<ActionResult<IEnumerable<DetectTagDto>>> SearchTags(
            [FromQuery] string? search = null,
            [FromQuery] string? instrumentClass = null,
            [FromQuery] Guid? groupId = null,
            [FromQuery] int? limit = null)
        {
            var result = await _detectService.SearchTagsAsync(search, instrumentClass, groupId, limit);
            return Ok(result);
        }

        [HttpGet("models")]
        public async Task<ActionResult<DetectModelsOverviewDto>> GetModels()
        {
            var result = await _detectService.GetModelsOverviewAsync();
            return Ok(result);
        }

        [HttpPost("models")]
        public async Task<ActionResult<DetectModelDto>> CreateModel([FromBody] DetectModelCreateRequest request)
        {
            var result = await _detectService.CreateModelAsync(request);
            return CreatedAtAction(nameof(GetModels), new { id = result.Id }, result);
        }

        [HttpPost("models/{id:guid}/deploy")]
        public async Task<IActionResult> DeployModel(Guid id)
        {
            await _detectService.DeployDraftAsync(id);
            return NoContent();
        }

        public record ToggleModelRequest(bool IsActive);

        [HttpPost("models/{id:guid}/toggle")]
        public async Task<IActionResult> ToggleModel(Guid id, [FromBody] ToggleModelRequest request)
        {
            await _detectService.ToggleModelAsync(id, request.IsActive);
            return NoContent();
        }

        [HttpPost("models/{id:guid}/retrain")]
        public async Task<ActionResult<DetectTrainingJobDto>> RequestRetrain(Guid id)
        {
            var job = await _detectService.RequestRetrainAsync(id);
            return Ok(job);
        }

        [HttpGet("models/{id:guid}/jobs")]
        public async Task<ActionResult<IEnumerable<DetectTrainingJobDto>>> GetJobs(Guid id, [FromQuery] int take = 5)
        {
            var jobs = await _detectService.GetRecentJobsAsync(id, take);
            return Ok(jobs);
        }

        [HttpGet("tags/{id:guid}/history")]
        public async Task<ActionResult<DetectTagHistoryDto>> GetTagHistory(Guid id, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (start == default || end == default)
            {
                return BadRequest(new { message = "Período inválido" });
            }

            try
            {
                var history = await _detectService.GetTagHistoryAsync(id, start, end);
                return Ok(history);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
