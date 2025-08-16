using ClientService.Domain.Validations;

namespace ClientService.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    
    protected Entity() {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow; // Podemos até definir a data de criação aqui!
    }
    public abstract void Validate(IValidationHandler handler);
    
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }
    
    public override int GetHashCode() => Id.GetHashCode();
    
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }
    
    public static bool operator !=(Entity? left, Entity? right) => !(left == right);


}