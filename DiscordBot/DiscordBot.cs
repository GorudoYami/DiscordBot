using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Handlers;
using DiscordBot.Services;
using NLog;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DiscordBot;

public class NotifyEventArgs : EventArgs {
	public ISocketMessageChannel Channel { get; private set; }
	public string Message { get; private set; }
	public Embed MessageEmbed { get; private set; }

	public NotifyEventArgs(ISocketMessageChannel channel, string message = null, Embed embed = null) {
		Channel = channel;
		Message = message;
		MessageEmbed = embed;
	}
}

public class DiscordBot {
	public DiscordSocketClient Client { get; private set; }
	public CommandService CmdService { get; private set; }
	public CommandHandler CmdHandler { get; private set; }
	public IConfiguration Configuration { get; private set; }

	private string Token { get; set; }
	private readonly ILogger<DiscordBot> Logger;

	public DiscordBot(IServiceProvider services) {
		Logger = services.GetService<ILogger<DiscordBot>>();
		var githubService = services.GetService<GithubService>();
		githubService.Notify += Notify;

		InitializeConfiguration();
		InitializeClient();
		InitializeCommandHandler(services);
	}

	private void InitializeConfiguration() {
		var builder = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
			.AddUserSecrets<Program>();
		Configuration = builder.Build();

		Token = Configuration.GetValue<string>("AppSettings:DiscordToken");
	}

	private void InitializeClient() {
		Client = new DiscordSocketClient();
		Client.Log += LogAsync;
	}

	private void InitializeCommandHandler(IServiceProvider services) {
		CommandServiceConfig cmdServiceConfig = new() {
			CaseSensitiveCommands = false,
			DefaultRunMode = RunMode.Async,
			IgnoreExtraArgs = false,
			LogLevel = LogSeverity.Debug,
			QuotationMarkAliasMap = null,
			SeparatorChar = ' ',
			ThrowOnError = true
		};
		CmdService = new CommandService(cmdServiceConfig);
		CmdService.Log += LogAsync;

		CmdHandler = new CommandHandler(services, Client, CmdService);
	}

	public async void Start() {
		await CmdHandler.LoadModulesAsync();
		await Client.LoginAsync(TokenType.Bot, Token);
		await Client.StartAsync();
	}

	public async void Stop() {
		await Client.LogoutAsync();
		await Client.StopAsync();
	}

	public async void Notify(object sender, NotifyEventArgs e) {
		await e.Channel.SendMessageAsync(text: e.Message, embed: e.MessageEmbed);
	}

	private Task LogAsync(LogMessage msg) {
		return Task.Run(() => Logger.Log(GetLogLevelFromSeverity(msg.Severity), $"{msg.Message}"));
	}

	private static LogLevel GetLogLevelFromSeverity(LogSeverity severity) {
		return severity switch {
			LogSeverity.Verbose => LogLevel.Trace,
			LogSeverity.Info => LogLevel.Information,
			LogSeverity.Debug => LogLevel.Debug,
			LogSeverity.Warning => LogLevel.Warning,
			LogSeverity.Error => LogLevel.Error,
			LogSeverity.Critical => LogLevel.Critical,
			_ => throw new ArgumentOutOfRangeException(nameof(severity))
		};
	}
}
