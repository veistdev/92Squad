namespace Supercell.Laser.Logic.Message.Battle
{
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Quest;

    public class BattleEnd3vs3Message : GameMessage
    {
        public int TrophiesReward;
        public int TokensReward;
        public int battleResult;
        public int mathResult;
        public int mapID;
        public int players;
        public int BrawlerID;
        public int SkinID;
        public int Team;
        public string Name;
        
        // Trophies
        public int BrawlerTrophies;
        public int BrawlerHighestTrophies;

        // Exp
        public int Exp;

        public override void Encode()
        {
            Random rnd = new Random();

            Stream.WriteVInt(0); // game mode
            Stream.WriteVInt(mathResult);

            Stream.WriteVInt(TokensReward); // tokens reward
            Stream.WriteVInt(TrophiesReward); // trophies reward
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

            Stream.WriteBoolean(false); // Star Token
            Stream.WriteBoolean(false); // no experience
            Stream.WriteBoolean(false); // no tokens left
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(true); // is PvP
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);

            Stream.WriteVInt(-1);
            Stream.WriteBoolean(false);

            Stream.WriteVInt(6);
            for (int i = 0; i < 1; i++)
            {
                Stream.WriteBoolean(true); // is own player
                Stream.WriteBoolean(false); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, 16000000 + BrawlerID);
                Stream.WriteVInt(0);
                //ByteStreamHelper.WriteDataReference(Stream, 29000000 + SkinID); // skin

                Stream.WriteVInt(BrawlerTrophies); // trophies
                Stream.WriteVInt(BrawlerHighestTrophies);
                Stream.WriteVInt(1); // power level
                bool isOwn = true;
                Stream.WriteBoolean(isOwn);
                if (isOwn)
                {
                    Stream.WriteInt(0);
                    Stream.WriteInt(1);
                }

                Stream.WriteString(Name);
                Stream.WriteVInt(100);
                Stream.WriteVInt(28000000);
                Stream.WriteVInt(43000000);

                // bot 1
                Stream.WriteBoolean(false); // is own player
                Stream.WriteBoolean(false); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, 16000000 + 21);
                Stream.WriteVInt(0); // skin

                Stream.WriteVInt(0); // trophies
                Stream.WriteVInt(0);
                Stream.WriteVInt(1); // power level
                Stream.WriteBoolean(false);

                Stream.WriteString("t.me/HamsteBrawl_OG");
                Stream.WriteVInt(100);
                Stream.WriteVInt(28000000);
                Stream.WriteVInt(43000000);

                // bot 2
                Stream.WriteBoolean(false); // is own player
                Stream.WriteBoolean(false); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, 16000000 + 21);
                ByteStreamHelper.WriteDataReference(Stream, 29000000 + 117); // skin

                Stream.WriteVInt(0); // trophies
                Stream.WriteVInt(0);
                Stream.WriteVInt(1); // power level
                Stream.WriteBoolean(false);

                Stream.WriteString("t.me/HamsteBrawl_OG");
                Stream.WriteVInt(100);
                Stream.WriteVInt(28000000);
                Stream.WriteVInt(43000000);

                // bot 3
                Stream.WriteBoolean(false); // is own player
                Stream.WriteBoolean(true); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, 16000000 + rnd.Next(0,36));
                Stream.WriteVInt(0); // skin

                Stream.WriteVInt(0); // trophies
                Stream.WriteVInt(0);
                Stream.WriteVInt(1); // power level
                Stream.WriteBoolean(false);

                Stream.WriteString("t.me/HamsteBrawl_OG");
                Stream.WriteVInt(100);
                Stream.WriteVInt(28000000);
                Stream.WriteVInt(43000000);

                // bot 4
                Stream.WriteBoolean(false); // is own player
                Stream.WriteBoolean(true); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, 16000000 + rnd.Next(0,36));
                Stream.WriteVInt(0); // skin

                Stream.WriteVInt(0); // trophies
                Stream.WriteVInt(0);
                Stream.WriteVInt(1); // power level
                Stream.WriteBoolean(false);

                Stream.WriteString("t.me/HamsteBrawl_OG");
                Stream.WriteVInt(100);
                Stream.WriteVInt(28000000);
                Stream.WriteVInt(43000000);

                // bot 5
                Stream.WriteBoolean(false); // is own player
                Stream.WriteBoolean(true); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, 16000000 + rnd.Next(0,36));
                Stream.WriteVInt(0); // skin

                Stream.WriteVInt(0); // trophies
                Stream.WriteVInt(0);
                Stream.WriteVInt(1); // power level
                Stream.WriteBoolean(false);

                Stream.WriteString("t.me/HamsteBrawl_OG");
                Stream.WriteVInt(100);
                Stream.WriteVInt(28000000);
                Stream.WriteVInt(43000000);
            }

            Stream.WriteVInt(0);

            Stream.WriteVInt(1);
            for (int i = 0; i < 1; i++)
            {
                Stream.WriteVInt(39);
                Stream.WriteVInt(20);
            }

            Stream.WriteVInt(2);
            {
                Stream.WriteVInt(1);
                Stream.WriteVInt(BrawlerTrophies); // Trophies
                Stream.WriteVInt(BrawlerHighestTrophies); // Highest Trophies

                Stream.WriteVInt(5);
                Stream.WriteVInt(Exp);
                Stream.WriteVInt(Exp);
            }

            ByteStreamHelper.WriteDataReference(Stream, 28000000);

            Stream.WriteBoolean(false);

            Stream.WriteVInt(0);

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
