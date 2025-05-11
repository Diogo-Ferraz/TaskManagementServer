using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Features.Projects.Queries.Handlers
{
    public class GetProjectsForUserQueryHandler : IRequestHandler<GetProjectsForUserQuery, IReadOnlyList<ProjectDto>>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetProjectsForUserQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsForUserQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var projectDtos = await _dbContext.Projects
                .Where(p => p.OwnerUserId == currentUserId || p.Members.Any(m => m.UserId == currentUserId))
                .OrderBy(p => p.Name)
                .ProjectTo<ProjectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return projectDtos;
        }
    }
}
