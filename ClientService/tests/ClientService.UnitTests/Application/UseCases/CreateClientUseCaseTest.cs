using ClientService.Application.Common;
using ClientService.Application.Services;
using ClientService.Application.UseCases.Clients.Create;
using ClientService.Domain.Aggregates;
using ClientService.Domain.Common;
using ClientService.Domain.Repositories;
using ClientService.Domain.Services;
using ClientService.Domain.Validations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClientService.UnitTests.Application.UseCases;

public class CreateClientUseCaseTest
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IKeycloakService> _keycloakServiceMock;
    private readonly Mock<IDomainEventPublisher> _domainEventPublisherMock;
    private readonly Mock<ILogger<CreateClientUseCase>> _loggerMock;
    private readonly CreateClientUseCase _useCase;

    public CreateClientUseCaseTest()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _keycloakServiceMock = new Mock<IKeycloakService>();
        _domainEventPublisherMock = new Mock<IDomainEventPublisher>();
        _loggerMock = new Mock<ILogger<CreateClientUseCase>>();
        
        _useCase = new CreateClientUseCase(
            _clientRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _keycloakServiceMock.Object,
            _domainEventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Execute_ComDadosValidos_DeveCriarClienteComSucesso()
    {
        // Arrange
        var input = new CreateClientInput(
            Username: "joaosilva",
            Email: "joao@email.com",
            FirstName: "João",
            LastName: "Silva",
            Password: "senha123");

        var keycloakResult = new KeycloakCreateUserResult(
            UserId: "keycloak-user-id",
            RoleAssigned: true);

        _keycloakServiceMock
            .Setup(x => x.CreateUserWithRoleAsync(It.IsAny<CreateKeycloakUserRequest>()))
            .ReturnsAsync(Result<KeycloakCreateUserResult, Notification>.Ok(keycloakResult));
        
        _clientRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<ClientService.Domain.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _useCase.Execute(input);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Message.Should().Be("Cliente criado com sucesso");
        result.Value.Email.Should().Be(input.Email.ToLowerInvariant());
        
        _clientRepositoryMock.Verify(x => x.Insert(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ComEmailExistente_DeveRetornarErro()
    {
        // Arrange
        var input = new CreateClientInput(
            Username: "joaosilva",
            Email: "joao@email.com",
            FirstName: "João",
            LastName: "Silva",
            Password: "senha123");

        _clientRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<ClientService.Domain.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Execute(input);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.HasErrors.Should().BeTrue();
        result.Error.Errors.Should().Contain(e => e.Code == "client.email.alreadyExists");
    }

    [Fact]
    public async Task Execute_ComDadosInvalidos_DeveRetornarErro()
    {
        // Arrange
        var input = new CreateClientInput(
            Username: "",
            Email: "",
            FirstName: "",
            LastName: "",
            Password: "");

        // Act
        var result = await _useCase.Execute(input);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.HasErrors.Should().BeTrue();
        result.Error.Errors.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Execute_ComFalhaNoKeycloak_DeveRetornarErro()
    {
        // Arrange
        var input = new CreateClientInput(
            Username: "joaosilva",
            Email: "joao@email.com",
            FirstName: "João",
            LastName: "Silva",
            Password: "senha123");

        var notification = new Notification();
        notification.Add(new Error("keycloak.error", "Erro no Keycloak"));

        _clientRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<ClientService.Domain.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _keycloakServiceMock.Setup(x => x.CreateUserWithRoleAsync(It.IsAny<CreateKeycloakUserRequest>()))
            .ReturnsAsync(Result<KeycloakCreateUserResult, Notification>.Fail(notification));

        // Act
        var result = await _useCase.Execute(input);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.HasErrors.Should().BeTrue();
    }
}