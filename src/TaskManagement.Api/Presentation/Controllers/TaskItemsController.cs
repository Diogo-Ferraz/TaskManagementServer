using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Application.TaskItems.Commands;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Application.TaskItems.Queries;
using TaskManagement.Api.Infrastructure.Identity;

namespace TaskManagement.Api.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class TaskItemsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TaskItemsController> _logger;
        private readonly ICurrentUser _currentUser;

        public TaskItemsController(IMediator mediator, ILogger<TaskItemsController> logger, ICurrentUser currentUser)
        {
            _mediator = mediator;
            _logger = logger;
            _currentUser = currentUser;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Retrieving Task with ID: {id}", id);
            var query = new GetTaskItemQuery
            {
                Id = id,
                RequestingUserId = _currentUser.Id
            };

            var result = await _mediator.Send(query);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByUser(string userId)
        {
            _logger.LogInformation("Retrieving all Tasks for user: {userId}", userId);
            var query = new GetTasksForUserQuery { UserId = userId };
            var result = await _mediator.Send(query);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpGet("project/{projectId}")]
        [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByProject(Guid projectId)
        {
            _logger.LogInformation("Retrieving all Tasks for Project: {projectId}", projectId);
            var query = new GetTasksForProjectQuery
            {
                ProjectId = projectId,
                RequestingUserId = _currentUser.Id
            };

            var result = await _mediator.Send(query);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(CreateTaskItemCommand command)
        {
            _logger.LogInformation("Creating new Task with title: {TaskName}", command.Title);
            command.RequestingUserId = _currentUser.Id;
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return Problem(result.Error);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value.Id },
                result.Value);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(Guid id, UpdateTaskItemCommand command)
        {
            _logger.LogInformation("Updating Task with ID: {id}", id);
            if (id != command.Id)
                return BadRequest();

            command.RequestingUserId = _currentUser.Id;
            var result = await _mediator.Send(command);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Deleting Task with ID: {id}", id);
            var command = new DeleteTaskItemCommand
            {
                Id = id,
                RequestingUserId = _currentUser.Id
            };

            var result = await _mediator.Send(command);

            return result.IsSuccess ? NoContent() : Problem(result.Error);
        }
    }
}
