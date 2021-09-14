using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKoffieNet.Commands
{
    class RequestHelp : BaseCommandModule
    {
        public async Task Help(CommandContext ctx)
        {
            await ctx.RespondAsync("This is a list of available commands.");
            
        }
    }
}
