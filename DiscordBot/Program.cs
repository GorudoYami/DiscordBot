using DiscordBot.Handlers;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Threading.Tasks;

namespace DiscordBot;

public class Program {
	private static IConfiguration Configuration;

	public static async Task Main(string[] args) {
		Configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddUserSecrets<Program>()
			.Build();

		LogManager.ThrowExceptions = true;
		LogManager.LoadConfiguration("nlog.config").Setup();

		DiscordBot bot = new(BuildServiceProvider());

		Console.WriteLine("Starting up...");
		bot.Start();
		Console.WriteLine("Bot started!");
		Console.ReadKey();
		Console.WriteLine("Stopping...");
		bot.Stop();
		Console.WriteLine("Bot stopped!");

		LogManager.Shutdown();
	}

	private static IServiceProvider BuildServiceProvider() {
		return new ServiceCollection()
			.AddLogging()
			.AddSingleton(Configuration)
			.AddSingleton<CommandHandler>()
			.AddSingleton<LeagueService>()
			.AddSingleton<GameHostingService>()
			.AddSingleton<GithubService>()
			.BuildServiceProvider();
	}
}
