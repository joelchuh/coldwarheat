using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace ColdWarStruggle.Core
{
    public enum CardKind { Event, Scoring, China }
    public enum CardAffiliation { US, USSR, Neutral, None }
    public enum CardDeckEntry { EraDeck, SpecialChina }
    public enum CardParameterBindingKind { NoParameters, Bound }
    public sealed class CardParameterReference
    {
        public string DatasetId { get; }
        public string JsonPointer { get; }
        public CardParameterReference(string datasetId,string jsonPointer)
        {
            if(datasetId==null || datasetId.Length>128 || !Regex.IsMatch(datasetId,@"^[a-z][a-z0-9_-]*(\.[a-z0-9_-]+)*\z",RegexOptions.CultureInvariant)
                || jsonPointer==null || jsonPointer.Length>512 || !Regex.IsMatch(jsonPointer,@"^(/([^~/]|~0|~1)*)*\z",RegexOptions.CultureInvariant))
                throw new ArgumentException("Invalid parameter reference.");
            DatasetId=datasetId;JsonPointer=jsonPointer;
        }
    }
    public sealed class CardEffectBinding
    {
        public string EffectId { get; }
        public string ContractVersion { get; }
        public CardParameterBindingKind Kind { get; }
        public IReadOnlyList<CardParameterReference> Parameters { get; }
        public CardEffectBinding(string effectId,string version,CardParameterBindingKind kind,IEnumerable<CardParameterReference> parameters)
        {
            if(effectId==null || effectId.Length>128 || !Regex.IsMatch(effectId,@"^effect\.[a-z][a-z0-9_]*(\.[a-z0-9_]+)*\z",RegexOptions.CultureInvariant)
                || version==null || version.Length>64 || !Regex.IsMatch(version,@"^[0-9]+\.[0-9]+\.[0-9]+\z",RegexOptions.CultureInvariant)
                || !Enum.IsDefined(typeof(CardParameterBindingKind),kind) || parameters==null)throw new ArgumentException("Invalid effect binding.");
            var copy=parameters.Take(129).ToArray();
            if(copy.Length>128 || copy.Any(p=>p==null) || copy.Select(p=>(p.DatasetId,p.JsonPointer)).Distinct().Count()!=copy.Length
                || (kind==CardParameterBindingKind.Bound?copy.Length==0:copy.Length!=0))throw new ArgumentException("Invalid binding parameter set.");
            EffectId=effectId;ContractVersion=version;Kind=kind;Parameters=Array.AsReadOnly(copy);
        }
    }
    public sealed class CardDefinition
    {
        public CardId Id { get; }
        public int PrintedNumber { get; }
        public string NameKey { get; }
        public string RulesTextKey { get; }
        public CardKind Kind { get; }
        public int? PrintedOps { get; }
        public CardAffiliation Affiliation { get; }
        public GameEra? Era { get; }
        public CardDeckEntry DeckEntry { get; }
        public bool RemoveAfterResolvedEvent { get; }
        public bool DisplayPersistentReminder { get; }
        public string ScoringRegion { get; }
        public CardEffectBinding Effect { get; }
        public CardDefinition(CardId id,int number,string nameKey,string rulesKey,CardKind kind,int? ops,CardAffiliation affiliation,GameEra? era,
            CardDeckEntry deckEntry,bool remove,bool reminder,string scoringRegion,CardEffectBinding effect)
        {
            if(string.IsNullOrWhiteSpace(id.Value) || number<1 || string.IsNullOrWhiteSpace(nameKey) || string.IsNullOrWhiteSpace(rulesKey)
                || !Enum.IsDefined(typeof(CardKind),kind) || !Enum.IsDefined(typeof(CardAffiliation),affiliation) || !Enum.IsDefined(typeof(CardDeckEntry),deckEntry)
                || (era.HasValue && !Enum.IsDefined(typeof(GameEra),era.Value)) || (ops.HasValue && (ops.Value<1 || ops.Value>4)))throw new ArgumentException("Invalid card definition.");
            if(kind==CardKind.China)
            {
                if(id.Value!="card.china" || !ops.HasValue || affiliation!=CardAffiliation.None || era.HasValue || deckEntry!=CardDeckEntry.SpecialChina
                    || remove || reminder || scoringRegion!=null || effect!=null)throw new ArgumentException("Invalid China definition.");
            }
            else
            {
                if(id.Value=="card.china" || !era.HasValue || deckEntry!=CardDeckEntry.EraDeck || effect==null)throw new ArgumentException("Invalid era card definition.");
                if(kind==CardKind.Event)
                {
                    if(!ops.HasValue || affiliation==CardAffiliation.None || scoringRegion!=null)throw new ArgumentException("Invalid event definition.");
                }
                else if(ops.HasValue || affiliation!=CardAffiliation.None || reminder || scoringRegion==null || scoringRegion.Length>128
                    || !Regex.IsMatch(scoringRegion,@"^region\.[a-z][a-z0-9_]*(\.[a-z0-9_]+)*\z",RegexOptions.CultureInvariant))throw new ArgumentException("Invalid scoring definition.");
            }
            Id=id;PrintedNumber=number;NameKey=nameKey;RulesTextKey=rulesKey;Kind=kind;PrintedOps=ops;Affiliation=affiliation;Era=era;DeckEntry=deckEntry;
            RemoveAfterResolvedEvent=remove;DisplayPersistentReminder=reminder;ScoringRegion=scoringRegion;Effect=effect;
        }
    }
}
