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
    class Music : BaseCommandModule
    {
        public static List<LavalinkTrack> queue = new List<LavalinkTrack>();
        static LavalinkGuildConnection? connection;
        //Play the song at the provided URL.
        [Command("play")]
        public async Task Play(CommandContext ctx, Uri url)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(url);
            if (result.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await ctx.RespondAsync("Failed to play song.");
                return;
            }
            LavalinkTrack track = result.Tracks.First();
            connection = conn;
            if (conn.CurrentState.CurrentTrack == null)
            {
                await conn.PlayAsync(track);
                return;
            }
            queue.Add(track);
        }
        [Command("search")]
        public async Task Search(CommandContext ctx, [RemainingText] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            LavalinkLoadResult result = await node.Rest.GetTracksAsync(search);
            if (result.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await ctx.RespondAsync("Failed to play song.");
                return;
            }
            LavalinkTrack track = result.Tracks.First();
            if (conn.CurrentState.CurrentTrack == null)
            {
                await conn.PlayAsync(track);
                return;
            }
            queue.Add(track);
        }
        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }
        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            LavalinkGuildConnection conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
        }
        public async static Task NextInQueue()
        {
            if(queue.Count == 0)
            {
                return;
            }
            if (connection == null) 
            {
                return;
            }
            await connection.PlayAsync(queue.First());
            queue.RemoveAt(0);
        }
    }
}
