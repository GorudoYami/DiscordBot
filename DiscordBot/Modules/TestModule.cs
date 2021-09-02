using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordBot.Modules {
    public class TestModule : ModuleBase<SocketCommandContext> {

        [Command("test")]
        public async Task TestAsync() {
            await ReplyAsync("UwU?");
        }
    }
}
