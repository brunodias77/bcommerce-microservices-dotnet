using ClientService.Domain.Aggregates;
using ClientService.Domain.Entities;
using ClientService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientService.Infra.ConfigEntityFramework;

public class SavedCardConfiguration : IEntityTypeConfiguration<SavedCard>
{
    public void Configure(EntityTypeBuilder<SavedCard> builder)
    {
        // Configuração da tabela
        builder.ToTable("saved_cards");
        
        // Configuração da chave primária
        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id)
            .HasColumnName("saved_card_id")
            .IsRequired();
        
        // Configuração dos campos herdados de Entity
        builder.Property(sc => sc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
            
        builder.Property(sc => sc.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.Property(sc => sc.DeletedAt)
            .HasColumnName("deleted_at");
        
        // Configuração da chave estrangeira para Client
        builder.Property(sc => sc.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
            
        // Configuração dos campos específicos do SavedCard
        builder.Property(sc => sc.Nickname)
            .HasColumnName("nickname")
            .HasMaxLength(50);
            
        builder.Property(sc => sc.LastFourDigits)
            .HasColumnName("last_four_digits")
            .HasMaxLength(4)
            .IsRequired();
            
        // Configuração do enum CardBrand com conversão para snake_case
        builder.Property(sc => sc.Brand)
            .HasColumnName("brand")
            .IsRequired()
            .HasConversion(
                brand => ConvertToSnakeCase(brand.ToString()),
                value => Enum.Parse<CardBrand>(ConvertToPascalCase(value), true));
                
        builder.Property(sc => sc.GatewayToken)
            .HasColumnName("gateway_token")
            .HasMaxLength(255)
            .IsRequired();
            
        builder.Property(sc => sc.ExpiryDate)
            .HasColumnName("expiry_date")
            .HasColumnType("date")
            .IsRequired();
            
        builder.Property(sc => sc.IsDefault)
            .HasColumnName("is_default")
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(sc => sc.Version)
            .HasColumnName("version")
            .IsRequired()
            .HasDefaultValue(1)
            .IsConcurrencyToken();
        
        // Configuração de índices
        builder.HasIndex(sc => sc.ClientId)
            .HasDatabaseName("idx_saved_cards_client_id");
            
        // Índice único para cartão padrão por cliente
        builder.HasIndex(sc => sc.ClientId)
            .IsUnique()
            .HasDatabaseName("uq_saved_cards_default_per_client")
            .HasFilter("is_default = TRUE AND deleted_at IS NULL");
            
        // Índice único para gateway_token
        builder.HasIndex(sc => sc.GatewayToken)
            .IsUnique()
            .HasDatabaseName("uq_saved_cards_gateway_token");
        
        // Configuração do relacionamento com Client
        builder.HasOne<Client>()
            .WithMany("SavedCards")
            .HasForeignKey(sc => sc.ClientId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_saved_cards_client_id");
        
        // Configuração do filtro global para soft delete
        builder.HasQueryFilter(sc => sc.DeletedAt == null);
    }
    
    // Método auxiliar para converter enum para snake_case
    private static string ConvertToSnakeCase(string input)
    {
        return input switch
        {
            "Visa" => "visa",
            "Mastercard" => "mastercard",
            "Amex" => "amex",
            "Elo" => "elo",
            "Hipercard" => "hipercard",
            "DinersClub" => "diners_club",
            "Discover" => "discover",
            "Jcb" => "jcb",
            "Aura" => "aura",
            "Other" => "other",
            _ => input.ToLower()
        };
    }
    
    // Método auxiliar para converter snake_case para PascalCase
    private static string ConvertToPascalCase(string input)
    {
        return input switch
        {
            "visa" => "Visa",
            "mastercard" => "Mastercard",
            "amex" => "Amex",
            "elo" => "Elo",
            "hipercard" => "Hipercard",
            "diners_club" => "DinersClub",
            "discover" => "Discover",
            "jcb" => "Jcb",
            "aura" => "Aura",
            "other" => "Other",
            _ => input
        };
    }
}