using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fireball.Game.Server;
using Fireball.Game.Server.Models;
using Fireball.Game.Server.Rng;
using Microsoft.Extensions.Logging;

namespace SampleProject
{
    public interface IGame
    {
        Task<MessageResult> HandleMessage(ParseResult result);
    }

    public class Game : IGame
    {
        private readonly IFireballLogger _logger;
        private readonly IFireball _fireball;
        private readonly IRng _fireballRng;

        public Game(ILogger<Game> logger, IFireball fireball, IRng fireballRng)
        {
            _logger = new FireballLogger(nameof(Game), logger);
            _fireball = fireball;
            _fireballRng = fireballRng;
        }

        public async Task<MessageResult> HandleMessage(ParseResult result)
        {
            if (result.IsSuccess)
            {
                switch (result.MessageName)
                {
                    case FireballConstants.MessagesNames.AUTHENTICATE:
                        return await Auth(result.ToMessage<AuthMessage>());
                    
                    case FireballConstants.MessagesNames.SESSION:
                        return await OnAuthSuccess(result.ToMessage<SessionMessage>());
                    
                    case FireballConstants.MessagesNames.AUTHENTICATE_REJECT:
                        return await OnAuthReject(result.ToMessage<ErrorMessage>());
                    
                    case GameMessages.SPIN_REQUEST:
                        return await MakeSpin(result.ToMessage<SampleSpinRequest>());

                    default:
                        return MessageResult.ErrorResult($"Undefined message name: {result.MessageName}");
                }
            }

            return MessageResult.ErrorResult("Error Parse message...");
        }
        
        private async Task<MessageResult> Auth(AuthMessage message)
        {
            _logger.Log($"authenticate: {message.ToJson()}");

            // send the authenticating message
            return await _fireball.Authenticate(message);
        }
        
        private async Task<MessageResult> OnAuthSuccess(SessionMessage message)
        {
            if (message.GameSession != null)
            {
                var gameSessionsList = await _fireball.GetAllGameSessions(message);

                foreach (var gameSession in gameSessionsList)
                {
                    // get game state from the game session
                    var gameState = gameSession.ParseGameState<SampleGameState>();

                    // close game session with game-over game state
                    if (gameState.IsGameOver)
                    {
                        bool closed = await _fireball.CloseGameSession(gameSession.Id);
                    }
                }
            }
            
            if (message.GameSession == null)
            {
                // create a new default custom game state
                var defaultGameState = new SampleGameState();

                // create a new permanent game session
                var gameSession = await _fireball.CreatePermanentGameSession(defaultGameState, message);

                // update session message with new game session data
                message.UpdateGameSession(gameSession);
            }
            
            return await _fireball.SendSessionToClient(message);
        }
        
        private async Task<MessageResult> OnAuthReject(ErrorMessage error)
        {
            return await _fireball.SendErrorToClient(error, ErrorCode.Authentication);
        }
        
        private async Task<MessageResult> MakeSpin(SampleSpinRequest message)
        {
            // example of custom calculation
            long winAmount = 0;
            var random = await _fireballRng.NextDouble(0, 1);
            if (random > 0.5f)
            {
                winAmount = message.BetAmount * 2;
            }
            
            // create a response message
            var response = new SampleSpinResult(message.GameType, winAmount, message);

            // send the response message to the game client
            return await _fireball.SendMessageToClient(response);
        }
        
        private async Task TestGameState(BaseMessage message)
        {
            var gameState = await _fireball.GetGameState<SampleGameState>(message.GameSession);
            
            // update game state fields
            gameState.CurrentScore = 123;
            gameState.GameBoard.Add("row_1", 5);

            // save updated game state into game session
            await _fireball.SaveGameState<SampleGameState>(message.GameSession, gameState);
            
            // update number field directly
            await _fireball.UpdateGameState(message.GameSession, "$.CurrentScore", 456);

            // update the dictionary object
            var gameBoard = new Dictionary<string, int>()
            {
                { "row_1", 1 },
                { "row_2", 2 },
            };
            await _fireball.UpdateGameState(message.GameSession, "$.GameBoard", gameBoard);
        }
    }
}