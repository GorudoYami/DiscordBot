using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Handlers;
using DiscordBot.Services;

namespace DiscordBot {
    public class DiscordBot {
        private readonly DiscordSocketClient client;

        private readonly CommandService cmdService;
        private readonly CommandHandler cmdHandler;
        private readonly IConfiguration configuration;
        private readonly string token;

        public DiscordBot() {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>();
            configuration = builder.Build();
            client = new DiscordSocketClient();

            CommandServiceConfig cmdServiceConfig = new() {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                IgnoreExtraArgs = false,
                LogLevel = LogSeverity.Debug,
                QuotationMarkAliasMap = null,
                SeparatorChar = ' ',
                ThrowOnError = true
            };
            cmdService = new CommandService(cmdServiceConfig);
            cmdHandler = new CommandHandler(BuildServiceProvider(), client, cmdService);

            client.Log += LogAsync;
            cmdService.Log += LogAsync;

            token = configuration.GetValue<string>("AppSettings:DiscordToken");
        }

        public async Task StartAsync() {
            await cmdHandler.LoadModulesAsync();
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
        }

        public async Task StopAsync() {
            await client.LogoutAsync();
            await client.StopAsync();
        }

        private Task LogAsync(LogMessage msg) {
            return Task.Run(() => Console.WriteLine(msg.ToString()));
        }

        private IServiceProvider BuildServiceProvider() => new ServiceCollection()
            .AddSingleton(client)
            .AddSingleton(cmdService)
            .AddSingleton(configuration)
            .AddSingleton<CommandHandler>()
            .AddSingleton<GameHostingService>()
            .AddSingleton<LeagueService>()
            .BuildServiceProvider();
    }
}
