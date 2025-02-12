using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Api.Application.TaskItems.Commands;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Application.TaskItems.Queries;

namespace TaskManagement.Api.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaskItemsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TaskItemsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetTaskItemQuery
            {
                Id = id,
                RequestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            var result = await _mediator.Send(query);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByUser(string userId)
        {
            var query = new GetTasksForUserQuery { UserId = userId };
            var result = await _mediator.Send(query);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpGet("project/{projectId}")]
        [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByProject(Guid projectId)
        {
            var query = new GetTasksForProjectQuery
            {
                ProjectId = projectId,
                RequestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
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
            command.RequestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            if (id != command.Id)
                return BadRequest();

            command.RequestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _mediator.Send(command);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteTaskItemCommand
            {
                Id = id,
                RequestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            var result = await _mediator.Send(command);

            return result.IsSuccess ? NoContent() : Problem(result.Error);
        }
    }
}
