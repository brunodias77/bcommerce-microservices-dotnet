using ClientService.Domain.Entities;
using ClientService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain;

public class SavedCardUnitTest
{
    [Fact]
    public void DeveCriarCartaoSalvoComSucesso()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var nickname = "Cartão Principal";
        var lastFourDigits = "1234";
        var brand = CardBrand.Visa;
        var gatewayToken = "tok_1234567890abcdef";
        var expiryDate = new DateTime(2025, 12, 31);
        var isDefault = true;

        // Act
        var savedCard = SavedCard.Create(
            clientId, nickname, lastFourDigits, brand, gatewayToken, expiryDate, isDefault);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.Id.Should().NotBeEmpty();
        savedCard.ClientId.Should().Be(clientId);
        savedCard.Nickname.Should().Be(nickname);
        savedCard.LastFourDigits.Should().Be(lastFourDigits);
        savedCard.Brand.Should().Be(brand);
        savedCard.GatewayToken.Should().Be(gatewayToken);
        savedCard.ExpiryDate.Should().Be(expiryDate);
        savedCard.IsDefault.Should().Be(isDefault);
        savedCard.Version.Should().Be(1);
        savedCard.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DeveCriarCartaoSalvoSemApelido()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "5678";
        var brand = CardBrand.Mastercard;
        var gatewayToken = "tok_abcdef1234567890";
        var expiryDate = new DateTime(2026, 6, 30);

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.Nickname.Should().BeNull();
        savedCard.IsDefault.Should().BeFalse(); // Valor padrão
    }

    [Theory]
    [InlineData(CardBrand.Visa)]
    [InlineData(CardBrand.Mastercard)]
    [InlineData(CardBrand.Amex)]
    [InlineData(CardBrand.Elo)]
    [InlineData(CardBrand.Hipercard)]
    [InlineData(CardBrand.DinersClub)]
    [InlineData(CardBrand.Discover)]
    [InlineData(CardBrand.Jcb)]
    [InlineData(CardBrand.Aura)]
    [InlineData(CardBrand.Other)]
    public void DeveCriarCartaoSalvoComTodasBandeirasValidas(CardBrand cardBrand)
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "9999";
        var gatewayToken = "tok_test123";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, cardBrand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.Brand.Should().Be(cardBrand);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeveCriarCartaoSalvoComStatusPadraoOuNao(bool isDefault)
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "1111";
        var brand = CardBrand.Visa;
        var gatewayToken = "tok_default_test";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate, isDefault);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.IsDefault.Should().Be(isDefault);
    }

    [Fact]
    public void DeveCriarCartaoSalvoComApelidoPersonalizado()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var nickname = "Cartão de Emergência";
        var lastFourDigits = "2222";
        var brand = CardBrand.Amex;
        var gatewayToken = "tok_emergency_card";
        var expiryDate = new DateTime(2027, 3, 15);

        // Act
        var savedCard = SavedCard.Create(
            clientId, nickname, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.Nickname.Should().Be(nickname);
    }

    [Fact]
    public void DeveDefinirVersaoInicialComoUm()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "3333";
        var brand = CardBrand.Elo;
        var gatewayToken = "tok_version_test";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Version.Should().Be(1);
    }

    [Fact]
    public void DeveDefinirIdUnicoParaCadaCartaoSalvo()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "4444";
        var brand = CardBrand.Hipercard;
        var gatewayToken = "tok_unique_test";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard1 = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);
        var savedCard2 = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard1.Id.Should().NotBe(savedCard2.Id);
        savedCard1.Id.Should().NotBeEmpty();
        savedCard2.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void DeveDefinirDataCriacaoCorretamente()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "5555";
        var brand = CardBrand.DinersClub;
        var gatewayToken = "tok_date_test";
        var expiryDate = new DateTime(2025, 12, 31);
        var beforeCreation = DateTime.UtcNow;

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);
        var afterCreation = DateTime.UtcNow;

        // Assert
        savedCard.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        savedCard.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void DevePermitirCartaoComDataVencimentoFutura()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "6666";
        var brand = CardBrand.Discover;
        var gatewayToken = "tok_future_expiry";
        var expiryDate = new DateTime(2030, 12, 31); // Data futura

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.ExpiryDate.Should().Be(expiryDate);
        savedCard.ExpiryDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void DevePermitirMesmoClienteMultiplosCartoesSalvos()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var card1LastFour = "1111";
        var card2LastFour = "2222";
        var brand1 = CardBrand.Visa;
        var brand2 = CardBrand.Mastercard;
        var gatewayToken1 = "tok_card1";
        var gatewayToken2 = "tok_card2";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard1 = SavedCard.Create(
            clientId, "Cartão 1", card1LastFour, brand1, gatewayToken1, expiryDate, true);
        var savedCard2 = SavedCard.Create(
            clientId, "Cartão 2", card2LastFour, brand2, gatewayToken2, expiryDate, false);

        // Assert
        savedCard1.Should().NotBeNull();
        savedCard2.Should().NotBeNull();
        savedCard1.ClientId.Should().Be(clientId);
        savedCard2.ClientId.Should().Be(clientId);
        savedCard1.Id.Should().NotBe(savedCard2.Id);
        savedCard1.LastFourDigits.Should().Be(card1LastFour);
        savedCard2.LastFourDigits.Should().Be(card2LastFour);
        savedCard1.IsDefault.Should().BeTrue();
        savedCard2.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void DevePermitirCartaoComTokenGatewayPersonalizado()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "7777";
        var brand = CardBrand.Jcb;
        var gatewayToken = "stripe_tok_1234567890abcdef";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.GatewayToken.Should().Be(gatewayToken);
    }

    [Fact]
    public void DevePermitirCartaoComUltimosQuatroDigitosValidos()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "0000";
        var brand = CardBrand.Aura;
        var gatewayToken = "tok_zeros";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard = SavedCard.Create(
            clientId, null, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.LastFourDigits.Should().Be(lastFourDigits);
        savedCard.LastFourDigits.Should().HaveLength(4);
    }

    [Fact]
    public void DevePermitirCartaoComBandeiraOutros()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var lastFourDigits = "8888";
        var brand = CardBrand.Other;
        var gatewayToken = "tok_other_brand";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard = SavedCard.Create(
            clientId, "Cartão Desconhecido", lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.Brand.Should().Be(CardBrand.Other);
        savedCard.Nickname.Should().Be("Cartão Desconhecido");
    }

    [Fact]
    public void DevePermitirApelidoVazio()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var nickname = string.Empty;
        var lastFourDigits = "9999";
        var brand = CardBrand.Visa;
        var gatewayToken = "tok_empty_nickname";
        var expiryDate = new DateTime(2025, 12, 31);

        // Act
        var savedCard = SavedCard.Create(
            clientId, nickname, lastFourDigits, brand, gatewayToken, expiryDate);

        // Assert
        savedCard.Should().NotBeNull();
        savedCard.Nickname.Should().Be(nickname);
    }
}