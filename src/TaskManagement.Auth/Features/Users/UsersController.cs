using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Auth.Features.Identity.Models;
using TaskManagement.Auth.Features.Users.Models;
using TaskManagement.Shared.Models;

namespace TaskManagement.Auth.Features.Users
{
    /// <summary>
    /// API controller for querying users.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private const int DefaultPageSize = 25;
        private const int MaxPageSize = 100;

        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        public UsersController(UserManager<ApplicationUser> userManager)
            => _userManager = userManager;

        /// <summary>
        /// Retrieves a paged list of users with optional search.
        /// </summary>
        /// <param name="search">Optional search term applied to display name, email, or username.</param>
        /// <param name="isActive">Optional status filter.</param>
        /// <param name="page">Optional 1-based page number.</param>
        /// <param name="pageSize">Optional page size (max 100).</param>
        /// <param name="skip">Optional number of records to skip.</param>
        /// <param name="take">Optional number of records to take.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paged list of users.</returns>
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet]
        public async Task<ActionResult<UserListResponse>> GetUsers(
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] int? skip,
            [FromQuery] int? take,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    (u.DisplayName ?? string.Empty).Contains(term) ||
                    (u.Email ?? string.Empty).Contains(term) ||
                    (u.UserName ?? string.Empty).Contains(term));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u =>
                    (!u.LockoutEnabled || !u.LockoutEnd.HasValue || u.LockoutEnd <= now) == isActive.Value);
            }

            var total = await query.CountAsync(cancellationToken);
            var effectivePageSize = Math.Clamp(pageSize ?? take ?? DefaultPageSize, 1, MaxPageSize);
            var offset = page.HasValue
                ? Math.Max(page.Value - 1, 0) * effectivePageSize
                : Math.Max(skip ?? 0, 0);

            var items = await query
                .OrderBy(u => u.DisplayName ?? u.UserName ?? u.Email ?? string.Empty)
                .ThenBy(u => u.Id)
                .Skip(offset)
                .Take(effectivePageSize)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    UserName = u.UserName,
                    Email = u.Email,
                    IsActive = !u.LockoutEnabled || !u.LockoutEnd.HasValue || u.LockoutEnd <= now
                })
                .ToListAsync(cancellationToken);

            return Ok(new UserListResponse
            {
                Total = total,
                Skip = offset,
                Take = effectivePageSize,
                Items = items
            });
        }

        /// <summary>
        /// Retrieves a user by ID.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user summary.</returns>
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserSummaryDto>> GetUserById(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var now = DateTimeOffset.UtcNow;
            var user = await _userManager.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    UserName = u.UserName,
                    Email = u.Email,
                    IsActive = !u.LockoutEnabled || !u.LockoutEnd.HasValue || u.LockoutEnd <= now
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        /// <summary>
        /// Activates or deactivates a user account.
        /// </summary>
        /// <param name="id">The target user ID.</param>
        /// <param name="request">Desired account status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Roles = Roles.Administrator)]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> SetUserStatus(
            string id,
            [FromBody] SetUserStatusRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            if (request.IsActive)
            {
                user.LockoutEnabled = false;
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
            }
            else
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ValidationProblem(
                    detail: string.Join("; ", result.Errors.Select(e => e.Description)),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return NoContent();
        }
    }
}
