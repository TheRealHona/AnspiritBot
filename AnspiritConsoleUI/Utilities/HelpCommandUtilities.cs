﻿using Discord;
using Discord.Commands;
using System;
using System.Linq;

namespace AnspiritConsoleUI.Utilities
{
    public static class HelpCommandUtilities
    {
        public static Embed GetModuleHelpEmbed(ModuleInfo module, ICommandContext context, IServiceProvider services)
        {
            var title = $"Help: **({module.Name})**";
            var validForCurrentUserCommands = module.Commands.Where(x => x.CheckPreconditionsAsync(context, services).GetAwaiter().GetResult().IsSuccess);

            var embedBuilder = new EmbedBuilder().WithTitle(title).WithColor(Color.Purple);
            foreach (var command in validForCurrentUserCommands)
            {
                embedBuilder.AddField($"**{'!' + command.Name}** " + GetParametersString(command).TrimEnd(' ', ','), $"{(command.Summary == string.Empty ? "No description" : command.Summary)}. ");
            }

            return embedBuilder.Build();
        }

        private static string GetSummaryString(string summary) => string.IsNullOrEmpty(summary) ? "" : $"({summary})";
        private static string GetParametersString(CommandInfo command)
        {
            var output = $"{command.Parameters.Aggregate("", (currentString, nextParameter) => currentString + $"[{nextParameter.Name}{(GetSummaryString(nextParameter.Summary) == string.Empty ? "" : GetSummaryString(nextParameter.Summary))}] ")}";
            return output.Trim() == "Parameters:" ? "" : output;
        }
    }
}