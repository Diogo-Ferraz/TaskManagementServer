using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Application.Projects.Commands;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Application.Projects.Queries;
using TaskManagement.Api.Domain.Common;
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
        public async Task<ActionResult<Result<ProjectDto>>> Create([FromBody] CreateProjectCommand command)
        {
            _logger.LogInformation("Creating new project with name: {ProjectName}", command.Name);
            command.UserId = _currentUser.Id;
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.ProjectManager)]
        public async Task<ActionResult<Result<ProjectDto>>> Update(Guid id, [FromBody] UpdateProjectCommand command)
        {
            _logger.LogInformation("Updating project with ID: {ProjectId}", id);
            command.Id = id;
            command.UserId = _currentUser.Id;
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.ProjectManager)]
        public async Task<ActionResult<Result<bool>>> Delete(Guid id)
        {
            _logger.LogInformation("Deleting project with ID: {ProjectId}", id);
            var command = new DeleteProjectCommand
            {
                Id = id,
                UserId = _currentUser.Id
            };
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Result<ProjectDto>>> Get(Guid id)
        {
            _logger.LogInformation("Retrieving project with ID: {ProjectId}", id);
            var result = await _mediator.Send(new GetProjectQuery { Id = id });
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpGet("user")]
        [Authorize(Roles = Roles.ProjectManager)]
        public async Task<ActionResult<Result<IReadOnlyList<ProjectDto>>>> GetUserProjects()
        {
            var userId = _currentUser.Id;
            _logger.LogInformation("Retrieving all projects for user: {userId}", userId);
            var result = await _mediator.Send(new GetProjectsForUserQuery { UserId = userId });
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
