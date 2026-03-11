namespace Ebony.Core.Extraction;

public abstract class Id(string key = "UNKNOWN")
{
    // Different backends can support different kinds of data types to identify their domain.
    // This class aims to support all of them as an abstraction layer.
    public const char KeySeparator = '⦙'; // Group Separator
    
    private string Key => key;

    public static Id Empty => new EmptyId();

    public static Id Undetermined => new UndeterminedId();
    
    protected abstract string GetId();

    public sealed override string ToString()
    {
        return $"{key}{KeySeparator}{GetId()}";
    }

    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
    
    public static bool operator ==(Id? left, Id? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Id? left, Id? right)
    {
        return !Equals(left, right);
    }

    public abstract class TypedId<TId>(TId value, string key) : Id(key)
    {
        public TId Value => value;

        protected override string GetId()
        {
            return value?.ToString() ?? string.Empty;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not TypedId<TId> other) return false;
            return Key == other.Key && EqualityComparer<TId>.Default.Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Value);
        }
    }

    private class EmptyId() : TypedId<string>("", "NIL");
    private class UndeterminedId() : TypedId<string>("", "NIL");
}