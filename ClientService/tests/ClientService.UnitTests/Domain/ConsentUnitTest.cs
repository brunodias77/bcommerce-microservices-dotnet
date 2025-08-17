using ClientService.Domain.Entities;
using ClientService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ClientService.UnitTests.Domain;

public class ConsentUnitTest
{
    [Fact]
    public void DeveCriarConsentimentoComSucesso()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.TermsOfService;
        var isGranted = true;
        var termsVersion = "v1.2.3";

        // Act
        var consent = Consent.Create(clientId, type, isGranted, termsVersion);

        // Assert
        consent.Should().NotBeNull();
        consent.Id.Should().NotBeEmpty();
        consent.ClientId.Should().Be(clientId);
        consent.Type.Should().Be(type);
        consent.IsGranted.Should().Be(isGranted);
        consent.TermsVersion.Should().Be(termsVersion);
        consent.Version.Should().Be(1);
        consent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DeveCriarConsentimentoSemVersaoTermos()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.MarketingEmail;
        var isGranted = false;

        // Act
        var consent = Consent.Create(clientId, type, isGranted);

        // Assert
        consent.Should().NotBeNull();
        consent.ClientId.Should().Be(clientId);
        consent.Type.Should().Be(type);
        consent.IsGranted.Should().Be(isGranted);
        consent.TermsVersion.Should().BeNull();
        consent.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(ConsentType.MarketingEmail)]
    [InlineData(ConsentType.NewsletterSubscription)]
    [InlineData(ConsentType.TermsOfService)]
    [InlineData(ConsentType.PrivacyPolicy)]
    [InlineData(ConsentType.CookiesEssential)]
    [InlineData(ConsentType.CookiesAnalytics)]
    [InlineData(ConsentType.CookiesMarketing)]
    public void DeveCriarConsentimentoComTodosTiposValidos(ConsentType consentType)
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var isGranted = true;

        // Act
        var consent = Consent.Create(clientId, consentType, isGranted);

        // Assert
        consent.Should().NotBeNull();
        consent.Type.Should().Be(consentType);
        consent.IsGranted.Should().Be(isGranted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeveCriarConsentimentoComStatusConcedidoOuNegado(bool isGranted)
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.PrivacyPolicy;

        // Act
        var consent = Consent.Create(clientId, type, isGranted);

        // Assert
        consent.Should().NotBeNull();
        consent.IsGranted.Should().Be(isGranted);
    }

    [Fact]
    public void DeveCriarConsentimentoComVersaoTermosPersonalizada()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.TermsOfService;
        var isGranted = true;
        var termsVersion = "v2.0.0-beta";

        // Act
        var consent = Consent.Create(clientId, type, isGranted, termsVersion);

        // Assert
        consent.Should().NotBeNull();
        consent.TermsVersion.Should().Be(termsVersion);
    }

    [Fact]
    public void DeveDefinirVersaoInicialComoUm()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.CookiesEssential;
        var isGranted = true;

        // Act
        var consent = Consent.Create(clientId, type, isGranted);

        // Assert
        consent.Version.Should().Be(1);
    }

    [Fact]
    public void DeveDefinirIdUnicoParaCadaConsentimento()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.NewsletterSubscription;
        var isGranted = true;

        // Act
        var consent1 = Consent.Create(clientId, type, isGranted);
        var consent2 = Consent.Create(clientId, type, isGranted);

        // Assert
        consent1.Id.Should().NotBe(consent2.Id);
        consent1.Id.Should().NotBeEmpty();
        consent2.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void DeveDefinirDataCriacaoCorretamente()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.MarketingEmail;
        var isGranted = false;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var consent = Consent.Create(clientId, type, isGranted);
        var afterCreation = DateTime.UtcNow;

        // Assert
        consent.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        consent.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void DevePermitirConsentimentoConcedidoComVersaoTermos()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.TermsOfService;
        var isGranted = true;
        var termsVersion = "v1.0.0";

        // Act
        var consent = Consent.Create(clientId, type, isGranted, termsVersion);

        // Assert
        consent.Should().NotBeNull();
        consent.IsGranted.Should().BeTrue();
        consent.TermsVersion.Should().Be(termsVersion);
    }

    [Fact]
    public void DevePermitirConsentimentoNegadoSemVersaoTermos()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.CookiesMarketing;
        var isGranted = false;

        // Act
        var consent = Consent.Create(clientId, type, isGranted);

        // Assert
        consent.Should().NotBeNull();
        consent.IsGranted.Should().BeFalse();
        consent.TermsVersion.Should().BeNull();
    }

    [Fact]
    public void DevePermitirMesmosClientesDiferentesTiposConsentimento()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type1 = ConsentType.TermsOfService;
        var type2 = ConsentType.PrivacyPolicy;
        var isGranted = true;

        // Act
        var consent1 = Consent.Create(clientId, type1, isGranted);
        var consent2 = Consent.Create(clientId, type2, isGranted);

        // Assert
        consent1.Should().NotBeNull();
        consent2.Should().NotBeNull();
        consent1.ClientId.Should().Be(clientId);
        consent2.ClientId.Should().Be(clientId);
        consent1.Type.Should().Be(type1);
        consent2.Type.Should().Be(type2);
        consent1.Id.Should().NotBe(consent2.Id);
    }

    [Fact]
    public void DevePermitirConsentimentoComVersaoTermosVazia()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.CookiesAnalytics;
        var isGranted = true;
        var termsVersion = string.Empty;

        // Act
        var consent = Consent.Create(clientId, type, isGranted, termsVersion);

        // Assert
        consent.Should().NotBeNull();
        consent.TermsVersion.Should().Be(termsVersion);
    }

    [Fact]
    public void DevePermitirConsentimentoParaCookiesEssenciais()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = ConsentType.CookiesEssential;
        var isGranted = true; // Cookies essenciais geralmente são sempre aceitos

        // Act
        var consent = Consent.Create(clientId, type, isGranted);

        // Assert
        consent.Should().NotBeNull();
        consent.Type.Should().Be(ConsentType.CookiesEssential);
        consent.IsGranted.Should().BeTrue();
    }

    [Fact]
    public void DevePermitirConsentimentoParaCookiesOpcionais()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var analyticsConsent = Consent.Create(clientId, ConsentType.CookiesAnalytics, false);
        var marketingConsent = Consent.Create(clientId, ConsentType.CookiesMarketing, true);

        // Assert
        analyticsConsent.Should().NotBeNull();
        analyticsConsent.Type.Should().Be(ConsentType.CookiesAnalytics);
        analyticsConsent.IsGranted.Should().BeFalse();
        
        marketingConsent.Should().NotBeNull();
        marketingConsent.Type.Should().Be(ConsentType.CookiesMarketing);
        marketingConsent.IsGranted.Should().BeTrue();
    }
}