using System;
using System.Collections.Generic;
using System.Linq;

namespace ColdWarStruggle.Core
{
    public enum GameEra { Early, Mid, Late }
    public sealed class EraTurnParameters
    {
        public GameEra Era { get; }
        public int FirstTurn { get; }
        public int LastTurn { get; }
        public int HandTarget { get; }
        public int NormalActionsPerSide { get; }
        public EraTurnParameters(GameEra era,int firstTurn,int lastTurn,int handTarget,int normalActionsPerSide)
        {
            if(!Enum.IsDefined(typeof(GameEra),era) || firstTurn<1 || lastTurn<firstTurn || handTarget<1 || normalActionsPerSide<1)
                throw new ArgumentOutOfRangeException();
            Era=era;FirstTurn=firstTurn;LastTurn=lastTurn;HandTarget=handTarget;NormalActionsPerSide=normalActionsPerSide;
        }
    }
    public sealed class GameTurnContext
    {
        public int Number { get; }
        public GameEra Era => Parameters.Era;
        public EraTurnParameters Parameters { get; }
        internal GameTurnContext(int number,EraTurnParameters parameters)
        {
            if(parameters==null || number<parameters.FirstTurn || number>parameters.LastTurn)throw new ArgumentOutOfRangeException();
            Number=number;Parameters=parameters;
        }
    }
    public sealed class TurnCalendar
    {
        public IReadOnlyList<EraTurnParameters> Bands { get; }
        public int LastTurn { get; }
        public TurnCalendar(IEnumerable<EraTurnParameters> bands)
        {
            var copy=bands?.Take(4).ToArray();
            if(copy==null || copy.Length!=3 || copy.Any(b=>b==null) || copy.Select(b=>b.Era).Distinct().Count()!=3)
                throw new ArgumentException("Calendar requires each supported era once.");
            copy=copy.OrderBy(b=>b.Era).ToArray();
            long next=1;
            foreach(var band in copy)
            {
                if(band.FirstTurn!=next)throw new ArgumentException("Calendar bands must be consecutive and ordered by era.");
                next=(long)band.LastTurn+1;
            }
            Bands=Array.AsReadOnly(copy);LastTurn=copy[copy.Length-1].LastTurn;
        }
        public GameTurnContext ForTurn(int number)
        {
            if(number<1 || number>LastTurn)throw new ArgumentOutOfRangeException(nameof(number));
            return new GameTurnContext(number,Bands.Single(b=>number>=b.FirstTurn && number<=b.LastTurn));
        }
    }
}
