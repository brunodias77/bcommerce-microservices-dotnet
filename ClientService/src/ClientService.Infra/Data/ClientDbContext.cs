using ClientService.Domain.Aggregates;
using ClientService.Domain.Common;
using ClientService.Domain.Entities;
using ClientService.Infra.ConfigEntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Infra.Data;

public class ClientDbContext : DbContext
{
    public ClientDbContext(DbContextOptions<ClientDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;
    public DbSet<Consent> Consents { get; set; } = null!;
    public DbSet<SavedCard> SavedCards { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ignorar tipos que não devem ser mapeados
        modelBuilder.Ignore<DomainEvent>();
        
        // Aplicar todas as configurações de entidades
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new AddressConfiguration());
        modelBuilder.ApplyConfiguration(new ConsentConfiguration());
        modelBuilder.ApplyConfiguration(new SavedCardConfiguration());
        
        // Configurações globais
        ConfigureGlobalSettings(modelBuilder);
        
        base.OnModelCreating(modelBuilder);
    }
    
    private static void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        // Configurar precisão decimal padrão
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
        
        // Configurar comportamento de exclusão padrão
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
        
        // Configurar nomes de tabelas em snake_case (se necessário)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entityType.SetTableName(tableName.ToLowerInvariant());
            }
        }
    }
    
    
}