using ClientService.Domain.Common;
using ClientService.Domain.Validations;

namespace ClientService.Domain.ValueObjects;

public class Cpf : ValueObject
{
    public string Value { get; private set; }

    private Cpf(string value)
    {
        Value = value;
    }

    public static Cpf Create(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) throw new ArgumentException("O CPF não pode ser vazio", nameof(cpf));
        var cleanCpf = cpf.Replace(".", "").Replace("-", "").Replace(" ", "");
        if (!IsValidCPF(cleanCpf)) throw new ArgumentException("CPF inválido", nameof(cpf));

        return new Cpf(cleanCpf);
    }
    
    private static bool IsValidCPF(string cpf)
    {
        if (cpf.Length != 11)
            return false;

        // Verifica se todos os dígitos são iguais
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Verifica se todos os caracteres são números
        if (!cpf.All(char.IsDigit))
            return false;

        var digits = cpf.Select(c => int.Parse(c.ToString())).ToArray();
        
        // Valida o primeiro dígito verificador
        var sum = 0;
        for (int i = 0; i < 9; i++)
            sum += digits[i] * (10 - i);
        
        var remainder = sum % 11;
        var firstDigit = remainder < 2 ? 0 : 11 - remainder;
        
        if (digits[9] != firstDigit)
            return false;

        // Valida o segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += digits[i] * (11 - i);
        
        remainder = sum % 11;
        var secondDigit = remainder < 2 ? 0 : 11 - remainder;
        
        return digits[10] == secondDigit;
    }
    
    public string FormattedValue => $"{Value.Substring(0, 3)}.{Value.Substring(3, 3)}.{Value.Substring(6, 3)}-{Value.Substring(9, 2)}";

    public override string ToString() => FormattedValue;
    public override bool Equals(ValueObject? other)
    {
        return other is Cpf cpf && Value == cpf.Value;
    }

    protected override int GetCustomHashCode()
    {
        return Value.GetHashCode();
    }

    public override void Validate(IValidationHandler handler)
    {
        if (string.IsNullOrWhiteSpace(Value))
            handler.Add(new Error("","CPF é obrigatório"));
            
        if (!IsValidCPF(Value))
            handler.Add(new Error("", "CPF inválido"));
    }
}