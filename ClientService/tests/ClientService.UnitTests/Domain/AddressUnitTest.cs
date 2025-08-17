using ClientService.Domain.Entities;
using ClientService.Domain.Enums;
using ClientService.Domain.Validations;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace ClientService.UnitTests.Domain;

public class AddressUnitTest
{
    [Fact]
    public void DeveCriarEnderecoComSucesso()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Shipping;
        var postalCode = "01234-567";
        var street = "Rua das Flores";
        var streetNumber = "123";
        var complement = "Apto 45";
        var neighborhood = "Centro";
        var city = "São Paulo";
        var stateCode = "SP";
        var countryCode = "BR";
        var phone = "11987654321";
        var isDefault = true;

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            complement, neighborhood, city, stateCode, countryCode, phone, isDefault);

        // Assert
        address.Should().NotBeNull();
        address.Id.Should().NotBeEmpty();
        address.ClientId.Should().Be(clientId);
        address.Type.Should().Be(type);
        address.PostalCode.Should().Be(postalCode);
        address.Street.Should().Be(street);
        address.StreetNumber.Should().Be(streetNumber);
        address.Complement.Should().Be(complement);
        address.Neighborhood.Should().Be(neighborhood);
        address.City.Should().Be(city);
        address.StateCode.Should().Be(stateCode);
        address.CountryCode.Should().Be(countryCode);
        address.Phone.Should().Be(phone);
        address.IsDefault.Should().Be(isDefault);
        address.Version.Should().Be(1);
        address.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DeveCriarEnderecoComValoresPadrao()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Billing;
        var postalCode = "12345-678";
        var street = "Avenida Paulista";
        var streetNumber = "1000";
        var neighborhood = "Bela Vista";
        var city = "São Paulo";
        var stateCode = "SP";

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode);

        // Assert
        address.Should().NotBeNull();
        address.ClientId.Should().Be(clientId);
        address.Type.Should().Be(type);
        address.CountryCode.Should().Be("BR"); // Valor padrão
        address.Phone.Should().BeNull(); // Valor padrão
        address.IsDefault.Should().BeFalse(); // Valor padrão
        address.Complement.Should().BeNull();
    }

    [Theory]
    [InlineData(AddressType.Shipping)]
    [InlineData(AddressType.Billing)]
    public void DeveCriarEnderecoComTiposValidos(AddressType addressType)
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var postalCode = "01234-567";
        var street = "Rua Teste";
        var streetNumber = "100";
        var neighborhood = "Bairro Teste";
        var city = "Cidade Teste";
        var stateCode = "SP";

        // Act
        var address = Address.Create(
            clientId, addressType, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode);

        // Assert
        address.Should().NotBeNull();
        address.Type.Should().Be(addressType);
    }

    [Fact]
    public void DeveCriarEnderecoComComplementoNulo()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Shipping;
        var postalCode = "01234-567";
        var street = "Rua das Flores";
        var streetNumber = "123";
        var neighborhood = "Centro";
        var city = "São Paulo";
        var stateCode = "SP";

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode);

        // Assert
        address.Should().NotBeNull();
        address.Complement.Should().BeNull();
    }

    [Fact]
    public void DeveCriarEnderecoComTelefoneNulo()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Billing;
        var postalCode = "01234-567";
        var street = "Rua das Flores";
        var streetNumber = "123";
        var neighborhood = "Centro";
        var city = "São Paulo";
        var stateCode = "SP";

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode, phone: null);

        // Assert
        address.Should().NotBeNull();
        address.Phone.Should().BeNull();
    }

    [Fact]
    public void DeveDefinirVersaoInicialComoUm()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Shipping;
        var postalCode = "01234-567";
        var street = "Rua das Flores";
        var streetNumber = "123";
        var neighborhood = "Centro";
        var city = "São Paulo";
        var stateCode = "SP";

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode);

        // Assert
        address.Version.Should().Be(1);
    }

    [Fact]
    public void DeveDefinirIdUnicoParaCadaEndereco()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Shipping;
        var postalCode = "01234-567";
        var street = "Rua das Flores";
        var streetNumber = "123";
        var neighborhood = "Centro";
        var city = "São Paulo";
        var stateCode = "SP";

        // Act
        var address1 = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode);
        var address2 = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode);

        // Assert
        address1.Id.Should().NotBe(address2.Id);
        address1.Id.Should().NotBeEmpty();
        address2.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void DeveDefinirDataCriacaoCorretamente()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Shipping;
        var postalCode = "01234-567";
        var street = "Rua das Flores";
        var streetNumber = "123";
        var neighborhood = "Centro";
        var city = "São Paulo";
        var stateCode = "SP";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode);
        var afterCreation = DateTime.UtcNow;

        // Assert
        address.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        address.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void DevePermitirEnderecoComCodigoPaisPersonalizado()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Shipping;
        var postalCode = "10001";
        var street = "Fifth Avenue";
        var streetNumber = "123";
        var neighborhood = "Manhattan";
        var city = "New York";
        var stateCode = "NY";
        var countryCode = "US";

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode, countryCode);

        // Assert
        address.Should().NotBeNull();
        address.CountryCode.Should().Be(countryCode);
    }

    [Fact]
    public void DevePermitirEnderecoComTelefonePersonalizado()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var type = AddressType.Billing;
        var postalCode = "01234-567";
        var street = "Rua das Flores";
        var streetNumber = "123";
        var neighborhood = "Centro";
        var city = "São Paulo";
        var stateCode = "SP";
        var phone = "11987654321";

        // Act
        var address = Address.Create(
            clientId, type, postalCode, street, streetNumber,
            null, neighborhood, city, stateCode, phone: phone);

        // Assert
        address.Should().NotBeNull();
        address.Phone.Should().Be(phone);
    }

    [Fact]
    public void Validate_ComClientIdVazio_DeveAdicionarErro()
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set ClientId to empty
        var clientIdProperty = typeof(Address).GetProperty("ClientId", BindingFlags.Public | BindingFlags.Instance);
        clientIdProperty?.SetValue(address, Guid.Empty);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.ClientId" && e.Message == "ClientId é obrigatório");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ComCepInvalido_DeveAdicionarErro(string cepInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set PostalCode
        var postalCodeProperty = typeof(Address).GetProperty("PostalCode", BindingFlags.Public | BindingFlags.Instance);
        postalCodeProperty?.SetValue(address, cepInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.PostalCode" && e.Message == "CEP é obrigatório");
    }

    [Theory]
    [InlineData("123")]     // Muito curto
    [InlineData("1234567890")] // Muito longo
    public void Validate_ComCepTamanhoInvalido_DeveAdicionarErro(string cepInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set PostalCode
        var postalCodeProperty = typeof(Address).GetProperty("PostalCode", BindingFlags.Public | BindingFlags.Instance);
        postalCodeProperty?.SetValue(address, cepInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.PostalCode" && e.Message == "CEP deve ter entre 8 e 9 caracteres");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ComLogradouroInvalido_DeveAdicionarErro(string logradouroInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set Street
        var streetProperty = typeof(Address).GetProperty("Street", BindingFlags.Public | BindingFlags.Instance);
        streetProperty?.SetValue(address, logradouroInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.Street" && e.Message == "Logradouro é obrigatório");
    }

    [Fact]
    public void Validate_ComLogradouroMuitoLongo_DeveAdicionarErro()
    {
        // Arrange
        var logradouroLongo = new string('A', 201); // 201 caracteres
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set Street
        var streetProperty = typeof(Address).GetProperty("Street", BindingFlags.Public | BindingFlags.Instance);
        streetProperty?.SetValue(address, logradouroLongo);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.Street" && e.Message == "Logradouro deve ter no máximo 200 caracteres");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ComNumeroInvalido_DeveAdicionarErro(string numeroInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set StreetNumber
        var streetNumberProperty = typeof(Address).GetProperty("StreetNumber", BindingFlags.Public | BindingFlags.Instance);
        streetNumberProperty?.SetValue(address, numeroInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.StreetNumber" && e.Message == "Número é obrigatório");
    }

    [Fact]
    public void Validate_ComNumeroMuitoLongo_DeveAdicionarErro()
    {
        // Arrange
        var numeroLongo = new string('1', 21); // 21 caracteres
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set StreetNumber
        var streetNumberProperty = typeof(Address).GetProperty("StreetNumber", BindingFlags.Public | BindingFlags.Instance);
        streetNumberProperty?.SetValue(address, numeroLongo);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.StreetNumber" && e.Message == "Número deve ter no máximo 20 caracteres");
    }

    [Fact]
    public void Validate_ComComplementoMuitoLongo_DeveAdicionarErro()
    {
        // Arrange
        var complementoLongo = new string('A', 101); // 101 caracteres
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            complementoLongo, "Centro", "São Paulo", "SP");
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.Complement" && e.Message == "Complemento deve ter no máximo 100 caracteres");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ComBairroInvalido_DeveAdicionarErro(string bairroInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set Neighborhood
        var neighborhoodProperty = typeof(Address).GetProperty("Neighborhood", BindingFlags.Public | BindingFlags.Instance);
        neighborhoodProperty?.SetValue(address, bairroInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.Neighborhood" && e.Message == "Bairro é obrigatório");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ComCidadeInvalida_DeveAdicionarErro(string cidadeInvalida)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set City
        var cityProperty = typeof(Address).GetProperty("City", BindingFlags.Public | BindingFlags.Instance);
        cityProperty?.SetValue(address, cidadeInvalida);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.City" && e.Message == "Cidade é obrigatória");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("S")]     // Muito curto
    [InlineData("SPP")]   // Muito longo
    public void Validate_ComCodigoEstadoInvalido_DeveAdicionarErro(string codigoEstadoInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set StateCode
        var stateCodeProperty = typeof(Address).GetProperty("StateCode", BindingFlags.Public | BindingFlags.Instance);
        stateCodeProperty?.SetValue(address, codigoEstadoInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        var expectedMessage = string.IsNullOrWhiteSpace(codigoEstadoInvalido) ? 
            "Código do estado é obrigatório" : "Código do estado deve ter 2 caracteres";
        handler.Errors.Should().Contain(e => e.Code == "Address.StateCode" && e.Message == expectedMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("B")]     // Muito curto
    [InlineData("BRR")]   // Muito longo
    public void Validate_ComCodigoPaisInvalido_DeveAdicionarErro(string codigoPaisInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP");
        
        // Use reflection to set CountryCode
        var countryCodeProperty = typeof(Address).GetProperty("CountryCode", BindingFlags.Public | BindingFlags.Instance);
        countryCodeProperty?.SetValue(address, codigoPaisInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        var expectedMessage = string.IsNullOrWhiteSpace(codigoPaisInvalido) ? 
            "Código do país é obrigatório" : "Código do país deve ter 2 caracteres";
        handler.Errors.Should().Contain(e => e.Code == "Address.CountryCode" && e.Message == expectedMessage);
    }

    [Theory]
    [InlineData("123")]        // Muito curto
    [InlineData("1234567890123456")] // Muito longo
    public void Validate_ComTelefoneInvalido_DeveAdicionarErro(string telefoneInvalido)
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua Teste", "123",
            null, "Centro", "São Paulo", "SP", phone: telefoneInvalido);
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeTrue();
        handler.Errors.Should().Contain(e => e.Code == "Address.Phone" && e.Message == "Telefone deve ter entre 10 e 15 caracteres");
    }

    [Fact]
    public void Validate_ComEnderecoValido_NaoDeveAdicionarErros()
    {
        // Arrange
        var address = Address.Create(
            Guid.NewGuid(), AddressType.Shipping, "01234-567", "Rua das Flores", "123",
            "Apto 45", "Centro", "São Paulo", "SP", "BR", "11987654321");
        
        var handler = new Notification();

        // Act
        address.Validate(handler);

        // Assert
        handler.HasErrors.Should().BeFalse();
        handler.Errors.Should().BeEmpty();
    }
}