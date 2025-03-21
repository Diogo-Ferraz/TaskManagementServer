using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Commands.Handlers;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Commands
{
    public class DeleteProjectCommandHandlerTests
    {
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IValidator<DeleteProjectCommand>> _validatorMock;
        private readonly DeleteProjectCommandHandler _handler;

        public DeleteProjectCommandHandlerTests()
        {
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userServiceMock = new Mock<IUserService>();
            _validatorMock = new Mock<IValidator<DeleteProjectCommand>>();

            _handler = new DeleteProjectCommandHandler(
                _projectRepositoryMock.Object,
                _userServiceMock.Object,
                _validatorMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldReturnSuccessResult()
        {
            var projectId = Guid.NewGuid();
            var projectName = "project123";

            var command = new DeleteProjectCommand
            {
                Id = projectId,
                UserId = "user123"
            };

            var existingProject = new Project
            {
                Id = projectId,
                Name = projectName,
                UserId = command.UserId
            };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(existingProject);

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.UserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
            _projectRepositoryMock.Verify(x => x.DeleteAsync(existingProject), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentProject_ShouldReturnFailureResult()
        {
            var command = new DeleteProjectCommand
            {
                Id = Guid.NewGuid(),
                UserId = "user123"
            };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync((Project?)null);

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project not found");
            _projectRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldReturnFailureResult()
        {
            var projectId = Guid.NewGuid();
            var projectName = "project123";

            var command = new DeleteProjectCommand
            {
                Id = projectId,
                UserId = "user123"
            };

            var existingProject = new Project
            {
                Id = projectId,
                Name = projectName,
                UserId = "differentUser"
            };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(existingProject);

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.UserId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to delete this project");
            _projectRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Project>()), Times.Never);
        }
    }
}
