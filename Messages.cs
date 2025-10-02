using System.Collections.Generic;
using Fireball.Game.Server.Models;

namespace SampleProject
{
    public class GameMessages
    {
        public const string SPIN_REQUEST = "spin-request";
        public const string SPIN_RESULT = "spin-result";
    }
    
    public class SampleSpinRequest : BaseMessage
    {
        public string GameType;
        public long BetAmount;

        public SampleSpinRequest()
        {
            Name = GameMessages.SPIN_REQUEST;
        }
    }

    public class SampleSpinResult : BaseMessage
    {
        public string GameType;
        public long WinAmount;

        public SampleSpinResult(string gameType, long winAmount, BaseMessage baseMessage)
        {
            CopyBaseParams(baseMessage);

            Name = GameMessages.SPIN_RESULT;
            GameType = gameType;
            WinAmount = winAmount;
        }
    }
}