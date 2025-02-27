using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Api.Application.TaskItems.Commands;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Application.TaskItems.Queries;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Infrastructure.Identity;
using TaskManagement.Api.Presentation.Controllers;

namespace TaskManagement.Api.Tests.UnitTests.Controllers
{
    public class TaskItemsControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ILogger<TaskItemsController>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly TaskItemsController _controller;

        public TaskItemsControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<TaskItemsController>>();
            _currentUserMock = new Mock<ICurrentUser>();

            _controller = new TaskItemsController(
                _mediatorMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object
            );
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsOkResult()
        {
            var taskId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var taskItemDto = new TaskItemDto
            {
                Id = taskId,
                Title = "Test Task",
                AssignedUserId = userId
            };

            _mediatorMock.Setup(x => x.Send(It.Is<GetTaskItemQuery>(q =>
                q.Id == taskId &&
                q.RequestingUserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TaskItemDto>.Success(taskItemDto));

            var result = await _controller.GetById(taskId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<TaskItemDto>().Subject;
            returnValue.Should().BeEquivalentTo(taskItemDto);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsProblemResult()
        {
            var taskId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var errorMessage = "Task not found";
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetTaskItemQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TaskItemDto>.Failure(errorMessage));

            var result = await _controller.GetById(taskId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task GetByUser_WithValidUserId_ReturnsOkResult()
        {
            var userId = "user123";
            var taskItems = new List<TaskItemDto>
            {
                new TaskItemDto { Id = Guid.NewGuid(), Title = "Task 1", AssignedUserId = userId },
                new TaskItemDto { Id = Guid.NewGuid(), Title = "Task 2", AssignedUserId = userId }
            };

            _mediatorMock.Setup(x => x.Send(It.Is<GetTasksForUserQuery>(q =>
                q.UserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<TaskItemDto>>.Success(taskItems));

            var result = await _controller.GetByUser(userId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IReadOnlyList<TaskItemDto>>().Subject;
            returnValue.Should().BeEquivalentTo(taskItems);
        }

        [Fact]
        public async Task GetByUser_WithFailureResult_ReturnsProblemResult()
        {
            var userId = "user123";
            var errorMessage = "Error retrieving tasks";

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetTasksForUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<TaskItemDto>>.Failure(errorMessage));

            var result = await _controller.GetByUser(userId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task GetByProject_WithValidProjectId_ReturnsOkResult()
        {
            var projectId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var taskItems = new List<TaskItemDto>
            {
                new TaskItemDto { Id = Guid.NewGuid(), Title = "Task 1", ProjectId = projectId },
                new TaskItemDto { Id = Guid.NewGuid(), Title = "Task 2", ProjectId = projectId }
            };

            _mediatorMock.Setup(x => x.Send(It.Is<GetTasksForProjectQuery>(q =>
                q.ProjectId == projectId &&
                q.RequestingUserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<TaskItemDto>>.Success(taskItems));

            var result = await _controller.GetByProject(projectId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IReadOnlyList<TaskItemDto>>().Subject;
            returnValue.Should().BeEquivalentTo(taskItems);
        }

        [Fact]
        public async Task GetByProject_WithFailureResult_ReturnsProblemResult()
        {
            var projectId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);
            var errorMessage = "Error retrieving tasks";

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetTasksForProjectQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<TaskItemDto>>.Failure(errorMessage));

            var result = await _controller.GetByProject(projectId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task Create_WithValidCommand_ReturnsCreatedResult()
        {
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var command = new CreateTaskItemCommand
            {
                Title = "New Task",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "user456"
            };

            var taskItemDto = new TaskItemDto
            {
                Id = Guid.NewGuid(),
                Title = command.Title,
                ProjectId = command.ProjectId,
                AssignedUserId = command.AssignedUserId
            };

            _mediatorMock.Setup(x => x.Send(It.Is<CreateTaskItemCommand>(c =>
                c.Title == command.Title &&
                c.RequestingUserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TaskItemDto>.Success(taskItemDto));

            var result = await _controller.Create(command);

            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(TaskItemsController.GetById));
            createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(taskItemDto.Id);
            var returnValue = createdResult.Value.Should().BeOfType<TaskItemDto>().Subject;
            returnValue.Should().BeEquivalentTo(taskItemDto);
        }

        [Fact]
        public async Task Create_WithFailureResult_ReturnsProblemResult()
        {
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var command = new CreateTaskItemCommand
            {
                Title = "New Task"
            };

            var errorMessage = "Error creating task";

            _mediatorMock.Setup(x => x.Send(It.IsAny<CreateTaskItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TaskItemDto>.Failure(errorMessage));

            var result = await _controller.Create(command);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task Update_WithValidCommand_ReturnsOkResult()
        {
            var taskId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var command = new UpdateTaskItemCommand
            {
                Id = taskId,
                Title = "Updated Task"
            };

            var taskItemDto = new TaskItemDto
            {
                Id = taskId,
                Title = command.Title
            };

            _mediatorMock.Setup(x => x.Send(It.Is<UpdateTaskItemCommand>(c =>
                c.Id == command.Id &&
                c.Title == command.Title &&
                c.RequestingUserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TaskItemDto>.Success(taskItemDto));

            var result = await _controller.Update(taskId, command);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<TaskItemDto>().Subject;
            returnValue.Should().BeEquivalentTo(taskItemDto);
        }

        [Fact]
        public async Task Update_WithMismatchedIds_ReturnsBadRequest()
        {
            var taskId = Guid.NewGuid();
            var differentId = Guid.NewGuid();
            var command = new UpdateTaskItemCommand { Id = differentId };

            var result = await _controller.Update(taskId, command);

            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task Update_WithFailureResult_ReturnsProblemResult()
        {
            var taskId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var command = new UpdateTaskItemCommand { Id = taskId };
            var errorMessage = "Error updating task";

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpdateTaskItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TaskItemDto>.Failure(errorMessage));

            var result = await _controller.Update(taskId, command);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }

        [Fact]
        public async Task Delete_WithValidId_ReturnsNoContentResult()
        {
            var taskId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            _mediatorMock.Setup(x => x.Send(It.Is<DeleteTaskItemCommand>(c =>
                c.Id == taskId &&
                c.RequestingUserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            var result = await _controller.Delete(taskId);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_WithFailureResult_ReturnsProblemResult()
        {
            var taskId = Guid.NewGuid();
            var userId = "user123";
            _currentUserMock.Setup(x => x.Id).Returns(userId);

            var errorMessage = "Error deleting task";

            _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteTaskItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure(errorMessage));

            var result = await _controller.Delete(taskId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Detail.Should().Be(errorMessage);
        }
    }
}