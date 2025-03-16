using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Commands.Handlers;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Commands
{
    public class CreateProjectCommandHandlerTests
    {
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IValidator<CreateProjectCommand>> _validatorMock;
        private readonly CreateProjectCommandHandler _handler;

        public CreateProjectCommandHandlerTests()
        {
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _validatorMock = new Mock<IValidator<CreateProjectCommand>>();

            _handler = new CreateProjectCommandHandler(
                _projectRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object,
                _validatorMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldReturnSuccessResult()
        {
            var command = new CreateProjectCommand
            {
                Name = "Test Project",
                Description = "Test Description",
                UserId = "user123"
            };

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Description = command.Description,
                UserId = command.UserId
            };

            var projectDto = new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                UserId = project.UserId,
                UserName = "testuser"
            };

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.UserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            _mapperMock.Setup(x => x.Map<Project>(command))
                .Returns(project);

            _mapperMock.Setup(x => x.Map<ProjectDto>(project))
                .Returns(projectDto);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.UserId))
                .ReturnsAsync(new User { UserName = "testuser" });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(projectDto);

            _projectRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Project>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidRequest_ShouldReturnFailureResult()
        {
            var command = new CreateProjectCommand();
            var validationResult = new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name is required")
            });

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Name is required");

            _projectRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldReturnFailureResult()
        {
            var command = new CreateProjectCommand
            {
                Name = "Test Project",
                UserId = "user123"
            };

            var validationResult = new ValidationResult();
            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.UserId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to create projects");

            _projectRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Project>()), Times.Never);
        }
    }
}
