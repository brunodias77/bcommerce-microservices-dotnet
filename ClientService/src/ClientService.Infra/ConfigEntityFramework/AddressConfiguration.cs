using ClientService.Domain.Aggregates;
using ClientService.Domain.Entities;
using ClientService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientService.Infra.ConfigEntityFramework;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        // Configuração da tabela
        builder.ToTable("addresses");
        
        // Configuração da chave primária
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("address_id")
            .IsRequired();
        
        // Configuração dos campos herdados de Entity
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
            
        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at");
        
        // Configuração da chave estrangeira para Client
        builder.Property(a => a.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
            
        // Configuração do enum AddressType com conversão para lowercase
        builder.Property(a => a.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasConversion(
                type => type.ToString().ToLower(),
                value => Enum.Parse<AddressType>(value, true));
        
        // Configuração dos campos de endereço
        builder.Property(a => a.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(8)
            .IsRequired();
            
        builder.Property(a => a.Street)
            .HasColumnName("street")
            .HasMaxLength(150)
            .IsRequired();
            
        builder.Property(a => a.StreetNumber)
            .HasColumnName("street_number")
            .HasMaxLength(20)
            .IsRequired();
            
        builder.Property(a => a.Complement)
            .HasColumnName("complement")
            .HasMaxLength(100);
            
        builder.Property(a => a.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(a => a.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(a => a.StateCode)
            .HasColumnName("state_code")
            .HasMaxLength(2)
            .IsRequired();
            
        builder.Property(a => a.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(2)
            .IsRequired()
            .HasDefaultValue("BR");
            
        builder.Property(a => a.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);
            
        builder.Property(a => a.IsDefault)
            .HasColumnName("is_default")
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(a => a.Version)
            .HasColumnName("version")
            .IsRequired()
            .HasDefaultValue(1)
            .IsConcurrencyToken();
        
        // Configuração de índices
        builder.HasIndex(a => a.ClientId)
            .HasDatabaseName("idx_addresses_client_id");
            
        // Índice único para endereço padrão por cliente e tipo
        builder.HasIndex(a => new { a.ClientId, a.Type })
            .IsUnique()
            .HasDatabaseName("uq_addresses_default_per_client_type")
            .HasFilter("is_default = TRUE AND deleted_at IS NULL");
        
        // Configuração do relacionamento com Client
        builder.HasOne<Client>()
            .WithMany("Addresses")
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_addresses_client_id");
        
        // Configuração do filtro global para soft delete
        builder.HasQueryFilter(a => a.DeletedAt == null);
    }
}