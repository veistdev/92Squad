﻿namespace Supercell.Laser.Server.Message
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Avatar.Structures;
    using Supercell.Laser.Logic.Club;
    using Supercell.Laser.Logic.Command.Avatar;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Friends;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Logic.Message.Account;
    using Supercell.Laser.Logic.Message.Club;
    using Supercell.Laser.Logic.Message.Friends;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Logic.Message.Ranking;
    using Supercell.Laser.Logic.Message.Security;
    using Supercell.Laser.Logic.Message.Team;
    using Supercell.Laser.Logic.Message.Udp;
    using Supercell.Laser.Logic.Stream.Entry;
    using Supercell.Laser.Logic.Team;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Logic.Game;
    using Supercell.Laser.Server.Networking;
    using Supercell.Laser.Server.Networking.Session;
    using Supercell.Laser.Server.Settings;
    using Supercell.Laser.Server.Logic;
    using System.Diagnostics;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Logic.Battle;
    using Supercell.Laser.Logic.Message.Battle;
    using Supercell.Laser.Server.Networking.UDP.Game;
    using Supercell.Laser.Server.Networking.Security;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Team.Stream;
    using Supercell.Laser.Logic.Message.Team.Stream;
    using Supercell.Laser.Logic.Message;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using MySql.Data.MySqlClient.Memcached;
    using Org.BouncyCastle.Asn1.X509;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Home.Items;

    public class MessageManager
    {
        public Connection Connection { get; }

        public HomeMode HomeMode;

        private DateTime LastKeepAlive;

        public List<Hero> Heroes;

        public MessageManager(Connection connection)
        {
            Connection = connection;
            LastKeepAlive = DateTime.UtcNow;
        }

        public bool IsAlive()
        {
            return (int)(DateTime.UtcNow - LastKeepAlive).TotalSeconds < 30;
        }

        public void ReceiveMessage(GameMessage message)
        {
            Heroes = new List<Hero>();
            switch (message.GetMessageType())
            {
                case 10100:
                    ClientHelloReceived((ClientHelloMessage)message);
                    break;
                case 10101:
                    LoginReceived((AuthenticationMessage)message);
                    break;
                case 10107:
                    ClientCapabilitesReceived((ClientCapabilitiesMessage)message);
                    break;
                case 10108:
                    LastKeepAlive = DateTime.UtcNow;
                    LobbyInfoMessage LobbyInfo = new LobbyInfoMessage();

                    LobbyInfo.PlayersOnline = Sessions.Count;
                    LobbyInfo.Version = "v27.269.1";

                    Connection.Send(new KeepAliveServerMessage());
                    //Connection.Send(LobbyInfo);
                    break;
                case 10110:
                    break;
                case 10212:
                    ChangeName((ChangeAvatarNameMessage)message);
                    break;
                case 10177:
                    ClientInfoReceived((ClientInfoMessage)message);
                    break;
                case 10501:
                    AcceptFriendReceived((AcceptFriendMessage)message);
                    break;
                case 10502:
                    AddFriendReceived((AddFriendMessage)message);
                    break;
                case 10504:
                    AskForFriendListReceived((AskForFriendListMessage)message);
                    break;
                case 10506:
                    RemoveFriendReceived((RemoveFriendMessage)message);
                    break;
                case 10555:
                    break;
                case 14101:
                    GoHomeReceived((GoHomeMessage)message);
                    break;
                case 14102:
                    EndClientTurnReceived((EndClientTurnMessage)message);
                    break;
                case 14103:
                    MatchmakeRequestReceived((MatchmakeRequestMessage)message);
                    break;
                case 14104:
                    StartSpectateReceived((StartSpectateMessage)message);
                    break;
                case 14106:
                    CancelMatchMaking((CancelMatchmakingMessage)message);
                    break;
                case 14107:
                    StopSpectateReceived((StopSpectateMessage)message);
                    break;
                case 14109:
                    GoHomeFromOfflinePractiseReceived((GoHomeFromOfflinePractiseMessage)message);
                    break;
                case 14110:
                    AskForBattleEndReceived((AskForBattleEndMessage)message);
                    break;
                case 14113:
                    GetPlayerProfile((GetPlayerProfileMessage)message);
                    break;
                case 14166:
                    break;
                case 14301:
                    CreateAllianceReceived((CreateAllianceMessage)message);
                    break;
                case 14302:
                    AskForAllianceDataReceived((AskForAllianceDataMessage)message);
                    break;
                case 14303:
                    AskForJoinableAllianceListReceived((AskForJoinableAllianceListMessage)message);
                    break;
                case 14305:
                    JoinAllianceReceived((JoinAllianceMessage)message);
                    break;
                case 14307:
                    KickAllianceMemberReceived((KickAllianceMemberMessage)message);
                    break;
                case 14308:
                    LeaveAllianceReceived((LeaveAllianceMessage)message);
                    break;
                case 14315:
                    ChatToAllianceStreamReceived((ChatToAllianceStreamMessage)message);
                    break;
                case 14316:
                    ChangeAllianceSettingsReceived((ChangeAllianceSettingsMessage)message);
                    break;
                case 14350:
                    TeamCreateReceived((TeamCreateMessage)message);
                    break;
                case 14353:
                    TeamLeaveReceived((TeamLeaveMessage)message);
                    break;
                case 14354:
                    TeamChangeMemberSettingsReceived((TeamChangeMemberSettingsMessage)message);
                    break;
                case 14355:
                    TeamSetMemberReadyReceived((TeamSetMemberReadyMessage)message);
                    break;
                case 14359:
                    TeamChatReceived((TeamChatMessage)message);
                    break;
                case 14361:
                    TeamMemberStatusReceived((TeamMemberStatusMessage)message);
                    break;
                case 14362:
                    TeamSetEventReceived((TeamSetEventMessage)message);
                    break;
                case 14363:
                    TeamSetLocationReceived((TeamSetLocationMessage)message);
                    break;
                case 14365:
                    TeamInviteReceived((TeamInviteMessage)message);
                    break;
                case 14366:
                    PlayerStatusReceived((PlayerStatusMessage)message);
                    break;
                case 14369:
                    TeamPremadeChatReceived((TeamPremadeChatMessage)message);
                    break;
                case 14403:
                    GetLeaderboardReceived((GetLeaderboardMessage)message);
                    break;
                case 14479:
                    TeamInvitationResponseReceived((TeamInvitationResponseMessage)message);
                    break;
                case 14600:
                    AvatarNameCheckRequestReceived((AvatarNameCheckRequestMessage)message);
                    break;

                default:
                    Logger.Print($"MessageManager::ReceiveMessage - no case for {message.GetType().Name} ({message.GetMessageType()})");
                    break;
            }
        }

        private void TeamPremadeChatReceived(TeamPremadeChatMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            QuickChatStreamEntry entry = new QuickChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.TargetId = message.TargetId;
            entry.Name = HomeMode.Avatar.Name;

            if (message.TargetId > 0)
            {
                TeamMember member = team.GetMember(message.TargetId);
                member.Namecolor = HomeMode.Home.Namecolor;
                if (member != null)
                {
                    entry.TargetPlayerName = member.DisplayData.Name;
                }
            }

            entry.MessageDataId = message.MessageDataId;
            entry.Unknown1 = message.Unknown1;
            entry.Unknown2 = message.Unknown2;

            team.AddStreamEntry(entry);
        }

        private void TeamChatReceived(TeamChatMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            ChatStreamEntry entry = new ChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.Name = HomeMode.Avatar.Name;
            entry.Message = message.Message;

            team.AddStreamEntry(entry);
        }

        private void AvatarNameCheckRequestReceived(AvatarNameCheckRequestMessage message)
        {
            LogicChangeAvatarNameCommand command = new LogicChangeAvatarNameCommand();
            command.Name = message.Name;
            command.ChangeNameCost = 0;
            command.Execute(HomeMode);
            AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
            serverCommandMessage.Command = command;
            Connection.Send(serverCommandMessage);
        }

        private void TeamSetEventReceived(TeamSetEventMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;
            if (message.EventSlot == 2) return;

            EventData data = Events.GetEvent(message.EventSlot);
            if (data == null) return;

            team.EventSlot = message.EventSlot;
            team.LocationId = data.LocationId;
            team.TeamUpdated();
        }

        private BattleMode SpectatedBattle;
        private void StopSpectateReceived(StopSpectateMessage message)
        {
            if (SpectatedBattle != null)
            {
                SpectatedBattle.RemoveSpectator(Connection.UdpSessionId);
                SpectatedBattle = null;
            }

            if (Connection.Home != null && Connection.Avatar != null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void StartSpectateReceived(StartSpectateMessage message)
        {
            Account data = Accounts.Load(message.AccountId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;
            long battleId = avatar.BattleId;

            BattleMode battle = Battles.Get(battleId);
            if (battle == null) return;

            SpectatedBattle = battle;
            UDPSocket socket = UDPGateway.CreateSocket();
            socket.Battle = battle;
            socket.IsSpectator = true;
            socket.TCPConnection = Connection;
            Connection.UdpSessionId = socket.SessionId;
            battle.AddSpectator(socket.SessionId, new UDPGameListener(socket, Connection));

            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.LocationId = battle.Location.GetGlobalId();
            startLoading.TeamIndex = 0;
            startLoading.OwnIndex = 0;
            startLoading.GameMode = battle.GetGameModeVariation() == 6 ? 6 : 1;
            startLoading.Players.AddRange(battle.GetPlayers());
            startLoading.SpectateMode = 1;

            Connection.Send(startLoading);

            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerAddress = Configuration.Instance.UdpHost;
            info.ServerPort = Configuration.Instance.UdpPort;
            Connection.Send(info);
        }

        private void GoHomeFromOfflinePractiseReceived(GoHomeFromOfflinePractiseMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                if (Connection.Avatar.IsTutorialState())
                {
                    Connection.Avatar.SkipTutorial();
                }

                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void AskForBattleEndReceived(AskForBattleEndMessage message)
        {
            Heroes = new List<Hero>();
            BattleEnd3vs3Message battleEnd3vs3 = new BattleEnd3vs3Message();
            BattleEndSoloMessage battleEndSolo = new BattleEndSoloMessage();
            OwnHomeDataMessage ohd = new OwnHomeDataMessage();
            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);

            if (hero.CharacterId == 16000000 + message.BrawlerID)
            {
                battleEnd3vs3.BrawlerTrophies = hero.Trophies;
                battleEnd3vs3.BrawlerHighestTrophies = hero.HighestTrophies;

                battleEndSolo.BrawlerTrophies = hero.Trophies;
                battleEndSolo.BrawlerHighestTrophies = hero.HighestTrophies;
            }

            ohd.Home = Connection.Home;
            ohd.Avatar = Connection.Avatar;

            //message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
            
            if (message.players == 6)
            {
                battleEnd3vs3.battleResult = message.battleResult;
                battleEnd3vs3.mathResult = message.mathResult;

                battleEnd3vs3.BrawlerID = message.BrawlerID;
                battleEnd3vs3.SkinID = message.SkinID;

                battleEnd3vs3.Name = ohd.Avatar.Name;
                battleEnd3vs3.Exp = ohd.Home.Exp;
            }
            if (message.players == 10)
            {
                battleEndSolo.battleResult = message.battleResult;
                battleEndSolo.mathResult = message.mathResult;

                battleEndSolo.BrawlerID = message.BrawlerID;
                battleEndSolo.SkinID = message.SkinID;

                battleEndSolo.Name = ohd.Avatar.Name;
                battleEndSolo.Exp = ohd.Home.Exp;
            }

            if (message.players == 6)
            {
                if (battleEnd3vs3.BrawlerTrophies >= 0 && battleEnd3vs3.BrawlerTrophies <= 49)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Home.Exp += 8;
                        ohd.Avatar.TrioWins += 1;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 0;
                        ohd.Home.Exp += 4;
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 50 && battleEnd3vs3.BrawlerTrophies <= 99)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Home.Exp += 8;
                        ohd.Avatar.TrioWins += 1;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -1;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 100 && battleEnd3vs3.BrawlerTrophies <= 199)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -2;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 200 && battleEnd3vs3.BrawlerTrophies <= 299)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -3;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 300 && battleEnd3vs3.BrawlerTrophies <= 399)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -4;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 400 && battleEnd3vs3.BrawlerTrophies <= 499)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -5;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 500 && battleEnd3vs3.BrawlerTrophies <= 599)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -6;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 600 && battleEnd3vs3.BrawlerTrophies <= 699)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -7;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 700 && battleEnd3vs3.BrawlerTrophies <= 799)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 8;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -8;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 800 && battleEnd3vs3.BrawlerTrophies <= 899)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 7;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -9;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 900 && battleEnd3vs3.BrawlerTrophies <= 999)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 6;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -10;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 1000 && battleEnd3vs3.BrawlerTrophies <= 1099)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 5;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -11;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
                if (battleEnd3vs3.BrawlerTrophies >= 1100 && battleEnd3vs3.BrawlerTrophies <= 1200)
                {
                    if (message.battleResult == 0)
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= 3;
                        battleEnd3vs3.TokensReward = 35;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
                        ohd.Avatar.TrioWins += 1;
                        ohd.Home.Exp += 8;
                    }
                    else
                    {
                        battleEnd3vs3.TrophiesReward = 0; //= -12;
                        ohd.Home.Exp += 4;
                        hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                    }
                }
            }
            if (message.players == 10)
            {
                if (battleEndSolo.BrawlerTrophies >= 0 && battleEndSolo.BrawlerTrophies <= 49)
                {
                    if (message.battleResult == 1)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 10;
                        battleEndSolo.TokensReward = 34;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Avatar.SoloWins += 1;
                        ohd.Home.Exp += 15;
                    }
                    if (message.battleResult == 2)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 8;
                        battleEndSolo.TokensReward = 28;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 12;
                    }
                    if (message.battleResult == 3)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 7;
                        battleEndSolo.TokensReward = 22;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 9;
                    }
                    if (message.battleResult == 4)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 6;
                        battleEndSolo.TokensReward = 16;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 6;
                    }
                    if (message.battleResult == 5)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 4;
                        battleEndSolo.TokensReward = 12;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 5;
                    }
                    if (message.battleResult == 6)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 2;
                        battleEndSolo.TokensReward = 8;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 4;
                    }
                    if (message.battleResult == 7)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 2;
                        battleEndSolo.TokensReward = 6;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 3;
                    }
                    if (message.battleResult == 8)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 1;
                        battleEndSolo.TokensReward = 4;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 2;
                    }
                    if (message.battleResult == 9)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 0;
                        battleEndSolo.TokensReward = 2;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 1;
                    }
                    if (message.battleResult == 10)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 0;
                        battleEndSolo.TokensReward = 0;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                    }
                }
                if (battleEndSolo.BrawlerTrophies >= 50 && battleEndSolo.BrawlerTrophies <= 99)
                {
                    if (message.battleResult == 1)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 10;
                        battleEndSolo.TokensReward = 34;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Avatar.SoloWins += 1;
                        ohd.Home.Exp += 15;
                    }
                    if (message.battleResult == 2)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 8;
                        battleEndSolo.TokensReward = 28;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 12;
                    }
                    if (message.battleResult == 3)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 7;
                        battleEndSolo.TokensReward = 22;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 9;
                    }
                    if (message.battleResult == 4)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 6;
                        battleEndSolo.TokensReward = 16;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 6;
                    }
                    if (message.battleResult == 5)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 3;
                        battleEndSolo.TokensReward = 12;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 5;
                    }
                    if (message.battleResult == 6)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 2;
                        battleEndSolo.TokensReward = 8;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 4;
                    }
                    if (message.battleResult == 7)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 2;
                        battleEndSolo.TokensReward = 6;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 3;
                    }
                    if (message.battleResult == 8)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 0;
                        battleEndSolo.TokensReward = 4;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 2;
                    }
                    if (message.battleResult == 9)
                    {
                        battleEndSolo.TrophiesReward = 0; //= -1;
                        battleEndSolo.TokensReward = 2;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 1;
                    }
                    if (message.battleResult == 10)
                    {
                        battleEndSolo.TrophiesReward = 0; //= -2;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                    }
                }
                if (battleEndSolo.BrawlerTrophies >= 100 && battleEndSolo.BrawlerTrophies <= 199)
                {
                    if (message.battleResult == 1)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 10;
                        battleEndSolo.TokensReward = 34;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Avatar.SoloWins += 1;
                        ohd.Home.Exp += 15;
                    }
                    if (message.battleResult == 2)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 8;
                        battleEndSolo.TokensReward = 28;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 12;
                    }
                    if (message.battleResult == 3)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 7;
                        battleEndSolo.TokensReward = 22;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 9;
                    }
                    if (message.battleResult == 4)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 6;
                        battleEndSolo.TokensReward = 16;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 6;
                    }
                    if (message.battleResult == 5)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 3;
                        battleEndSolo.TokensReward = 12;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 5;
                    }
                    if (message.battleResult == 6)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 1;
                        battleEndSolo.TokensReward = 8;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 4;
                    }
                    if (message.battleResult == 7)
                    {
                        battleEndSolo.TrophiesReward = 0; //= 0;
                        battleEndSolo.TokensReward = 6;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 3;
                    }
                    if (message.battleResult == 8)
                    {
                        battleEndSolo.TrophiesReward = 0; //= -1;
                        battleEndSolo.TokensReward = 4;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 2;
                    }
                    if (message.battleResult == 9)
                    {
                        battleEndSolo.TrophiesReward = 0; //= -2;
                        battleEndSolo.TokensReward = 2;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                        ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
                        ohd.Home.Exp += 1;
                    }
                    if (message.battleResult == 10)
                    {
                        battleEndSolo.TrophiesReward = 0; //= -2;
                        hero.AddTrophies(battleEndSolo.TrophiesReward);
                    }
                }
            }
            if (ohd.Avatar.IsPremium == true && message.players == 6)
            {
                battleEnd3vs3.TrophiesReward = 0; //+= 4;
                battleEnd3vs3.TokensReward += 17;
                hero.AddTrophies(battleEnd3vs3.TrophiesReward);
                ohd.Avatar.AddTokens(battleEnd3vs3.TokensReward);
            }
            if (ohd.Avatar.IsPremium == true && message.players == 10)
            {
                battleEndSolo.TrophiesReward = 0; //+= 6;
                battleEndSolo.TokensReward += 20;
                hero.AddTrophies(battleEndSolo.TrophiesReward);
                ohd.Avatar.AddTokens(battleEndSolo.TokensReward);
            }
            if (message.players == 10)
            {
                Connection.Send(battleEndSolo);
            }
            if (message.players == 6)
            {
                Connection.Send(battleEnd3vs3);
            }
        }

        private void TeamSetLocationReceived(TeamSetLocationMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            team.LocationId = 15000000 + message.LocationId;

            team.Type = 1;
            team.TeamUpdated();
        }

        private void ChangeAllianceSettingsReceived(ChangeAllianceSettingsMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            if (HomeMode.Avatar.AllianceRole != AllianceRole.Leader) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
            {
                alliance.AllianceBadgeId = message.BadgeId;
            }
            else
            {
                alliance.AllianceBadgeId = 8000000;
            }

            alliance.Description = message.Description;
            alliance.RequiredTrophies = message.RequiredTrophies;

            Connection.Send(new AllianceResponseMessage()
            {
                ResponseType = 10
            });

            MyAllianceMessage myAlliance = new MyAllianceMessage();
            myAlliance.Role = HomeMode.Avatar.AllianceRole;
            myAlliance.OnlineMembers = alliance.OnlinePlayers;
            myAlliance.AllianceHeader = alliance.Header;
            Connection.Send(myAlliance);
        }

        private void KickAllianceMemberReceived(KickAllianceMemberMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            AllianceMember member = alliance.GetMemberById(message.AccountId);
            if (member == null) return;

            ClientAvatar avatar = Accounts.Load(message.AccountId).Avatar;

            if (HomeMode.Avatar.AllianceRole <= avatar.AllianceRole) return;

            alliance.Members.Remove(member);
            avatar.AllianceId = -1;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = avatar.AccountId;
            entry.PlayerName = avatar.Name;
            entry.Type = 4;
            entry.Event = 1; // kicked
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 70;
            Connection.Send(response);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new AllianceResponseMessage()
                {
                    ResponseType = 100
                });
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new MyAllianceMessage());
            }
        }

        private void TeamSetMemberReadyReceived(TeamSetMemberReadyMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            if (HomeMode.Home.Character.Disabled)
            {
                Connection.Send(new OutOfSyncMessage());
                return;
            }

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            member.IsReady = message.IsReady;
            member.Namecolor = HomeMode.Home.Namecolor;

            team.TeamUpdated();

            if (team.IsEveryoneReady())
            {
                Teams.StartGame(team);
            }
        }

        private void TeamChangeMemberSettingsReceived(TeamChangeMemberSettingsMessage message)
        {
            ;
        }

        private void TeamMemberStatusReceived(TeamMemberStatusMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            member.State = message.Status;
            member.Namecolor = HomeMode.Home.Namecolor;
            team.TeamUpdated();
        }

        private void TeamInvitationResponseReceived(TeamInvitationResponseMessage message)
        {
            bool isAccept = message.Response == 1;

            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null) return;

            TeamInviteEntry invite = team.GetInviteById(HomeMode.Avatar.AccountId);
            if (invite == null) return;

            team.Invites.Remove(invite);

            if (isAccept)
            {
                TeamMember member = new TeamMember();
                member.AccountId = HomeMode.Avatar.AccountId;
                member.CharacterId = HomeMode.Home.CharacterId;
                member.Namecolor = HomeMode.Home.Namecolor;
                member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Avatar.Name);

                Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
                member.HeroTrophies = hero.Trophies;
                member.HeroHighestTrophies = hero.HighestTrophies;
                member.HeroLevel = hero.PowerLevel;
                member.IsOwner = false;
                member.State = 0;
                team.Members.Add(member);

                HomeMode.Avatar.TeamId = team.Id;
            }

            team.TeamUpdated();
        }

        private TeamEntry GetTeam()
        {
            return Teams.Get(HomeMode.Avatar.TeamId);
        }

        private void TeamInviteReceived(TeamInviteMessage message)
        {
            TeamEntry team = GetTeam();
            if (team == null) return;

            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            TeamInviteEntry entry = new TeamInviteEntry();
            entry.Slot = message.Team;
            entry.Name = data.Avatar.Name;
            entry.Id = message.AvatarId;
            entry.InviterId = HomeMode.Avatar.AccountId;

            team.Invites.Add(entry);

            team.TeamUpdated();

            LogicGameListener gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);
            if (gameListener != null)
            {
                TeamInvitationMessage teamInvitationMessage = new TeamInvitationMessage();
                teamInvitationMessage.TeamId = team.Id;

                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Avatar.Name);
                friendEntry.Trophies = HomeMode.Avatar.Trophies;
                teamInvitationMessage.Unknown = 1;
                teamInvitationMessage.FriendEntry = friendEntry;

                gameListener.SendTCPMessage(teamInvitationMessage);
            }
        }

        private void TeamLeaveReceived(TeamLeaveMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);

            if (team == null)
            {
                Logger.Print("TeamLeave - Team is NULL!");
                HomeMode.Avatar.TeamId = -1;
                Connection.Send(new TeamLeftMessage());
                return;
            }

            TeamMember entry = team.GetMember(HomeMode.Avatar.AccountId);

            if (entry == null) return;
            HomeMode.Avatar.TeamId = -1;

            team.Members.Remove(entry);

            Connection.Send(new TeamLeftMessage());
            team.TeamUpdated();

            if (team.Members.Count == 0)
            {
                Teams.Remove(team.Id);
            }
        }

        private void TeamCreateReceived(TeamCreateMessage message)
        {
            TeamEntry team = Teams.Create();

            team.Type = message.TeamType;
            team.LocationId = Events.GetEvents()[0].LocationId;

            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.Namecolor = HomeMode.Home.Namecolor;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Avatar.Name);

            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.IsOwner = true;
            member.State = 0;
            team.Members.Add(member);

            TeamMessage teamMessage = new TeamMessage();
            teamMessage.Team = team;
            HomeMode.Avatar.TeamId = team.Id;
            Connection.Send(teamMessage);
        }

        private void AcceptFriendReceived(AcceptFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            {
                Friend entry = HomeMode.Avatar.GetRequestFriendById(message.AvatarId);
                if (entry == null) return;

                Friend oldFriend = HomeMode.Avatar.GetAcceptedFriendById(message.AvatarId);
                if (oldFriend != null)
                {
                    HomeMode.Avatar.Friends.Remove(entry);
                    Connection.Send(new OutOfSyncMessage());
                    return;
                }

                entry.FriendReason = 0;
                entry.FriendState = 4;

                FriendListUpdateMessage update = new FriendListUpdateMessage();
                update.Entry = entry;
                Connection.Send(update);
            }

            {
                ClientAvatar avatar = data.Avatar;
                Friend entry = avatar.GetFriendById(HomeMode.Avatar.AccountId);
                if (entry == null) return;

                entry.FriendState = 4;
                entry.FriendReason = 0;

                if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
                {
                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = entry;
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update);
                }
            }
        }

        private void RemoveFriendReceived(RemoveFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;

            Friend MyEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (MyEntry == null) return;

            MyEntry.FriendState = 0;

            HomeMode.Avatar.Friends.Remove(MyEntry);

            FriendListUpdateMessage update = new FriendListUpdateMessage();
            update.Entry = MyEntry;
            Connection.Send(update);

            Friend OtherEntry = avatar.GetFriendById(HomeMode.Avatar.AccountId);

            if (OtherEntry == null) return;

            OtherEntry.FriendState = 0;

            avatar.Friends.Remove(OtherEntry);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = OtherEntry;
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update2);
            }
        }

        private void AddFriendReceived(AddFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;

            Friend requestEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (requestEntry != null)
            {
                AcceptFriendReceived(new AcceptFriendMessage()
                {
                    AvatarId = message.AvatarId
                });
                return;
            }
            else
            {
                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Avatar.Name);
                friendEntry.FriendReason = message.Reason;
                friendEntry.FriendState = 3;
                if (message.Reason >= 0 && message.ReasonDetails >= 0 || HomeMode.Avatar.Name == "Brawler" || HomeMode.Avatar.Trophies == 0)
                {
                    avatar.Friends.Add(friendEntry);   
                }
                else
                {
                    avatar.Friends.Add(friendEntry);  
                }

                Friend request = new Friend();
                request.AccountId = avatar.AccountId;
                request.DisplayData = new PlayerDisplayData(data.Home.ThumbnailId, data.Avatar.Name);
                request.FriendReason = 0;
                request.FriendState = 2;
                if (message.Reason >= 0 && message.ReasonDetails >= 0 || HomeMode.Avatar.Name == "Brawler" || HomeMode.Avatar.Trophies == 0)
                {
                    HomeMode.Avatar.Friends.Add(request);   
                }
                else
                {
                    HomeMode.Avatar.Friends.Add(request);  
                }

                if (LogicServerListener.Instance.IsPlayerOnline(message.AvatarId))
                {
                    var gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);

                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = friendEntry;

                    gameListener.SendTCPMessage(update);
                }

                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = request;
                Connection.Send(update2);
            }
        }

        private void AskForFriendListReceived(AskForFriendListMessage message)
        {
            FriendListMessage friendList = new FriendListMessage();
            friendList.Friends = HomeMode.Avatar.Friends.ToArray();
            Connection.Send(friendList);
        }

        private void PlayerStatusReceived(PlayerStatusMessage message)
        {
            if (HomeMode == null) return;

            HomeMode.Avatar.PlayerStatus = message.Status;

            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
        }

        private void SendMyAllianceData(Alliance alliance)
        {
            MyAllianceMessage myAlliance = new MyAllianceMessage();
            myAlliance.Role = HomeMode.Avatar.AllianceRole;
            myAlliance.OnlineMembers = alliance.OnlinePlayers;
            myAlliance.AllianceHeader = alliance.Header;
            Connection.Send(myAlliance);

            AllianceStreamMessage stream = new AllianceStreamMessage();
            stream.Entries = alliance.Stream.GetEntries();
            Connection.Send(stream);
        }

        private int BotIdCounter;

        private void ChatToAllianceStreamReceived(ChatToAllianceStreamMessage message)
        {
            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            if (message.Message.StartsWith("/"))
            {
                string[] cmd = message.Message.Substring(1).Split(' ');
                if (cmd.Length == 0) return;

                AllianceStreamEntryMessage response = new AllianceStreamEntryMessage();
                response.Entry = new AllianceStreamEntry();
                response.Entry.AuthorName = "Debugger";
                response.Entry.AuthorId = 1;
                response.Entry.Id = alliance.Stream.EntryIdCounter + 667 + BotIdCounter++;
                response.Entry.AuthorRole = AllianceRole.Member;
                response.Entry.Type = 2;

                long accountId = HomeMode.Avatar.AccountId;

                switch (cmd[0])
                {
                    case "status":
                        long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
                        response.Entry.Message = $"Server Status:\nServer Version: v{Program.SERVER_VERSION} (for v27.269) ({Program.BUILD_TYPE})\nPlayers Online: {Sessions.Count}\n" +
                            $"Cached accounts: {AccountCache.Count}\nCached alliances: {AllianceCache.Count}\nCached teams: {Teams.Count}\n" +
                            $"Your id: {accountId.GetHigherInt()}-{accountId.GetLowerInt()} ({LogicLongCodeGenerator.ToCode(accountId)})\n" +
                            $"Memory Used: {megabytesUsed}MB";
                        Connection.Send(response);
                        break;
                    case "sex":
                        long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
                        response.Entry.Message = $"У тебя писька ещё не выросла хыхыхыхахахалох";
                        Connection.Send(response);
                        break;
                     case "beer":
                        Notification beerNotification = new Notification
                        {
                Id = 83,
                PrimaryMessageEntry = "",
                SecondaryMessageEntry = "",
                ButtonMessageEntry = "Перейти",
                FileLocation = "ware_banner.png",
                FileSha = "38bf5339f07cd48b2d68c797c80994e732f50f18",
                ExtLint = "brawlstars://extlink?page=https%3A%2F%2Ft.me/steel_brawl",
                        };
                        HomeMode.Home.NotificationFactory.Add(beerNotification);
                        LogicAddNotificationCommand addBeerNotificationCommand = new()
                        {
                            Notification = beerNotification
                        };
                        AvailableServerCommandMessage addBeerServerCommandMessage = new AvailableServerCommandMessage
                        {
                            Command = addBeerNotificationCommand
                        };
                        if (Sessions.IsSessionActive(HomeMode.Avatar.AccountId))
                        {
                            var session = Sessions.GetSession(HomeMode.Avatar.AccountId);
                            session.GameListener.SendTCPMessage(addBeerServerCommandMessage);
                        }
                        response.Entry.Message = $"Выдано ыыыы";
                        Connection.Send(response);
                        break; 
                    case "help":
                        response.Entry.Message = $"Список команд:\n/help - показывает это сообщение\n/status - посмотреть статус сервера\n/beer - выпить пиво\n/sex - секс"; // /usecode [code] - use bonus code
                        Connection.Send(response);
                        break;
                    case "gems":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[3] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[3] + "</c>";
                                }
                                Notification nGems = new Notification
                                {
                                    Id = 89,
                                    DonationCount = int.Parse(cmd[2]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                LogicAddNotificationCommand acmGems = new()
                                {
                                    Notification = nGems
                                };
                                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }
                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    case "addbrawler":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[3] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[3] + "</c>";
                                }
                                Notification nGems = new Notification
                                {
                                    Id = 93,
                                    BrawlerID = int.Parse(cmd[2]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                LogicAddNotificationCommand acmGems = new()
                                {
                                    Notification = nGems
                                };
                                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }
                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    case "addbrawlerbundle":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[5] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[5] + "</c>";
                                }
                                Notification nGems = new Notification
                                {
                                    Id = 93,
                                    BrawlerID = int.Parse(cmd[2]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                LogicAddNotificationCommand acmGems = new()
                                {
                                    Notification = nGems
                                };
                                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }



                                nGems = new Notification
                                {
                                    Id = 92,
                                    BrawlerID = int.Parse(cmd[2]),
                                    DonationCount = int.Parse(cmd[3]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                acmGems = new()
                                {
                                    Notification = nGems
                                };
                                asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }

                                nGems = new Notification
                                {
                                    Id = 89,
                                    DonationCount = int.Parse(cmd[4]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                acmGems = new()
                                {
                                    Notification = nGems
                                };
                                asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }


                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    case "addbrawlerpp":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[4] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[4] + "</c>";
                                }
                                Notification nGems = new Notification
                                {
                                    Id = 92,
                                    BrawlerID = int.Parse(cmd[2]),
                                    DonationCount = int.Parse(cmd[3]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                LogicAddNotificationCommand acmGems = new()
                                {
                                    Notification = nGems
                                };
                                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }
                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    case "addemote":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[3] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[3] + "</c>";
                                }
                                Notification nGems = new Notification
                                {
                                    Id = 72,
                                    BrawlerID = (52000000 + int.Parse(cmd[2])),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                LogicAddNotificationCommand acmGems = new()
                                {
                                    Notification = nGems
                                };
                                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }
                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    case "gold":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[3] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[3] + "</c>";
                                }
                                Notification nGems = new Notification
                                {
                                    Id = 90,
                                    DonationCount = int.Parse(cmd[2]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                LogicAddNotificationCommand acmGems = new()
                                {
                                    Notification = nGems
                                };
                                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }
                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    case "skin":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[3] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[3] + "</c>";
                                }
                                Notification nGems = new Notification
                                {
                                    Id = 90,
                                    DonationCount = 1,
                                    SkinID = 29000000 + int.Parse(cmd[2]),
                                    MessageEntry = gemsMessage
                                };
                                targetAccountGems.Home.NotificationFactory.Add(nGems);
                                LogicAddNotificationCommand acmGems = new()
                                {
                                    Notification = nGems
                                };
                                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                asmGems.Command = acmGems;
                                if (Sessions.IsSessionActive(qwidGems))
                                {
                                    var sessionGems = Sessions.GetSession(qwidGems);
                                    sessionGems.GameListener.SendTCPMessage(asmGems);
                                }
                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    case "battlepass":
                        if (HomeMode.Avatar.IsDev){
                            long id = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account = Accounts.Load(id);
                            if (account != null)
                            {
                                //account.Avatar.Diamonds = account.Avatar.Diamonds + int.Parse(cmd[2]);
                                long qwidGems = LogicLongCodeGenerator.ToId(cmd[1]);
                                Account targetAccountGems = Accounts.Load(qwidGems);
                                if (targetAccountGems == null)
                                {
                                    Console.WriteLine("Fail: account not found!");
                                    return;
                                }

                                string gemsMessage;

                                if (cmd[2] == "1"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                    Notification nGems = new Notification
                                    {
                                        Id = 73,
                                        MessageEntry = gemsMessage
                                    };
                                    targetAccountGems.Home.NotificationFactory.Add(nGems);
                                    LogicAddNotificationCommand acmGems = new()
                                    {
                                        Notification = nGems
                                    };
                                    AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                    asmGems.Command = acmGems;
                                    if (Sessions.IsSessionActive(qwidGems))
                                    {
                                        var sessionGems = Sessions.GetSession(qwidGems);
                                        sessionGems.GameListener.SendTCPMessage(asmGems);
                                    }
                                } 
                                if (cmd[2] == "2"){
                                    gemsMessage = $"<c6>Cпасибо за поддержку сервера<3</c>";
                                    Notification nGems = new Notification
                                    {
                                        Id = 73,
                                        MessageEntry = gemsMessage
                                    };
                                    targetAccountGems.Home.NotificationFactory.Add(nGems);
                                    LogicAddNotificationCommand acmGems = new()
                                    {
                                        Notification = nGems
                                    };
                                    AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                                    asmGems.Command = acmGems;
                                    if (Sessions.IsSessionActive(qwidGems))
                                    {
                                        var sessionGems = Sessions.GetSession(qwidGems);
                                        sessionGems.GameListener.SendTCPMessage(asmGems);
                                    }

                                    nGems = new Notification
                                    {
                                        Id = 90,
                                        DonationCount = 1000,
                                        MessageEntry = gemsMessage
                                    };
                                    targetAccountGems.Home.NotificationFactory.Add(nGems);
                                    acmGems = new()
                                    {
                                        Notification = nGems
                                    };
                                    asmGems = new AvailableServerCommandMessage();
                                    asmGems.Command = acmGems;
                                    if (Sessions.IsSessionActive(qwidGems))
                                    {
                                        var sessionGems = Sessions.GetSession(qwidGems);
                                        sessionGems.GameListener.SendTCPMessage(asmGems);
                                    }

                                    nGems = new Notification
                                    {
                                        Id = 89,
                                        DonationCount = 100,
                                        MessageEntry = gemsMessage
                                    };
                                    targetAccountGems.Home.NotificationFactory.Add(nGems);
                                    acmGems = new()
                                    {
                                        Notification = nGems
                                    };
                                    asmGems = new AvailableServerCommandMessage();
                                    asmGems.Command = acmGems;
                                    if (Sessions.IsSessionActive(qwidGems))
                                    {
                                        var sessionGems = Sessions.GetSession(qwidGems);
                                        sessionGems.GameListener.SendTCPMessage(asmGems);
                                    }

                                    nGems = new Notification
                                    {
                                        Id = 93,
                                        BrawlerID = 11,
                                        MessageEntry = gemsMessage
                                    };
                                    targetAccountGems.Home.NotificationFactory.Add(nGems);
                                    acmGems = new()
                                    {
                                        Notification = nGems
                                    };
                                    asmGems = new AvailableServerCommandMessage();
                                    asmGems.Command = acmGems;
                                    if (Sessions.IsSessionActive(qwidGems))
                                    {
                                        var sessionGems = Sessions.GetSession(qwidGems);
                                        sessionGems.GameListener.SendTCPMessage(asmGems);
                                    }
                                } 
                                else {
                                    gemsMessage = $"<c6>" + cmd[2] + "</c>";
                                }
                                break;
                            }
                            else {
                                response.Entry.Message = "Тег хуйня"; // /usecode [code] - use bonus code
                                Connection.Send(response);
                            }
                        }
                        else{
                            response.Entry.Message = "Иди нахуй!"; // /usecode [code] - use bonus code
                            Connection.Send(response);
                        }
                        break;
                    default:
                        response.Entry.Message = $"Unknown command \"{cmd[0]}\" - type \"/help\" to get command list!";
                        Connection.Send(response);
                        break;
                }

                return;
            }

            alliance.SendChatMessage(HomeMode.Avatar.AccountId, message.Message);
        }

        private void JoinAllianceReceived(JoinAllianceMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (HomeMode.Avatar.AllianceId > 0) return;
            if (alliance == null) return;
            if (alliance.Members.Count >= 100) return;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = HomeMode.Avatar.AccountId;
            entry.PlayerName = HomeMode.Avatar.Name;
            entry.Type = 4;
            entry.Event = 3;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            HomeMode.Avatar.AllianceRole = AllianceRole.Member;
            HomeMode.Avatar.AllianceId = alliance.Id;
            alliance.Members.Add(new AllianceMember(HomeMode.Avatar));

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 40;
            Connection.Send(response);

            SendMyAllianceData(alliance);
        }

        private void LeaveAllianceReceived(LeaveAllianceMessage message)
        {
            if (HomeMode.Avatar.AllianceId < 0 || HomeMode.Avatar.AllianceRole == AllianceRole.None) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            alliance.RemoveMemberById(HomeMode.Avatar.AccountId);
            HomeMode.Avatar.AllianceId = -1;
            HomeMode.Avatar.AllianceRole = AllianceRole.None;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = HomeMode.Avatar.AccountId;
            entry.PlayerName = HomeMode.Avatar.Name;
            entry.Type = 4;
            entry.Event = 4;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 80;
            Connection.Send(response);

            MyAllianceMessage myAlliance = new MyAllianceMessage();
            Connection.Send(myAlliance);
        }

        private void CreateAllianceReceived(CreateAllianceMessage message)
        {
            if (HomeMode.Avatar.AllianceId >= 0) return;

            Alliance alliance = new Alliance();
            alliance.Name = message.Name;
            alliance.Description = message.Description;
            alliance.RequiredTrophies = message.RequiredTrophies;

            if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
            {
                alliance.AllianceBadgeId = message.BadgeId;
            }
            else
            {
                alliance.AllianceBadgeId = 8000000;
            }

            HomeMode.Avatar.AllianceRole = AllianceRole.Leader;
            alliance.Members.Add(new AllianceMember(HomeMode.Avatar));

            Alliances.Create(alliance);

            HomeMode.Avatar.AllianceId = alliance.Id;

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 20;
            Connection.Send(response);

            SendMyAllianceData(alliance);
        }

        private void AskForAllianceDataReceived(AskForAllianceDataMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (alliance == null) return;

            AllianceDataMessage data = new AllianceDataMessage();
            data.Alliance = alliance;
            data.IsMyAlliance = message.AllianceId == HomeMode.Avatar.AllianceId;
            Connection.Send(data);
        }

        private void AskForJoinableAllianceListReceived(AskForJoinableAllianceListMessage message)
        {
            JoinableAllianceListMessage list = new JoinableAllianceListMessage();
            List<Alliance> alliances = Alliances.GetRandomAlliances(10);
            foreach (Alliance alliance in alliances)
            {
                list.JoinableAlliances.Add(alliance.Header);
            }
            Connection.Send(list);
        }

        private void ClientCapabilitesReceived(ClientCapabilitiesMessage message)
        {
            Connection.PingUpdated(message.Ping);
            LobbyInfoMessage lobbyInfo = new LobbyInfoMessage();
            lobbyInfo.Ping = message.Ping;
            lobbyInfo.PlayersOnline = Sessions.Count;
            Connection.Send(lobbyInfo);
        }

        private void GetLeaderboardReceived(GetLeaderboardMessage message)
        {
            if (message.LeaderboardType == 1)
            {
                Account[] rankingList = Leaderboards.GetAvatarRankingList();

                LeaderboardMessage leaderboard = new LeaderboardMessage();
                leaderboard.LeaderboardType = 1;
                leaderboard.Region = message.IsRegional ? "RU" : null;
                foreach (Account data in rankingList)
                {
                    leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                }
                leaderboard.OwnAvatarId = Connection.Avatar.AccountId;

                Connection.Send(leaderboard);
            }
            else if (message.LeaderboardType == 2)
            {
                Alliance[] rankingList = Leaderboards.GetAllianceRankingList();

                LeaderboardMessage leaderboard = new LeaderboardMessage();
                leaderboard.LeaderboardType = 2;
                leaderboard.Region = message.IsRegional ? "RU" : null;
                leaderboard.AllianceList.AddRange(rankingList);

                Connection.Send(leaderboard);
            }

            if (message.LeaderboardType == 0)
            {
                Account[] rankingList = Leaderboards.GetAvatarRankingList();
                Hero hero = HomeMode.Avatar.GetHero(message.BrawlerID);

                LeaderboardMessage leaderboard = new LeaderboardMessage();
                leaderboard.LeaderboardType = 0;
                leaderboard.Region = message.IsRegional ? "RU" : null;
                leaderboard.BrawlerTrophies = hero.Trophies;
                leaderboard.BrawlerID = message.BrawlerID;
                foreach (Account data in rankingList)
                {
                    leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                }
                leaderboard.OwnAvatarId = Connection.Avatar.AccountId;

                Connection.Send(leaderboard);
            }
        }

        private void GoHomeReceived(GoHomeMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void ClientInfoReceived(ClientInfoMessage message)
        {
            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerAddress = Configuration.Instance.UdpHost;
            info.ServerPort = Configuration.Instance.UdpPort;
            Connection.Send(info);
        }

        private void CancelMatchMaking(CancelMatchmakingMessage message)
        {
            Matchmaking.CancelMatchmake(Connection);
            Connection.Send(new MatchMakingCancelledMessage());
        }

        private void MatchmakeRequestReceived(MatchmakeRequestMessage message)
        {
            int slot = message.EventSlot;
            ClientHelloMessage hello = new ClientHelloMessage();

            if (HomeMode.Home.Character.Disabled || hello.KeyVersion == 14 && hello.MajorVersion == 27)
            {
                Connection.Send(new OutOfSyncMessage());
                return;
            }

            if (!Events.HasSlot(slot))
            {
                slot = 1;
            }

            if (HomeMode.Home.Character.Disabled || hello.KeyVersion == 14 && hello.MajorVersion == 27)
            {
                Connection.Send(new OutOfSyncMessage());
                return;
            }
            else
            {
                Matchmaking.RequestMatchmake(Connection, slot);
            }
        }

        private void EndClientTurnReceived(EndClientTurnMessage message)
        {
            HomeMode.ClientTurnReceived(message.Tick, message.Checksum, message.Commands);
        }

        private void GetPlayerProfile(GetPlayerProfileMessage message)
        {
            Account data = Accounts.Load(message.AccountId);
            if (data == null) return;

            Profile profile = Profile.Create(data.Home, data.Avatar);

            PlayerProfileMessage profileMessage = new PlayerProfileMessage();
            profileMessage.Profile = profile;
            if (data.Avatar.AllianceId >= 0)
            {
                Alliance alliance = Alliances.Load(data.Avatar.AllianceId);
                if (alliance != null)
                {
                    profileMessage.AllianceHeader = alliance.Header;
                    profileMessage.AllianceRole = data.Avatar.AllianceRole;
                }
            }
            Connection.Send(profileMessage);
        }

        private void ChangeName(ChangeAvatarNameMessage message)
        {
            OwnHomeDataMessage ohd = new OwnHomeDataMessage();
            string name = message.Name;
            if (name.StartsWith("t.me") || name.StartsWith("Тг") || name.StartsWith("ТГ") || name.StartsWith("тГ") || name.StartsWith("TG") || name.StartsWith("Tg") || name.StartsWith("tG") || name.StartsWith("tg") || name.StartsWith("тг"))
            {
                ohd.Avatar.Banned = true;
                ohd.Avatar.ResetTrophies();
                ohd.Avatar.Name = "Brawler";
                if (Sessions.IsSessionActive(ohd.Avatar.AccountId))
                {
                    var session = Sessions.GetSession(ohd.Avatar.AccountId);
                    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                    {
                        Message = "Your account banned!"
                    });
                    Sessions.Remove(ohd.Avatar.AccountId);
                }
            }
            else if (message.Name.Length >= 2 && message.Name.Length <= 16)
            {
                LogicChangeAvatarNameCommand command = new LogicChangeAvatarNameCommand();
                command.Name = message.Name;
                command.ChangeNameCost = 0;
                command.Execute(HomeMode);
                AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                serverCommandMessage.Command = command;
                Connection.Send(serverCommandMessage);
            }
        }

        private void OnChangeCharacter(int characterId)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            Hero hero = HomeMode.Avatar.GetHero(characterId);
            if (hero == null) return;
            member.CharacterId = characterId;
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.Namecolor = HomeMode.Home.Namecolor;

            team.TeamUpdated();
        }

        private void LoginReceived(AuthenticationMessage message)
        {
            Account account = null;

            if (message.AccountId == 0)
            {
                account = Accounts.Create();
            }
            else
            {
                account = Accounts.Load(message.AccountId);
                if (account.PassToken != message.PassToken)
                {
                    account = null;
                }
            }

            if (account == null)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 1;
                loginFailed.Message = "Your account not found.\nTry to clear app data.";
                Connection.Send(loginFailed);

                return;
            }

            if (account.Avatar.Banned)
            {
                var session = Sessions.GetSession(message.AccountId);
                Sessions.Remove(message.AccountId);
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 11;
                Connection.Send(loginFailed);
                return;
            }

            if (message.ClientBuild == 275)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 1;
                loginFailed.Message = "Your Version is not supported\nPls download new";
                loginFailed.UpdateUrl = "https://t.me/alahservers/136";
                Connection.Send(loginFailed);
                return;
            }

            if (message.OsVersion == null)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 11;
                account.Avatar.Banned = true;
                account.Avatar.ResetTrophies();
                account.Avatar.Name = "Brawler";
                
                Connection.Send(loginFailed);
                return;
            }

            if (account.Avatar.Name == "Bot" && account.Avatar.Name == "Точно не бот")
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 11;
                Connection.Send(loginFailed);
                return;
            }

            AuthenticationOkMessage loginOk = new AuthenticationOkMessage();
            loginOk.AccountId = account.AccountId;
            loginOk.PassToken = account.PassToken;
            loginOk.ServerEnvironment = "prod";

            Connection.Send(loginOk);

            HomeMode = HomeMode.LoadHomeState(new HomeGameListener(Connection), account.Home, account.Avatar, Events.GetEvents());
            HomeMode.CharacterChanged += OnChangeCharacter;

            BattleMode battle = null;
            if (HomeMode.Avatar.BattleId > 0)
            {
                battle = Battles.Get(HomeMode.Avatar.BattleId);
            }

            if (battle == null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = HomeMode.Home;
                ohd.Avatar = HomeMode.Avatar;
                Connection.Send(ohd);
            }
            else
            {
                StartLoadingMessage startLoading = new StartLoadingMessage();
                startLoading.LocationId = battle.Location.GetGlobalId();
                startLoading.TeamIndex = HomeMode.Avatar.TeamIndex;
                startLoading.OwnIndex = HomeMode.Avatar.OwnIndex;
                startLoading.GameMode = battle.GetGameModeVariation() == 6 ? 6 : 1;
                startLoading.Players.AddRange(battle.GetPlayers());
                UDPSocket socket = UDPGateway.CreateSocket();
                socket.TCPConnection = Connection;
                socket.Battle = battle;
                Connection.UdpSessionId = socket.SessionId;
                battle.ChangePlayerSessionId(HomeMode.Avatar.UdpSessionId, socket.SessionId);
                HomeMode.Avatar.UdpSessionId = socket.SessionId;
                Connection.Send(startLoading);
            }

            Connection.Avatar.LastOnline = DateTime.UtcNow;

            Sessions.Create(HomeMode, Connection);

            FriendListMessage friendList = new FriendListMessage();
            friendList.Friends = HomeMode.Avatar.Friends.ToArray();
            Connection.Send(friendList);

            if (HomeMode.Avatar.AllianceRole != AllianceRole.None && HomeMode.Avatar.AllianceId > 0)
            {
                Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);

                if (alliance != null)
                {
                    SendMyAllianceData(alliance);
                    AllianceDataMessage data = new AllianceDataMessage();
                    data.Alliance = alliance;
                    data.IsMyAlliance = true;
                    Connection.Send(data);
                }
            }

            foreach (Friend entry in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(entry.AccountId))
                {
                    FriendOnlineStatusEntryMessage statusEntryMessage = new FriendOnlineStatusEntryMessage();
                    statusEntryMessage.AvatarId = entry.AccountId;
                    statusEntryMessage.PlayerStatus = entry.Avatar.PlayerStatus;
                    Connection.Send(statusEntryMessage);
                }
            }

            if (HomeMode.Avatar.TeamId > 0)
            {
                TeamMessage teamMessage = new TeamMessage();
                teamMessage.Team = Teams.Get(HomeMode.Avatar.TeamId);
                if (teamMessage.Team != null)
                {
                    Connection.Send(teamMessage);
                    TeamMember member = teamMessage.Team.GetMember(HomeMode.Avatar.AccountId);
                    member.State = 0;
                    member.Namecolor = HomeMode.Home.Namecolor;
                    teamMessage.Team.TeamUpdated();
                }
            }
        }

        private void ClientHelloReceived(ClientHelloMessage message)
        {

            Connection.Messaging.Seed = message.ClientSeed;

            ServerHelloMessage hello = new ServerHelloMessage();
            hello.SetServerHelloToken(Connection.Messaging.SessionToken);
            Connection.Send(hello);
        }
    }
}
