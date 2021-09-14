using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKoffieNet.Commands
{
    public class MusicMain : BaseCommandModule
    {
        
        [Command("join")]
        public async Task Join(CommandContext ctx, DiscordChannel channel) 
        {
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
            await ctx.RespondAsync($"Joining voice channel, {channel.Name}.");
            await node.ConnectAsync(channel);
        }
        [Command("leave")]
        public async Task Leave(CommandContext ctx, DiscordChannel channel)
        {
            await ctx.RespondAsync("Leaving voice channel.");
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
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Leaving {channel.Name}!");
        }
        [Command("play")]
        public async Task Play(CommandContext ctx, Uri url)
        {
            if(ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("Lavalink is not enabled.");
                return;
            }
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if(conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(url);
            if(result.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await ctx.RespondAsync($"Failed to play {url}");
                return;
            }
            LavalinkTrack track = result.Tracks.First();
            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing {track.Title}!");
        }
        [Command("search")]
        public async Task Search(CommandContext ctx, string query)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("Lavalink is not enabled.");
                return;
            }
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(query);
            if (result.LoadResultType == LavalinkLoadResultType.LoadFailed || result.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Could not find {query}!");
                return;
            }
            LavalinkTrack track = result.Tracks.First();
            await conn.PlayAsync(track);
            await ctx.RespondAsync($"Now playing {track.Title}!");
        }
        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("pong!");
        }
    }
}
