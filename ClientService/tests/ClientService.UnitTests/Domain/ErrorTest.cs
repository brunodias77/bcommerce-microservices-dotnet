using ClientService.Domain.Common;
using ClientService.Domain.Validations;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain;

public class ErrorTest
{
    [Fact]
    public void DeveCriarErroComMensagem()
    {
        // Arrange
        var message = "Erro de teste";

        // Act
        var error = new Error("", message);

        // Assert
        error.Should().NotBeNull();
        error.Message.Should().Be(message);
        error.Code.Should().BeNull();
    }

    [Fact]
    public void DeveCriarErroComMensagemECodigo()
    {
        // Arrange
        var message = "Erro de teste";
        var code = "TEST_ERROR";

        // Act
        var error = new Error(code, message);

        // Assert
        error.Should().NotBeNull();
        error.Message.Should().Be(message);
        error.Code.Should().Be(code);
    }

    [Fact]
    public void DevePermitirMensagemNula()
    {
        // Arrange & Act
        var error = new Error(null, null!);

        // Assert
        error.Should().NotBeNull();
        error.Message.Should().BeNull();
        error.Code.Should().BeNull();
    }

    [Fact]
    public void DevePermitirCodigoNulo()
    {
        // Arrange
        var message = "Erro de teste";

        // Act
        var error = new Error(null, message);

        // Assert
        error.Should().NotBeNull();
        error.Message.Should().Be(message);
        error.Code.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void DevePermitirMensagemVaziaOuEspacos(string message)
    {
        // Act
        var error = new Error("", message);

        // Assert
        error.Should().NotBeNull();
        error.Message.Should().Be(message);
    }
}