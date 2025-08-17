using System.Reflection;
using ClientService.Domain.ValueObjects;
using ClientService.Domain.Validations;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain.ValueObjects;

public class EmailUnitTest
{
    [Fact]
    public void Criar_ComEmailValido_DeveCriarEmailComSucesso()
    {
        // Arrange
        var emailValido = "usuario@exemplo.com";

        // Act
        var email = Email.Create(emailValido);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(emailValido.ToLowerInvariant());
    }

    [Theory]
    [InlineData("USUARIO@EXEMPLO.COM")]
    [InlineData("Usuario@Exemplo.Com")]
    [InlineData("USUARIO.TESTE@EXEMPLO.COM.BR")]
    public void Criar_ComEmailMaiusculo_DeveConverterParaMinusculo(string emailMaiusculo)
    {
        // Act
        var email = Email.Create(emailMaiusculo);

        // Assert
        email.Value.Should().Be(emailMaiusculo.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Criar_ComEmailVazioOuNulo_DeveLancarExcecao(string emailInvalido)
    {
        // Act & Assert
        var act = () => Email.Create(emailInvalido);
        act.Should().Throw<ArgumentException>()
           .WithMessage("O e-mail não pode ser vazio*");
    }

    [Theory]
    [InlineData("email-sem-arroba")]
    [InlineData("@exemplo.com")]
    [InlineData("usuario@")]
    [InlineData("usuario@@exemplo.com")]
    [InlineData("usuario@exemplo")]
    [InlineData("usuario.exemplo.com")]
    [InlineData("usuario @exemplo.com")]
    [InlineData("usuario@exemplo .com")]
    public void Criar_ComFormatoInvalido_DeveLancarExcecao(string emailInvalido)
    {
        // Act & Assert
        var act = () => Email.Create(emailInvalido);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Formato de e-mail inválido*");
    }

    [Fact]
    public void ToString_DeveRetornarValorDoEmail()
    {
        // Arrange
        var emailTexto = "teste@exemplo.com";
        var email = Email.Create(emailTexto);

        // Act
        var resultado = email.ToString();

        // Assert
        resultado.Should().Be(emailTexto);
    }

    [Fact]
    public void Equals_ComEmailsIguais_DeveRetornarTrue()
    {
        // Arrange
        var email1 = Email.Create("teste@exemplo.com");
        var email2 = Email.Create("teste@exemplo.com");

        // Act & Assert
        email1.Equals(email2).Should().BeTrue();
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ComEmailsDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var email1 = Email.Create("teste1@exemplo.com");
        var email2 = Email.Create("teste2@exemplo.com");

        // Act & Assert
        email1.Equals(email2).Should().BeFalse();
        (email1 == email2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ComObjetoNulo_DeveRetornarFalse()
    {
        // Arrange
        var email = Email.Create("teste@exemplo.com");

        // Act & Assert
        email.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ComEmailsIguais_DeveRetornarHashIgual()
    {
        // Arrange
        var email1 = Email.Create("teste@exemplo.com");
        var email2 = Email.Create("teste@exemplo.com");

        // Act & Assert
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ComEmailsDiferentes_DeveRetornarHashDiferente()
    {
        // Arrange
        var email1 = Email.Create("teste1@exemplo.com");
        var email2 = Email.Create("teste2@exemplo.com");

        // Act & Assert
        email1.GetHashCode().Should().NotBe(email2.GetHashCode());
    }

    [Fact]
    public void Validate_ComEmailValido_NaoDeveAdicionarErros()
    {
        // Arrange
        var email = Email.Create("teste@exemplo.com");
        var handler = new Notification();

        // Act
        email.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeFalse();
        handler.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ComEmailVazio_DeveAdicionarErro()
    {
        // Arrange
        var email = Email.Create("temp@test.com"); // Criar um email válido primeiro
        
        // Usar reflection para definir o Value como vazio para testar a validação
        var valueProperty = typeof(Email).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        var backingField = typeof(Email).GetField("<Value>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (backingField != null)
        {
            backingField.SetValue(email, "");
        }
        
        var handler = new Notification();

        // Act
        email.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().HaveCount(2); // Espera 2 erros: obrigatório + formato inválido
        handler.Errors.Should().Contain(e => e.Message == "E-mail é obrigatório");
        handler.Errors.Should().Contain(e => e.Message == "Formato de e-mail inválido");
    }

    [Theory]
    [InlineData("usuario@exemplo.com")]
    [InlineData("teste.email@dominio.com.br")]
    [InlineData("user123@test-domain.org")]
    [InlineData("a@b.co")]
    public void Criar_ComEmailsValidosVariados_DeveCriarComSucesso(string emailValido)
    {
        // Act
        var email = Email.Create(emailValido);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(emailValido.ToLowerInvariant());
    }
}