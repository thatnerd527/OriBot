using System;
using System.Linq;
using System.Reflection;

using Discord.WebSocket;
using OriBot.Framework;
using OriBot.Framework.UserProfiles;
using OriBot.PassiveHandlers.RequirementEngine;
using OriBot.Utilities;

namespace OriBot.PassiveHandlers
{
    public class AnyModPassiveHandler : OricordPassiveHandler
    {
        public AnyModPassiveHandler(DiscordSocketClient client, SocketMessage message) : base(client, message)
        {
        }

        [PassiveHandler]
        public void OnMessage() {
            var guild = ((SocketTextChannel)message.Channel).Guild;
            var channel = (SocketTextChannel)message.Channel;
            var moderatorrolename = Config.properties["rolenames"]["moderators"];
            var moderators = guild.Users
                .Where(x => 
                (x.Roles.Where(x => x.Name.Equals(moderatorrolename)).Any()) &&
                x.Status != Discord.UserStatus.Offline
                )
                .ToList();
            if (!moderators.Any())
            {
                var modrole = guild.Roles.Where(x => x.Name.Equals(moderatorrolename)).First();
                channel.SendMessageAsync("<@" + modrole + ">, I have pinged all moderators for you.");
            } else
            {
                var moderator = moderators[(int)Math.Round(Random.Shared.NextDouble() * (moderators.Count - 1))];
                channel.SendMessageAsync("<@" + moderator.Id + ">, Will be here to assist you.");
            }
        }
    }
}