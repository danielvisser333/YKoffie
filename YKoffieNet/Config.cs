using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;   
using System.Diagnostics;
using Newtonsoft.Json;

namespace YKoffieNet
{
    internal class Config
    {
        public static string GetTokenFromConfig()
        {
            string currentDir = Environment.CurrentDirectory;
            currentDir += "//token.txt";
            if (File.Exists(currentDir))
            {
                StreamReader reader = new(currentDir);
                string token = reader.ReadToEnd();
                return token;
            }
            else
            {
                File.Create(currentDir);
                Environment.Exit(0);
                return "";
            }
        }
        public static BotConfig GetBotConfig()
        {
            string currentDir = Environment.CurrentDirectory;
            currentDir += "//config.json";
            if (!File.Exists(currentDir))
            {
                File.Create(currentDir);
                return new BotConfig()
                {
                    guildConfigs = new List<GuildConfig>()
                };
            }
            StreamReader reader = new(currentDir);
            BotConfig? config = JsonConvert.DeserializeObject<BotConfig>(reader.ReadToEnd());
            reader.Close();
            if (config == null)
            {
                return new BotConfig()
                {
                    guildConfigs = new List<GuildConfig>()
                };
            }
            return config;
        }
        public async static Task SaveBotConfig(BotConfig config)
        {
            string currentDir = Environment.CurrentDirectory;
            currentDir += "//config.json";
            if (!File.Exists(currentDir))
            {
                File.Create(currentDir);
            }
            try
            {
                StreamWriter writer = new(currentDir);
                string serial = JsonConvert.SerializeObject(config);
                await writer.WriteAsync(serial);
                writer.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public class BotConfig
        {
            public List<GuildConfig> guildConfigs = new();
        }
        public class GuildConfig
        {
            public ulong guildId;
            public int maxPlaylistLength = 0;
            public bool allowDuplicates = true;
        }
    }
}