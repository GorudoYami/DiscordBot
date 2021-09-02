using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

//dotnet user-secrets set "AppSettings:Token" "Value"

namespace DiscordBot {
    public class Program {
        public static async Task Main(string[] args) {
            var bot = new DiscordBot();
            Console.WriteLine("Starting up...");
            await bot.StartAsync();
            Console.WriteLine("Bot started!");
            Console.ReadKey();
            Console.WriteLine("Stopping...");
            await bot.StopAsync();
            Console.WriteLine("Bot stopped!");
        }
    }
}