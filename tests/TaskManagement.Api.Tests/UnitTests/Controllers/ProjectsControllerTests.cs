using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Controllers;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Projects.Queries;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Tests.UnitTests.Controllers
{
    public class ProjectsControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ILogger<ProjectsController>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly ProjectsController _controller;

        public ProjectsControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<ProjectsController>>();
            _currentUserMock = new Mock<ICurrentUserService>();

            _controller = new ProjectsController(
                _mediatorMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object
            );
        }

        [Fact]
        public async Task Create_WithValidCommand_ReturnsCreatedAtActionResult()
        {
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var command = new CreateProjectCommand
            {
                Name = "Test Project",
                Description = "Test Description"
            };

            var projectDto = new ProjectDto
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Description = command.Description,
                UserId = userId
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<CreateProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto>.Success(projectDto));

            var result = await _controller.Create(command);

            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnValue = createdResult.Value.Should().BeOfType<ProjectDto>().Subject;
            returnValue.Should().BeEquivalentTo(projectDto);
            createdResult.ActionName.Should().Be(nameof(ProjectsController.GetById));
            createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(projectDto.Id);
        }

        [Fact]
        public async Task Create_WithInvalidCommand_ReturnsProblemResult()
        {
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var command = new CreateProjectCommand();
            var errorMessage = "Name is required";

            _mediatorMock.Setup(x => x.Send(It.IsAny<CreateProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto>.Failure(errorMessage));

            var result = await _controller.Create(command);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task Update_WithValidCommand_ReturnsOkResult()
        {
            var userId = "user123";
            var projectId = Guid.NewGuid();
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var command = new UpdateProjectCommand
            {
                Id = projectId,
                Name = "Updated Project",
                Description = "Updated Description"
            };

            var projectDto = new ProjectDto
            {
                Id = projectId,
                Name = command.Name,
                Description = command.Description,
                UserId = userId
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpdateProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto>.Success(projectDto));

            var result = await _controller.Update(projectId, command);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
            returnValue.Should().BeEquivalentTo(projectDto);
        }

        [Fact]
        public async Task Update_WithMismatchedIds_ReturnsBadRequest()
        {
            var projectId = Guid.NewGuid();
            var differentId = Guid.NewGuid();
            var command = new UpdateProjectCommand
            {
                Id = differentId,
                Name = "Updated Project"
            };

            var result = await _controller.Update(projectId, command);

            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task Update_WithInvalidCommand_ReturnsProblemResult()
        {
            var projectId = Guid.NewGuid();
            var command = new UpdateProjectCommand
            {
                Id = projectId,
                Name = "Updated Project"
            };
            var errorMessage = "Project not found";

            _currentUserMock.Setup(x => x.Id).Returns("user123");

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpdateProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto>.Failure(errorMessage));

            var result = await _controller.Update(projectId, command);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task Delete_WithValidId_ReturnsOkResult()
        {
            var projectId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            var result = await _controller.Delete(projectId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeOfType<bool>().Subject.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WithInvalidId_ReturnsProblemResult()
        {
            var projectId = Guid.NewGuid();
            var errorMessage = "Project not found";

            _currentUserMock.Setup(x => x.Id).Returns("user123");

            _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteProjectCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure(errorMessage));

            var result = await _controller.Delete(projectId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsOkResult()
        {
            var projectId = Guid.NewGuid();
            var projectDto = new ProjectDto
            {
                Id = projectId,
                Name = "Test Project",
                Description = "Test Description",
                UserId = "user123"
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetProjectQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto>.Success(projectDto));

            var result = await _controller.GetById(projectId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
            returnValue.Should().BeEquivalentTo(projectDto);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsProblemResult()
        {
            var projectId = Guid.NewGuid();
            var errorMessage = "Project not found";

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetProjectQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto>.Failure(errorMessage));

            var result = await _controller.GetById(projectId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task GetUserProjects_ReturnsOkResult()
        {
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var projectDtos = new List<ProjectDto>
            {
                new ProjectDto { Id = Guid.NewGuid(), Name = "Project 1", UserId = userId },
                new ProjectDto { Id = Guid.NewGuid(), Name = "Project 2", UserId = userId }
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetProjectsForUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectDto>>.Success(projectDtos));

            var result = await _controller.GetUserProjects();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>()
                .Which.Should().BeEquivalentTo(projectDtos);
        }

        [Fact]
        public async Task GetUserProjects_WithError_ReturnsProblemResult()
        {
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);
            var errorMessage = "Error retrieving projects";

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetProjectsForUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectDto>>.Failure(errorMessage));

            var result = await _controller.GetUserProjects();

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }
    }
}