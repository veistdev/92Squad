namespace Supercell.Laser.Logic.Command.Home
{
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Message.Account.Auth;

    public class LogicViewInboxNotificationCommand : Command
    {
        public int NotificationIndex;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            NotificationIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode h)
        {
            if (NotificationIndex < 0) return -1;
            if (NotificationIndex > h.Home.NotificationFactory.GetIndex()) return -1;
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].IsViewed) return -1;
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 79)
            {
                foreach (int a in h.Home.NotificationFactory.NotificationList[NotificationIndex].StarpointsAwarded)
                {
                    h.Avatar.StarPoints += a;
                }
            }
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 89)
            {
                LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(8);
                reward.Count = h.Home.NotificationFactory.NotificationList[NotificationIndex].DonationCount;
                unit.AddDrop(reward);
                delivery.DeliveryUnits.Add(unit);
                delivery.Execute(h);

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = delivery;
                h.GameListener.SendMessage(message);
            }
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 91)
            {
                LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(3);
                reward.Count = h.Home.NotificationFactory.NotificationList[NotificationIndex].DonationCount;
                unit.AddDrop(reward);
                delivery.DeliveryUnits.Add(unit);
                delivery.Execute(h);

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = delivery;
                h.GameListener.SendMessage(message);
            }
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 90)
            {
                LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
                if (h.Home.NotificationFactory.NotificationList[NotificationIndex].SkinID < 29000000){ 
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(7);
                    reward.Count = h.Home.NotificationFactory.NotificationList[NotificationIndex].DonationCount;
                    unit.AddDrop(reward);
                    delivery.DeliveryUnits.Add(unit);
                    delivery.Execute(h);
                }
                else{
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.Count = h.Home.NotificationFactory.NotificationList[NotificationIndex].DonationCount;
                    reward.DataGlobalId = 29000000;
                    reward.PinGlobalId = (h.Home.NotificationFactory.NotificationList[NotificationIndex].SkinID - 29000000);
                    unit.AddDrop(reward);
                    delivery.DeliveryUnits.Add(unit);
                    delivery.Execute(h);
                }

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = delivery;
                h.GameListener.SendMessage(message);
            }
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 93)
            {
                LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(1);
                reward.DataGlobalId = 16000000 + h.Home.NotificationFactory.NotificationList[NotificationIndex].BrawlerID;
                reward.Count = 1;
                unit.AddDrop(reward);
                delivery.DeliveryUnits.Add(unit);
                delivery.Execute(h);

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = delivery;
                h.GameListener.SendMessage(message);
            }
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 92)
            {
                LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(6);
                reward.DataGlobalId = 16000000 + h.Home.NotificationFactory.NotificationList[NotificationIndex].BrawlerID;
                reward.Count = h.Home.NotificationFactory.NotificationList[NotificationIndex].DonationCount;
                unit.AddDrop(reward);
                delivery.DeliveryUnits.Add(unit);
                delivery.Execute(h);

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = delivery;
                h.GameListener.SendMessage(message);
            }
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 72)
            {
                LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(8);
                h.Home.UnlockedEmotes.Add(h.Home.NotificationFactory.NotificationList[NotificationIndex].BrawlerID);
                reward.Count = 1;
                unit.AddDrop(reward);
                delivery.DeliveryUnits.Add(unit);
                delivery.Execute(h);

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = delivery;
                h.GameListener.SendMessage(message);

                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 1;
                loginFailed.Message = "Пин выдан!";
                h.GameListener.SendMessage(loginFailed);
            }
            if (h.Home.NotificationFactory.NotificationList[NotificationIndex].Id == 73)
            {
                h.Home.HasPremiumPass = true;
            }
            h.Home.NotificationFactory.NotificationList[NotificationIndex].IsViewed = true;
            return 0;
        }

        public override int GetCommandType()
        {
            return 528;
        }
    }
}
