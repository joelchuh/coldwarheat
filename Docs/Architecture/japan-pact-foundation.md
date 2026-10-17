# Japan pact foundation

Purpose: make the next real US/Japan pact event possible without coupling card rules to Unity or map art. This batch adds a tested foundation; actual card dispatch, permanent state retention by the practice host and UI follow in a separate batch. User has explicitly requested autonomous continued development without approval questions.

## Decisions

Use a reusable AddToControlInfluenceEffect through the existing influence Query/Plan/Flow. It only increases the beneficiary to opponent + map stability, never clears the opponent or reduces excess influence; unchanged still resolves. A checked private mutation protects amount, version and event-sequence overflow. Existing no-interference, setup, ended and selection gates stay effective.

Use a bounded JapanPactDefinition and JapanPactState rather than prematurely supporting arbitrary treaty JSON or NATO. NATO needs successful-event history and country exceptions not implemented here. Hardcoding a UI toggle would bypass all non-UI actions; a general effect interpreter would add unsupported semantics. The chosen state is immutable, separate from turn OPS expiry, and retains source identity, actor, game, source revision/sequence and parameter version/digest. No save import or cancellation API is claimed.

JapanPactDefinitionLoader.Load(json, schema, map, cards) validates the entire declared schema, the release-candidate 0.1.0 Deluxe profile, exact source catalog and verified Japan references, then only assembles the Japan subset. Its semantics are no gate, permanent duration, AddToControl for US/Japan, USSR Coup+Realignment only, no exceptions. Printed binding must match card/effect/parameter pointer, US event, starred and persistent. Nonconsumed NATO values remain schema-checked, not advertised as runtime-implemented. Map and card catalog references bind the definition. Full source-document canonical digest and content version travel with it.

JapanPactState.Create(definition, gameId) creates authoritative inactive state. Activate(board, controller, resolutionId) returns a new candidate only; validates map/game, ended/setup, actual actor, normalized bounded ID, checked activation sequence. Same ID and identical source revision/sequence/controller returns AlreadyApplied; changed request conflicts; a different ID while active requires ruling. Activation does not itself apply the immediate grant: the integration caller must publish grant + activation + card lifecycle atomically. No public setter, implicit turn expiry or UI mutation.

JapanPactRules.Evaluate(board, treaty, action, actor, country) is a pure single-treaty query. It distinguishes InvalidContext, BlockedByTreaty and NoTreatyBlock; the last is not overall permission. The active pact blocks USSR Coup/Realignment in Japan regardless of current control, root actor or turn. It does not block US, other countries, influence placement or event attacks. Invalid enums, country, map/game, ended and setup cannot become permissions. DEFCON, target influence and budget remain separate later gates.

## Evidence and boundaries

Official GMT English Deluxe #27 (TS_Cards_Deluxe.pdf, p5) and 2015 rules §2.1.7, §7.3 checked during this task; no new project interpretation. Existing formal treaty-protection parameters remain the only shared source. Test both zero grant and overflow, adverse JSON and printed binding, replay/conflict, mutable-control and across-turn queries. This batch changes no Player registration or appearance and requires no visual acceptance. Source: https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf and https://www.gmtgames.com/nnts/TS_Rules-2015.pdf.
