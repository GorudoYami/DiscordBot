using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordBot.Services;

public class GithubService {
	public bool IsEnabled => UpdateTimer.Enabled;
	public EventHandler<NotifyEventArgs> Notify { get; set; }
	public ISocketMessageChannel Channel { get; private set; }

	private readonly GitHubClient Github;
	private readonly Timer UpdateTimer;
	private readonly long RepositoryId;
	private readonly IConfiguration Configuration;
	private readonly ICollection<PullRequest> PullRequestCache;

	public GithubService(IConfiguration configuration) {
		Configuration = configuration;

		var tokenAuth = new Credentials(Configuration.GetValue<string>("AppSettings:GithubToken"));
		Github = new GitHubClient(new ProductHeaderValue("GorudoYami")) {
			Credentials = tokenAuth
		};

		UpdateTimer = new Timer(60 * 1000);
		UpdateTimer.Elapsed += Update;

		RepositoryId = Configuration.GetValue<long>("AppSettings:GithubRepositoryId");
		PullRequestCache = new List<PullRequest>();
	}

	public void Start(ISocketMessageChannel channel) {
		Channel = channel;
		UpdateTimer.Start();
	}

	public void Stop() {
		UpdateTimer.Stop();
	}

	private async void Update(object sender, ElapsedEventArgs e) {
		var pullRequests = await Github.PullRequest.GetAllForRepository(RepositoryId);

		// Check for new pull requests
		foreach (var pr in pullRequests) {
			if (!PullRequestCache.Any(x => x.Id == pr.Id)) {
				PullRequestCache.Add(pr);
				SendNewPullRequestNotification(pr);
			}
		}

		// Check for done pull requests
		var pullRequestsToRemove = new List<PullRequest>();
		foreach (var pr in PullRequestCache) {
			if (!pullRequests.Any(x => x.Id == pr.Id)) {
				pullRequestsToRemove.Add(pr);
				SendClosedPullRequestNotification(pr);
			}
		}

		foreach (var pullRequest in pullRequestsToRemove)
			PullRequestCache.Remove(pullRequest);
	}

	private void SendNewPullRequestNotification(PullRequest pullRequest) {
		Embed embed = CreateEmbed(pullRequest);
		Notify.Invoke(this, new NotifyEventArgs(Channel, embed: embed));
	}

	private void SendClosedPullRequestNotification(PullRequest pullRequest) {
		Embed embed = CreateEmbed(pullRequest, false);
		Notify.Invoke(this, new NotifyEventArgs(Channel, embed: embed));
	}

	private static Embed CreateEmbed(PullRequest pullRequest, bool newPullRequest = true) {
		var builder = new EmbedBuilder() {
			Title = pullRequest.Title,
			Description = newPullRequest ? "New pull request" : "Pull request closed",
			Url = pullRequest.HtmlUrl,
			Color = newPullRequest ? Color.Green : Color.LightGrey,
			Timestamp = pullRequest.CreatedAt
		};
		builder.WithFooter(footer => footer.Text = "Can I have headpats?");
		return builder.Build();
	}
}
