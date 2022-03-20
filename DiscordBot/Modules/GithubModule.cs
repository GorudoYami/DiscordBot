using Discord.Commands;
using DiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules;

[Group("github")]
public class GithubModule : ModuleBase<SocketCommandContext> {
	private readonly GithubService Github;

	public GithubModule(GithubService github) {
		Github = github;
	}

	[Command("notifications")]
	[Summary("Enables or disables Github notifications")]
	public async Task ToggleNotificationsAsync([Summary("Turn on or off")] string value) {
		value = value.Trim().ToUpper();

		if (value == "ON" && !Github.IsEnabled) {
			Github.Start(Context.Channel);
			await ReplyAsync("Github pull request notifications have been activated for this channel.");
		}
		else if (value == "ON" && Github.IsEnabled) {
			await ReplyAsync($"Github pull requests notifications are already active on channel: {Github.Channel.Name}");
			return;
		}
		else if (value == "OFF" && Github.IsEnabled) {
			Github.Stop();
			await ReplyAsync($"Github pull request notifications have been disabled on channel: {Github.Channel.Name}");
		}
		else if (value == "OFF" && !Github.IsEnabled)
			await ReplyAsync("Github pull requests notifications are already disabled!");
		else
			await ReplyAsync("You can only use ON/OFF as a parameter");
	}
}
