using ClientService.Domain.Aggregates;
using ClientService.Domain.Entities;
using ClientService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientService.Infra.ConfigEntityFramework;

public class ConsentConfiguration : IEntityTypeConfiguration<Consent>
{
    public void Configure(EntityTypeBuilder<Consent> builder)
    {
        // Configuração da tabela
        builder.ToTable("consents");
        
        // Configuração da chave primária
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("consent_id")
            .IsRequired();
        
        // Configuração dos campos herdados de Entity
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
            
        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");
            
        // Nota: DeletedAt não existe na tabela consents do banco
        builder.Ignore(c => c.DeletedAt);
        
        // Configuração da chave estrangeira para Client
        builder.Property(c => c.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
            
        // Configuração do enum ConsentType com conversão para snake_case
        builder.Property(c => c.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasConversion(
                type => ConvertToSnakeCase(type.ToString()),
                value => Enum.Parse<ConsentType>(ConvertToPascalCase(value), true));
        
        // Configuração dos campos específicos do Consent
        builder.Property(c => c.TermsVersion)
            .HasColumnName("terms_version")
            .HasMaxLength(30);
            
        builder.Property(c => c.IsGranted)
            .HasColumnName("is_granted")
            .IsRequired();
            
        builder.Property(c => c.Version)
            .HasColumnName("version")
            .IsRequired()
            .HasDefaultValue(1)
            .IsConcurrencyToken();
        
        // Configuração de índices
        builder.HasIndex(c => c.ClientId)
            .HasDatabaseName("idx_consents_client_id");
            
        // Constraint única para client_id + type
        builder.HasIndex(c => new { c.ClientId, c.Type })
            .IsUnique()
            .HasDatabaseName("uq_client_consent_type");
        
        // Configuração do relacionamento com Client
        builder.HasOne<Client>()
            .WithMany("Consents")
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_consents_client_id");
    }
    
    // Método auxiliar para converter enum para snake_case
    private static string ConvertToSnakeCase(string input)
    {
        return input switch
        {
            "MarketingEmail" => "marketing_email",
            "NewsletterSubscription" => "newsletter_subscription",
            "TermsOfService" => "terms_of_service",
            "PrivacyPolicy" => "privacy_policy",
            "CookiesEssential" => "cookies_essential",
            "CookiesAnalytics" => "cookies_analytics",
            "CookiesMarketing" => "cookies_marketing",
            _ => input.ToLower()
        };
    }
    
    // Método auxiliar para converter snake_case para PascalCase
    private static string ConvertToPascalCase(string input)
    {
        return input switch
        {
            "marketing_email" => "MarketingEmail",
            "newsletter_subscription" => "NewsletterSubscription",
            "terms_of_service" => "TermsOfService",
            "privacy_policy" => "PrivacyPolicy",
            "cookies_essential" => "CookiesEssential",
            "cookies_analytics" => "CookiesAnalytics",
            "cookies_marketing" => "CookiesMarketing",
            _ => input
        };
    }
}