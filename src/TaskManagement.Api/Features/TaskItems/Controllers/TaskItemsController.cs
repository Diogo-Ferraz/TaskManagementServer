using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Queries;
using TaskManagement.Shared.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Features.TaskItems.Controllers
{
    /// <summary>
    /// API controller for managing task items.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class TaskItemsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TaskItemsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskItemsController"/> class.
        /// </summary>
        public TaskItemsController(IMediator mediator, ILogger<TaskItemsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new task item.
        /// </summary>
        /// <param name="command">The command containing task item details.</param>
        /// <returns>The created task item.</returns>
        [HttpPost]
        [Authorize(Policy = Policies.CanManageTasks)]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create([FromBody] CreateTaskItemCommand command)
        {
            _logger.LogInformation("Attempting to create task item with title: {Title} for ProjectId: {ProjectId}", command.Title, command.ProjectId);
            var createdTaskDto = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetById), new { id = createdTaskDto.Id }, createdTaskDto);
        }

        /// <summary>
        /// Retrieves a task item by its ID.
        /// </summary>
        /// <param name="id">The ID of the task item.</param>
        /// <returns>The requested task item.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Retrieving task item with ID: {TaskItemId}", id);
            var taskItemDto = await _mediator.Send(new GetTaskItemQuery { Id = id });
            return Ok(taskItemDto);
        }

        /// <summary>
        /// Retrieves task items using optional filters.
        /// </summary>
        /// <param name="projectId">Optional project ID filter.</param>
        /// <param name="assignedUserId">Optional assigned user ID filter.</param>
        /// <param name="updatedByUserId">Optional last-modified-by user ID filter.</param>
        /// <param name="search">Optional text search in task title/description.</param>
        /// <param name="lastModifiedFrom">Optional inclusive lower bound for last modified timestamp (UTC recommended).</param>
        /// <param name="lastModifiedTo">Optional inclusive upper bound for last modified timestamp (UTC recommended).</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="unassignedOnly">Optional filter for unassigned tasks only.</param>
        /// <param name="limit">Legacy maximum number of tasks to return (uses first page).</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page (max 500).</param>
        /// <returns>A filtered list of task items.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTasks(
            [FromQuery] Guid? projectId,
            [FromQuery] string? assignedUserId,
            [FromQuery] string? updatedByUserId,
            [FromQuery] string? search,
            [FromQuery] DateTime? lastModifiedFrom,
            [FromQuery] DateTime? lastModifiedTo,
            [FromQuery] TaskStatus? status,
            [FromQuery] bool? unassignedOnly,
            [FromQuery] int? limit,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            _logger.LogInformation(
                "Retrieving tasks with filters. ProjectId: {ProjectId}, AssignedUserId: {AssignedUserId}, UpdatedByUserId: {UpdatedByUserId}, Search: {Search}, LastModifiedFrom: {LastModifiedFrom}, LastModifiedTo: {LastModifiedTo}, Status: {Status}, UnassignedOnly: {UnassignedOnly}, Limit: {Limit}, Page: {Page}, PageSize: {PageSize}",
                projectId,
                assignedUserId,
                updatedByUserId,
                search,
                lastModifiedFrom,
                lastModifiedTo,
                status,
                unassignedOnly,
                limit,
                page,
                pageSize);

            var taskDtos = await _mediator.Send(new GetTasksQuery
            {
                ProjectId = projectId,
                AssignedUserId = assignedUserId,
                UpdatedByUserId = updatedByUserId,
                Search = search,
                LastModifiedFrom = lastModifiedFrom,
                LastModifiedTo = lastModifiedTo,
                Status = status,
                UnassignedOnly = unassignedOnly,
                Limit = limit,
                Page = page,
                PageSize = pageSize
            });

            return Ok(taskDtos);
        }

        /// <summary>
        /// Retrieves all task items for a specific project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>A list of task items for the project.</returns>
        [HttpGet("project/{projectId:guid}")]
        [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTasksForProject(Guid projectId)
        {
            _logger.LogInformation("Retrieving tasks for project ID: {ProjectId}", projectId);
            var taskDtos = await _mediator.Send(new GetTasksForProjectQuery { ProjectId = projectId });
            return Ok(taskDtos);
        }

        /// <summary>
        /// Updates an existing task item.
        /// </summary>
        /// <param name="id">The ID of the task item to update.</param>
        /// <param name="command">The command containing the updated task item details.</param>
        /// <returns>The updated task item.</returns>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = Policies.CanManageTasks)]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskItemCommand command)
        {
            _logger.LogInformation("Attempting to update task item with ID: {TaskItemId}", id);
            command.Id = id;
            var updatedTaskDto = await _mediator.Send(command);
            return Ok(updatedTaskDto);
        }

        /// <summary>
        /// Partially updates an existing task item.
        /// </summary>
        /// <param name="id">The ID of the task item to patch.</param>
        /// <param name="command">The command containing partial task updates.</param>
        /// <returns>The patched task item.</returns>
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = Policies.CanManageTasks)]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Patch(Guid id, [FromBody] PatchTaskItemCommand command)
        {
            _logger.LogInformation("Attempting to patch task item with ID: {TaskItemId}", id);
            command.Id = id;
            var updatedTaskDto = await _mediator.Send(command);
            return Ok(updatedTaskDto);
        }

        /// <summary>
        /// Deletes a task item.
        /// </summary>
        /// <param name="id">The ID of the task item to delete.</param>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.CanManageTasks)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Attempting to delete task item with ID: {TaskItemId}", id);
            await _mediator.Send(new DeleteTaskItemCommand { Id = id });
            return NoContent();
        }
    }
}
