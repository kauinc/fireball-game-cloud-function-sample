using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Fireball.Game.Server;
using Fireball.Game.Server.Models;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleProject
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddFireballDependencies();
            services.AddSingleton<IGame, Game>();

            base.ConfigureServices(context, services);
        }
    }

    [FunctionsStartup(typeof(Startup))]
    public class Function : ICloudEventFunction<MessagePublishedData>
    {
        private readonly IFireballLogger _logger;
        private readonly IFireball _fireball;
        private readonly IGame _game;

        public Function(ILogger<Function> logger, IFireball fireball, IGame game)
        {
            _logger = new FireballLogger(nameof(Function), logger); ;
            _fireball = fireball;
            _game = game;
        }

        public async Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
        {
            try
            {
                string messageJson = data.Message?.TextData;
                ParseResult result = await _fireball.ParseMessage(messageJson);
                _logger.Log($"Message: {result.MessageName}");

                MessageResult messageResult = await _game.HandleMessage(result);
                if (messageResult != null && messageResult.IsSuccess())
                {
                    _logger.Log($"Result: success - {messageResult?.Message}");
                }
                else
                {
                    _logger.LogError($"Result: error - {messageResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex}");
            }
        }
    }
}