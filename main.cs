using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;

using Microsoft.Extensions.Logging.Abstractions;

using Newtonsoft.Json.Linq;

using OldOriBot.Data;
using OldOriBot.Data.Commands;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Data.Persistence;
using OldOriBot.Utility;

using OriBot;
using OriBot.Commands;
using OriBot.DB;
using OriBot.EventHandlers;
using OriBot.Framework;
using OriBot.Framework.UserBehaviour;
using OriBot.Framework.UserProfiles;
using OriBot.PassiveHandlers;
using OriBot.Storage;
using OriBot.Utilities;

namespace main
{
    public static class Memory
    {
        public static Dictionary<string, Context> ContextStorage = new Dictionary<string, Context>();
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            _ = new Program().MainAsync();
        }

        private static DiscordSocketClient _client;

        // private PassiveHandlerHub _passiveHandlerHub;

        public async Task MainAsync()
        {
            // just in case the bot crashes or is forcefully shut off, this gets triggered

            Task.Run(Login);
            Thread.Sleep(int.MaxValue);


        }

        private static ulong IDGenerator(UserProfile profile)
        {
            if (profile.BehaviourLogs.Logs.Count == 0)
            {
                return 1;
            }
            else
            {
                return profile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
            }
        }

        public static async Task Login()
        {
            Logger.Info($"##############################");
            Logger.Info($"### Starting Oribot profile migrator v{Constants.OriBotVersion} ###");
            Logger.Info($"##############################");

            string storage = UserProfile.BaseStorageDir;
            string database = Path.Combine(AppContext.BaseDirectory, "Data", "db.db");
            InfractionLogProvider infractionLogProvider = new InfractionLogProvider(@"D:\TRASO\Code\visual\C#\OribotMigration\bin\Debug\net8.0\Data\Infractions");
            List<UserProfile> profiles = Directory.EnumerateFiles(UserProfile.BaseStorageDir)
                .Where(x => x.EndsWith(".json") || x.EndsWith(".profile"))
                .Select(x => ulong.Parse(Path.GetFileNameWithoutExtension(x)))
                .Select(x => ProfileManager.GetUserProfile(x))
                .ToList();
            DataPersistence.DoStaticInit();
            await DiscordClient.Setup();
            GatewayIntent intents =
                GatewayIntent.DIRECT_MESSAGES |
                GatewayIntent.GUILDS |
                GatewayIntent.GUILD_PRESENCES |
                GatewayIntent.GUILD_BANS |
                GatewayIntent.GUILD_MEMBERS |
                GatewayIntent.GUILD_MESSAGES |
                GatewayIntent.GUILD_MESSAGE_REACTIONS |
                GatewayIntent.GUILD_VOICE_STATES;
            var token = File.ReadAllText("token.txt");
            var discordclient = new DiscordClient(token, intents)
            {
                ReconnectOnFailure = true,
                DevMode = false
            };
            BotContextRegistry.InitializeBotContexts();


            // what da bot gonna do fo today ####



            await discordclient.ConnectAsync();
            CommandMarshaller.Initialize();
            while ((await DiscordClient.Current.Events.GuildEvents.OnGuildCreated.Wait()).ID != 1005355539447959552) { } 
            var botcontext = BotContextRegistry.GetContext(new Snowflake(1005355539447959552));
            var muteutil = MemberMuteUtility.GetOrCreate(botcontext);
            foreach (var item in profiles)
            {
                var logsfor = infractionLogProvider.For(new Snowflake(item.UserID)).Entries;

                foreach (var item1 in logsfor)
                {
                    if (item1.Hidden)
                    {
                        //switch (item1.Type)
                        //{
                        //    case InfractionLogProvider.LogType.Note:
                        //        {
                        //            var logentry = UserBehaviourLogRegistry.CreateLogEntry<Modera>();
                        //            logentry.ModeratorId = item1.ModeratorID.Value;
                        //            logentry.Note = item1.Information;
                        //            item.BehaviourLogs.AddLogEntry(logentry);
                        //        }
                        //        break;
                        //}
                        continue;
                    }

                    switch (item1.Type)
                    {
                        case InfractionLogProvider.LogType.Note:
                        {
                            var logentry = new ModeratorNoteLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, item1.ModeratorID);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;
                        case InfractionLogProvider.LogType.Unmute:
                        {
                            var logentry = new ModeratorUnmuteLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, item1.ModeratorID);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;
                        case InfractionLogProvider.LogType.Warning:
                        {
                            var logentry = new ModeratorWarnLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, WarnType.Normal, item1.ModeratorID);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;
                        case InfractionLogProvider.LogType.MinorWarning:
                        {
                            var logentry = new ModeratorWarnLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, WarnType.Minor, item1.ModeratorID);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;
                        case InfractionLogProvider.LogType.MajorWarning:
                        {
                            var logentry = new ModeratorWarnLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, WarnType.Harsh, item1.ModeratorID);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;
                        case InfractionLogProvider.LogType.Ban:
                        {
                            var logentry = new ModeratorBanLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, item1.ModeratorID, 0);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;
                        case InfractionLogProvider.LogType.Pardon:
                        {
                            var logentry = new ModeratorUnbanLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, item1.ModeratorID);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;
                        case InfractionLogProvider.LogType.Mute:
                        {

                            var logentry = new ModeratorMuteLogEntry(IDGenerator(item), (ulong)item1.Time.ToUnixTimeMilliseconds(), item1.Information, item1.ModeratorID, "", DateTime.MinValue);

                            item.BehaviourLogs.AddLogEntry(logentry);
                        }
                        break;

                    }
                }

                if (muteutil.IsMutedInRegistry(new Snowflake(item.UserID)))
                {
                    var hasmutes = item.BehaviourLogs.Logs.Where(x => x is ModeratorMuteLogEntry).Any();

                    if (hasmutes)
                    {
                        var lastitem = item.BehaviourLogs.Logs.Where(x => x is ModeratorMuteLogEntry).Last().Instantiate() as ModeratorMuteLogEntry;
                        lastitem.MuteEndUTC = DateTime.UtcNow + muteutil.GetRemainingMuteTime(new Snowflake(item.UserID));
                        item.BehaviourLogs.RemoveByID(lastitem.ID);
                        item.BehaviourLogs.AddLogEntry(lastitem);
                    }

                    // @traso please handle this too, add some logic if the user is muted.

                }
            }





            using var db = new SpiritContext(database);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            foreach (UserProfile profile in profiles)
            {
                User dbUser = new User
                {
                    UserId = profile.UserID
                };
                db.Users.Add(dbUser);

                foreach (OriBot.Framework.UserProfiles.Badges.Badge badge in profile.Badges)
                {
                    Badge dbBadge = db.Badges.FirstOrDefault(b => b.BadgeName == badge.Name);
                    if (dbBadge is null)
                    {
                        dbBadge = new Badge
                        {
                            BadgeName = badge.Name,
                            BadgeDescription = badge.Description,
                            BadgeEmote = badge.Icon
                        };

                        db.Badges.Add(dbBadge);
                    }

                    UserBadge dbUserBadge = new UserBadge
                    {
                        User = dbUser,
                        Badge = dbBadge,
                        Count = badge.Level
                    };
                    db.UserBadges.Add(dbUserBadge);
                }
            }
            db.SaveChanges();
            foreach (UserProfile profile in profiles)
            {
                foreach (UserBehaviourLogEntry log in profile.BehaviourLogs.Logs)
                {
                    if (log is ModeratorNoteLogEntry note)
                    {
                        User dbUserP = db.Users.Single(u => u.UserId == profile.UserID);
                        User dbUserI = db.Users.Single(u => u.UserId == note.ModeratorId);

                        Punishment dbNote = new()
                        {
                            Type = PunishmentType.Note,
                            Reason = note.Note,
                            Issued = DateTimeOffset.FromUnixTimeMilliseconds((long)note.TimestampUTC).DateTime,
                            Expiry = null,
                            CheckForExpiry = false,
                            Punished = dbUserP,
                            Issuer = dbUserI
                        };

                        db.Punishments.Add(dbNote);
                    }
                    else if (log is ModeratorUnmuteLogEntry unmute)
                    {
                        Punishment dbUnmute = db.Punishments.Where(p => p.Type == PunishmentType.Mute).OrderByDescending(p => p.Issued).First();

                        dbUnmute.Reason += $" (This mute was removed for the following reason: {unmute.Reason})";
                    }
                    else if (log is ModeratorWarnLogEntry warn)
                    {
                        User dbUserP = db.Users.Single(u => u.UserId == profile.UserID);
                        User dbUserI = db.Users.Single(u => u.UserId == warn.ModeratorId);

                        Punishment dbWarn = new()
                        {
                            Type = PunishmentType.Warn,
                            Reason = warn.Reason,
                            Issued = DateTimeOffset.FromUnixTimeMilliseconds((long)warn.TimestampUTC).DateTime,
                            Expiry = null,
                            CheckForExpiry = false,
                            Punished = dbUserP,
                            Issuer = dbUserI
                        };

                        db.Punishments.Add(dbWarn);
                    }
                    else if (log is ModeratorBanLogEntry ban)
                    {
                        User dbUserP = db.Users.Single(u => u.UserId == profile.UserID);
                        User dbUserI = db.Users.Single(u => u.UserId == ban.ModeratorId);

                        Punishment dbBan = new()
                        {
                            Type = PunishmentType.Ban,
                            Reason = ban.Reason,
                            Issued = DateTimeOffset.FromUnixTimeMilliseconds((long)ban.TimestampUTC).DateTime,
                            Expiry = null,
                            CheckForExpiry = false,
                            Punished = dbUserP,
                            Issuer = dbUserI
                        };

                        db.Punishments.Add(dbBan);
                    }
                    else if (log is ModeratorUnbanLogEntry unban)
                    {
                        Punishment dbUnban = db.Punishments.Where(p => p.Type == PunishmentType.Ban).OrderByDescending(p => p.Issued).First();

                        dbUnban.Reason += $" (This ban was removed for the following reason: {unban.Reason})";
                    }
                    else if (log is ModeratorMuteLogEntry mute)
                    {
                        User dbUserP = db.Users.Single(u => u.UserId == profile.UserID);
                        User dbUserI = db.Users.Single(u => u.UserId == mute.ModeratorId);

                        Punishment dbMute = new()
                        {
                            Type = PunishmentType.Ban,
                            Reason = mute.Reason,
                            Issued = DateTimeOffset.FromUnixTimeMilliseconds((long)mute.TimestampUTC).DateTime,
                            Expiry = mute.MuteEndUTC,
                            CheckForExpiry = false,
                            Punished = dbUserP,
                            Issuer = dbUserI
                        };

                        db.Punishments.Add(dbMute);
                    }
                }
            }
            db.SaveChanges();
        }

        private void RegisterSlashCommands()
        {
            _client.Ready += async () =>
            {
                var _interactionService = new InteractionService(_client.Rest);
                await _interactionService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                                services: null);

                await _interactionService.RegisterCommandsGloballyAsync(false);
                _client.InteractionCreated += async (x) =>
                {
                    var ctx = new SocketInteractionContext(_client, x);
                    await _interactionService.ExecuteCommandAsync(ctx, null);
                };

                GlobalTimerStorage.Load();
            };
        }

        private void AddAllContexts()
        {
            Memory.ContextStorage.Add("oricord", new OricordContext());
        }

        private async Task Cleanup(int exitCode)
        {
            Logger.Info($"Shutting down with exit code {exitCode}");
            Logger.Cleanup();
            ProfileManager.SaveAllNow();
            Environment.Exit(exitCode);
            await Task.CompletedTask;
        }

        private Task Log(LogMessage text)
        {
            var translateLevel = text.Severity;

            switch (translateLevel)
            {
                case LogSeverity.Info:
                    Logger.Info(text.ToString()[9..]);
                    break;

                case LogSeverity.Verbose:
                    Logger.Verbose(text.ToString()[9..]);
                    break;

                case LogSeverity.Warning:
                    Logger.Warning(text.ToString()[9..]);
                    break;

                case LogSeverity.Error:
                    Logger.Error(text.ToString()[9..]);
                    break;

                case LogSeverity.Critical:
                    Logger.Fatal(text.ToString()[9..]);
                    break;

                default:
                    // just in case
                    Logger.Verbose("Missed to log a gateway level");
                    break;
            }
            return Task.CompletedTask;
        }

        //   private Task BotReady()
        //   {
        //       logger.Log("Bot is ready");

        //       return Task.CompletedTask;
        //   }

        //   private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        //{
        // // If the message was not in the cache, downloading it will result in getting a copy of `after`.
        // var message = await before.GetOrDownloadAsync();
        // Console.WriteLine($"{message} -> {after}");
        //}

        public static DiscordSocketClient Client => _client;
    }
}