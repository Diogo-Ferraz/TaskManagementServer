using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Queries;
using TaskManagement.Shared.Models;

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
