using Discord;
using Discord.Commands;
using DiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules {
    [Group("league")]
    public class LeagueModule : ModuleBase<SocketCommandContext> {
        private readonly LeagueService leagueService;
        
        public LeagueModule(LeagueService leagueService) {
            this.leagueService = leagueService;
        }

        [Command("game")]
        [Summary("Queries for an active game")]
        public async Task GameAsync([Summary("Summoner name")] string summonerName) {
            Embed embed = await leagueService.CurrentGameAsync(summonerName);
            if (embed != null)
                await ReplyAsync(embed: embed);
            else
                await ReplyAsync(leagueService.GetErrorMessage());
        }
    }
}
