using ClientService.Domain.Aggregates;
using ClientService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientService.Infra.ConfigEntityFramework;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        // Configuração da tabela
        builder.ToTable("clients");
        
        // Configuração da chave primária
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("client_id")
            .IsRequired();
        
        // Configuração dos campos herdados de Entity
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
            
        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at");
        
        // Configuração dos campos específicos do Client
        builder.Property(c => c.KeycloakUserId)
            .HasColumnName("keycloak_user_id");
            
        builder.Property(c => c.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(c => c.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(155)
            .IsRequired();
            
        // Configuração do Value Object Email
        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(
                email => email.Value,
                value => Domain.ValueObjects.Email.Create(value));
                
        builder.Property(c => c.EmailVerifiedAt)
            .HasColumnName("email_verified_at");
            
        builder.Property(c => c.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();
            
        // Configuração do Value Object CPF
        builder.Property(c => c.Cpf)
            .HasColumnName("cpf")
            .HasMaxLength(11)
            .HasConversion(
                cpf => cpf != null ? cpf.Value : null,
                value => value != null ? Domain.ValueObjects.Cpf.Create(value) : null);
                
        builder.Property(c => c.DateOfBirth)
            .HasColumnName("date_of_birth")
            .HasColumnType("date");
            
        // Configuração do Value Object Phone
        builder.Property(c => c.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => value != null ? Domain.ValueObjects.Phone.Create(value) : null);
                
        builder.Property(c => c.NewsletterOptIn)
            .HasColumnName("newsletter_opt_in")
            .IsRequired()
            .HasDefaultValue(false);
            
        // Configuração do Enum Status com conversão para string lowercase
        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(ClientStatus.Ativo)
            .HasConversion(
                status => status.ToString().ToLower(),
                value => Enum.Parse<ClientStatus>(value, true));
                
        // Configuração do Enum Role com conversão para string lowercase
        builder.Property(c => c.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasDefaultValue(UserRole.USER)
            .HasConversion(
                role => role.ToString().ToLower(),
                value => Enum.Parse<UserRole>(value, true));
                
        builder.Property(c => c.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(c => c.AccountLockedUntil)
            .HasColumnName("account_locked_until");
            
        builder.Property(c => c.Version)
            .HasColumnName("version")
            .IsRequired()
            .HasDefaultValue(1)
            .IsConcurrencyToken();
        
        // Configuração de índices únicos
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("idx_clients_active_email")
            .HasFilter("deleted_at IS NULL");
            
        builder.HasIndex(c => c.KeycloakUserId)
            .IsUnique()
            .HasFilter("keycloak_user_id IS NOT NULL");
            
        builder.HasIndex(c => c.Cpf)
            .IsUnique()
            .HasFilter("cpf IS NOT NULL AND deleted_at IS NULL");
            
        // Configuração de índices para performance
        builder.HasIndex(c => c.Status)
            .HasDatabaseName("idx_clients_status")
            .HasFilter("deleted_at IS NULL");
            
        builder.HasIndex(c => c.Role)
            .HasDatabaseName("idx_clients_role");
        
        // Configuração de relacionamentos com entidades filhas
        builder.HasMany(c => c.Addresses)
            .WithOne()
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(c => c.Consents)
            .WithOne()
            .HasForeignKey(co => co.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(c => c.SavedCards)
            .WithOne()
            .HasForeignKey(sc => sc.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configuração de Soft Delete - Query Filter Global
        builder.HasQueryFilter(c => c.DeletedAt == null);
        
        // Ignorar propriedades de navegação que são apenas para leitura
        builder.Ignore(c => c.Events);
    }
}