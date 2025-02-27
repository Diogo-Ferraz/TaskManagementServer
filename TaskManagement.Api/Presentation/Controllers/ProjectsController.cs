using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Application.Projects.Commands;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Application.Projects.Queries;
using TaskManagement.Api.Infrastructure.Identity;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProjectsController> _logger;
        private readonly ICurrentUser _currentUser;

        public ProjectsController(IMediator mediator, ILogger<ProjectsController> logger, ICurrentUser currentUser)
        {
            _mediator = mediator;
            _logger = logger;
            _currentUser = currentUser;
        }

        [HttpPost]
        [Authorize(Roles = Roles.ProjectManager)]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateProjectCommand command)
        {
            _logger.LogInformation("Creating new project with name: {ProjectName}", command.Name);
            command.UserId = _currentUser.Id;
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return Problem(result.Error);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value.Id },
                result.Value);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.ProjectManager)]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectCommand command)
        {
            _logger.LogInformation("Updating project with ID: {ProjectId}", id);
            if (id != command.Id)
                return BadRequest();

            command.UserId = _currentUser.Id;
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.ProjectManager)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Deleting project with ID: {ProjectId}", id);
            var command = new DeleteProjectCommand
            {
                Id = id,
                UserId = _currentUser.Id
            };
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Retrieving project with ID: {ProjectId}", id);
            var result = await _mediator.Send(new GetProjectQuery { Id = id });
            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }

        [HttpGet("user")]
        [Authorize(Roles = Roles.ProjectManager)]
        [ProducesResponseType(typeof(IReadOnlyList<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserProjects()
        {
            var userId = _currentUser.Id;
            _logger.LogInformation("Retrieving all projects for user: {userId}", userId);
            var result = await _mediator.Send(new GetProjectsForUserQuery { UserId = userId });
            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
        }
    }
}
