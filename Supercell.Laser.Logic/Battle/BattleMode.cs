﻿ using System.Threading.Tasks;

namespace Supercell.Laser.Logic.Battle
{
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Battle.Component;
    using Supercell.Laser.Logic.Battle.Objects;
    using Supercell.Laser.Logic.Battle.Input;
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Battle.Level.Factory;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Logic.Message.Battle;
    using Supercell.Laser.Logic.Time;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Debug;
    using Supercell.Laser.Titan.Math;
    using System;
    using System.Threading;
    using Supercell.Laser.Logic.Message.Home;


    public struct DiedEntry
    {
        public int DeathTick;
        public BattlePlayer Player;
    }

    public class BattleMode
    {
        public const int ORBS_TO_COLLECT_NORMAL = 0xA;

        private Timer m_updateTimer;

        private int m_locationId;
        private int m_gameModeVariation;
        private int m_playersCountWithGameModeVariation;

        private Queue<ClientInput> m_inputQueue;

        private List<BattlePlayer> m_players;
        private Dictionary<long, BattlePlayer> m_playersBySessionId;
        private Dictionary<int, BattlePlayer> m_playersByObjectGlobalId;
        private Dictionary<long, LogicGameListener> m_spectators;
        private GameObjectManager m_gameObjectManager;

        private Rect m_playArea;
        private TileMap m_tileMap;
        private GameTime m_time;
        private LogicRandom m_random;
        private int m_randomSeed;

        private int m_winnerTeam;

        private int isBugStop;

        private Queue<DiedEntry> m_deadPlayers;
        private int m_playersAlive;

        private int m_gemGrabCountdown;

        public bool IsGameOver { get; private set; }

        public BattleMode(int locationId)
        {
            m_winnerTeam = -1;

            m_locationId = locationId;
            m_gameModeVariation = GameModeUtil.GetGameModeVariation(Location.GameMode);
            m_playersCountWithGameModeVariation = GamePlayUtil.GetPlayerCountWithGameModeVariation(m_gameModeVariation);

            m_inputQueue = new Queue<ClientInput>();

            m_randomSeed = 0;
            m_random = new LogicRandom(m_randomSeed);

            m_players = new List<BattlePlayer>();
            m_playersBySessionId = new Dictionary<long, BattlePlayer>();
            m_playersByObjectGlobalId = new Dictionary<int, BattlePlayer>();
            m_deadPlayers = new Queue<DiedEntry>();

            m_time = new GameTime();
            m_tileMap = TileMapFactory.CreateTileMap(Location.AllowedMaps);
            m_playArea = new Rect(0, 0, m_tileMap.LogicWidth, m_tileMap.LogicHeight);
            m_gameObjectManager = new GameObjectManager(this);

            m_spectators = new Dictionary<long, LogicGameListener>();
        }

        public int GetGemGrabCountdown()
        {
            return m_gemGrabCountdown;
        }

        public int GetPlayersAliveCountForBattleRoyale()
        {
            return m_playersAlive;
        }

        private void TickSpawnHeroes()
        {
            DiedEntry[] entries = m_deadPlayers.ToArray();
            m_deadPlayers.Clear();

            foreach (DiedEntry entry in entries)
            {
                if (GetTicksGone() - entry.DeathTick < GameModeUtil.GetRespawnSeconds(m_gameModeVariation) * 20)
                {
                    m_deadPlayers.Enqueue(entry);
                    continue;
                }

                BattlePlayer player = entry.Player;
                LogicVector2 spawnPoint = player.GetSpawnPoint();

                Character character = new Character(16, GlobalId.GetInstanceId(player.CharacterId));
                character.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                character.SetHeroLevel(player.HeroPowerLevel);
                character.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                character.SetBot(player.IsBot());
                character.SetImmunity(60, 100);
                m_gameObjectManager.AddGameObject(character);
                player.OwnObjectId = character.GetGlobalID();
                m_playersByObjectGlobalId.Add(player.OwnObjectId, player);
            }
        }

        public void PlayerDied(BattlePlayer player)
        {
            if (m_gameModeVariation == 6)
            {
                int rank = m_playersAlive;
                m_playersAlive--;
                player.IsAlive = false;
                player.BattleRoyaleRank = rank;

                BattleEndMessage message = new BattleEndMessage();
                message.GameMode = 2;
                message.IsPvP = true;
                message.Players = new List<BattlePlayer>();
                message.Players.Add(player);
                message.OwnPlayer = player;

                if (player.Avatar == null) return;
                player.Avatar.BattleId = -1;

                if (player.GameListener == null) return;

                Hero hero = player.Avatar.GetHero(player.CharacterId);

                message.Result = rank;
                int tokensReward = 40 / rank;
                message.TokensReward = tokensReward;
                Console.WriteLine("[BattleMode] Result " + message.Result);
                bool isWin = false;

                if (rank == 1 || rank == 2 || rank == 3 || rank == 4){
                    isWin = true;
                }

                if (message.OwnPlayer.Trophies >= 0 && message.OwnPlayer.Trophies <= 49)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 50 && message.OwnPlayer.Trophies <= 99)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }
                if (message.OwnPlayer.Trophies >= 100 && message.OwnPlayer.Trophies <= 199)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 200 && message.OwnPlayer.Trophies <= 299)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 300 && message.OwnPlayer.Trophies <= 399)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 400 && message.OwnPlayer.Trophies <= 499)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 500 && message.OwnPlayer.Trophies <= 599)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 600 && message.OwnPlayer.Trophies <= 699)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -7;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 700 && message.OwnPlayer.Trophies <= 799)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 10;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -9;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 800 && message.OwnPlayer.Trophies <= 899)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 9;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 7;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 2;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -4;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -7;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -9;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -10;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 900 && message.OwnPlayer.Trophies <= 999)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 8;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -3;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -10;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -11;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 1000 && message.OwnPlayer.Trophies <= 1099)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 6;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -5;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -9;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -11;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -12;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 1100 && message.OwnPlayer.Trophies <= 1199)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 4;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 1;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -7;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -10;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -12;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -13;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                if (message.OwnPlayer.Trophies >= 1200)
                {
                    if (message.Result == 1)
                    {
                        message.TrophiesReward = 5;
                        message.TokensReward = 35;
                        
                        player.Home.Exp += 15;
                        player.Avatar.SoloWins += 1;
                    }
                    if (message.Result == 2)
                    {
                        message.TrophiesReward = 3;
                        message.TokensReward = 28;
                        
                        player.Home.Exp += 12;
                    }
                    if (message.Result == 3)
                    {
                        message.TrophiesReward = 0;
                        message.TokensReward = 22;
                        
                        player.Home.Exp += 9;
                    }
                    if (message.Result == 4)
                    {
                        message.TrophiesReward = -1;
                        message.TokensReward = 16;
                        
                        player.Home.Exp += 6;
                    }
                    if (message.Result == 5)
                    {
                        message.TrophiesReward = -2;
                        message.TokensReward = 12;
                        
                        player.Home.Exp += 5;
                    }
                    if (message.Result == 6)
                    {
                        message.TrophiesReward = -6;
                        message.TokensReward = 8;
                        
                        player.Home.Exp += 4;
                    }
                    if (message.Result == 7)
                    {
                        message.TrophiesReward = -8;
                        message.TokensReward = 6;
                        
                        player.Home.Exp += 3;
                    }
                    if (message.Result == 8)
                    {
                        message.TrophiesReward = -11;
                        message.TokensReward = 4;
                        
                        player.Home.Exp += 2;
                    }
                    if (message.Result == 9)
                    {
                        message.TrophiesReward = -12;
                        message.TokensReward = 2;
                        
                        player.Home.Exp += 1;
                    }
                    if (message.Result == 10)
                    {
                        message.TrophiesReward = -13;
                        message.TokensReward = 0;
                        
                        player.Home.Exp += 0;
                    }
                }

                player.Avatar.AddTokens(message.TokensReward);

                try
                {
                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                    message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                }
                catch{
                    Console.WriteLine("Yeah");
                }

                hero.AddTrophies(message.TrophiesReward);
                

                player.GameListener.SendTCPMessage(message);

                return;
            }

            if (m_gameModeVariation == 0)
            {
                player.ResetScore();
            }

            player.OwnObjectId = 0;

            DiedEntry entry = new DiedEntry();
            entry.Player = player;
            entry.DeathTick = GetTicksGone();

            m_deadPlayers.Enqueue(entry);
        }

        public BattlePlayer GetPlayerWithObject(int globalId)
        {
            if (m_playersByObjectGlobalId.ContainsKey(globalId))
            {
                return m_playersByObjectGlobalId[globalId];
            }
            return null;
        }

        public TileMap GetTileMap()
        {
            return m_tileMap;
        }

        public void Start()
        {
            m_updateTimer = new Timer(new TimerCallback(Update), null, 0, 1000 / 20);
        }

        public void Update(object stateInfo)
        {
            this.ExecuteOneTick();
        }

        public BattlePlayer GetPlayer(int globalId)
        {
            if (m_playersByObjectGlobalId.ContainsKey(globalId))
            {
                return m_playersByObjectGlobalId[globalId];
            }
            return null;
        }

        public void AddSpectator(long sessionId, LogicGameListener gameListener)
        {
            m_spectators.Add(sessionId, gameListener);
        }

        public void ChangePlayerSessionId(long old, long newId)
        {
            if (m_playersBySessionId.ContainsKey(old))
            {
                BattlePlayer player = m_playersBySessionId[old];
                player.LastHandledInput = 0;
                m_playersBySessionId.Remove(old);
                m_playersBySessionId.Add(newId, player);
            }
        }

        public BattlePlayer GetPlayerBySessionId(long sessionId)
        {
            if (m_playersBySessionId.ContainsKey(sessionId))
            {
                return m_playersBySessionId[sessionId];
            }
            return null;
        }

        public void AddPlayer(BattlePlayer player, long sessionId)
        {
            if (Debugger.DoAssert(player != null, "LogicBattle::AddPlayer - player is NULL!"))
            {
                player.SessionId = sessionId;
                m_players.Add(player);
                if (sessionId > 0)
                {
                    m_playersBySessionId.Add(sessionId, player);
                }
                if (player.Avatar != null)
                {
                    player.Avatar.BattleId = Id;
                    player.Avatar.TeamIndex = player.TeamIndex;
                    player.Avatar.OwnIndex = player.PlayerIndex;
                }
            }
        }

        public void AddGameObjects()
        {
            m_playersAlive = m_players.Count;

            int team1Indexer = 0;
            int team2Indexer = 0;

            foreach (BattlePlayer player in m_players)
            {
                Character character = new Character(16, GlobalId.GetInstanceId(player.CharacterId));
                character.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                character.SetHeroLevel(player.HeroPowerLevel);
                character.SetBot(player.IsBot());
                character.SetImmunity(60, 100);

                if (GameModeUtil.HasTwoTeams(m_gameModeVariation))
                {
                    if (player.TeamIndex == 0)
                    {
                        Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++];
                        character.SetPosition(tile.X, tile.Y, 0);
                        player.SetSpawnPoint(tile.X, tile.Y);
                    }
                    else
                    {
                        Tile tile = m_tileMap.SpawnPointsTeam2[team2Indexer++];
                        character.SetPosition(tile.X, tile.Y, 0);
                        player.SetSpawnPoint(tile.X, tile.Y);
                    }
                }
                else
                {
                    Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++];
                    character.SetPosition(tile.X, tile.Y, 0);
                    player.SetSpawnPoint(tile.X, tile.Y);
                }

                m_gameObjectManager.AddGameObject(character);
                player.OwnObjectId = character.GetGlobalID();
                m_playersByObjectGlobalId.Add(player.OwnObjectId, player);
            }

            if (m_gameModeVariation == 0)
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("OrbSpawner");
                Item item = new Item(18, data.GetInstanceId());
                item.SetPosition(3000, 5000, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }

            if (m_gameModeVariation == 3)
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("Money");
                Item item = new Item(18, data.GetInstanceId());
                item.SetPosition(2950, 4950, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            } 

            if (m_gameModeVariation == 6)
            {
                CharacterData data = DataTables.Get(16).GetData<CharacterData>("LootBox");
                for (int i = 0; i < m_tileMap.Height; i++)
                {
                    for (int j = 0; j < m_tileMap.Width; j++)
                    {
                        Tile tile = m_tileMap.GetTile(i, j, true);
                        if (tile.Code == '4')
                        {
                            bool shouldSpawnBox = GetRandomInt(0, 120) < 60;

                            if (shouldSpawnBox)
                            {
                                Character box = new Character(16, data.GetInstanceId());
                                box.SetPosition(tile.X, tile.Y, 0);
                                box.SetIndex(10 * 16);
                                m_gameObjectManager.AddGameObject(box);
                            }
                        }
                    }
                }
            }

            int instanceId = DataTables.Get(16).GetInstanceId("LaserBall");
            Character gem = new Character(16, instanceId);
            gem.SetPosition(2950 + 450, 4950 - 333, 0);
          //  m_gameObjectManager.AddGameObject(gem);
        }

        public void RemoveSpectator(long id)
        {
            if (m_spectators.ContainsKey(id))
            {
                m_spectators.Remove(id);
            }
        }

        public bool IsInPlayArea(int x, int y)
        {
            return m_playArea.IsInside(x, y);
        }

        public int GetTeamPlayersCount(int teamIndex)
        {
            int result = 0;
            foreach (BattlePlayer player in GetPlayers())
            {
                if (player.TeamIndex == teamIndex) result++;
            }
            return result;
        }

        public void AddClientInput(ClientInput input, long sessionId)
        {
            if (!m_playersBySessionId.ContainsKey(sessionId)) return;

            input.OwnerSessionId = sessionId;
            m_inputQueue.Enqueue(input);
        }

        public void HandleSpectatorInput(ClientInput input, long sessionId)
        {
            if (input == null) return;

            if (!m_spectators.ContainsKey(sessionId)) return;
            m_spectators[sessionId].HandledInputs = input.Index;
        }

        private void HandleClientInput(ClientInput input)
        {
            if (input == null) return;

            BattlePlayer player = GetPlayerBySessionId(input.OwnerSessionId);
            try{
                Character scharacter = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                if (scharacter.m_stun != null) return;
            }
            catch{;}

            if (player == null) return;
            if (player.LastHandledInput >= input.Index) return;

            player.LastHandledInput = input.Index;
            switch (input.Type)
            {
                case 0:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;

                        Skill skill = character.GetWeaponSkill();
                        if (skill == null) return;

                        character.UltiDisabled();

                        bool indirection = false;
                        if (skill.SkillData.Projectile != null)
                        {
                            ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);
                            indirection = projectileData.Indirect;
                        }

                        if (!input.AutoAttack && !indirection)
                        {
                            character.ActivateSkill(false, input.X, input.Y);
                        }
                        else
                        {
                            character.ActivateSkill(false, input.X - character.GetX(), input.Y - character.GetY());
                        }

                        break;
                    }
                case 1:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;

                        Skill skill = character.GetUltimateSkill();
                        if (skill == null) return;

                        if (!player.HasUlti()) return;
                        player.UseUlti();

                        character.UltiEnabled();

                        if (skill.SkillData.BehaviorType == "Shield"){
                            character.SetImmunity(skill.SkillData.ActiveTime/10, skill.SkillData.MsBetweenAttacks);
                            break;
                        }
                        if (skill.SkillData.AreaEffectObject == "SpeedyUltiArea"){
                            character.SetSpeedBoost(150, 12);
                            break;
                        }

                        bool indirection = false;
                        if (skill.SkillData.Projectile != null)
                        {
                            ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);
                            indirection = projectileData.Indirect;
                        }

                        if (!input.AutoAttack && !indirection)
                        {
                            character.ActivateSkill(true, input.X, input.Y);
                        }
                        else
                        {
                            character.ActivateSkill(true, input.X - character.GetX(), input.Y - character.GetY());
                        }

                        break;
                    }
                case 2:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;

                        character.MoveTo(input.X, input.Y);

                        break;
                    }
                case 5:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        character?.UltiEnabled();
                        break;
                    }
                case 6:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        character?.UltiDisabled();
                        break;
                    }
                default:
                    Debugger.Warning("Input is unhandled: " + input.Type);
                    break;
            }
        }

        public bool IsTileOnPoisonArea(int xTile, int yTile)
        {
            if (m_gameModeVariation != 6) return false;

            int tick = GetTicksGone();

            if (tick > 500)
            {
                int poisons = 0;
                poisons += (tick - 500) / 100;

                if (xTile <= poisons || xTile >= 59 - poisons || yTile <= poisons || yTile >= 59 - poisons)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleIncomingInputMessages()
        {
            while (m_inputQueue.Count > 0)
            {
                this.HandleClientInput(m_inputQueue.Dequeue());
            }
        }

        public void ExecuteOneTick()
        {
            try
            {
                this.HandleIncomingInputMessages();
                foreach (BattlePlayer player in GetPlayers())
                {
                    player.KillList.Clear();
                }

                if (this.CalculateIsGameOver())
                {
                    this.m_updateTimer.Dispose();
                    this.GameOver();
                    this.IsGameOver = true;
                    return;
                }

                this.m_gameObjectManager.PreTick();
                this.Tick();
                this.m_time.IncreaseTick();
                this.SendVisionUpdateToPlayers();
            } catch (Exception e)
            {
                Console.WriteLine("Battle stopped with exception! Message: " + e.Message + " Trace: " + e.StackTrace);
                m_updateTimer.Dispose();
                IsGameOver = true;
                isBugStop = 999;
                SendBattleEndToPlayers();
            }
        }

        private void TickSpawnEventStuffDelayed()
        {
            if (m_gameModeVariation == 0)
            {
                if (GetTicksGone() % 100 == 0)
                {
                    int instanceId = DataTables.Get(18).GetInstanceId("Point");
                    Item gem = new Item(18, instanceId);
                    gem.SetPosition(2950, 4950, 0);
                    gem.SetAngle(GetRandomInt(0, 360));
                    m_gameObjectManager.AddGameObject(gem);
                }
            }
            if (m_gameModeVariation == 6)
            {
                if (GetTicksGone() % 10 == 0)
                {
                    Random rand = new Random();
                    int x_spawn = rand.Next(900, 17100);
                    int y_spawn = rand.Next(900, 17100);
                    Tile tile1 = m_tileMap.GetTile(((x_spawn) / 300), ((y_spawn) / 300), true);

                    if ((tile1.Data.BlocksMovement && !tile1.IsDestructed())){
                        Console.WriteLine("NotOk");
                    }
                    else{
                        int instanceId = DataTables.Get(18).GetInstanceId("DamageAndSpeed");
                        Item gem = new Item(18, instanceId);
                        gem.SetPosition(x_spawn, y_spawn, 0);
                        gem.SetAngle(GetRandomInt(0, 360));
                        m_gameObjectManager.AddGameObject(gem);
                    }
                }
            }
        }

        public void GameOver()
        {
            SendBattleEndToPlayers();
        }

        public void SendBattleEndToPlayers()
        {
            Random rand = new Random();

            foreach (BattlePlayer player in m_players)
            {
                if (player.SessionId < 0) continue;
                if (!player.IsAlive) continue;
                if (player.BattleRoyaleRank == -1) player.BattleRoyaleRank = 1;
                if (player.Avatar == null) continue;
                int rank = player.BattleRoyaleRank;
                player.Avatar.BattleId = -1;

                bool isWin = false;

                bool isVictory = m_winnerTeam == player.TeamIndex;

                BattleEndMessage message = new BattleEndMessage();
                Hero hero = player.Avatar.GetHero(player.CharacterId);

                if (!player.IsBot())
                {
                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                    if (homeMode != null)
                    {
                        if (homeMode.Home.Quests != null)
                        {
                            message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                        }
                    }
                }

                if (m_gameModeVariation != 6)
                {
                    message.GameMode = 1;
                    message.IsPvP = true;
                    message.Players = m_players;
                    message.OwnPlayer = player;

                    if (m_winnerTeam == -1) // Draw
                    {
                        message.Result = 2;
                    }

                    if (isVictory)
                    {
                        message.Result = 0;
                    }
                    else if (m_winnerTeam != -1)
                    {
                        message.Result = 1;
                    }

                    if (isVictory){
                        isWin = true;
                    } 

                    

                    if (message.OwnPlayer.Trophies >= 0 && message.OwnPlayer.Trophies <= 49)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 50 && message.OwnPlayer.Trophies <= 99)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 100 && message.OwnPlayer.Trophies <= 199)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 200 && message.OwnPlayer.Trophies <= 299)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 300 && message.OwnPlayer.Trophies <= 399)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 400 && message.OwnPlayer.Trophies <= 499)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 500 && message.OwnPlayer.Trophies <= 599)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 600 && message.OwnPlayer.Trophies <= 699)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 700 && message.OwnPlayer.Trophies <= 799)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }
                    
                    if (message.OwnPlayer.Trophies >= 800 && message.OwnPlayer.Trophies <= 899)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 900 && message.OwnPlayer.Trophies <= 999)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -10;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 1000 && message.OwnPlayer.Trophies <= 1099)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 1100 && message.OwnPlayer.Trophies <= 1199)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                            
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    if (message.OwnPlayer.Trophies >= 1200)
                    {
                        if (message.Result == 0)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 20;
                            message.ExpReward = 8;
                            
                            
                            player.Home.Exp += 8;
                            player.Avatar.TrioWins += 1;
                        }
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 10;
                            message.ExpReward = 4;
                        
                            
                            player.Home.Exp += 4;
                            player.Avatar.TrioWins += 0;
                        }
                    }

                    player.Avatar.AddTokens(message.TokensReward);
                    hero.AddTrophies(message.TrophiesReward);
                }
                else
                {
                    message.IsPvP = true;
                    message.GameMode = 2;
                    message.Result = player.BattleRoyaleRank;
                    message.Players = new List<BattlePlayer>();
                    message.Players.Add(player);
                    message.OwnPlayer = player;
                    int tokensReward = 40 / rank;
                    message.TokensReward = tokensReward;

                    if (rank == 1 || rank == 2 || rank == 3 || rank == 4){
                        isWin = true;
                    }   

                    if (message.OwnPlayer.Trophies >= 0 && message.OwnPlayer.Trophies <= 49)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 50 && message.OwnPlayer.Trophies <= 99)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 100 && message.OwnPlayer.Trophies <= 199)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 200 && message.OwnPlayer.Trophies <= 299)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 300 && message.OwnPlayer.Trophies <= 399)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 400 && message.OwnPlayer.Trophies <= 499)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 500 && message.OwnPlayer.Trophies <= 599)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 600 && message.OwnPlayer.Trophies <= 699)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 700 && message.OwnPlayer.Trophies <= 799)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 10;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 800 && message.OwnPlayer.Trophies <= 899)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 9;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 7;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 2;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -4;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -10;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 900 && message.OwnPlayer.Trophies <= 999)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 8;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -3;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 1000 && message.OwnPlayer.Trophies <= 1099)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 6;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -5;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -9;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 1100 && message.OwnPlayer.Trophies <= 1199)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 4;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 1;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -7;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -10;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -13;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (message.OwnPlayer.Trophies >= 1200)
                    {
                        if (message.Result == 1)
                        {
                            message.TrophiesReward = 5;
                            message.TokensReward = 35;
                            player.Home.Exp += 15;
                            player.Avatar.SoloWins += 1;
                        }
                        if (message.Result == 2)
                        {
                            message.TrophiesReward = 3;
                            message.TokensReward = 28;
                            player.Home.Exp += 12;
                        }
                        if (message.Result == 3)
                        {
                            message.TrophiesReward = 0;
                            message.TokensReward = 22;
                            player.Home.Exp += 9;
                        }
                        if (message.Result == 4)
                        {
                            message.TrophiesReward = -1;
                            message.TokensReward = 16;
                            player.Home.Exp += 6;
                        }
                        if (message.Result == 5)
                        {
                            message.TrophiesReward = -2;
                            message.TokensReward = 12;
                            player.Home.Exp += 5;
                        }
                        if (message.Result == 6)
                        {
                            message.TrophiesReward = -6;
                            message.TokensReward = 8;
                            player.Home.Exp += 4;
                        }
                        if (message.Result == 7)
                        {
                            message.TrophiesReward = -8;
                            message.TokensReward = 6;
                            player.Home.Exp += 3;
                        }
                        if (message.Result == 8)
                        {
                            message.TrophiesReward = -11;
                            message.TokensReward = 4;
                            player.Home.Exp += 2;
                        }
                        if (message.Result == 9)
                        {
                            message.TrophiesReward = -12;
                            message.TokensReward = 2;
                            player.Home.Exp += 1;
                        }
                        if (message.Result == 10)
                        {
                            message.TrophiesReward = -13;
                            message.TokensReward = 0;
                            player.Home.Exp += 0;
                        }
                    }
                    if (isBugStop == 999) {message.TrophiesReward = 0;}

                    player.Avatar.AddTokens(message.TokensReward);
                    hero.AddTrophies(message.TrophiesReward);
                }
                try
                {
                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                    message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                }
                catch{
                    Console.WriteLine("Yeah");
                }

                if (player.Avatar == null) continue;
                if (player.GameListener == null) continue;
                player.GameListener.SendTCPMessage(message);
            }
        }

        public int GetTeamScore(int team)
        {
            int score = 0;
            foreach (BattlePlayer player in m_players)
            {
                if (player.TeamIndex == team) score += player.GetScore();
            }
            return score;
        }

        private bool CalculateIsGameOver()
        {
            if (m_gameModeVariation == 3)
            {
                if (GetTicksGone() >= 20 * 120 + 120)
                {
                    if (GetTeamScore(0) > GetTeamScore(1))
                    {
                        m_winnerTeam = 0;
                    }
                    else if (GetTeamScore(0) < GetTeamScore(1))
                    {
                        m_winnerTeam = 1;
                    }
                    else
                    {
                        m_winnerTeam = -1;
                    }
                    return true;
                }
            }
            else if (m_gameModeVariation == 0)
            {
                if (GetTeamScore(0) > GetTeamScore(1) && GetTeamScore(0) >= 10)
                {
                    if (m_gemGrabCountdown == 0)
                    {
                        m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                    }
                    else if (GetTicksGone() > m_gemGrabCountdown)
                    {
                        m_winnerTeam = 0;
                        return true;
                    }
                }
                else if (GetTeamScore(0) < GetTeamScore(1) && GetTeamScore(1) >= 10)
                {
                    if (m_gemGrabCountdown == 0)
                    {
                        m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                    }
                    else if (GetTicksGone() > m_gemGrabCountdown)
                    {
                        m_winnerTeam = 1;
                        return true;
                    }
                }
                else
                {
                    m_gemGrabCountdown = 0;
                }
            }
            else if (m_gameModeVariation == 6)
            {
                if (m_playersAlive <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private void Tick()
        {
            TickSpawnEventStuffDelayed();
            m_gameObjectManager.Tick();
            TickSpawnHeroes();
        }

        private void SendVisionUpdateToPlayers()
        {
            Parallel.ForEach(m_players, player =>
            {
                try
                {
                    if (player.GameListener != null)
                    {
                        BitStream visionBitStream = new BitStream(64);
                        m_gameObjectManager.Encode(visionBitStream, m_tileMap, player.OwnObjectId, player.PlayerIndex, player.TeamIndex);

                        VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                        visionUpdate.Tick = GetTicksGone();
                        visionUpdate.HandledInputs = player.LastHandledInput;
                        visionUpdate.Viewers = m_spectators.Count;
                        visionUpdate.VisionBitStream = visionBitStream;

                        player.GameListener.SendMessage(visionUpdate);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending vision update to player {player.PlayerIndex}: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Consider additional actions for handling the error:
                    // - Log to a file
                    // - Notify other parts of the game
                    // - Retry sending the update (if appropriate)
                }
            });

            BitStream spectateStream = new BitStream(64);
            m_gameObjectManager.Encode(spectateStream, m_tileMap, 0, -1, -1);

            Task.Run(() =>
            {
                foreach (LogicGameListener gameListener in m_spectators.Values.ToArray())
                {
                    try
                    {
                        VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                        visionUpdate.Tick = GetTicksGone();
                        visionUpdate.HandledInputs = gameListener.HandledInputs;
                        visionUpdate.Viewers = m_spectators.Count;
                        visionUpdate.VisionBitStream = spectateStream;

                        gameListener.SendMessage(visionUpdate);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending spectate vision update: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        // Consider additional actions for handling the error (same as above)
                    }
                }
            });
        }

        public BattlePlayer[] GetPlayers()
        {
            return m_players.ToArray();
        }

        public int GetRandomInt(int min, int max)
        {
            return m_random.Rand(max - min) + min;
        }

        public int GetRandomInt(int max)
        {
            return m_random.Rand(max);
        }

        public int GetTicksGone()
        {
            return m_time.GetTick();
        }

        public int GetGameModeVariation()
        {
            return m_gameModeVariation;
        }

        public int GetPlayersCountWithGameModeVariation()
        {
            return m_playersCountWithGameModeVariation;
        }

        public int GetRandomSeed()
        {
            return m_randomSeed;
        }

        public LocationData Location
        {
            get
            {
                return DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(m_locationId);
            }
        }

        public long Id { get; set; }
    }
}