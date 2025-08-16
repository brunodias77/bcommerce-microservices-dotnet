using ClientService.Domain.Common;
using ClientService.Domain.Validations;

namespace ClientService.Domain.ValueObjects;

public class CardNumber : ValueObject
{
    public string LastFourDigits { get; private set; }
    
    // MaskedNumber como propriedade calculada (não persistida)
    public string MaskedNumber => $"**** **** **** {LastFourDigits}";

    // Construtor privado sem parâmetros para o Entity Framework
    private CardNumber() 
    {
        LastFourDigits = string.Empty;
    }

    // Construtor que aceita apenas LastFourDigits
    private CardNumber(string lastFourDigits)
    {
        LastFourDigits = lastFourDigits;
    }

    public static CardNumber Create(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ArgumentException("O número do cartão não pode ser vazio", nameof(cardNumber));

        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");
        
        if (!IsValidCardNumber(cleanNumber))
            throw new ArgumentException("Número de cartão inválido", nameof(cardNumber));

        var lastFour = cleanNumber.Substring(cleanNumber.Length - 4);
        return new CardNumber(lastFour);
    }

    private static bool IsValidCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || !cardNumber.All(char.IsDigit))
            return false;

        // Algoritmo de Luhn para validação de cartão de crédito
        int sum = 0;
        bool alternate = false;
        
        for (int i = cardNumber.Length - 1; i >= 0; i--)
        {
            int digit = int.Parse(cardNumber[i].ToString());
            
            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit = (digit % 10) + 1;
            }
            
            sum += digit;
            alternate = !alternate;
        }
        
        return sum % 10 == 0;
    }

    public override bool Equals(ValueObject? other)
    {
        return other is CardNumber card &&
               LastFourDigits == card.LastFourDigits;
    }

    protected override int GetCustomHashCode()
    {
        return LastFourDigits.GetHashCode();
    }

    public override void Validate(IValidationHandler handler)
    {
        throw new NotImplementedException();
    }
}