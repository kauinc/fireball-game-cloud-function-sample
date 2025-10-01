using System.Collections.Generic;

namespace SampleProject
{
    public class SampleGameState
    {
        public bool IsGameOver;
        public int CurrentScore;
        public string CurrentState;
        public Dictionary<string, int> GameBoard;
    }
}