namespace Domain.Shared;

public abstract class ValueObject : IEquatable<ValueObject>
{
    public abstract IEnumerable<object> GetEqualityCommponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var valueObject = (ValueObject) obj;

        return GetEqualityCommponents().SequenceEqual(valueObject.GetEqualityCommponents());
    }

    public bool Equals(ValueObject? other) => Equals((object?)other);

    public static bool operator ==(ValueObject left, ValueObject rigth) => Equals(left, rigth);

    public static bool operator !=(ValueObject left, ValueObject rigth) => !Equals(left, rigth);

    public override int GetHashCode()
    {
        return GetEqualityCommponents().Select(x => x?.GetHashCode() ?? 0)
                                       .Aggregate((x, y) => x^y);
    }
}