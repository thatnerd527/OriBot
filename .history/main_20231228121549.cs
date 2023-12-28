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
using OldOriBot.Data.Persistence;
using OriBot;
using OriBot.Commands;
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
        public static Task Main(string[] args) => new Program().MainAsync();

        private static DiscordSocketClient _client;

        // private PassiveHandlerHub _passiveHandlerHub;

        public async Task MainAsync()
        {
            Logger.Cleanup(); // just in case the bot crashes or is forcefully shut off, this gets triggered
            using var ct = new CancellationTokenSource();
            var task = Login(ct.Token);
            var inputTask = ReadConsoleInputAsync(ct.Token);
            await Task.WhenAny(task, inputTask);
            ct.Cancel();
            await inputTask.ContinueWith(_ => { });
            await task;
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

        public async Task Login(CancellationToken ct)
        {
            Logger.Info($"##############################");
            Logger.Info($"### Starting Oribot profile migrator v{Constants.OriBotVersion} ###");
            Logger.Info($"##############################");

            List<UserProfile> profiles = Directory.EnumerateFiles(UserProfile.BaseStorageDir)
            .Select(x => ulong.Parse(Path.GetFileNameWithoutExtension(x)))
            .Select(x => ProfileManager.GetUserProfile(x))
            .ToList();

            InfractionLogProvider infractionLogProvider = new InfractionLogProvider(@"F:\visualstudio\OriBot\Infractions");
            foreach (var item in infractionLogProvider.GetAllLogs())
            {
                Logger.Info(item.UserID.ToString());
            }



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