using System;
namespace ColdWarStruggle.Core
{
    public sealed class JapanPactInstance
    {
        public JapanPactDefinition Definition { get; }
        public string ResolutionId { get; }
        public PlayerSide Controller { get; }
        public long SourceVersion { get; }
        public long SourceEventSequence { get; }
        public long ActivationSequence { get; }
        internal JapanPactInstance(JapanPactDefinition definition,string resolution,PlayerSide controller,GameState source,long sequence)
        { Definition=definition;ResolutionId=resolution;Controller=controller;SourceVersion=source.Version;SourceEventSequence=source.EventSequence;ActivationSequence=sequence; }
    }
    public sealed class JapanPactActivation
    {
        public RuleError Error { get; }
        public bool Accepted => Error==RuleError.None;
        public JapanPactState After { get; }
        public JapanPactInstance Instance => After?.Instance;
        public bool AlreadyApplied { get; }
        internal JapanPactActivation(RuleError error,JapanPactState after=null,bool applied=false)
        { Error=error;After=after;AlreadyApplied=applied; }
    }
    public sealed class JapanPactState
    {
        public JapanPactDefinition Definition { get; }
        public string GameId { get; }
        public JapanPactInstance Instance { get; }
        JapanPactState(JapanPactDefinition definition,string gameId,JapanPactInstance instance)
        { Definition=definition;GameId=gameId;Instance=instance; }
        public static JapanPactState Create(JapanPactDefinition definition,string gameId)
        {
            if(definition==null || string.IsNullOrWhiteSpace(gameId))throw new ArgumentException("Missing Japan pact binding.");
            return new JapanPactState(definition,gameId,null);
        }
        internal RuleError CheckBoard(GameState board)
        {
            if(board==null)return RuleError.InvalidCommand;
            if(board.GameId!=GameId)return RuleError.WrongGame;
            if(!ReferenceEquals(board.Map,Definition.Map))return RuleError.InvalidDefinition;
            if(board.Ended)return RuleError.GameAlreadyEnded;
            if(board.Deployment!=null)return RuleError.SetupInProgress;
            return RuleError.None;
        }
        // A private candidate: the caller must commit the grant, this instance and card lifecycle together.
        public JapanPactActivation Activate(GameState board,PlayerSide controller,string resolutionId)
        {
            var error=CheckBoard(board);if(error!=RuleError.None)return new JapanPactActivation(error);
            if((controller!=PlayerSide.US && controller!=PlayerSide.USSR) || string.IsNullOrWhiteSpace(resolutionId)
                || resolutionId.Length>128 || resolutionId!=resolutionId.Trim())return new JapanPactActivation(RuleError.InvalidCommand);
            if(Instance!=null)
            {
                if(Instance.ResolutionId!=resolutionId)return new JapanPactActivation(RuleError.EventRequiresRuling);
                return Instance.Controller==controller && Instance.SourceVersion==board.Version && Instance.SourceEventSequence==board.EventSequence
                    ?new JapanPactActivation(RuleError.None,this,true):new JapanPactActivation(RuleError.CommandIdConflict);
            }
            try
            {
                var instance=new JapanPactInstance(Definition,resolutionId,controller,board,checked(board.EventSequence+1));
                return new JapanPactActivation(RuleError.None,new JapanPactState(Definition,GameId,instance));
            }
            catch(OverflowException){return new JapanPactActivation(RuleError.NumericOverflow);}
        }
    }
}
