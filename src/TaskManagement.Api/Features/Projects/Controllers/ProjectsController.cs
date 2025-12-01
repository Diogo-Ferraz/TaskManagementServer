using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Projects.Queries;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Projects.Controllers
{
    /// <summary>
    /// API controller for managing projects.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProjectsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectsController"/> class.
        /// </summary>
        public ProjectsController(IMediator mediator, ILogger<ProjectsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new project.
        /// </summary>
        /// <param name="command">The project creation command.</param>
        /// <returns>The created project.</returns>
        /// <response code="201">Project created successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        [HttpPost]
        [Authorize(Policy = Policies.CanManageProjects)]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateProjectCommand command)
        {
            _logger.LogInformation("API: Creating new project with name: {ProjectName}", command.Name);
            var createdProjectDto = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = createdProjectDto.Id }, createdProjectDto);
        }

        /// <summary>
        /// Updates an existing project.
        /// </summary>
        /// <param name="id">The ID of the project to update.</param>
        /// <param name="command">The project update command.</param>
        /// <returns>The updated project.</returns>
        /// <response code="200">Project updated successfully.</response>
        /// <response code="400">Invalid request data or ID mismatch.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Project not found.</response>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = Policies.CanManageProjects)]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectCommand command)
        {
            _logger.LogInformation("API: Updating project with ID: {ProjectId}", id);
            command.Id = id;
            var updatedProjectDto = await _mediator.Send(command);
            return Ok(updatedProjectDto);
        }

        /// <summary>
        /// Deletes a project by ID.
        /// </summary>
        /// <param name="id">The ID of the project to delete.</param>
        /// <returns>No content.</returns>
        /// <response code="204">Project deleted successfully.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Project not found.</response>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.CanManageProjects)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("API: Deleting project with ID: {ProjectId}", id);
            await _mediator.Send(new DeleteProjectCommand { Id = id });
            return NoContent();
        }

        /// <summary>
        /// Retrieves a project by ID.
        /// </summary>
        /// <param name="id">The ID of the project to retrieve.</param>
        /// <returns>The project details.</returns>
        /// <response code="200">Project found.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Project not found.</response>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = Policies.CanManageProjects)]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("API: Retrieving project with ID: {ProjectId}", id);
            var projectDto = await _mediator.Send(new GetProjectQuery { Id = id });
            return Ok(projectDto);
        }

        /// <summary>
        /// Retrieves the list of projects for the current user.
        /// </summary>
        /// <returns>List of projects owned by the current user.</returns>
        /// <response code="200">Projects retrieved successfully.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpGet("my-projects")]
        [Authorize(Policy = Policies.CanViewOwnProjects)]
        [ProducesResponseType(typeof(IReadOnlyList<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProjects()
        {
            _logger.LogInformation("API: Retrieving projects for current user.");
            var projectDtos = await _mediator.Send(new GetProjectsForUserQuery());
            return Ok(projectDtos);
        }
    }
}
