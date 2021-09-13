using DSharpPlus;

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
                }
            );
            await discord.ConnectAsync();
            discord.MessageCreated += async (s, e) =>
            {
                
            };
            await Task.Delay(-1);
        }
        static void handle_message(DiscordClient discord, ) { 
        
        }
    }
}