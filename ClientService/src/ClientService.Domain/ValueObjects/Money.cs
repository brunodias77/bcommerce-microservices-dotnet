using ClientService.Domain.Common;
using ClientService.Domain.Validations;

namespace ClientService.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ArgumentException("O valor não pode ser negativo", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("A moeda não pode ser vazia", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("A moeda deve ter 3 caracteres", nameof(currency));

        return new Money(amount, currency.ToUpper());
    }

    public override bool Equals(ValueObject? other)
    {
        return other is Money money &&
               Amount == money.Amount &&
               Currency == money.Currency;
    }

    protected override int GetCustomHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    public override void Validate(IValidationHandler handler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"{Amount:C} {Currency}";
    }
}