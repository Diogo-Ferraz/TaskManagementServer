using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Commands.Handlers;
using TaskManagement.Api.Features.Projects.Mappings;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Commands
{
    public class CreateProjectCommandHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly CreateProjectCommandHandler _handler;

        private readonly string _testUserId = "user-creator-123";

        public CreateProjectCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_CreateProject_{Guid.NewGuid()}")
                .Options;

            _mockCurrentUser = new Mock<ICurrentUserService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<ProjectMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            _handler = new CreateProjectCommandHandler(_dbContext, _mockCurrentUser.Object, _mapper);
        }

        [Fact]
        public async Task Handle_ShouldCreateProjectAndAddOwnerAsMember_WhenRequestIsValidAndUserIsAuthenticated()
        {
            // Arrange
            var command = new CreateProjectCommand { Name = "New Valid Project", Description = "Desc" };
            _mockCurrentUser.Setup(u => u.Id).Returns(_testUserId);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Name.Should().Be(command.Name);
            resultDto.Description.Should().Be(command.Description);
            resultDto.OwnerUserId.Should().Be(_testUserId);
            resultDto.Id.Should().NotBeEmpty();

            var createdProject = await _dbContext.Projects.FindAsync(resultDto.Id);
            createdProject.Should().NotBeNull();
            createdProject!.Name.Should().Be(command.Name);
            createdProject.OwnerUserId.Should().Be(_testUserId);
            createdProject.CreatedByUserId.Should().Be(_testUserId);
            createdProject.LastModifiedByUserId.Should().Be(_testUserId);
            createdProject.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            createdProject.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));


            var createdMember = await _dbContext.ProjectMembers
                                      .FirstOrDefaultAsync(pm => pm.ProjectId == resultDto.Id && pm.UserId == _testUserId);
            createdMember.Should().NotBeNull();

            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var command = new CreateProjectCommand { Name = "New Project" };
            _mockCurrentUser.Setup(u => u.Id).Returns((string?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once); // Verify check was made
        }

        // Add test case for ValidationException if NOT relying solely on pipeline behavior
        // [Fact]
        // public async Task Handle_ShouldThrowValidationException_WhenRequestIsInvalid() { ... }

        public void Dispose()
        {
            _dbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}