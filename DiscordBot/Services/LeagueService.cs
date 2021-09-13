using System;
using System.Net;
using System.Threading.Tasks;
using Discord;
using RestSharp;
using DiscordBot.LeagueAPI;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Services {
    public class LeagueService {
        public enum ErrorCode {
            NoError,
            InternalError,
            InvalidSummoner,
            NotInGame,
            ApiKeyExpired
        }

        private readonly RestClient client;
        private readonly string token;
        private ErrorCode errorCode;

        public LeagueService(IConfiguration configuration) {
            token = configuration.GetValue<string>("AppSettings:RiotToken");
            errorCode = ErrorCode.NoError;
            client = new RestClient("https://eun1.api.riotgames.com");
            client.AddDefaultHeader("X-Riot-Token", token);
        }

        public async Task<Embed> CurrentGameAsync(string summonerName) {
            // Get sum info
            string summonerId = await GetSummonerIdAsync(summonerName);
            if (errorCode != ErrorCode.NoError)
                return null;

            CurrentGameInfo gameInfo = await GetCurrentGameAsync(summonerId);
            if (errorCode != ErrorCode.NoError)
                return null;

            // Mind the spectator delay!
            gameInfo.GameLength += 3 * 60;

            var embed = new EmbedBuilder() {
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
                        if (errorCode != ErrorCode.NoError)
                            return null;

                        if (rankInfo.Count == 0)
                            teamBlue[i, 2] = "UNRANKED";
                        else if (rankInfo.Count == 1)
                            teamBlue[i, 2] = rankInfo[0].Tier + " " + rankInfo[0].Rank;
                        else if (rankInfo.Count > 1)
                            teamBlue[i, 2] = rankInfo[rankInfo.Count - 1].Tier + " " + rankInfo[rankInfo.Count - 1].Rank;
                        i++;
                    }
                    else {
                        // Set champion
                        teamRed[q, 0] = Enum.GetName(typeof(Champion), participant.ChampionId);
                        // Set summoner name
                        teamRed[q, 1] = participant.SummonerName;

                        // Set rank
                        List<LeagueEntryDTO> rankInfo = await GetRankInfoAsync(participant.SummonerId);
                        if (errorCode != ErrorCode.NoError)
                            return null;

                        if (rankInfo.Count == 0)
                            teamRed[q, 2] = "UNRANKED";
                        else if (rankInfo.Count == 1)
                            teamRed[q, 2] = rankInfo[0].Tier + " " + rankInfo[0].Rank;
                        else if (rankInfo.Count > 1)
                            teamRed[q, 2] = rankInfo[rankInfo.Count - 1].Tier + " " + rankInfo[rankInfo.Count - 1].Rank;
                        q++;
                    }
                }
            }

            // Build team blue champs
            string blueChamps = string.Empty;
            for (int i = 0; i < 5; i++)
                blueChamps += teamBlue[i, 0] + "\n";

            // Build team blue summoners
            string blueSummoners = string.Empty;
            for (int i = 0; i < 5; i++)
                blueSummoners += teamBlue[i, 1] + "\n";

            // Build team blue ranks
            string blueRanks = string.Empty;
            for (int i = 0; i < 5; i++)
                blueRanks += teamBlue[i, 2] + "\n";

            // Build team red champs
            string redChamps = string.Empty;
            for (int i = 0; i < 5; i++)
                redChamps += teamRed[i, 0] + "\n";

            // Build team red summoners
            string redSummoners = string.Empty;
            for (int i = 0; i < 5; i++)
                redSummoners += teamRed[i, 1] + "\n";

            // Build team red ranks
            string redRanks = string.Empty;
            for (int i = 0; i < 5; i++)
                redRanks += teamRed[i, 2] + "\n";

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
            switch (errorCode) {
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
            return errorCode;
        }

        public async Task<string> GetSummonerIdAsync(string summonerName) {
            var request = new RestRequest("lol/summoner/v4/summoners/by-name/" + summonerName, Method.GET, DataFormat.Json);
            var response = await client.ExecuteAsync<SummonerDTO>(request);
            if (response.IsSuccessful)
                errorCode = ErrorCode.NoError;
            else if (response.StatusCode == HttpStatusCode.NotFound)
                errorCode = ErrorCode.InvalidSummoner;
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                errorCode = ErrorCode.ApiKeyExpired;
            else
                errorCode = ErrorCode.InternalError;

            return response.Data.Id;
        }

        public async Task<CurrentGameInfo> GetCurrentGameAsync(string summonerId) {
            var request = new RestRequest("lol/spectator/v4/active-games/by-summoner/" + summonerId, Method.GET, DataFormat.Json);
            var response = await client.ExecuteAsync<CurrentGameInfo>(request);
            if (response.IsSuccessful)
                errorCode = ErrorCode.NoError;
            else if (response.StatusCode == HttpStatusCode.NotFound)
                errorCode = ErrorCode.NotInGame;
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                errorCode = ErrorCode.ApiKeyExpired;
            else
                errorCode = ErrorCode.InternalError;
            return response.Data;
        }

        public async Task<List<LeagueEntryDTO>> GetRankInfoAsync(string summonerId) {
            var request = new RestRequest("lol/league/v4/entries/by-summoner/" + summonerId, Method.GET, DataFormat.Json);
            var response = await client.ExecuteAsync<List<LeagueEntryDTO>>(request);
            if (response.IsSuccessful)
                errorCode = ErrorCode.NoError;
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                errorCode = ErrorCode.ApiKeyExpired;
            else
                errorCode = ErrorCode.InternalError;

            return response.Data;
        }
    }
}
