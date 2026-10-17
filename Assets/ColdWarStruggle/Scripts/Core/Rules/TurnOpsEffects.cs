using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum TurnOpsAffectedSide { US, OpponentOfController, USSR }
    // This bounded model expresses remainder-of-turn flat or regional modifiers, not arbitrary persistent effects.
    public sealed class TurnOpsEffectDefinition
    {
        public CardId Card { get; }
        public string EffectId { get; }
        public TurnOpsAffectedSide AffectedSide { get; }
        public int Delta { get; }
        public OpsRegionalBonus RegionalBonus { get; }
        public AddFixedInfluenceEffect ImmediateInfluence { get; }
        public TurnOpsEffectDefinition(CardId card,string effectId,OpsRegionalBonus regional,AddFixedInfluenceEffect immediate)
        {
            if(string.IsNullOrWhiteSpace(card.Value) || string.IsNullOrWhiteSpace(effectId) || effectId.Length>128 || effectId!=effectId.Trim()
                || regional==null || immediate==null || regional.Id!=effectId || immediate.CardId!=card.Value || immediate.EffectId!=effectId
                || immediate.Beneficiary!=PlayerSide.USSR)throw new ArgumentException("Invalid regional turn OPS definition.");
            Card=card;EffectId=effectId;AffectedSide=TurnOpsAffectedSide.USSR;Delta=regional.Delta;RegionalBonus=regional;ImmediateInfluence=immediate;
        }
        public TurnOpsEffectDefinition(CardId card,string effectId,TurnOpsAffectedSide affectedSide,int delta)
        {
            if(string.IsNullOrWhiteSpace(card.Value) || string.IsNullOrWhiteSpace(effectId) || effectId.Length>128 || effectId!=effectId.Trim()
                || (affectedSide!=TurnOpsAffectedSide.US && affectedSide!=TurnOpsAffectedSide.OpponentOfController) || (affectedSide==TurnOpsAffectedSide.US?delta<=0:delta>=0))throw new ArgumentException("Invalid flat turn OPS definition.");
            Card=card;EffectId=effectId;AffectedSide=affectedSide;Delta=delta;
        }
    }
    public sealed class PracticeTurnOpsCatalog
    {
        public CardCatalog Cards { get; }
        public IReadOnlyDictionary<CardId,TurnOpsEffectDefinition> Effects { get; }
        public PracticeTurnOpsCatalog(CardCatalog cards,IEnumerable<TurnOpsEffectDefinition> effects)
        {
            if(cards==null || effects==null)throw new ArgumentNullException();
            var list=effects.Take(3).ToArray();
            if(list.Length<1 || list.Length>2 || list.Any(e=>e==null || e.RegionalBonus!=null) || list.Select(e=>e.Card).Distinct().Count()!=list.Length)throw new ArgumentException("Invalid flat OPS catalog.");
            foreach(var e in list)
            {
                var printed=cards.GetCard(e.Card);var name=e.AffectedSide==TurnOpsAffectedSide.US?"containment":"red_scare_purge";
                if(e.Card.Value!="card."+name || e.EffectId!="effect."+name || printed.Kind!=CardKind.Event || printed.Effect?.EffectId!=e.EffectId
                    || printed.Affiliation!=(e.AffectedSide==TurnOpsAffectedSide.US?CardAffiliation.US:CardAffiliation.Neutral))throw new ArgumentException("Unsupported flat OPS binding.");
            }
            Cards=cards;Effects=new ReadOnlyDictionary<CardId,TurnOpsEffectDefinition>(list.ToDictionary(e=>e.Card));
        }
    }
    public sealed class TurnOpsEffectInstance
    {
        public TurnOpsEffectDefinition Definition { get; }
        public string ResolutionId { get; }
        public PlayerSide Controller { get; }
        public PlayerSide AffectedSide { get; }
        public int Turn { get; }
        public long ActivationSequence { get; }
        public string ExpiryBoundary => "ExpireAndReset";
        internal TurnOpsEffectInstance(TurnOpsEffectDefinition definition,string resolution,PlayerSide controller,int turn,long sequence)
        { Definition=definition;ResolutionId=resolution;Controller=controller;Turn=turn;ActivationSequence=sequence;AffectedSide=definition.AffectedSide==TurnOpsAffectedSide.US?PlayerSide.US:definition.AffectedSide==TurnOpsAffectedSide.USSR?PlayerSide.USSR:controller==PlayerSide.US?PlayerSide.USSR:PlayerSide.US; }
    }
    public sealed class TurnOpsActivation
    {
        public RuleError Error { get; }
        public bool Accepted => Error==RuleError.None;
        public TurnOpsEffectState After { get; }
        public TurnOpsEffectInstance Instance { get; }
        public bool AlreadyApplied { get; }
        internal TurnOpsActivation(RuleError error,TurnOpsEffectState after=null,TurnOpsEffectInstance instance=null,bool applied=false)
        { Error=error;After=after;Instance=instance;AlreadyApplied=applied; }
    }
    public sealed class TurnOpsEffectState
    {
        public GameTurnContext Turn { get; }
        public IReadOnlyList<TurnOpsEffectInstance> Instances { get; }
        TurnOpsEffectState(GameTurnContext turn,IEnumerable<TurnOpsEffectInstance> instances)
        { Turn=turn;Instances=Array.AsReadOnly(instances.ToArray()); }
        public static TurnOpsEffectState Create(GameTurnContext turn)
            =>new TurnOpsEffectState(turn??throw new ArgumentNullException(nameof(turn)),Array.Empty<TurnOpsEffectInstance>());
        bool Matches(GameTurnContext turn)=>turn!=null && turn.Number==Turn.Number && turn.Era==Turn.Era;
        public TurnOpsActivation Activate(TurnOpsEffectDefinition effect,PlayerSide controller,string resolutionId,long sequence,GameTurnContext currentTurn)
        {
            if(effect==null)return new TurnOpsActivation(RuleError.InvalidDefinition);
            if((controller!=PlayerSide.US && controller!=PlayerSide.USSR) || string.IsNullOrWhiteSpace(resolutionId) || resolutionId.Length>128 || resolutionId!=resolutionId.Trim() || sequence<0)return new TurnOpsActivation(RuleError.InvalidCommand);
            if(!Matches(currentTurn))return new TurnOpsActivation(RuleError.StaleContext);
            var old=Instances.SingleOrDefault(i=>i.ResolutionId==resolutionId);
            if(old!=null)return ReferenceEquals(old.Definition,effect) && old.Controller==controller && old.ActivationSequence==sequence
                ?new TurnOpsActivation(RuleError.None,this,old,true):new TurnOpsActivation(RuleError.CommandIdConflict);
            var instance=new TurnOpsEffectInstance(effect,resolutionId,controller,Turn.Number,sequence);
            if(Instances.Any(i=>i.Definition.EffectId==effect.EffectId && i.AffectedSide==instance.AffectedSide))return new TurnOpsActivation(RuleError.EventRequiresRuling);
            if(Instances.Count>=128)return new TurnOpsActivation(RuleError.InvalidDefinition);
            return new TurnOpsActivation(RuleError.None,new TurnOpsEffectState(Turn,Instances.Append(instance)),instance);
        }
        internal TurnOpsEffectState Expire(GameTurnContext currentTurn)
        {
            if(!Matches(currentTurn))throw new ArgumentException("Cannot expire another turn's effects.");
            return Create(currentTurn);
        }
        public IReadOnlyList<OpsRegionalBonus> GetRegionalBonuses(GameTurnContext currentTurn,PlayerSide actor)
        {
            if(!Matches(currentTurn))throw new ArgumentException("Turn effects require explicit expiry before changing turn.");
            if(actor!=PlayerSide.US && actor!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(actor));
            return Array.AsReadOnly(Instances.Where(i=>i.AffectedSide==actor && i.Definition.RegionalBonus!=null).Select(i=>i.Definition.RegionalBonus).ToArray());
        }
        public IReadOnlyList<OpsFlatModifier> GetModifiers(GameTurnContext currentTurn)
        {
            if(!Matches(currentTurn))throw new ArgumentException("Turn effects require explicit expiry before changing turn.");
            return Array.AsReadOnly(Instances.Where(i=>i.Definition.RegionalBonus==null).Select(i=>new OpsFlatModifier(i.Definition.EffectId+":"+i.AffectedSide,i.AffectedSide,i.Definition.Delta)).ToArray());
        }
    }
}
