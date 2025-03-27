namespace Supercell.Laser.Logic.Message.Battle
{
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Quest;

    public class BattleEndMessage : GameMessage
    {
        public BattleEndMessage() : base()
        {
            ProgressiveQuests = new List<Quest>();
        }

        public int Result;
        public int TokensReward;
        public int ExpReward;
        public int TrophiesReward;
        public List<BattlePlayer> Players;
        public List<Quest> ProgressiveQuests;
        public BattlePlayer OwnPlayer;
        public bool StarToken;

        public int GameMode;
        public bool IsPvP;
        public int Counts;

        public override void Encode()
        {
            Stream.WriteVInt(GameMode);
            Stream.WriteVInt(Result);

            Stream.WriteVInt(TokensReward);
            Stream.WriteVInt(TrophiesReward);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);

            Stream.WriteBoolean(StarToken);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(IsPvP);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);

            Stream.WriteVInt(-1);
            Stream.WriteBoolean(false);

            Counts = Players.Count;
            Stream.WriteVInt(Counts);
            foreach (BattlePlayer player in Players)
            {
                Stream.WriteBoolean(player.AccountId == OwnPlayer.AccountId);
                Stream.WriteBoolean(player.TeamIndex != OwnPlayer.TeamIndex);
                Stream.WriteBoolean(false);

                ByteStreamHelper.WriteDataReference(Stream, player.CharacterId);
                Stream.WriteVInt(player.IsBot() ? 0 : 29);
                if (!player.IsBot())
                {
                    Stream.WriteVInt(player.SkinId);
                }

                Stream.WriteVInt(player.Trophies);
                Stream.WriteVInt(0);
                Stream.WriteVInt(player.HeroPowerLevel + 1);
                bool isOwn = player.AccountId == OwnPlayer.AccountId;
                Stream.WriteBoolean(isOwn);
                if (isOwn)
                {
                    Stream.WriteLong(player.AccountId);
                }

                player.DisplayData.Encode(Stream);
            }

            Stream.WriteVInt(2);
            Stream.WriteVInt(0);
            Stream.WriteVInt(ExpReward);
            Stream.WriteVInt(8);
            Stream.WriteVInt(0);

            Stream.WriteVInt(1);
            Stream.WriteVInt(39);
            Stream.WriteVInt(20);

            Stream.WriteVInt(2);
            Stream.WriteVInt(1);
            Stream.WriteVInt(OwnPlayer.Trophies);
            Stream.WriteVInt(OwnPlayer.HighestTrophies);
            Stream.WriteVInt(5);
            Stream.WriteVInt(100);
            Stream.WriteVInt(100);

            ByteStreamHelper.WriteDataReference(Stream, 28000000);
            Stream.WriteBoolean(false);

            if (Stream.WriteBoolean(ProgressiveQuests.Count > 0))
            {
                Stream.WriteVInt(ProgressiveQuests.Count);
                foreach (Quest quest in ProgressiveQuests)
                {
                    quest.Encode(Stream);
                }
            }
        }

        public override int GetMessageType()
        {
            return 23456;
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}