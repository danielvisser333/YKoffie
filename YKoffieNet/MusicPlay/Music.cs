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

namespace YKoffieNet.MusicPlay
{
    internal class Music : BaseCommandModule
    {

        LavalinkNodeConnection? Gnode;
        List<LavalinkGuildConnection> connections = new List<LavalinkGuildConnection>();
        List<(DiscordGuild, List<LavalinkTrack>)> queues = new List<(DiscordGuild, List<LavalinkTrack>)>();
        #region JoinLeave
        //Join the channel the requested member is in.
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            if (channel == null)
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
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("The requested channel is not a voice channel.");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("Internal Server Error.");
                return;
            }
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            Gnode = node;
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }
            await ctx.RespondAsync($"Joining voice channel, {channel.Name}!");
            await node.ConnectAsync(channel);
            connections.Add(node.GetGuildConnection(channel.Guild));
            queues.Add((channel.Guild,new List<LavalinkTrack>()));
            node.PlaybackFinished += async (s, e) =>
            {
                await UpdateQueues();
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
            await Leave(ctx, channel);
        }
        //Leave the specified channel.
        public async Task Leave(CommandContext ctx, DiscordChannel channel)
        {
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("The requested channel is not a voice channel.");
                return;
            }
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                await ctx.RespondAsync($"Leaving {channel.Name}!");
                await conn.DisconnectAsync();
                connections.Remove(conn);
            }
            catch (Exception)
            {
                return;
            }
        }
        #endregion JoinLeave
        #region Play
        [Command("p")]
        public async Task P(CommandContext ctx, [RemainingText] string search) { await Play(ctx, search); }
        // Search the requested song
        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                if (Gnode == null)
                {
                    return;
                }
                LavalinkLoadResult result = await Gnode.Rest.GetTracksAsync(search);
                if (result.LoadResultType == LavalinkLoadResultType.LoadFailed || result.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.RespondAsync("Failed to play song.");
                    return;
                }
                LavalinkTrack track = result.Tracks.First();
                if (conn.CurrentState.CurrentTrack == null)
                {
                    await ctx.RespondAsync($"Playing found track {track.Title}!");
                    await conn.PlayAsync(track);
                    return;
                }
                AddToQueue(ctx.Guild,track);
                await ctx.RespondAsync($"Adding {track.Title} to the queue.");
            }
            catch (Exception)
            {
                await Join(ctx);
                await Play(ctx, search);
                return;
            }
        }
        #endregion
        #region Queue
        public void AddToQueue(DiscordGuild guild, LavalinkTrack track)
        {
            try
            {
                List<LavalinkTrack> queue = queues.Where(i => i.Item1 == guild).First().Item2;
                queue.Add(track);

            }
            catch (Exception)
            {
                return;
            }
        }
        public async Task UpdateQueues()
        {
            foreach (LavalinkGuildConnection conn in connections)
            {
                if (conn.CurrentState.CurrentTrack == null) {
                    try
                    {
                        /*(DiscordGuild guild, List<LavalinkTrack> queue) = queues.Where(i => i.Item1 == conn.Guild).First();
                        await conn.PlayAsync(queue.First());
                        await conn.Channel.SendMessageAsync($"Now playing {queue.First()}");
                        queues.Find((guild,queue)).*/
                        int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
                        await conn.PlayAsync(queues[queueIndex].Item2.First());
                        queues[queueIndex].Item2.RemoveAt(0);
                    }
                    catch (Exception) { }
                }
            }
        }
        #endregion Queue
        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                await conn.StopAsync();
                await ctx.RespondAsync("Skipping the current track.");
                await UpdateQueues();
            }
            catch (Exception){ }
        }
        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                await conn.PauseAsync();
                await ctx.RespondAsync("Pausing the current track.");
                await UpdateQueues();
            }
            catch (Exception) { }
        }
        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                await conn.ResumeAsync();
                await ctx.RespondAsync("Resuming the current track.");
                await UpdateQueues();
            }
            catch (Exception) { }
        }
        [Command("shuffle")]
        public async Task Shuffle(CommandContext ctx)
        {
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
                Random rand = new Random();
                queues[queueIndex].Item2.OrderBy(x => rand.Next());
            }
            catch (Exception) { }
        }
    }
}
