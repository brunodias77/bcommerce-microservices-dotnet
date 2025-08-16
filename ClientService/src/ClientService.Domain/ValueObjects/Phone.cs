using ClientService.Domain.Common;
using ClientService.Domain.Validations;

namespace ClientService.Domain.ValueObjects;

public class Phone : ValueObject
{
    public string Value { get; private set; }

    private Phone(string value)
    {
        Value = value;
    }

    public static Phone Create(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("O telefone não pode ser vazio", nameof(phone));

        var cleanPhone = phone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        
        if (!IsValidPhone(cleanPhone))
            throw new ArgumentException("Formato de telefone inválido", nameof(phone));

        return new Phone(cleanPhone);
    }

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Verifica se todos os caracteres são números
        if (!phone.All(char.IsDigit))
            return false;

        // Validação básica para números brasileiros: 10 ou 11 dígitos
        // Formatos aceitos: (XX) XXXX-XXXX ou (XX) 9XXXX-XXXX
        return phone.Length == 10 || phone.Length == 11;
    }

    public string FormattedValue
    {
        get
        {
            if (Value.Length == 11)
                return $"({Value.Substring(0, 2)}) {Value.Substring(2, 5)}-{Value.Substring(7, 4)}";
            else if (Value.Length == 10)
                return $"({Value.Substring(0, 2)}) {Value.Substring(2, 4)}-{Value.Substring(6, 4)}";
            else
                return Value;
        }
    }
    
    public override string ToString() => FormattedValue;
    public override bool Equals(ValueObject? other)
    {
        return other is Phone phone && Value == phone.Value;
    }

    protected override int GetCustomHashCode()
    {
        return Value.GetHashCode();
    }

    public override void Validate(IValidationHandler handler)
    {
        if (string.IsNullOrWhiteSpace(Value))
            handler.Add(new Error("","Telefone é obrigatório"));
            
        if (!IsValidPhone(Value))
            handler.Add(new Error("","Formato de telefone inválido"));
    }
}