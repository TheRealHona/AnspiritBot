﻿using System;
using System.Threading.Tasks;
using AnspiritConsoleUI.Commands.Preconditions;
using AnspiritConsoleUI.Services;
using AnspiritConsoleUI.Utilities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AnspiritConsoleUI.Commands.Groups
{
    [RequireAnspiritDiscordOfficerPrecondition(Group = "DiscordOfficerDebug")]
    [RequireOwner(Group = "DiscordOfficerDebug")]
    [Group("officernotes")]
    [Alias("on")]
    public class AnzacOfficerNotesCommandModule : AnspiritModuleBase
    {
        public AnspiritDatabaseService DbService { get; set; }
        [Command("add")]
        [Summary("Adds an officers notes categories")]
        public async Task AddOfficerNote(IUser discordUser, string categoryName, [Remainder] string notes)
        {
            if (DiscordUtilities.UserIsInCallingOfficersGuild(Context.User as SocketGuildUser, discordUser as SocketGuildUser))
            {
                await DbService.AddOfficerNote(categoryName, discordUser.Id, notes);
                await ReplyNewEmbed("Added the note successfully", Color.Green);
            }
            else
            {
                await ReplyNewEmbed("You cannot access members in other guilds.", Color.Red);
            }
        }
        [Command("remove")]
        [Alias("delete", "del", "rem")]
        [Summary("Removes an officer note")]
        public async Task RemoveOfficerNote(IUser discordUser, string categoryName, [Summary("dd/mm/yyyy")] string dateTime, [Remainder] string comment = null)
        {
            if (DiscordUtilities.UserIsInCallingOfficersGuild(Context.User as SocketGuildUser, discordUser as SocketGuildUser))
            {
                // TODO: This could be parsed simply by DateTime.Parse
                var datetimeParts = dateTime.Split('/');
                var dayString = datetimeParts[0];
                var monthString = datetimeParts[1];
                var yearString = datetimeParts[2];

                if (!int.TryParse(dayString, out var day))
                {
                    throw new Exception("Could not parse the day from the datetime string, are you using dd/mm/yyyy format?");
                }
                if (!int.TryParse(monthString, out var month))
                {
                    throw new Exception("Could not parse the month from the datetime string, are you using dd/mm/yyyy format?");
                }
                if (!int.TryParse(yearString, out var year))
                {
                    throw new Exception("Could not parse the year from the datetime string, are you using dd/mm/yyyy format?");
                }

                await DbService.RemoveOfficerNotesAsync(discordUser.Id, categoryName, new DateTime(year, month, day), comment);
                await ReplyNewEmbed($"Removed the note successfully", Color.Green);
            }
            else
            {
                await ReplyNewEmbed("You cannot access members in other guilds.", Color.Red);
            }
        }
        [Command("list")]
        [Alias("ls", "get", "")]
        [Summary("Returns a list of the officer notes against a user")]
        public async Task ListOfficerNotes(IUser user)
        {
            var outputEmbeds = await AnspiritUtilities.GetOfficerNotesEmbedsAsync(user, DbService);
            await outputEmbeds.SendAllAsync(Context.Channel);
        }
    }
}
