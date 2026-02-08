using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Auth.Features.Identity.Models;
using TaskManagement.Auth.Features.Users.Models;

namespace TaskManagement.Auth.Features.Users
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private const int DefaultPageSize = 25;
        private const int MaxPageSize = 100;

        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
            => _userManager = userManager;

        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet]
        public async Task<ActionResult<UserListResponse>> GetUsers(
            [FromQuery] string? search,
            [FromQuery] int? skip,
            [FromQuery] int? take,
            CancellationToken cancellationToken)
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    (u.DisplayName ?? string.Empty).Contains(term) ||
                    (u.Email ?? string.Empty).Contains(term) ||
                    (u.UserName ?? string.Empty).Contains(term));
            }

            var total = await query.CountAsync(cancellationToken);
            var pageSize = Math.Clamp(take ?? DefaultPageSize, 1, MaxPageSize);
            var offset = Math.Max(skip ?? 0, 0);

            var items = await query
                .OrderBy(u => u.DisplayName ?? u.UserName ?? u.Email ?? string.Empty)
                .ThenBy(u => u.Id)
                .Skip(offset)
                .Take(pageSize)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    UserName = u.UserName,
                    Email = u.Email
                })
                .ToListAsync(cancellationToken);

            return Ok(new UserListResponse
            {
                Total = total,
                Skip = offset,
                Take = pageSize,
                Items = items
            });
        }

        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserSummaryDto>> GetUserById(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user = await _userManager.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    UserName = u.UserName,
                    Email = u.Email
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        }
    }
}
