using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum SpaceAttemptEligibility { Allowed, ScoringCard, TrackCompleted, AttemptLimitReached, InsufficientOps }
    public enum SpaceCardDisposition { Discard, ChinaTransferRequired }
    public sealed class SpaceAttemptRequest
    {
        public string Id { get; }
        public string SourceActionId { get; }
        public int TurnNumber { get; }
        public PlayerSide Actor { get; }
        public CardDefinition Card { get; }
        public int EffectiveOps { get; }
        // The caller must supply a trusted card and effective OPS quote, not client-declared values.
        public SpaceAttemptRequest(string id, string sourceActionId, int turnNumber, PlayerSide actor, CardDefinition card, int effectiveOps)
        {
            SpaceDefinitionGuard.Id(id); SpaceDefinitionGuard.Id(sourceActionId);
            if (turnNumber < 1 || effectiveOps < 1 || (actor != PlayerSide.US && actor != PlayerSide.USSR)) throw new ArgumentException("Invalid space attempt facts.");
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Id = id; SourceActionId = sourceActionId; TurnNumber = turnNumber; Actor = actor; EffectiveOps = effectiveOps;
        }
        // Convenient trusted binding; ownership, phase and active restrictions remain host checks.
        public static SpaceAttemptRequest FromQuote(string id,string sourceActionId,int turnNumber,OpsGeneralQuote quote)
        {
            if (quote==null) throw new ArgumentNullException(nameof(quote));
            if (quote.Card==null || quote.Purpose!=OpsValuePurpose.Space) throw new ArgumentException("A real-card Space quote is required.");
            return new SpaceAttemptRequest(id,sourceActionId,turnNumber,quote.Actor,quote.Card,quote.GeneralValue);
        }
        internal bool SameFacts(SpaceAttemptRequest other) => other != null && Id == other.Id && SourceActionId == other.SourceActionId
            && TurnNumber == other.TurnNumber && Actor == other.Actor && ReferenceEquals(Card, other.Card) && EffectiveOps == other.EffectiveOps;
    }
    public sealed class SpaceAttemptReceipt
    {
        internal SpaceAttemptReceipt(SpaceAttemptRequest request, int roll, int from, int to, bool? first, int award, RandomSnapshot before, RandomSnapshot after)
        {
            Request=request;Roll=roll;FromPosition=from;ToPosition=to;Success=to>from;FirstArrival=first;AwardVp=award;
            NetVpForUS=request.Actor==PlayerSide.US?award:-award;
            Disposition=request.Card.Kind==CardKind.China?SpaceCardDisposition.ChinaTransferRequired:SpaceCardDisposition.Discard;
            RandomBefore=before;RandomAfter=after;
        }
        public SpaceAttemptRequest Request { get; }
        public int Roll { get; }
        public int FromPosition { get; }
        public int ToPosition { get; }
        public bool Success { get; }
        public bool? FirstArrival { get; }
        public int AwardVp { get; }
        public int NetVpForUS { get; }
        public SpaceCardDisposition Disposition { get; }
        public bool ExecuteCardEvent => false;
        public RandomSnapshot RandomBefore { get; }
        public RandomSnapshot RandomAfter { get; }
    }
    public sealed class SpaceAttemptState
    {
        public SpaceTrackState Track { get; }
        public TurnCalendar Calendar { get; }
        public GameTurnContext Turn { get; }
        public int UsAttempts { get; }
        public int UssrAttempts { get; }
        public IReadOnlyDictionary<string, SpaceAttemptReceipt> Receipts { get; }
        internal SpaceAttemptState(SpaceTrackState track, TurnCalendar calendar, GameTurnContext turn, int us, int ussr, IReadOnlyDictionary<string, SpaceAttemptReceipt> receipts)
        {
            Track = track; Calendar = calendar; Turn = turn; UsAttempts = us; UssrAttempts = ussr;
            Receipts = new ReadOnlyDictionary<string, SpaceAttemptReceipt>(receipts.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));
        }
        public static SpaceAttemptState Create(SpaceTrackState track, TurnCalendar calendar, int turnNumber)
        {
            if (track == null || calendar == null) throw new ArgumentNullException();
            return new SpaceAttemptState(track, calendar, calendar.ForTurn(turnNumber), 0, 0, new Dictionary<string, SpaceAttemptReceipt>());
        }
        public int AttemptsFor(PlayerSide side) => side switch
        {
            PlayerSide.US => UsAttempts,
            PlayerSide.USSR => UssrAttempts,
            _ => throw new ArgumentOutOfRangeException(nameof(side))
        };
    }
}
