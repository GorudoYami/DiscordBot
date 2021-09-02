using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.LeagueAPI {
    public class BannedChampion {
        public int PickTurn { get; set; }
        public long ChampionId { get; set; }
        public long TeamId { get; set; }
    }
}
