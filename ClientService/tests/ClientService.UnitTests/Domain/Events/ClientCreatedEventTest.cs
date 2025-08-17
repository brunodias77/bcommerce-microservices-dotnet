using ClientService.Domain.Events.Clients;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain.Events;

public class ClientCreatedEventTest
{
    [Fact]
    public void DeveCriarEventoClienteCriadoComSucesso()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var email = "teste@email.com";
        var firstName = "João";
        var lastName = "Silva";

        // Act
        var evento = new ClientCreatedEvent(clientId, email, firstName, lastName);

        // Assert
        evento.Should().NotBeNull();
        evento.ClientId.Should().Be(clientId);
        evento.Email.Should().Be(email);
        evento.FirstName.Should().Be(firstName);
        evento.LastName.Should().Be(lastName);
        evento.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DevePermitirCriacaoComEmailVazio()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var email = "";
        var firstName = "João";
        var lastName = "Silva";

        // Act
        var evento = new ClientCreatedEvent(clientId, email, firstName, lastName);

        // Assert
        evento.Should().NotBeNull();
        evento.Email.Should().Be(email);
    }

    [Fact]
    public void DevePermitirCriacaoComNomesVazios()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var email = "teste@email.com";
        var firstName = "";
        var lastName = "";

        // Act
        var evento = new ClientCreatedEvent(clientId, email, firstName, lastName);

        // Assert
        evento.Should().NotBeNull();
        evento.FirstName.Should().Be(firstName);
        evento.LastName.Should().Be(lastName);
    }

    [Fact]
    public void DevePermitirClientIdVazio()
    {
        // Arrange
        var clientId = Guid.Empty;
        var email = "teste@email.com";
        var firstName = "João";
        var lastName = "Silva";

        // Act
        var evento = new ClientCreatedEvent(clientId, email, firstName, lastName);

        // Assert
        evento.Should().NotBeNull();
        evento.ClientId.Should().Be(Guid.Empty);
    }
}