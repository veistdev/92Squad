namespace Supercell.Laser.Logic.Command.Home
{
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Titan.DataStream;

    public class LogicSelectSkinCommand : Command
    {
        public int CharacterId;
        public int SkinId;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            SkinId = stream.ReadVInt();
            CharacterId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            List<int> CommonSkins = new List<int>();

CommonSkins.Add(0);
CommonSkins.Add(1);
CommonSkins.Add(4);
CommonSkins.Add(5);
CommonSkins.Add(7);
CommonSkins.Add(8);
CommonSkins.Add(9);
CommonSkins.Add(10);
CommonSkins.Add(11);
CommonSkins.Add(13);
CommonSkins.Add(14);
CommonSkins.Add(15);
CommonSkins.Add(17);
CommonSkins.Add(19);
CommonSkins.Add(20);
CommonSkins.Add(21);
CommonSkins.Add(22);
CommonSkins.Add(23);
CommonSkins.Add(24);
CommonSkins.Add(31);
CommonSkins.Add(32);
CommonSkins.Add(33);
CommonSkins.Add(34);
CommonSkins.Add(35);
CommonSkins.Add(36);
CommonSkins.Add(37);
CommonSkins.Add(38);
CommonSkins.Add(39);
CommonSkins.Add(40);
CommonSkins.Add(41);
CommonSkins.Add(42);
CommonSkins.Add(43);
CommonSkins.Add(52);
CommonSkins.Add(62);
CommonSkins.Add(67);
CommonSkins.Add(73);
CommonSkins.Add(76);
CommonSkins.Add(77);
CommonSkins.Add(81);
CommonSkins.Add(87);
CommonSkins.Add(88);
CommonSkins.Add(106);
CommonSkins.Add(107);
CommonSkins.Add(113);
CommonSkins.Add(114);
CommonSkins.Add(115);
CommonSkins.Add(119);
CommonSkins.Add(121);
CommonSkins.Add(127);
CommonSkins.Add(129);
CommonSkins.Add(139);
CommonSkins.Add(142);
CommonSkins.Add(150);
CommonSkins.Add(151);
CommonSkins.Add(155);
CommonSkins.Add(156);
CommonSkins.Add(157);
CommonSkins.Add(169);
CommonSkins.Add(170);
CommonSkins.Add(181);


            if (homeMode.Home.UnlockedSkins.Contains(CharacterId) || CommonSkins.Contains(CharacterId)){
                homeMode.Home.SkinId = CharacterId; 
                homeMode.Home.SkinSelectState = 1;
                return 0;
            }
            return -1;
        }

        public override int GetCommandType()
        {
            return 506;
        }
    }
}
