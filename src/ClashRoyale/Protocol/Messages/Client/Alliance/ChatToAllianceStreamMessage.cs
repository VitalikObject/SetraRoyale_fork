using System;
using System.Linq;
using ClashRoyale.Database;
using ClashRoyale.Logic;
using ClashRoyale.Logic.Clan.StreamEntry.Entries;
using ClashRoyale.Logic.Home.Decks;
using ClashRoyale.Protocol.Messages.Server;
using ClashRoyale.Utilities;
using ClashRoyale.Utilities.Netty;
using DotNetty.Buffers;

namespace ClashRoyale.Protocol.Messages.Client.Alliance
{
    public class ChatToAllianceStreamMessage : PiranhaMessage
    {
        public ChatToAllianceStreamMessage(Device device, IByteBuffer buffer) : base(device, buffer)
        {
            Id = 14315;
        }

        public string Message { get; set; }

        public override void Decode()
        {
            Message = Reader.ReadScString();
        }

        public override async void Process()
        {
            var info = Device.Player.Home.AllianceInfo;
            if (!info.HasAlliance) return;

            var alliance = await Resources.Alliances.GetAllianceAsync(info.Id);
            if (alliance == null) return;

            if (Message.StartsWith('/'))
            {
                var cmd = Message.Split(' ');
                var cmdType = cmd[0];
                var cmdValue = 0;

                if (cmd.Length > 1)
                    if (Message.Split(' ')[1].Any(char.IsDigit))
                        int.TryParse(Message.Split(' ')[1], out cmdValue);

                switch (cmdType)
                {
                    case "/max":
                    {
                        var deck = Device.Player.Home.Deck;

                        foreach (var card in Cards.GetAllCards())
                        {
                            deck.Add(card);

                            for (var i = 0; i < 12; i++) deck.UpgradeCard(card.ClassId, card.InstanceId, true);
                        }

                        await new ServerErrorMessage(Device)
                        {
                            Message = "Added all cards with max level"
                        }.SendAsync();

                        break;
                    }

                    case "/unlock":
                    {
                        var deck = Device.Player.Home.Deck;

                        foreach (var card in Cards.GetAllCards()) deck.Add(card);

                        await new ServerErrorMessage(Device)
                        {
                            Message = "Added all cards"
                        }.SendAsync();

                        break;
                    }

                    case "/gold":
                    {
                        Device.Player.Home.Gold += cmdValue;
                        Device.Disconnect();
                        break;
                    }

                    case "/gems":
                        {
                            Device.Player.Home.Diamonds += cmdValue;
                            Device.Disconnect();
                            break;
                        }

                    case "/status":
                    {
                            var entry = new ChatStreamEntry
                            {
                                Message =
                                $"Server status:\nBuild version: 1.5 (for 1.9.2)\nFingerprint SHA:\n{Resources.Fingerprint.Sha}\nOnline Players: {Resources.Players.Count}\nTotal Players: {await PlayerDb.CountAsync()}\n1v1 Battles: {Resources.Battles.Count}\n2v2 Battles: {Resources.DuoBattles.Count}\nTotal Clans: {await AllianceDb.CountAsync()}\nUptime: {DateTime.UtcNow.Subtract(Resources.StartTime).ToReadableString()}\nUsed RAM: {System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024) + " MB"}"
                            };

                            entry.SetSender(Device.Player);

                            alliance.AddEntry(entry);
                            break;
                    }

                    case "/free":
                    {
                        Device.Player.Home.FreeChestTime = Device.Player.Home.FreeChestTime.Subtract(TimeSpan.FromMinutes(245));
                        Device.Disconnect();
                        break;
                    }

                        /*case "/replay":
                        {
                            await new HomeBattleReplayDataMessage(Device).SendAsync();
                            break;
                        }*/

                        case "/trophies":
                        {
                            if (cmdValue >= 0)
                                Device.Player.Home.Arena.AddTrophies(cmdValue);
                            else if (cmdValue < 0)
                                Device.Player.Home.Arena.RemoveTrophies(cmdValue);

                            Device.Disconnect();
                            break;
                        }
                    case "/set":
                        {
                            Device.Player.Home.Arena.SetTrophies(cmdValue);

                            Device.Disconnect();
                            break;
                        }
                     case "/help":
                    {
                            var help = new ChatStreamEntry
                            {
                                Message =
                                $"List of commands:\n/max - open all cards max. level\n/unlock - open all cards\n/gold x - give out gold, where x - amount of gold\n/ gems x - give out gems, where x - amount of gems\n/ status - a command that shows the server status (needed for admins)\n / free - resets the timer of the free chest\n/trophies x - adds trophies, where x - the number of trophies (can be negative)\n/ set x - the specified number of trophies is available, where x - the number of trophies"
                            };

                            help.SetSender(Device.Player);

                            alliance.AddEntry(help);
                            break;
                    }
                    default:
                        var error = new ChatStreamEntry
                        {
                            Message =
                             $"Command not found. Use /help for the list of commands."
                        };

                        error.SetSender(Device.Player);

                        alliance.AddEntry(error);
                        break;
                }
            }
            else
            {
                var entry = new ChatStreamEntry
                {
                    Message = Message
                };

                entry.SetSender(Device.Player);

                alliance.AddEntry(entry);
            }
        }
    }
}
