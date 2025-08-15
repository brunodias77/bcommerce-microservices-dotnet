using ClientService.Domain.Validations;

namespace ClientService.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    public abstract bool Equals(ValueObject? other);

    protected abstract int GetCustomHashCode();

    public override bool Equals(object? obj)
    {
        // Verifica se o objeto é nulo
        if (ReferenceEquals(null, obj)) return false;
        // Verifica se são a mesma referência
        if (ReferenceEquals(this, obj)) return true;
        // Verifica se são do mesmo tipo e compara valores
        return obj.GetType() == this.GetType() && Equals((ValueObject)obj);
    }
    
    public override int GetHashCode() => GetCustomHashCode();

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is not null && left.Equals(right);
    
    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);

    public abstract void Validate(IValidationHandler handler);
}