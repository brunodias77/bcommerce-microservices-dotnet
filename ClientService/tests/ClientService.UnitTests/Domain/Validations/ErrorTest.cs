using ClientService.Domain.Validations;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain.Validations;

public class ErrorTest
{
    [Fact]
    public void DeveCriarErrorComCodigoEMensagem()
    {
        // Arrange
        var code = "test.code";
        var message = "Mensagem de teste";

        // Act
        var error = new Error(code, message);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void DeveCriarErrorComCodigoVazio()
    {
        // Arrange
        var code = "";
        var message = "Mensagem de teste";

        // Act
        var error = new Error(code, message);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void DeveCriarErrorComMensagemVazia()
    {
        // Arrange
        var code = "test.code";
        var message = "";

        // Act
        var error = new Error(code, message);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
    }

    [Fact]
    public void DeveCriarErrorComCodigoNulo()
    {
        // Arrange
        string code = null;
        var message = "Mensagem de teste";

        // Act
        var error = new Error(code, message);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().BeNull();
        error.Message.Should().Be(message);
    }

    [Fact]
    public void DeveCriarErrorComMensagemNula()
    {
        // Arrange
        var code = "test.code";
        string message = null;

        // Act
        var error = new Error(code, message);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be(code);
        error.Message.Should().BeNull();
    }

    [Fact]
    public void DeveImplementarIgualdadeCorretamente()
    {
        // Arrange
        var error1 = new Error("test.code", "Mensagem de teste");
        var error2 = new Error("test.code", "Mensagem de teste");
        var error3 = new Error("other.code", "Outra mensagem");

        // Act & Assert
        error1.Should().Be(error2);
        error1.Should().NotBe(error3);
        error1.GetHashCode().Should().Be(error2.GetHashCode());
    }
}