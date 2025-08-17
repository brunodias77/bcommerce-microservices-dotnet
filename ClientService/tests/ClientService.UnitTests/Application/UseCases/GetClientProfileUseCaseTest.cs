using ClientService.Application.Common;
using ClientService.Application.UseCases.Clients.GetProfile;
using ClientService.Domain.Aggregates;
using ClientService.Domain.Services;
using ClientService.Domain.Validations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClientService.UnitTests.Application.UseCases;

public class GetClientProfileUseCaseTest
{
    private readonly Mock<ILoggedUser> _loggedUserMock;
    private readonly Mock<ILogger<GetClientProfileUseCase>> _loggerMock;
    private readonly GetClientProfileUseCase _useCase;

    public GetClientProfileUseCaseTest()
    {
        _loggedUserMock = new Mock<ILoggedUser>();
        _loggerMock = new Mock<ILogger<GetClientProfileUseCase>>();
        _useCase = new GetClientProfileUseCase(_loggedUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Execute_ComUsuarioLogado_DeveRetornarPerfilComSucesso()
    {
        // Arrange
        var client = Client.Create(
            Guid.NewGuid(),
            "João",
            "Silva",
            "joao@email.com",
            "hashedPassword",
            "USER");

        _loggedUserMock.Setup(x => x.User()).Returns(Task.FromResult<Client?>(client));

        // Act
        var result = await _useCase.Execute();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(client.Id);
        result.Value.FirstName.Should().Be("João");
        result.Value.LastName.Should().Be("Silva");
        result.Value.Email.Should().Be("joao@email.com");
    }

    [Fact]
    public async Task Execute_ComUsuarioNaoEncontrado_DeveRetornarErro()
    {
        // Arrange
        _loggedUserMock.Setup(x => x.User()).Returns(Task.FromResult<Client?>(null));

        // Act
        var result = await _useCase.Execute();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.HasErrors.Should().BeTrue();
        result.Error.Errors.Should().Contain(e => e.Code == "client.notFound");
    }
}