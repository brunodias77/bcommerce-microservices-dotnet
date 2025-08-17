using ClientService.Domain.Validations;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain.Validations;

public class NotificationTest
{
    [Fact]
    public void DeveCriarNotificationVazia()
    {
        // Act
        var notification = new Notification();

        // Assert
        notification.Should().NotBeNull();
        notification.HasErrors.Should().BeFalse();
        notification.Errors.Should().BeEmpty();
    }

    [Fact]
    public void DeveAdicionarErroComSucesso()
    {
        // Arrange
        var notification = new Notification();
        var error = new Error("test.code", "Mensagem de teste");

        // Act
        notification.Add(error);

        // Assert
        notification.HasErrors.Should().BeTrue();
        notification.Errors.Should().HaveCount(1);
        notification.Errors.Should().Contain(error);
    }

    [Fact]
    public void DeveAdicionarMultiplosErros()
    {
        // Arrange
        var notification = new Notification();
        var error1 = new Error("test.code1", "Mensagem 1");
        var error2 = new Error("test.code2", "Mensagem 2");

        // Act
        notification.Add(error1);
        notification.Add(error2);

        // Assert
        notification.HasErrors.Should().BeTrue();
        notification.Errors.Should().HaveCount(2);
        notification.Errors.Should().Contain(error1);
        notification.Errors.Should().Contain(error2);
    }

    [Fact]
    public void DeveAdicionarOutraNotification()
    {
        // Arrange
        var notification1 = new Notification();
        var notification2 = new Notification();
        var error1 = new Error("test.code1", "Mensagem 1");
        var error2 = new Error("test.code2", "Mensagem 2");
        
        notification2.Add(error1);
        notification2.Add(error2);

        // Act
        notification1.Add(notification2);

        // Assert
        notification1.HasErrors.Should().BeTrue();
        notification1.Errors.Should().HaveCount(2);
        notification1.Errors.Should().Contain(error1);
        notification1.Errors.Should().Contain(error2);
    }

    [Fact]
    public void DevePermitirAdicionarErroNulo()
    {
        // Arrange
        var notification = new Notification();
        Error nullError = null;

        // Act & Assert
        var action = () => notification.Add(nullError);
        action.Should().NotThrow();
        notification.HasErrors.Should().BeFalse();
    }
}