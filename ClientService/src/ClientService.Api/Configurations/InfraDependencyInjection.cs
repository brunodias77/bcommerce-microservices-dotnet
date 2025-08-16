using ClientService.Domain.Common;
using ClientService.Domain.Repositories;
using ClientService.Infra.Data;
using ClientService.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Api.Configurations;

public static class InfraDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRepositories(services);
        AddPasswordEncrypter(services, configuration);
        AddServices(services, configuration);
        AddEvents(services, configuration);
        AddLoggedUser(services, configuration);
        AddLoggedUser(services);
        // AddLoggedCustomer(services, configuration);
        // AddToken(services, configuration);
    }
    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // services.AddScoped<IAddressRepository, AddressRepository>();
        // services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        // services.AddScoped<IClientRepository, ClientRepository>();
        // services.AddScoped<ICategoryRepository, CategoryRepository>(); // <<< ADICIONE ESTA LINHA
        // services.AddScoped<IUnitOfWork, DapperUnitOfWork>();
        // services.AddScoped<IBrandRepository, BrandRepository>(); // <<< ADICIONE ESTA LINHA
        // services.AddScoped<IProductRepository, ProductRepository>();
        // services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>(); // <-- ADICIONE ESTA LINHA
        // services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();
        // services.AddScoped<ICartRepository, CartRepository>(); // <-- ADICIONE
        // services.AddScoped<IOrderRepository, OrderRepository>();
        // services.AddScoped<ICouponRepository, CouponRepository>();
        // services.AddScoped<IColorRepository, ColorRepository>();
        // services.AddScoped<ISizeRepository, SizeRepository>();
    }

    private static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // services.AddSingleton<IEmailService, ConsoleEmailService>(); // Singleton para o serviço de console
        // services.AddScoped<ITokenService, JwtTokenService>();
        // // Para desenvolvimento, usamos o gateway falso. Em produção, trocaríamos esta linha.
        // services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        // services.AddScoped<IPaymentGatewayService, FakePaymentGatewayService>(); // <-- Adicionar esta linha

    }

    private static void AddPasswordEncrypter(IServiceCollection services, IConfiguration configuration)
    {
        // services.AddScoped<IPasswordEncripter, PasswordEncripter>();
    }

    private static void AddEvents(IServiceCollection services, IConfiguration configuration)
    {
        // services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

    }

    private static void AddLoggedUser(IServiceCollection services, IConfiguration configuration)
    {
        // services.AddScoped<ILoggedUser, LoggedUser>();
    }



    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ClientDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddLoggedUser(IServiceCollection services)
    {
        // services.AddScoped<ILoggedUser, LoggedUser>();
    }
}