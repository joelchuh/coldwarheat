using System;
using System.Text.RegularExpressions;
namespace ColdWarStruggle.Core
{
    public readonly struct CardId : IEquatable<CardId>
    {
        public string Value { get; }
        public CardId(string value)
        {
            if(value==null || value.Length>128 || !Regex.IsMatch(value,@"^card\.[a-z][a-z0-9_]*(\.[a-z0-9_]+)*\z",RegexOptions.CultureInvariant))
                throw new ArgumentException("Invalid stable card ID.",nameof(value));
            Value=value;
        }
        public bool Equals(CardId other)=>StringComparer.Ordinal.Equals(Value,other.Value);
        public override bool Equals(object other)=>other is CardId id && Equals(id);
        public override int GetHashCode()=>Value==null?0:StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString()=>Value??"";
    }
}
