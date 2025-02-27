using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Application.Projects.Queries;
using TaskManagement.Api.Application.Projects.Queries.Handlers;
using TaskManagement.Api.Domain.Entities;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Projects.Queries
{
    public class GetProjectsForUserQueryHandlerTests
    {
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetProjectsForAdminQueryHandler _handler;

        public GetProjectsForUserQueryHandlerTests()
        {
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();

            _handler = new GetProjectsForAdminQueryHandler(
                _projectRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnUserProjects()
        {
            var userId = "user123";
            var query = new GetProjectsForUserQuery { UserId = userId };

            var projects = new List<Project>
            {
                new Project { Id = Guid.NewGuid(), Name = "Project 1", UserId = userId },
                new Project { Id = Guid.NewGuid(), Name = "Project 2", UserId = userId }
            };

            var projectDtos = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                UserId = p.UserId,
                UserName = "testuser"
            }).ToList();

            _projectRepositoryMock.Setup(x => x.GetProjectsByUserIdAsync(userId))
                .ReturnsAsync(projects);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<ProjectDto>>(projects))
                .Returns(projectDtos);

            _userServiceMock.Setup(x => x.IsInRoleAsync(query.UserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(projectDtos);
        }

        [Fact]
        public async Task Handle_EmptyProjectsList_ShouldReturnEmptyCollection()
        {
            var userId = "user123";
            var query = new GetProjectsForUserQuery { UserId = userId };

            var emptyProjects = new List<Project>();
            var emptyDtos = new List<ProjectDto>();

            _projectRepositoryMock.Setup(x => x.GetProjectsByUserIdAsync(userId))
                .ReturnsAsync(emptyProjects);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<ProjectDto>>(emptyProjects))
                .Returns(emptyDtos);

            _userServiceMock.Setup(x => x.IsInRoleAsync(query.UserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_NullUserId_ShouldStillCheckAuthorization()
        {
            string userId = null;
            var query = new GetProjectsForUserQuery { UserId = userId };

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to view projects");
            _userServiceMock.Verify(x => x.IsInRoleAsync(userId, Roles.ProjectManager), Times.Once);
        }
    }
}
