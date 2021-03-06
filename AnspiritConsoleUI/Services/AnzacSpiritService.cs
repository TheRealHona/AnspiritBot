﻿using AnspiritConsoleUI.Models;
using AnspiritConsoleUI.Services.Google;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnspiritConsoleUI.Services
{
    public class AnzacSpiritService
    {
        private AnspiritSheetsService _anspiritSheetsService;
        private AnspiritDatabaseService _dbService;
        public AnzacSpiritService(AnspiritSheetsService anspiritSheetsService, AnspiritDatabaseService dbService)
        {
            _anspiritSheetsService = anspiritSheetsService;
            _dbService = dbService;
        }
        public WarZones GetWarZones()
        {
            var data = _anspiritSheetsService.GetWarPlacementValues();

            var warZones = new WarZones();

            for (int column = 0; column < data.Count; column += 2)
            {
                for (int row = 1; row < data[column].Count; row++)
                {
                    var player = (string)data[column][row];
                    var team = (string)data[column + 1][row];

                    if (string.IsNullOrWhiteSpace(player))
                    {
                        throw new Exception($"Player name is null or whitespace, at column {0}, row {row}");
                    }
                    if (string.IsNullOrWhiteSpace(team))
                    {
                        throw new Exception($"Team name is null or whitespace, at column {0 + 1}, row {row}");
                    }

                    warZones[column].Add(new Deployment
                    {
                        Player = player,
                        Team = team
                    });
                }
            }

            return warZones;
        }
        public List<Tuple<string, Deployment>> GetWarOrdersSortedByZone(WarZones orders)
        {
            PropertyInfo[] properties = typeof(WarZones).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var allOrders = new List<Tuple<string, Deployment>>();
            foreach (PropertyInfo p in properties)
            {
                // Only work with strings
                if (p.PropertyType == typeof(List<Deployment>) && p.CanRead && p.CanWrite)
                {
                    var zone = (List<Deployment>)p.GetValue(orders);
                    var data = zone.Select(x => new Tuple<string, Deployment>(p.Name, x));
                    allOrders.AddRange(data);
                }

            }

            return allOrders;
        }
        public void PrintDistictPlayers(List<Tuple<string, Deployment>> orders)
        {
            var allPlayers = orders.Select(x => x.Item2.Player).Distinct().ToList();
            Console.WriteLine("Players: " + allPlayers.Count);
            Console.WriteLine(string.Join(Environment.NewLine, allPlayers));
        }
        public Dictionary<ulong, List<Tuple<string, Deployment>>> GetWarOrdersSortedByDiscordUser()
        {
            var orders = GetWarOrdersSortedByZone(GetWarZones());

            foreach (var order in orders)
            {
                order.Item2.Player = order.Item2.Player.Trim();
            }

            var allPlayers = orders.Select(x => x.Item2.Player.Trim()).Distinct().ToList();

            var output = new Dictionary<ulong, List<Tuple<string, Deployment>>>();

            var playerDiscords = _dbService.GetInGamePlayerDiscordLinks().ToList();
            foreach (var player in playerDiscords)
            {
                player.InGameName = player.InGameName.Trim();
            }

            foreach (var player in allPlayers)
            {
                // Should always be >= 1
                var deploymentOrders = orders.Where(x => x.Item2.Player == player).ToList();
                var playerLink = playerDiscords.FirstOrDefault(x => string.Equals(x.InGameName, player, StringComparison.CurrentCultureIgnoreCase));
                if (playerLink == null)
                {
                    throw new Exception("Could not find a player link for ingame name of " + player);
                }

                var discordId = playerLink.DiscordId;

                if (output.ContainsKey(discordId))
                {
                    // Already added, so its an alt account
                    deploymentOrders = deploymentOrders.Select(x => new Tuple<string, Deployment>(x.Item1, new Deployment { Player = x.Item2.Player, Team = x.Item2.Team + $" ({player})" })).ToList();
                    output[discordId].AddRange(deploymentOrders);
                }
                else
                {
                    output.Add(discordId, deploymentOrders);
                }
            }
            return output;
        }
        public Embed GetPlayerOrdersEmbed(KeyValuePair<ulong, List<Tuple<string, Deployment>>> playerOrder)
        {
            var embedBuilder = new EmbedBuilder()
            {
                Color = Color.Purple,
                Title = playerOrder.Value[0].Item2.Player,
                Timestamp = DateTime.Now
            };

            var zones = playerOrder.Value.Select(x => x.Item1).Distinct();

            foreach (var zone in zones)
            {
                embedBuilder.AddField(zone, string.Join(", ", playerOrder.Value.Where(x => x.Item1 == zone).Select(x => x.Item2.Team)));
            }

            return embedBuilder.Build();
        }
        public IEnumerable<Embed> GetOfficerNotesEmbeds()
        {
            return null;
        }
    }
}
