using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Projects.Services.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Projects.Queries.Handlers
{
    public class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, IReadOnlyList<ProjectMemberDto>>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectMembershipService _projectMembershipService;
        private readonly IUserDirectoryService _userDirectoryService;

        public GetProjectMembersQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IProjectMembershipService projectMembershipService,
            IUserDirectoryService userDirectoryService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _projectMembershipService = projectMembershipService;
            _userDirectoryService = userDirectoryService;
        }

        public async Task<IReadOnlyList<ProjectMemberDto>> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var project = await _dbContext.Projects
                .AsNoTracking()
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (project == null)
            {
                throw new NotFoundException($"Project with ID {request.ProjectId} not found.");
            }

            if (!_currentUserService.IsInRole(Roles.Administrator))
            {
                var isMember = await _projectMembershipService.IsMemberAsync(project.Id, currentUserId, cancellationToken);
                if (!isMember)
                {
                    throw new ForbiddenAccessException("User is not authorized to view members for this project.");
                }
            }

            var members = project.Members
                .Select(m => m.UserId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            members.Add(project.OwnerUserId);

            var displayNameEntries = await Task.WhenAll(
                members.Select(async userId => new
                {
                    UserId = userId,
                    DisplayName = await _userDirectoryService.GetDisplayNameAsync(userId, cancellationToken)
                }));

            var displayNames = displayNameEntries.ToDictionary(
                entry => entry.UserId,
                entry => entry.DisplayName,
                StringComparer.OrdinalIgnoreCase);

            var result = members
                .Select(userId => new ProjectMemberDto
                {
                    UserId = userId,
                    DisplayName = displayNames[userId] ?? userId,
                    IsOwner = string.Equals(userId, project.OwnerUserId, StringComparison.OrdinalIgnoreCase)
                })
                .OrderByDescending(member => member.IsOwner)
                .ThenBy(member => member.DisplayName)
                .ToList();

            return result;
        }
    }
}
