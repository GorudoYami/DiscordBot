using Discord;
using DiscordBot.LeagueAPI;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DiscordBot.Services;

public class LeagueService {
	public enum ErrorCode {
		NoError,
		InternalError,
		InvalidSummoner,
		NotInGame,
		ApiKeyExpired
	}

	private readonly RestClient Client;
	private readonly string Token;
	private readonly IConfiguration Configuration;
	private ErrorCode Error;

	public LeagueService(IConfiguration configuration) {
		Configuration = configuration;
		Token = Configuration.GetValue<string>("AppSettings:RiotToken");
		Error = ErrorCode.NoError;
		Client = new RestClient("https://eun1.api.riotgames.com");
		Client.AddDefaultHeader("X-Riot-Token", Token);
	}

	public async Task<Embed> CurrentGameAsync(string summonerName) {
		// Get sum info
		string summonerId = await GetSummonerIdAsync(summonerName);
		if (Error != ErrorCode.NoError) {
			return null;
		}

		CurrentGameInfo gameInfo = await GetCurrentGameAsync(summonerId);
		if (Error != ErrorCode.NoError) {
			return null;
		}

		// Mind the spectator delay!
		gameInfo.GameLength += 3 * 60;

		EmbedBuilder embed = new EmbedBuilder() {
			Title = summonerName + "'s game",
			// Description below could be prettier
			Description = gameInfo.GameMode + " " + (gameInfo.GameLength / 60) + ":" + (gameInfo.GameLength % 60),
			Url = "https://porofessor.gg/live/eune/" + summonerName.Replace(" ", "%20"),
			Color = Color.DarkPurple,
			Timestamp = DateTimeOffset.Now
		};
		embed.WithFooter(footer => footer.Text = "Can I have headpats?");

		string[,] teamBlue = new string[5, 3];
		string[,] teamRed = new string[5, 3];
		{
			int i = 0;
			int q = 0;
			foreach (CurrentGameParticipant participant in gameInfo.Participants) {
				if (participant.TeamId == (long)Team.Blue) {
					// Set champion
					teamBlue[i, 0] = Enum.GetName(typeof(Champion), participant.ChampionId);
					// Set summoner name
					teamBlue[i, 1] = participant.SummonerName;

					// Set rank
					List<LeagueEntryDTO> rankInfo = await GetRankInfoAsync(participant.SummonerId);
					if (Error != ErrorCode.NoError) {
						return null;
					}

					if (rankInfo.Count == 0) {
						teamBlue[i, 2] = "UNRANKED";
					}
					else if (rankInfo.Count == 1) {
						teamBlue[i, 2] = rankInfo[0].Tier + " " + rankInfo[0].Rank;
					}
					else if (rankInfo.Count > 1) {
						teamBlue[i, 2] = rankInfo[rankInfo.Count - 1].Tier + " " + rankInfo[rankInfo.Count - 1].Rank;
					}

					i++;
				}
				else {
					// Set champion
					teamRed[q, 0] = Enum.GetName(typeof(Champion), participant.ChampionId);
					// Set summoner name
					teamRed[q, 1] = participant.SummonerName;

					// Set rank
					List<LeagueEntryDTO> rankInfo = await GetRankInfoAsync(participant.SummonerId);
					if (Error != ErrorCode.NoError) {
						return null;
					}

					if (rankInfo.Count == 0) {
						teamRed[q, 2] = "UNRANKED";
					}
					else if (rankInfo.Count == 1) {
						teamRed[q, 2] = rankInfo[0].Tier + " " + rankInfo[0].Rank;
					}
					else if (rankInfo.Count > 1) {
						teamRed[q, 2] = rankInfo[rankInfo.Count - 1].Tier + " " + rankInfo[rankInfo.Count - 1].Rank;
					}

					q++;
				}
			}
		}

		// Build team blue champs
		string blueChamps = string.Empty;
		for (int i = 0; i < 5; i++) {
			blueChamps += teamBlue[i, 0] + "\n";
		}

		// Build team blue summoners
		string blueSummoners = string.Empty;
		for (int i = 0; i < 5; i++) {
			blueSummoners += teamBlue[i, 1] + "\n";
		}

		// Build team blue ranks
		string blueRanks = string.Empty;
		for (int i = 0; i < 5; i++) {
			blueRanks += teamBlue[i, 2] + "\n";
		}

		// Build team red champs
		string redChamps = string.Empty;
		for (int i = 0; i < 5; i++) {
			redChamps += teamRed[i, 0] + "\n";
		}

		// Build team red summoners
		string redSummoners = string.Empty;
		for (int i = 0; i < 5; i++) {
			redSummoners += teamRed[i, 1] + "\n";
		}

		// Build team red ranks
		string redRanks = string.Empty;
		for (int i = 0; i < 5; i++) {
			redRanks += teamRed[i, 2] + "\n";
		}

		embed.AddField(field => {
			field.Name = "\u200B";
			field.Value = "**== Team blue ==**";
		});
		embed.AddField(field => {
			field.Name = "Champion";
			field.Value = blueChamps;
			field.IsInline = true;
		});
		embed.AddField(field => {
			field.Name = "Player";
			field.Value = blueSummoners;
			field.IsInline = true;
		});
		embed.AddField(field => {
			field.Name = "Rank";
			field.Value = blueRanks;
			field.IsInline = true;
		});

		embed.AddField(field => {
			field.Name = "\u200B";
			field.Value = "**== Team red ==**";
		});
		embed.AddField(field => {
			field.Name = "Champion";
			field.Value = redChamps;
			field.IsInline = true;
		});
		embed.AddField(field => {
			field.Name = "Player";
			field.Value = redSummoners;
			field.IsInline = true;
		});
		embed.AddField(field => {
			field.Name = "Rank";
			field.Value = redRanks;
			field.IsInline = true;
		});

		embed.AddField(field => {
			field.Name = "Game summary";
			field.Value = "[leagueofgraphs.com](https://leagueofgraphs.com/match/eune/" + gameInfo.GameId + ")";
		});

		return embed.Build();
	}
	public string GetErrorMessage() {
		switch (Error) {
			case ErrorCode.InternalError:
				return "Something went wrong... You should probably inform my Onii-chan about that.";
			case ErrorCode.InvalidSummoner:
				return "This summoner doesn't exist. Please remember that I can only check games on EUNE >:C";
			case ErrorCode.ApiKeyExpired:
				return "Onii-chan forgot to replace API key... as always. Sorry, no results until that's done!";
			case ErrorCode.NotInGame:
				return "Summoner is not in game.";
			default:
				return "You shouldn't even see this message.";
		}
	}

	public ErrorCode GetErrorCode() {
		return Error;
	}

	public async Task<string> GetSummonerIdAsync(string summonerName) {
		RestRequest request = new($"lol/summoner/v4/summoners/by-name/{summonerName}");
		RestResponse<SummonerDTO> response = await Client.ExecuteAsync<SummonerDTO>(request);
		if (response.IsSuccessful) {
			Error = ErrorCode.NoError;
		}
		else if (response.StatusCode == HttpStatusCode.NotFound) {
			Error = ErrorCode.InvalidSummoner;
		}
		else if (response.StatusCode == HttpStatusCode.Unauthorized) {
			Error = ErrorCode.ApiKeyExpired;
		}
		else {
			Error = ErrorCode.InternalError;
		}

		return response.Data.Id;
	}

	public async Task<CurrentGameInfo> GetCurrentGameAsync(string summonerId) {
		RestRequest request = new($"lol/spectator/v4/active-games/by-summoner/{summonerId}");
		RestResponse<CurrentGameInfo> response = await Client.ExecuteAsync<CurrentGameInfo>(request);
		if (response.IsSuccessful) {
			Error = ErrorCode.NoError;
		}
		else if (response.StatusCode == HttpStatusCode.NotFound) {
			Error = ErrorCode.NotInGame;
		}
		else if (response.StatusCode == HttpStatusCode.Unauthorized) {
			Error = ErrorCode.ApiKeyExpired;
		}
		else {
			Error = ErrorCode.InternalError;
		}

		return response.Data;
	}

	public async Task<List<LeagueEntryDTO>> GetRankInfoAsync(string summonerId) {
		RestRequest request = new($"lol/league/v4/entries/by-summoner/{summonerId}");
		RestResponse<List<LeagueEntryDTO>> response = await Client.ExecuteAsync<List<LeagueEntryDTO>>(request);
		if (response.IsSuccessful) {
			Error = ErrorCode.NoError;
		}
		else if (response.StatusCode == HttpStatusCode.Unauthorized) {
			Error = ErrorCode.ApiKeyExpired;
		}
		else {
			Error = ErrorCode.InternalError;
		}

		return response.Data;
	}
}
