using System.Threading.Tasks;
using Fireball.Game.Server;
using Fireball.Game.Server.Models;
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

        public Game(ILogger<Game> logger, IFireball fireball)
        {
            _logger = new FireballLogger(nameof(Game), logger);
            _fireball = fireball;
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
    }
}