using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Handlers {
    public class CommandHandler {
        private readonly DiscordSocketClient client;
        private readonly CommandService cmdService;
        private readonly IServiceProvider services;

        public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService cmdService) {
            this.services = services;
            this.client = client;
            this.cmdService = cmdService;

            this.client.MessageReceived += HandleCommandAsync;
            this.cmdService.CommandExecuted += OnCommandExecutedAsync;
        }

        public async Task LoadModulesAsync() =>
            await cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), services);

        private async Task HandleCommandAsync(SocketMessage msgParam) {
            // If msg is null then it means it is a system message
            SocketUserMessage msg = msgParam as SocketUserMessage;
            if (msg == null)
                return;

            // Check if message has prefix or mention and isn't sent by bot
            int argPos = 0;
            if (!(msg.HasCharPrefix('&', ref argPos) ||
                msg.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                msg.Author.IsBot)
                return;

            SocketCommandContext context = new SocketCommandContext(client, msg);

            await cmdService.ExecuteAsync(context, argPos, services);
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {
            if (!string.IsNullOrEmpty(result?.ErrorReason))
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}
