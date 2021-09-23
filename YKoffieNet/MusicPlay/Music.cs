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
using static YKoffieNet.Config;

namespace YKoffieNet.MusicPlay
{
    internal class Music : BaseCommandModule
    {
        List<DiscordChannel> musicChannels = new();
        LavalinkNodeConnection? Gnode;
        List<LavalinkGuildConnection> connections = new();
        List<(DiscordGuild, List<LavalinkTrack>)> queues = new();
        BotConfig config = new();
        
        #region JoinLeave
        //Join the channel the requested member is in.
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            if (!config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any())
            {
                config.guildConfigs.Add(new()
                {
                    guildId = ctx.Guild.Id,
                });
                await SaveBotConfig(config);
            }
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            if (channel == null)
            {
                DiscordEmbedBuilder embed = new()
                {
                    Title = "You are not in a voice channel!"
                };
                await ctx.RespondAsync(embed.Build());
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
                DiscordEmbedBuilder embedError = new();
                embedError.Title = $"{channel.Name} is not a valid voice channel!";
                await ctx.RespondAsync(embedError.Build());
                return;
            }
            LavalinkExtension lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                DiscordEmbedBuilder embedError = new()
                {
                    Title = "Internal server error!"
                };
                await ctx.RespondAsync(embedError.Build());
                return;
            }
            LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
            Gnode = node;
            DiscordEmbedBuilder embed = new();
            embed.Title = $"Joining voice channel, {channel.Name}!";
            await ctx.RespondAsync(embed.Build());
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
                DiscordEmbedBuilder embed = new();
                embed.Title = $"Leaving {channel.Name}!";
                await ctx.RespondAsync(embed.Build());
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
                YoutubeClient client = new();
                DiscordEmbedBuilder embed = new();
                embed.Title = $"Now playing {track.Title}, Duration: {track.Length}.";
                embed.Url = track.Uri.ToString();
                Video video = await client.Videos.GetAsync(track.Uri.ToString());
                embed.Thumbnail = new EmbedThumbnail
                {
                    Url = video.Thumbnails[0].Url
                };
                embed.Author = new EmbedAuthor
                {
                    Name = track.Author
                };
                if (conn.CurrentState.CurrentTrack == null)
                {
                    int confIndex = config.guildConfigs.FindIndex(i => i.guildId == ctx.Guild.Id);
                    if (!config.guildConfigs[confIndex].banList.Contains(track.Uri))
                    {
                        await conn.PlayAsync(track);
                    }
                    else
                    {
                        embed.Title = "The specified track is banned on YKoffie!";
                    }
                    await ctx.RespondAsync(embed.Build());
                    return;
                }
                GuildConfig conf = config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).First();
                List<LavalinkTrack> queue = queues.Where(i => i.Item1 == ctx.Guild).First().Item2;
                if (conf == null)
                {
                    conf = new();
                }
                if (conf.allowDuplicates || !queue.Where(i => i.Uri == track.Uri).Any())
                {
                    int confIndex = config.guildConfigs.FindIndex(i => i.guildId == ctx.Guild.Id);
                    if (!config.guildConfigs[confIndex].banList.Contains(track.Uri))
                    {
                        AddToQueue(ctx.Guild, track);
                        embed.Title = $"Queued {track.Title}!";
                    }
                    else
                    {
                        embed.Title = "The specified track is banned on YKoffie!";
                    }
                        await ctx.RespondAsync(embed.Build());
                }
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
            await Join(ctx);
            YoutubeClient client = new();
            IAsyncEnumerable<PlaylistVideo> playlist = client.Playlists.GetVideosAsync(url);
            int i = 0;
            List<string> videos = new();
            await foreach (var video in playlist)
            {
                videos.Add(video.Url);
                i++;
            }
            DiscordEmbedBuilder embed = new();
            GuildConfig conf = config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).First();
            int maxL = conf.maxPlaylistLength;
            if(maxL == 0) { maxL = int.MaxValue; }
            embed.Title = $"Adding {Math.Min(videos.Count,maxL)} songs to the queue!";
            await ctx.Channel.SendMessageAsync(embed.Build());
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            if (Gnode == null)
            {
                LavalinkExtension lava = ctx.Client.GetLavalink();
                if (!lava.ConnectedNodes.Any())
                {
                    await ctx.RespondAsync("`Internal Server Error.`");
                    return;
                }
                LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
                Gnode = node;
            }
            maxL = Math.Min(videos.Count, maxL);
            i = 1;
            foreach (var video in videos)
            {
                if(maxL >= i)
                {
                    LavalinkLoadResult result = await Gnode.Rest.GetTracksAsync(video);
                    AddToQueue(ctx.Guild, result.Tracks.First());
                    if (conn.CurrentState.CurrentTrack == null)
                    {
                        await UpdateQueues();
                    }
                    i++;
                }
            }
        }
        #endregion
        #region Queue
        public void AddToQueue(DiscordGuild guild, LavalinkTrack track, bool playTop = false)
        {
            try
            {
                List<LavalinkTrack> queue = queues.Where(i => i.Item1 == guild).First().Item2;

                if (!playTop) { queue.Add(track); }
                else { queue.Insert(0, track); }

            }
            catch (Exception){}
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
                        YoutubeClient client = new();
                        DiscordEmbedBuilder embed = new();
                        embed.Title = $"Now playing {queues[queueIndex].Item2[0].Title}";
                        embed.Url = queues[queueIndex].Item2[0].Uri.ToString();
                        Video video = await client.Videos.GetAsync(queues[queueIndex].Item2[0].Uri.ToString());
                        embed.Thumbnail = new EmbedThumbnail
                        {
                            Url = video.Thumbnails[0].Url
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
                DiscordEmbedBuilder embed = new();
                embed.Title = $"Skipping {conn.CurrentState.CurrentTrack.Title}!";
                embed.Url = conn.CurrentState.CurrentTrack.Uri.ToString();
                await ctx.RespondAsync(embed);
                await conn.StopAsync();
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
        public async Task Shuffle(CommandContext ctx)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
            Random rand = new();
            queues[queueIndex] = new(queues[queueIndex].Item1,queues[queueIndex].Item2.OrderBy(x => rand.Next()).ToList());
            DiscordEmbedBuilder embed = new();
            embed.Title = "Shuffled the queue!";
            await ctx.RespondAsync(embed);
        }
        [Command("queue")]
        public async Task ShowQueue(CommandContext ctx)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
            int queueLength = queues[queueIndex].Item2.Count;
            DiscordEmbedBuilder embed = new();
            embed.WithTitle($"Queue length: {queueLength}!");
            int i = 1;
            foreach(LavalinkTrack song in queues[queueIndex].Item2)
            {
                embed.AddField($"{i}: ", $"[{song.Title}]({song.Uri})");
                i++;
            }
            await ctx.RespondAsync(embed.Build());
        }
        [Command("remove")]
        public async Task RemoveFromQueue(CommandContext ctx, int index)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
            DiscordEmbedBuilder embed = new();
            embed.WithUrl(queues[queueIndex].Item2[index - 1].Uri.ToString());
            embed.WithTitle("Removing "+queues[queueIndex].Item2[index - 1].Title+"!");
            await ctx.RespondAsync(embed);
            queues[queueIndex].Item2.RemoveAt(index-1);
        }
        [Command("clear")]
        public async Task ClearQueue(CommandContext ctx)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            int queueIndex = queues.FindIndex(i => i.Item1 == conn.Guild);
            DiscordEmbedBuilder embed = new()
            {
                Title = "Clearing the queue!"
            };
            await ctx.RespondAsync(embed);
            queues[queueIndex].Item2.Clear();
        }
        [Command("now")]
        public async Task NowPlaying(CommandContext ctx)
        {
            LavalinkGuildConnection conn = connections.Where(i => i.Guild == ctx.Guild).First();
            YoutubeClient client = new();
            DiscordEmbedBuilder embed = new();
            embed.Title = $"Now playing {conn.CurrentState.CurrentTrack.Title}";
            embed.Description = $"Song duration:{conn.CurrentState.CurrentTrack.Length}. Time remaining: {conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition}.";
            embed.Url = conn.CurrentState.CurrentTrack.Uri.ToString();
            Video video = await client.Videos.GetAsync(conn.CurrentState.CurrentTrack.Uri.ToString());
            embed.Thumbnail = new EmbedThumbnail
            {
                Url = video.Thumbnails[0].Url
            };
            embed.Author = new EmbedAuthor
            {
                Name = conn.CurrentState.CurrentTrack.Author
            };
            await ctx.RespondAsync(embed);
        }
        [Command("config")]
        public async Task Config(CommandContext ctx)
        {
            if (!config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any())
            {
                config = GetBotConfig();
            }
            if (!config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any())
            {
                config.guildConfigs.Add(new() { guildId = ctx.Guild.Id});
                await SaveBotConfig(config);
            }
            GuildConfig conf = config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).First();
            string banList = "";
            foreach (Uri url in conf.banList)
            {
                banList += $"\r\n{url}";
            }
            banList += ".";
            DiscordEmbedBuilder embed = new();
            embed.Title = "Config";
            embed.AddField("Maximum playlist length: ", conf.maxPlaylistLength.ToString());
            embed.AddField("Allow duplicates: ", conf.allowDuplicates.ToString());
            embed.AddField("Banned songs: ", banList);
            await ctx.Channel.SendMessageAsync(embed);
        }
        [Command("config")]
        public async Task Config(CommandContext ctx, string value)
        {
            if (!config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any())
            {
                config.guildConfigs.Add(new() { guildId = ctx.Guild.Id});
                await SaveBotConfig(config);
            }
            if (config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any())
            {
                int confIndex = config.guildConfigs.FindIndex(i => i.guildId == ctx.Guild.Id);
                DiscordEmbedBuilder embed = new();
                embed.Title = "Config";
                if (value.ToLower() == "maximumplaylistlenght")
                {
                    embed.Description = $"Maximum playlist length: {config.guildConfigs[confIndex].maxPlaylistLength}.";
                }
                if (value.ToLower() == "allowduplicates")
                {
                    embed.Description = $"Allowing Duplicates: {config.guildConfigs[confIndex].allowDuplicates}.";
                }
                if (value.ToLower() == "banlist")
                {
                    string banList = "";
                    foreach (Uri url in config.guildConfigs[confIndex].banList)
                    {
                        banList += $"\r\n{url}";
                    }
                    embed.Description = "Banned songs: \r\n" + banList;
                }
                    await ctx.Channel.SendMessageAsync(embed);
            }
        }
        [Command("config")]
        public async Task Config(CommandContext ctx, string value, string contents)
        {
            if (!config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any())
            {
                config.guildConfigs.Add(new() { guildId = ctx.Guild.Id});
                await SaveBotConfig(config);
            }
            if (config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any())
            {
                int confIndex = config.guildConfigs.FindIndex(i => i.guildId == ctx.Guild.Id);
                DiscordEmbedBuilder embed = new();
                embed.Title = "Config";
                int a = 0;
                bool b = false;
                if (value.ToLower() == "maximumplaylistlength" && int.TryParse(contents, out a))
                {
                    config.guildConfigs[confIndex].maxPlaylistLength = a;
                    embed.Description = $"Set maximum playlist length to {a}.";
                }
                else if (value.ToLower() == "allowduplicates" && bool.TryParse(contents, out b))
                {
                    config.guildConfigs[confIndex].allowDuplicates = b;
                    embed.Description = $"Set allow duplicates to {b}.";
                }
                else
                {
                    embed.Description = "Invalid config option!";
                }
                await ctx.Channel.SendMessageAsync(embed);
                await SaveBotConfig(config);
            }
        }
        [Command("playtop")]
        public async Task Playtop(CommandContext ctx, [RemainingText] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("`You are not in a voice channel.`");
                return;
            }
            try
            {
                if (!musicChannels.Where(i => i.Guild == ctx.Guild).Any())
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
                YoutubeClient client = new();
                DiscordEmbedBuilder embed = new();
                embed.Title = $"Now playing {track.Title}";
                embed.Url = track.Uri.ToString();
                Video video = await client.Videos.GetAsync(track.Uri.ToString());
                embed.Thumbnail = new EmbedThumbnail
                {
                    Url = video.Thumbnails[0].Url
                };
                embed.Author = new EmbedAuthor
                {
                    Name = track.Author
                };
                int confIndex = config.guildConfigs.FindIndex(i => i.guildId == ctx.Guild.Id);
                if (config.guildConfigs[confIndex].banList.Contains(track.Uri))
                {
                    embed.Title = "The specified song is banned from the server!";
                    await ctx.RespondAsync(embed.Build());
                    return;
                }
                if (conn.CurrentState.CurrentTrack == null)
                {
                    await ctx.RespondAsync(embed.Build());
                    await conn.PlayAsync(track);
                    return;
                }
                if (!config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).Any() && config.guildConfigs.Count == 0)
                {
                    config = GetBotConfig();
                }
                GuildConfig conf = config.guildConfigs.Where(i => i.guildId == ctx.Guild.Id).First();
                List<LavalinkTrack> queue = queues.Where(i => i.Item1 == ctx.Guild).First().Item2;
                if (conf == null)
                {
                    conf = new();
                }
                if (conf.allowDuplicates || !queue.Where(i => i.Uri == track.Uri).Any())
                {
                    AddToQueue(ctx.Guild, track, true);
                    embed.Title = $"Added {track.Title} to the top of the queue!";
                    await ctx.RespondAsync(embed.Build());
                }
                else
                {
                    embed.Title = "This guild does not allow duplicate songs!";
                }
            }
            catch (Exception)
            {
                await Join(ctx);
                await Playtop(ctx, search);
                return;
            }
        }
        [Command("ban")]
        public async Task Ban(CommandContext ctx, [RemainingText] string search)
        {
            if (Gnode == null)
            {
                return;
            }
            LavalinkLoadResult result = await Gnode.Rest.GetTracksAsync(search);
            int confIndex = config.guildConfigs.FindIndex(i => i.guildId == ctx.Guild.Id);
            config.guildConfigs[confIndex].banList.Add(result.Tracks.First().Uri);
            await SaveBotConfig(config);
            DiscordEmbedBuilder embed = new();
            embed.Title = "Ban";
            embed.Description = $"Banned {result.Tracks.First().Title} from YKoffie!";
            embed.Url = result.Tracks.First().Uri.ToString();
            await ctx.RespondAsync(embed.Build());
        }
        [Command("unban")]
        public async Task Unban(CommandContext ctx, string search)
        {
            if (Gnode == null)
            {
                return;
            }
            LavalinkLoadResult result = await Gnode.Rest.GetTracksAsync(search);
            int confIndex = config.guildConfigs.FindIndex(i => i.guildId == ctx.Guild.Id);
            config.guildConfigs[confIndex].banList.Remove(result.Tracks.First().Uri);
            await SaveBotConfig(config);
            DiscordEmbedBuilder embed = new();
            embed.Title = "Unban";
            embed.Description = $"Unbanned {result.Tracks.First().Title} from YKoffie!";
            embed.Url = result.Tracks.First().Uri.ToString();
            await ctx.RespondAsync(embed.Build());
        }
    }
}
