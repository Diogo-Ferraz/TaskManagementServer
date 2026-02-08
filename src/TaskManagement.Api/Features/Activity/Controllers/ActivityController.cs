using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Features.Activity.Models.DTOs;
using TaskManagement.Api.Features.Activity.Queries;

namespace TaskManagement.Api.Features.Activity.Controllers
{
    /// <summary>
    /// API controller for retrieving activity feed entries.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class ActivityController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ActivityController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityController"/> class.
        /// </summary>
        public ActivityController(IMediator mediator, ILogger<ActivityController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves activity feed entries for the current user (optionally filtered by project).
        /// </summary>
        /// <param name="projectId">Optional project ID filter.</param>
        /// <param name="limit">Maximum number of entries to return.</param>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<ActivityLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get([FromQuery] Guid? projectId, [FromQuery] int limit = 50)
        {
            _logger.LogInformation("Retrieving activity feed. ProjectId: {ProjectId}, Limit: {Limit}", projectId, limit);

            var result = await _mediator.Send(new GetActivityFeedQuery
            {
                ProjectId = projectId,
                Limit = limit
            });

            return Ok(result);
        }
    }
}
