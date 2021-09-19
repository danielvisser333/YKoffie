using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace YKoffieNet.MusicPlay
{
    internal class Music : BaseCommandModule
    {
        List<DiscordChannel> musicChannels = new List<DiscordChannel>();
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
                await ctx.RespondAsync("`You are not in a voice channel!`");
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
                await ctx.RespondAsync("`The requested channel is not a voice channel.`");
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("`Internal Server Error.`");
                return;
            }
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            Gnode = node;
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("`Not a valid voice channel.`");
                return;
            }
            await ctx.RespondAsync($"`Joining voice channel, {channel.Name}!`");
            await node.ConnectAsync(channel);
            connections.Add(node.GetGuildConnection(channel.Guild));
            musicChannels.Add(channel);
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
                await ctx.RespondAsync("`You are not in a voice channel!`");
                return;
            }
            await Leave(ctx, channel);
        }
        //Leave the specified channel.
        public async Task Leave(CommandContext ctx, DiscordChannel channel)
        {
            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("`The requested channel is not a voice channel.`");
                return;
            }
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                await ctx.RespondAsync($"`Leaving {channel.Name}!`");
                await ClearQueue(ctx);
                await conn.DisconnectAsync();
                musicChannels.Remove(musicChannels.Where(i => i.Guild == ctx.Guild).First());
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
                await ctx.RespondAsync("`You are not in a voice channel.`");
                return;
            }
            try
            {
                if(!musicChannels.Where(i=>i.Guild == ctx.Guild).Any())
                {
                    musicChannels.Add(ctx.Channel);
                }
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                if (Gnode == null)
                {
                    return;
                }
                LavalinkLoadResult result = await Gnode.Rest.GetTracksAsync(search);
                if (result.LoadResultType == LavalinkLoadResultType.LoadFailed || result.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.RespondAsync("`Failed to play song.`");
                    return;
                }
                LavalinkTrack track = result.Tracks.First();
                YoutubeClient client = new YoutubeClient();
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = $"Now playing {track.Title}";
                embed.Url = track.Uri.ToString();
                Video video = await client.Videos.GetAsync(track.Uri.ToString());
                embed.Thumbnail = new EmbedThumbnail
                {
                    Url = video.Thumbnails.First().Url
                };
                embed.Author = new EmbedAuthor
                {
                    Name = track.Author
                };
                if (conn.CurrentState.CurrentTrack == null)
                {
                    await ctx.RespondAsync(embed.Build());
                    await conn.PlayAsync(track);
                    return;
                }
                AddToQueue(ctx.Guild,track);
                embed.Title= $"Queued {track.Title}!";
                await ctx.RespondAsync(embed.Build());
            }
            catch (Exception)
            {
                await Join(ctx);
                await Play(ctx, search);
                return;
            }
        }
        [Command("playlist")]
        public async Task Playlist(CommandContext ctx, [RemainingText] string url)
        {
            YoutubeClient client = new YoutubeClient();
            IAsyncEnumerable<PlaylistVideo> playlist = client.Playlists.GetVideosAsync(url);
            int i = 0;
            List<string> videos = new List<string>();
            await foreach (var video in playlist)
            {
                videos.Add(video.Url);
                i++;
            }
            await ctx.RespondAsync($"`Number of songs: {i}!`");
            foreach (var video in videos)
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                if (conn.CurrentState.CurrentTrack == null)
                {
                    await UpdateQueues();
                }
                
                    if (Gnode == null)
                {
                    return;
                }
                LavalinkLoadResult result = await Gnode.Rest.GetTracksAsync(video);
                AddToQueue(ctx.Guild,result.Tracks.First());
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
                        int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
                        await conn.PlayAsync(queues[queueIndex].Item2.First());
                        YoutubeClient client = new YoutubeClient();
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                        embed.Title = $"Now playing {queues[queueIndex].Item2[0].Title}";
                        embed.Url = queues[queueIndex].Item2[0].Uri.ToString();
                        Video video = await client.Videos.GetAsync(queues[queueIndex].Item2[0].Uri.ToString());
                        embed.Thumbnail = new EmbedThumbnail
                        {
                            Url = video.Thumbnails.First().Url
                        };
                        embed.Author = new EmbedAuthor
                        {
                            Name = queues[queueIndex].Item2[0].Author
                        };
                        await musicChannels.Where(i => i.Guild == conn.Guild).First().SendMessageAsync(embed.Build());
                        queues[queueIndex].Item2.RemoveAt(0);
                    }
                    catch (Exception) {  }
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
                await ctx.RespondAsync("`Skipping the current track.`");
                await UpdateQueues();
                //queues.Where(i => i.Item1 == conn.Guild).First().Item2.RemoveAt(0);
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
                await ctx.RespondAsync("`Pausing the current track.`");
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
                await ctx.RespondAsync("`Resuming the current track.`");
                await UpdateQueues();
            }
            catch (Exception) { }
        }
        [Command("shuffle")]
        public void Shuffle(CommandContext ctx)
        {
            try
            {
                LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
                int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
                Random rand = new Random();
                queues[queueIndex].Item2.OrderBy(x => rand.Next());
                ctx.RespondAsync("`Suffled the queue!`");
            }
            catch (Exception) { }
        }
        [Command("queue")]
        public async Task ShowQueue(CommandContext ctx)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
            int queueLength = queues[queueIndex].Item2.Count;
            string queueFormatted = $"```Number of songs: {queueLength}.\r\n";
            int i = 1;
            foreach(LavalinkTrack song in queues[queueIndex].Item2)
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.WithUrl(song.Uri.ToString());
                embed.WithTitle(song.Title);
                queueFormatted += i+":{embed}\r\n";
                i++;
            }
            queueFormatted += "```";
            await ctx.RespondAsync(queueFormatted);
        }
        [Command("remove")]
        public async Task RemoveFromQueue(CommandContext ctx, int index)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.WithUrl(queues[queueIndex].Item2[index - 1].Uri.ToString());
            embed.WithTitle(queues[queueIndex].Item2[index - 1].Title);
            await ctx.RespondAsync($"`Removing {embed}!`");
            queues[queueIndex].Item2.RemoveAt(index-1);
        }
        [Command("clear")]
        public async Task ClearQueue(CommandContext ctx)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
            await ctx.RespondAsync("`Clearing the queue!`");
            queues[queueIndex].Item2.Clear();
        }
        [Command("now")]
        public async Task NowPlaying(CommandContext ctx)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            YoutubeClient client = new YoutubeClient();
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = $"Now playing {conn.CurrentState.CurrentTrack.Title}";
            embed.Url = conn.CurrentState.CurrentTrack.Uri.ToString();
            Video video = await client.Videos.GetAsync(conn.CurrentState.CurrentTrack.Uri.ToString());
            embed.Thumbnail = new EmbedThumbnail
            {
                Url = video.Thumbnails.First().Url
            };
            embed.Author = new EmbedAuthor
            {
                Name = conn.CurrentState.CurrentTrack.Author
            };
            await ctx.RespondAsync(embed);
        }
    }
}
