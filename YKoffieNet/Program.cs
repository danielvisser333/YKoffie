using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using YKoffieNet.MusicPlay;

namespace YKoffieNet {
    public class YKoffieNet
    {
        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }
        static async Task MainAsync()
        {
            string token = Config.GetTokenFromConfig();
            DiscordClient discord = new(
                new DiscordConfiguration()
                {
                    Token = token,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged,
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                }
            );
            ConnectionEndpoint endpoint = new()
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            LavalinkConfiguration lavalinkConfig = new()
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            LavalinkExtension lavalink = discord.UseLavalink();
            CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { "?" }
            });
            commands.RegisterCommands<PingPong>();
            commands.RegisterCommands<Music>();
            //commands.SetHelpFormatter<Help>();
            await discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);
            await Task.Delay(-1);
        }
    }
}