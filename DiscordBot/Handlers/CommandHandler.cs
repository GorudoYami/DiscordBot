using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Handlers;

public class CommandHandler {
	private readonly DiscordSocketClient Client;
	private readonly CommandService CmdService;
	private readonly IServiceProvider Services;

	public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService cmdService) {
		Services = services;
		Client = client;
		CmdService = cmdService;

		Client.MessageReceived += HandleCommandAsync;
		CmdService.CommandExecuted += OnCommandExecutedAsync;
	}

	public async Task LoadModulesAsync() =>
		await CmdService.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

	private async Task HandleCommandAsync(SocketMessage msgParam) {
		// If msg is null then it means it is a system message
		if (msgParam is not SocketUserMessage msg)
			return;

		// Check if message has prefix or mention and isn't sent by bot
		int argPos = 0;
		if (!(msg.HasCharPrefix('&', ref argPos) ||
			msg.HasMentionPrefix(Client.CurrentUser, ref argPos)) ||
			msg.Author.IsBot)
			return;

		var context = new SocketCommandContext(Client, msg);

		await CmdService.ExecuteAsync(context, argPos, Services);
	}

	private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {
		if (!string.IsNullOrEmpty(result?.ErrorReason))
			await context.Channel.SendMessageAsync(result.ErrorReason);
	}
}
