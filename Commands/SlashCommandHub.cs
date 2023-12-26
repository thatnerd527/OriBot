using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands.RequirementEngine;
using OriBot.Framework;
using OriBot.Utilities;

namespace OriBot.Commands
{
    public class RequirementsAttribute : PreconditionAttribute
    {
        public Type classtarget;

        public RequirementsAttribute(Type requirementsengine)
        {
            classtarget = requirementsengine;
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (classtarget.IsAbstract)
            {
                return PreconditionResult.FromError("Abstract. ");
            }
            var tmp = Activator.CreateInstance(classtarget);
            if (!(tmp is IRequirementCheck engine))
            {
                Logger.Error($"PLEASE FIX: {tmp.GetType().Name} does not implement IPermissionCheck.");
                return PreconditionResult.FromError("PLEASE FIX: " + tmp.GetType().Name + " does not implement IPermissionCheck.");
            } else {
                if (await engine.GetRequirements().CheckRequirements(context,commandInfo,services)) {
                    return PreconditionResult.FromSuccess();
                } else {
                    return PreconditionResult.FromError("You do not meet the requirements");
                }
            }
        }
    }
}