using System;
using System.Threading.Tasks;

using Discord.Commands;

using DiscordBot.Services;

namespace DiscordBot.Modules {
    [Group("host")]
    public class HostModule : ModuleBase<SocketCommandContext> {
        private readonly GameHostingService hostService;

        public HostModule(GameHostingService hostService) {
            this.hostService = hostService;
        }

        [Command("status")]
        [Summary("Displays status of specified host")]
        public async Task StatusAsync([Summary("Name of the host")] string host) {
            GameHostingService.Host _host = Enum.Parse<GameHostingService.Host>(host, true);
            bool status = hostService.Status(_host);
            await ReplyAsync(Enum.GetName(typeof(GameHostingService.Host), _host) + " host is " + (status ? "online" : "offline"));
        }

        [Command("start")]
        [Summary("Starts specified host")]
        public async Task StartAsync([Summary("Name of the host")] string host) {
            if (!Enum.TryParse(host, true, out GameHostingService.Host _host)) {
                await ReplyAsync("Host doesn't exist. Available hosts: Terraria, Minecraft");
                return;
            }

            if (await hostService.StartHost(_host))
                await ReplyAsync("Host " + Enum.GetName(typeof(GameHostingService.Host), _host) + " has been started successfully!");
            else
                await ReplyAsync("Host couldn't be started. It might be already active or an error occured.");
        }

        [Command("stop")]
        [Summary("Stops specified host")]
        public async Task StopAsync([Summary("Name of the host")] string host) {
            if (!Enum.TryParse(host, true, out GameHostingService.Host _host)) {
                await ReplyAsync("Host doesn't exist. Available hosts: Terraria, Minecraft");
                return;
            }

            if (await hostService.StopHost(_host))
                await ReplyAsync("Host " + Enum.GetName(typeof(GameHostingService.Host), _host) + " stopped!");
            else
                await ReplyAsync("Host couldn't be stopped. It might be already dead or an error occured.");
        }

        [Command("disable")]
        [Summary("Disables the service")]
        public async Task DisableAsync() {
            if (Context.User.Username == "GorudoYami") {
                if (hostService.serviceEnabled) {
                    hostService.serviceEnabled = false;
                    await ReplyAsync("Host service disabled");
                }
                else
                    await ReplyAsync("Host service is already disabled");
            }
            else
                await ReplyAsync("You're not worthy of this command!");
        }

        [Command("enable")]
        [Summary("Enables the service")]
        public async Task EnableAsync() {
            if (Context.User.Username == "GorudoYami") {
                if (!hostService.serviceEnabled) {
                    hostService.serviceEnabled = true;
                    await ReplyAsync("Host service enabled");
                }
                else
                    await ReplyAsync("Host service is already enabled");
            }
            else
                await ReplyAsync("You're not worthy of this command!");
        }

    }
}
