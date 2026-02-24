using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using TaskManagement.Auth.Features.Identity.Models;
using TaskManagement.Auth.Features.Users.Models;
using TaskManagement.Auth.Infrastructure.Common.Settings;
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UsersController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paged list of users with optional search.
        /// </summary>
        /// <param name="search">Optional search term applied to display name, email, or username.</param>
        /// <param name="isActive">Optional status filter.</param>
        /// <param name="role">Optional role filter.</param>
        /// <param name="page">Optional 1-based page number.</param>
        /// <param name="pageSize">Optional page size (max 100).</param>
        /// <param name="skip">Optional number of records to skip.</param>
        /// <param name="take">Optional number of records to take.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paged list of users.</returns>
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Roles = $"{Roles.Administrator},{Roles.ProjectManager}")]
        [EnableRateLimiting(RateLimitingPolicies.AdminUserManagement)]
        [HttpGet]
        [SwaggerOperation(
            Summary = "List users (admin/project manager)",
            Description = "Returns a paged list of users with optional filters. Project managers are restricted to role=User queries.")]
        [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<UserListResponse>> GetUsers(
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] string? role,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] int? skip,
            [FromQuery] int? take,
            CancellationToken cancellationToken)
        {
            var isAdmin = User.IsInRole(Roles.Administrator);
            if (!isAdmin)
            {
                // Project managers can only list assignable contributors.
                var requestedRole = role?.Trim();
                if (!string.Equals(requestedRole, Roles.User, StringComparison.Ordinal))
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

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

            if (!string.IsNullOrWhiteSpace(role))
            {
                var normalizedRole = role.Trim();
                if (!await _roleManager.RoleExistsAsync(normalizedRole))
                {
                    return ValidationProblem(
                        detail: $"Role '{normalizedRole}' does not exist.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var usersInRole = await _userManager.GetUsersInRoleAsync(normalizedRole);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            var total = await query.CountAsync(cancellationToken);
            var effectivePageSize = Math.Clamp(pageSize ?? take ?? DefaultPageSize, 1, MaxPageSize);
            var offset = page.HasValue
                ? Math.Max(page.Value - 1, 0) * effectivePageSize
                : Math.Max(skip ?? 0, 0);

            var users = await query
                .OrderBy(u => u.DisplayName ?? u.UserName ?? u.Email ?? string.Empty)
                .ThenBy(u => u.Id)
                .Skip(offset)
                .Take(effectivePageSize)
                .ToListAsync(cancellationToken);
            var items = new List<UserSummaryDto>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserSummaryDto
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsActive = IsUserActive(user, now),
                    Roles = roles.ToList()
                });
            }

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
        [SwaggerOperation(
            Summary = "Get user by id",
            Description = "Returns a basic user summary for authenticated service-to-service lookups.")]
        [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserSummaryDto>> GetUserById(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var now = DateTimeOffset.UtcNow;
            var user = await _userManager.Users
                .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserSummaryDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                UserName = user.UserName,
                Email = user.Email,
                IsActive = IsUserActive(user, now),
                Roles = roles.ToList()
            });
        }

        /// <summary>
        /// Retrieves detailed user information by ID for administrative user management.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detailed user information.</returns>
        [Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
            Roles = Roles.Administrator)]
        [EnableRateLimiting(RateLimitingPolicies.AdminUserManagement)]
        [HttpGet("{id}/details")]
        [SwaggerOperation(
            Summary = "Get user details (admin)",
            Description = "Returns admin-facing user details for user management screens.")]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<UserDetailsDto>> GetUserDetailsById(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var now = DateTimeOffset.UtcNow;
            var user = await _userManager.Users
                .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserDetailsDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                UserName = user.UserName,
                Email = user.Email,
                IsActive = IsUserActive(user, now),
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles.ToList()
            });
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
        [EnableRateLimiting(RateLimitingPolicies.AdminUserManagement)]
        [HttpPatch("{id}/status")]
        [SwaggerOperation(
            Summary = "Set user active status (admin)",
            Description = "Activates or deactivates a user account with admin safety guards.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

            var currentUserId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wasActive = IsUserActive(user, DateTimeOffset.UtcNow);
            if (!request.IsActive &&
                !string.IsNullOrWhiteSpace(currentUserId) &&
                string.Equals(currentUserId, id, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "AUDIT user-status-change blocked: self-deactivation. ActorUserId={ActorUserId}, TargetUserId={TargetUserId}",
                    currentUserId,
                    id);
                return ValidationProblem(
                    detail: "Administrators cannot deactivate their own account.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (!request.IsActive && await _userManager.IsInRoleAsync(user, Roles.Administrator))
            {
                var activeAdministrators = (await _userManager.GetUsersInRoleAsync(Roles.Administrator))
                    .Count(u => IsUserActive(u, DateTimeOffset.UtcNow));

                if (IsUserActive(user, DateTimeOffset.UtcNow) && activeAdministrators <= 1)
                {
                    _logger.LogWarning(
                        "AUDIT user-status-change blocked: last-active-admin protection. ActorUserId={ActorUserId}, TargetUserId={TargetUserId}",
                        currentUserId,
                        id);
                    return ValidationProblem(
                        detail: "You cannot deactivate the last active administrator account.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
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
                var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(
                    "AUDIT user-status-change failed. ActorUserId={ActorUserId}, TargetUserId={TargetUserId}, WasActive={WasActive}, RequestedIsActive={RequestedIsActive}, Errors={Errors}",
                    currentUserId,
                    id,
                    wasActive,
                    request.IsActive,
                    errorMessage);
                return ValidationProblem(
                    detail: errorMessage,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation(
                "AUDIT user-status-change succeeded. ActorUserId={ActorUserId}, TargetUserId={TargetUserId}, WasActive={WasActive}, IsActive={IsActive}",
                currentUserId,
                id,
                wasActive,
                request.IsActive);

            cancellationToken.ThrowIfCancellationRequested();
            return NoContent();
        }

        private static bool IsUserActive(ApplicationUser user, DateTimeOffset now)
            => !user.LockoutEnabled || !user.LockoutEnd.HasValue || user.LockoutEnd <= now;
    }
}
