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
using EtiBotCore.Data.Structs;
using OldOriBot.Data.Persistence;
using OriBot;
using OriBot.;
using OriBot.Commands;
using OriBot.DB;
using OriBot.EventHandlers;
using OriBot.Framework;
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
        public static void Main(string[] args) => Login();

        private static DiscordSocketClient _client;

        // private PassiveHandlerHub _passiveHandlerHub;

        public async Task MainAsync()
        {
            Logger.Cleanup(); // just in case the bot crashes or is forcefully shut off, this gets triggered
            using var ct = new CancellationTokenSource();
            var inputTask = ReadConsoleInputAsync(ct.Token);
            await Task.WhenAny(inputTask);
            ct.Cancel();
            await inputTask.ContinueWith(_ => { });
        }

        private async Task ReadConsoleInputAsync(CancellationToken cancellationToken)
        {
            // TODO: may wanna fix this
            var exit = "exit";
            var help = "help";
            var sel = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                // Asynchronously read the next line from the console
                var input = await Task.Run(Console.ReadLine);

                if (input.ToLower() == exit)
                {
                    sel = 1;
                }

                if (input.ToLower() == help)
                {
                    sel = 2;
                }

                switch (sel)
                {
                    case 1:
                        sel = 0;
                        await Cleanup(0);
                        break;

                    case 2:
                        Logger.Info("define help here please lol");
                        sel = 0;
                        break;

                    default:
                        Logger.Info("'" + input + "' is not reconized as an internal command. Try 'help' for more information.");
                        sel = 0;
                        break;
                }
            }
        }

        public static void Login()
        {
            Logger.Info($"##############################");
            Logger.Info($"### Starting Oribot profile migrator v{Constants.OriBotVersion} ###");
            Logger.Info($"##############################");

            string storage = UserProfile.BaseStorageDir;
            string database = Path.Combine(AppContext.BaseDirectory, "Data", "db.db");

            List<UserProfile> profiles = Directory.EnumerateFiles(UserProfile.BaseStorageDir)
                .Select(x => ulong.Parse(Path.GetFileNameWithoutExtension(x)))
                .Select(x => ProfileManager.GetUserProfile(x))
                .ToList();

            InfractionLogProvider infractionLogProvider = new InfractionLogProvider(@"F:\visualstudio\OriBot\Infractions");
            infractionLogProvider.AppendAlert(345819451247296516, new Snowflake())
            foreach (var item in infractionLogProvider.GetAllLogs())
            {
                Logger.Info(item);
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

            // put the code for infractions here, once we figure it out lmao
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