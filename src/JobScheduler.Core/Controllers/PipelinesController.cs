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

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelinesController"/> class.
    /// </summary>
    /// <param name="pipelineService">The pipeline service.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipelineService"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public PipelinesController(JobPipelineService pipelineService, ILogger<PipelinesController> logger)
    {
        ArgumentNullException.ThrowIfNull(pipelineService);
        ArgumentNullException.ThrowIfNull(logger);

        _pipelineService = pipelineService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new pipeline from an ordered list of job IDs.
    /// Sequential dependency edges are automatically registered between consecutive steps.
    /// </summary>
    /// <param name="request">The pipeline definition containing the name and ordered job IDs.</param>
    /// <returns>The created pipeline, with a location header pointing to <see cref="GetPipeline"/>.</returns>
    /// <response code="201">The pipeline was created.</response>
    /// <response code="400">The name is missing or the pipeline has fewer than two steps.</response>
    /// <response code="404">One of the referenced jobs does not exist.</response>
    /// <response code="500">An unexpected error occurred while creating the pipeline.</response>
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
    /// <param name="id">The unique identifier of the pipeline.</param>
    /// <returns>The pipeline with its steps and job details.</returns>
    /// <response code="200">The pipeline was found and returned.</response>
    /// <response code="404">No pipeline exists with the given ID.</response>
    /// <response code="500">An unexpected error occurred while retrieving the pipeline.</response>
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
    /// <returns>All pipelines; an empty collection when none exist.</returns>
    /// <response code="200">The pipelines were returned.</response>
    /// <response code="500">An unexpected error occurred while listing pipelines.</response>
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
    /// <param name="id">The unique identifier of the pipeline to delete.</param>
    /// <response code="204">The pipeline was deleted.</response>
    /// <response code="404">No pipeline exists with the given ID.</response>
    /// <response code="500">An unexpected error occurred while deleting the pipeline.</response>
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
    /// <param name="id">The unique identifier of the pipeline.</param>
    /// <returns>The per-step status of the pipeline.</returns>
    /// <response code="200">The pipeline status was returned.</response>
    /// <response code="404">No pipeline exists with the given ID.</response>
    /// <response code="500">An unexpected error occurred while retrieving the pipeline status.</response>
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
