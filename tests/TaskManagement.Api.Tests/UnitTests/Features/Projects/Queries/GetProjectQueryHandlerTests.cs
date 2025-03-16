using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Projects.Queries;
using TaskManagement.Api.Features.Projects.Queries.Handlers;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Queries
{
    public class GetProjectQueryHandlerTests
    {
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetProjectQueryHandler _handler;

        public GetProjectQueryHandlerTests()
        {
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _mapperMock = new Mock<IMapper>();

            _handler = new GetProjectQueryHandler(
                _projectRepositoryMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithExistingProject_ShouldReturnSuccessResult()
        {
            var projectId = Guid.NewGuid();
            var query = new GetProjectQuery { Id = projectId };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project",
                UserId = "user123"
            };

            var projectDto = new ProjectDto
            {
                Id = projectId,
                Name = "Test Project",
                UserId = "user123",
                UserName = "testuser"
            };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _mapperMock.Setup(x => x.Map<ProjectDto>(project))
                .Returns(projectDto);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(projectDto);
        }

        [Fact]
        public async Task Handle_WithNonExistentProject_ShouldReturnFailureResult()
        {
            var query = new GetProjectQuery { Id = Guid.NewGuid() };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(query.Id))
                .ReturnsAsync((Project)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project not found");
        }
    }
}
