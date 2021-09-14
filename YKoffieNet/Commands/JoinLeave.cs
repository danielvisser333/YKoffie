using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using YKoffieNet.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKoffieNet.Commands
{
    class JoinLeave : BaseCommandModule
    {
        //Join the channel the requested member is in.
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            if(channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel!");
                return;
            }
            await Join(ctx, channel);
        }
        //Join with a specified channel name.
        [Command("join")]
        public async Task Join(CommandContext ctx, DiscordChannel channel)
        {
            if(channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("The requested channel is not a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("Lavalink is not enabled.");
                return;
            }
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }
            await ctx.RespondAsync($"Joining voice channel, {channel.Name}!");
            await node.ConnectAsync(channel);
            node.PlaybackFinished += async (s, e) =>
            {
                await Music.NextInQueue();
            };
        }
        //Leave the channel the member is in.
        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            if (channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel!");
                return;
            }
            await Leave(ctx,channel);
        }
        //Leave the specified channel.
        public async Task Leave(CommandContext ctx, DiscordChannel channel)
        {
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("The requested channel is not a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("Lavalink is not enabled.");
                return;
            }
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }
            LavalinkGuildConnection conn = node.GetGuildConnection(channel.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Connection error.");
                return;
            }
            await ctx.RespondAsync($"Leaving {channel.Name}!");
            await conn.DisconnectAsync();
        }
    }
}
