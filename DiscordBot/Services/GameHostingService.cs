using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiscordBot.Services {
    public class GameHostingService {
        public enum Host {
            Terraria = 0,
            Minecraft = 1
        }

        private readonly Dictionary<Host, bool> hostEnabled;
        public bool serviceEnabled;

        public GameHostingService() {
            hostEnabled = new Dictionary<Host, bool> {
                [Host.Terraria] = false,
                [Host.Minecraft] = false
            };

            serviceEnabled = true;
        }

        public async Task<bool> StartHost(Host host) {
            return await Task.Run(() => {
                if (!serviceEnabled)
                    return false;

                if (!hostEnabled[host]) {
                switch (host) {
                    case Host.Terraria:
                        Process.Start("bash", "/srv/terraria/server start");
                        break;
                    case Host.Minecraft:
                        Process.Start("bash", "/srv/minecraft/paper/server start");
                        break;
                }
                hostEnabled[host] = true;
                return true;
                }
                else
                    return false;
            });
        }

        public async Task<bool> StopHost(Host host) {
            return await Task.Run(() => {
                if (!serviceEnabled)
                    return false;

                if (hostEnabled[host]) {
                    switch (host) {
                        case Host.Terraria:
                            Process.Start("bash", "/srv/terraria/server stop");
                            break;
                        case Host.Minecraft:
                            Process.Start("bash", "/srv/minecraft/paper/server stop");
                            break;
                    }
                    hostEnabled[host] = false;
                    return true;
                }
                else
                    return false;
            });
        }

        public bool Status(Host host) {
            return hostEnabled[host];
        }
    }
}
