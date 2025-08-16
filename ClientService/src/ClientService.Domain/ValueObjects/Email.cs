using System.Text.RegularExpressions;
using ClientService.Domain.Common;
using ClientService.Domain.Validations;

namespace ClientService.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; private set; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O e-mail não pode ser vazio", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Formato de e-mail inválido", nameof(email));

        return new Email(email.ToLowerInvariant());
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Usa regex para validação de e-mail
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Value;


    public override bool Equals(ValueObject? other)
    {
        return other is Email email && Value == email.Value;
    }

    protected override int GetCustomHashCode()
    {
        return Value.GetHashCode();
    }

    public override void Validate(IValidationHandler handler)
    {
        if (string.IsNullOrWhiteSpace(Value))
            handler.Add(new Error("","E-mail é obrigatório"));
            
        if (!IsValidEmail(Value))
            handler.Add(new Error("", "Formato de e-mail inválido"));
    }
}