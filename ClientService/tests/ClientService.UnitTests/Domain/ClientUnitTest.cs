using ClientService.Domain.Aggregates;
using ClientService.Domain.Enums;
using ClientService.Domain.Events.Clients;
using ClientService.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain;

public class ClientUnitTest
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarClienteComSucesso()
    {
        // Arrange
        var keycloakUserId = Guid.NewGuid();
        var firstName = "João";
        var lastName = "Silva";
        var email = "joao.silva@email.com";
        var passwordHash = "hashedPassword123";
        var role = "USER";

        // Act
        var client = Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            email,
            passwordHash,
            role);

        // Assert
        client.Should().NotBeNull();
        client.Id.Should().NotBeEmpty();
        client.KeycloakUserId.Should().Be(keycloakUserId);
        client.FirstName.Should().Be(firstName);
        client.LastName.Should().Be(lastName);
        client.Email.Value.Should().Be(email.ToLowerInvariant());
        client.PasswordHash.Should().Be(passwordHash);
        client.Role.Should().Be(UserRole.USER);
        client.Status.Should().Be(ClientStatus.Ativo);
        client.NewsletterOptIn.Should().BeFalse();
        client.FailedLoginAttempts.Should().Be(0);
        client.Version.Should().Be(1);
        client.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Criar_ComDadosValidos_DeveDispararEventoClienteCriado()
    {
        // Arrange
        var keycloakUserId = Guid.NewGuid();
        var firstName = "Maria";
        var lastName = "Santos";
        var email = "maria.santos@email.com";
        var passwordHash = "hashedPassword456";
        var role = "USER";

        // Act
        var client = Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            email,
            passwordHash,
            role);

        // Assert
        client.HasEvents.Should().BeTrue();
        client.Events.Should().HaveCount(1);
        
        var domainEvent = client.Events.First();
        domainEvent.Should().BeOfType<ClientCreatedEvent>();
        
        var clientCreatedEvent = (ClientCreatedEvent)domainEvent;
        clientCreatedEvent.ClientId.Should().Be(client.Id);
        clientCreatedEvent.Email.Should().Be(email.ToLowerInvariant());
        clientCreatedEvent.FirstName.Should().Be(firstName);
        clientCreatedEvent.LastName.Should().Be(lastName);
    }

    [Theory]
    [InlineData("USER")]
    [InlineData("ADMIN")]
    [InlineData("user")]
    [InlineData("admin")]
    public void Criar_ComPerfilValido_DeveAnalisarPerfilCorretamente(string role)
    {
        // Arrange
        var keycloakUserId = Guid.NewGuid();
        var firstName = "Test";
        var lastName = "User";
        var email = "test@email.com";
        var passwordHash = "hashedPassword";

        // Act
        var client = Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            email,
            passwordHash,
            role);

        // Assert
        var expectedRole = Enum.Parse<UserRole>(role.ToUpper());
        client.Role.Should().Be(expectedRole);
    }

    [Fact]
    public void Criar_ComPerfilInvalido_DeveLancarArgumentException()
    {
        // Arrange
        var keycloakUserId = Guid.NewGuid();
        var firstName = "Test";
        var lastName = "User";
        var email = "test@email.com";
        var passwordHash = "hashedPassword";
        var invalidRole = "PERFIL_INVALIDO";

        // Act & Assert
        var act = () => Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            email,
            passwordHash,
            invalidRole);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Criar_ComEmailInvalido_DeveLancarArgumentException()
    {
        // Arrange
        var keycloakUserId = Guid.NewGuid();
        var firstName = "Test";
        var lastName = "User";
        var invalidEmail = "email-invalido";
        var passwordHash = "hashedPassword";
        var role = "USER";

        // Act & Assert
        var act = () => Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            invalidEmail,
            passwordHash,
            role);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComPrimeiroNomeInvalido_DeveCriarComPrimeiroNomeVazio(string firstName)
    {
        // Arrange
        var keycloakUserId = Guid.NewGuid();
        var lastName = "User";
        var email = "test@email.com";
        var passwordHash = "hashedPassword";
        var role = "USER";

        // Act
        var client = Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            email,
            passwordHash,
            role);

        // Assert
        client.FirstName.Should().Be(firstName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComSobrenomeInvalido_DeveCriarComSobrenomeVazio(string lastName)
    {
        // Arrange
        var keycloakUserId = Guid.NewGuid();
        var firstName = "Test";
        var email = "test@email.com";
        var passwordHash = "hashedPassword";
        var role = "USER";

        // Act
        var client = Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            email,
            passwordHash,
            role);

        // Assert
        client.LastName.Should().Be(lastName);
    }

    [Fact]
    public void Criar_ComKeycloakUserIdNulo_DeveCriarClienteComSucesso()
    {
        // Arrange
        Guid? keycloakUserId = null;
        var firstName = "Test";
        var lastName = "User";
        var email = "test@email.com";
        var passwordHash = "hashedPassword";
        var role = "USER";

        // Act
        var client = Client.Create(
            keycloakUserId,
            firstName,
            lastName,
            email,
            passwordHash,
            role);

        // Assert
        client.KeycloakUserId.Should().BeNull();
        client.Should().NotBeNull();
    }

    [Fact]
    public void Cliente_EstadoInicial_DeveTerPadroesCorretos()
    {
        // Arrange & Act
        var client = Client.Create(
            Guid.NewGuid(),
            "Test",
            "User",
            "test@email.com",
            "hashedPassword",
            "USER");

        // Assert
        client.Status.Should().Be(ClientStatus.Ativo);
        client.NewsletterOptIn.Should().BeFalse();
        client.FailedLoginAttempts.Should().Be(0);
        client.AccountLockedUntil.Should().BeNull();
        client.EmailVerifiedAt.Should().BeNull();
        client.Version.Should().Be(1);
        client.Cpf.Should().BeNull();
        client.DateOfBirth.Should().BeNull();
        client.Phone.Should().BeNull();
        client.Addresses.Should().BeEmpty();
        client.Consents.Should().BeEmpty();
        client.SavedCards.Should().BeEmpty();
    }

    [Fact]
    public void Cliente_Colecoes_DevemSerSomenteParaLeitura()
    {
        // Arrange
        var client = Client.Create(
            Guid.NewGuid(),
            "Test",
            "User",
            "test@email.com",
            "hashedPassword",
            "USER");

        // Act & Assert
        client.Addresses.Should().BeAssignableTo<IReadOnlyCollection<ClientService.Domain.Entities.Address>>();
        client.Consents.Should().BeAssignableTo<IReadOnlyCollection<ClientService.Domain.Entities.Consent>>();
        client.SavedCards.Should().BeAssignableTo<IReadOnlyCollection<ClientService.Domain.Entities.SavedCard>>();
    }
}