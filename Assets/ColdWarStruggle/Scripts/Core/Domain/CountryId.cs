using System;
namespace ColdWarStruggle.Core
{
    public readonly struct CountryId : IEquatable<CountryId>
    {
        public string Value { get; }
        public CountryId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Country ID is required.", nameof(value));
            Value = value;
        }
        public bool Equals(CountryId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is CountryId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "";
    }
}
