using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKoffieNet.MusicPlay
{
    internal class PingPong : BaseCommandModule
    {
        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("Pong!");
        }
        [Command("rythm")]
        public async Task Rythm(CommandContext ctx)
        {
            await ctx.RespondAsync("Rythm left behind an unfillable void in my heart.");
        }
    }
}
