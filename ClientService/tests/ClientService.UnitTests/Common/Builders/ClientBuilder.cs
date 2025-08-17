using ClientService.Domain.Aggregates;
using ClientService.Domain.Enums;
using Bogus;

namespace ClientService.UnitTests.Common.Builders;

public class ClientBuilder
{
    private Guid? _keycloakUserId = Guid.NewGuid();
    private string _firstName = "Test";
    private string _lastName = "User";
    private string _email = "test@email.com";
    private string _passwordHash = "hashedPassword123";
    private string _role = "USER";

    public static ClientBuilder New() => new();

    public ClientBuilder WithKeycloakUserId(Guid? keycloakUserId)
    {
        _keycloakUserId = keycloakUserId;
        return this;
    }

    public ClientBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public ClientBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public ClientBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public ClientBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public ClientBuilder WithRole(string role)
    {
        _role = role;
        return this;
    }

    public ClientBuilder WithRole(UserRole role)
    {
        _role = role.ToString();
        return this;
    }

    public ClientBuilder WithRandomData()
    {
        var faker = new Faker("pt_BR");
        
        _keycloakUserId = Guid.NewGuid();
        _firstName = faker.Name.FirstName();
        _lastName = faker.Name.LastName();
        _email = faker.Internet.Email(_firstName, _lastName).ToLowerInvariant();
        _passwordHash = faker.Internet.Password();
        _role = faker.PickRandom("USER", "ADMIN");
        
        return this;
    }

    public Client Build()
    {
        return Client.Create(
            _keycloakUserId,
            _firstName,
            _lastName,
            _email,
            _passwordHash,
            _role);
    }
}