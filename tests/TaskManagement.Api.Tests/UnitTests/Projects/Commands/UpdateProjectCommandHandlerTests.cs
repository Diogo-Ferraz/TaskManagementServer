using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Projects.Commands;
using TaskManagement.Api.Application.Projects.Commands.Handlers;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Entities;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Projects.Commands
{
    public class UpdateProjectCommandHandlerTests
    {
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IValidator<UpdateProjectCommand>> _validatorMock;
        private readonly UpdateProjectCommandHandler _handler;

        public UpdateProjectCommandHandlerTests()
        {
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _validatorMock = new Mock<IValidator<UpdateProjectCommand>>();

            _handler = new UpdateProjectCommandHandler(
                _projectRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object,
                _validatorMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldReturnSuccessResult()
        {
            var projectId = Guid.NewGuid();
            var command = new UpdateProjectCommand
            {
                Id = projectId,
                Name = "Updated Project",
                Description = "Updated Description",
                UserId = "user123"
            };

            var existingProject = new Project
            {
                Id = projectId,
                Name = "Old Name",
                Description = "Old Description",
                UserId = command.UserId
            };

            var updatedProjectDto = new ProjectDto
            {
                Id = projectId,
                Name = command.Name,
                Description = command.Description,
                UserId = command.UserId,
                UserName = "testuser"
            };

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(existingProject);

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.UserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            _mapperMock.Setup(x => x.Map<ProjectDto>(It.IsAny<Project>()))
                .Returns(updatedProjectDto);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.UserId))
                .ReturnsAsync(new User { UserName = "testuser" });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(updatedProjectDto);
            _projectRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Project>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentProject_ShouldReturnFailureResult()
        {
            var command = new UpdateProjectCommand { Id = Guid.NewGuid() };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync((Project)null);

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project not found");
            _projectRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldReturnFailureResult()
        {
            var projectId = Guid.NewGuid();
            var projectName = "project123";

            var command = new UpdateProjectCommand
            {
                Id = projectId,
                Name = projectName,
                UserId = "user123"
            };

            var existingProject = new Project
            {
                Id = projectId,
                Name = projectName,
                UserId = "differentUser"
            };

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(existingProject);

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.UserId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to update projects");
            _projectRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidUser_ShouldReturnFailureResult()
        {
            var projectId = Guid.NewGuid();
            var projectName = "project123";

            var command = new UpdateProjectCommand
            {
                Id = projectId,
                Name = projectName,
                UserId = "user123"
            };

            var existingProject = new Project
            {
                Id = projectId,
                Name = projectName,
                UserId = "differentUser"
            };

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(existingProject);

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.UserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to update this project");
            _projectRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }
    }
}
