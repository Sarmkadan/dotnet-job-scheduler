#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Manages job pipeline CRUD operations and provides real-time pipeline status.
/// A pipeline is an ordered chain of jobs where each step waits for the previous
/// step to complete successfully before starting.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PipelinesController : ControllerBase
{
    private readonly JobPipelineService _pipelineService;
    private readonly ILogger<PipelinesController> _logger;

    public PipelinesController(JobPipelineService pipelineService, ILogger<PipelinesController> logger)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new pipeline from an ordered list of job IDs.
    /// Sequential dependency edges are automatically registered between consecutive steps.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PipelineResponse>> CreatePipeline([FromBody] CreatePipelineRequest request)
    {
        try
        {
            var pipeline = await _pipelineService.CreatePipelineAsync(request, User?.Identity?.Name);
            var response = JobPipelineService.MapToResponse(pipeline);

            _logger.LogInformation("Pipeline created: {PipelineId} - {PipelineName}", pipeline.Id, pipeline.Name);

            return CreatedAtAction(nameof(GetPipeline), new { id = pipeline.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Pipeline creation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (JobNotFoundException ex)
        {
            _logger.LogWarning("Pipeline creation failed: referenced job not found. {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pipeline");
            return StatusCode(500, new { error = "Failed to create pipeline" });
        }
    }

    /// <summary>
    /// Returns a specific pipeline by ID with all steps and job details.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PipelineResponse>> GetPipeline(Guid id)
    {
        try
        {
            var pipeline = await _pipelineService.GetPipelineAsync(id);
            if (pipeline is null)
                return NotFound(new { error = "Pipeline not found" });

            return Ok(JobPipelineService.MapToResponse(pipeline));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pipeline {PipelineId}", id);
            return StatusCode(500, new { error = "Failed to retrieve pipeline" });
        }
    }

    /// <summary>
    /// Returns all pipelines ordered by creation date (newest first).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PipelineResponse>>> ListPipelines()
    {
        try
        {
            var pipelines = await _pipelineService.GetAllPipelinesAsync();
            return Ok(pipelines.Select(JobPipelineService.MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing pipelines");
            return StatusCode(500, new { error = "Failed to list pipelines" });
        }
    }

    /// <summary>
    /// Deletes a pipeline and removes its inter-step dependency edges.
    /// Individual jobs referenced by the pipeline are not deleted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePipeline(Guid id)
    {
        try
        {
            var deleted = await _pipelineService.DeletePipelineAsync(id);
            if (!deleted)
                return NotFound(new { error = "Pipeline not found" });

            _logger.LogInformation("Pipeline deleted: {PipelineId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pipeline {PipelineId}", id);
            return StatusCode(500, new { error = "Failed to delete pipeline" });
        }
    }

    /// <summary>
    /// Returns the current execution status of each step in the pipeline.
    /// Indicates which steps have completed, failed, or are ready to run.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PipelineStatusResponse>> GetPipelineStatus(Guid id)
    {
        try
        {
            var status = await _pipelineService.GetPipelineStatusAsync(id);
            if (status is null)
                return NotFound(new { error = "Pipeline not found" });

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status for pipeline {PipelineId}", id);
            return StatusCode(500, new { error = "Failed to retrieve pipeline status" });
        }
    }
}
