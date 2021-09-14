using DSharpPlus;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using DSharpPlus.CommandsNext;
using YKoffieNet.Commands;

namespace YKoffieNet 
{ 
    public class YKoffieNet
    {
        static string token = "ODg3MDM0NzA4NzA4NDM4MDQ2.YT-Rcg.p-23nlGLWrl8GTLdYNSACEGDqbM";
        
        public static void Main(string[] Args) 
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            DiscordClient discord = new DiscordClient(
                new DiscordConfiguration()
                {
                    Token = YKoffieNet.token,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged,
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                }
            );
            ConnectionEndpoint endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            LavalinkConfiguration lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            LavalinkExtension lavalink = discord.UseLavalink();
            CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration() { 
                StringPrefixes = new string[] { "?" }
            });
            commands.RegisterCommands<MusicMain>();
            await discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);

            await Task.Delay(-1);
        }
    }
}