namespace Supercell.Laser.Server.Handler
{
    using Supercell.Laser.Logic.Notification;
    using Supercell.Laser.Logic.Message;
    using Supercell.Laser.Logic.Command;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Message.Account;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Networking.Session;
    using System.Reflection;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Server.Utils;
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
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Command;
    using Supercell.Laser.Logic.Battle.Structures;
    using Newtonsoft.Json.Linq;
    using System.Reflection;
    using System.Numerics;
    using Supercell.Laser.Logic.Home.Quest;
    using Supercell.Laser.Logic.Data.Helper;
    using System.Linq;
    using Supercell.Laser.Logic.Command.Home;
    using Ubiety.Dns.Core;
    using System.Xml.Linq;
    using Microsoft.VisualBasic;
    using Org.BouncyCastle.Cms;
    using System;

    public static class CmdHandler
    {
        public static void Start()
        {
            while (true)
            {
                try
                {
                    string cmd = Console.ReadLine();
                    if (cmd == null) continue;
                    if (!cmd.StartsWith("/")) continue;
                    cmd = cmd.Substring(1);
                    string[] args = cmd.Split(" ");
                    if (args.Length < 1) continue;
                    switch (args[0])
                    {
                        case "unban":
                            ExecuteUnbanAccount(args);
                            break;
                        case "changename":
                            ExecuteChangeNameForAccount(args);
                            break;
                        case "devtag":
                            Dev(args); 
                            break;                           
                        case "getvalue":
                            ExecuteGetFieldValue(args);
                            break;
                        case "changevalue":
                            ExecuteChangeValueForAccount(args);
                            break;
                        case "PlusTrophies":
                            PlusTrophies(args);
                            break;
                        case "unlockall":
                            ExecuteUnlockAllForAccount(args);
                            break;
                        case "skin":
                            ExecuteGiveSkin(args);
                            break;
                        case "/addgold":
                            ExecuteAddGold(args);
                            break;
                        case "addbrawler":
                            ExecuteAddBrawler(args);
                            break;
                        case "maintenance":
                            Console.WriteLine("Starting maintenance...");
                            ExecuteShutdown();
                            Console.WriteLine("Maintenance started!");
                            break;
                        case "offer": 
                            ExecuteGiveMegaGemsToAccount(args);
                            break;
                        case "addemote":
                            ExecuteAddEmote(args);
                            break;
                        case "upgradebrawler":
                            ExecuteUpgradeBrawler(args);
                            break;
                        case "addbrawlerbundle":
                            ExecuteAddBrawlerBundle(args);
                            break;
                        case "battlepass":
                            ExecuteGiveBattlePass(args);
                            break;
                        case "gems":  
                            ExecuteGiveGemsToAccount(args);
                            break; 
                        case "send":
                            ExecuteSendCommand(args);
                            break;   

                    }
                }
                catch (Exception) { }
            }
        }



private static void ExecuteSendCommand(string[] args)
    {
        try
        {
            if (args.Length < 3)
                throw new ArgumentException("Недостаточно параметров\nИспользование: /send <target> <message>\nДоступные цели:\n0- Все игроки\n#TAG - Игрок по тегу (например #2PP)\nX-Y - Диапазон ID (например 100-200)\n ID - Конкретный ID игрока");

            string target = args[1];
            string message = string.Join(" ", args.Skip(2));
            int sentCount = 0;

            switch (DetermineTargetType(target))
            {
                case TargetType.AllPlayers:
                    SendToAllPlayers(message, ref sentCount);
                    break;
                
                case TargetType.PlayerTag:
                    SendToPlayerByTag(target, message, ref sentCount);
                    break;
                
                case TargetType.Range:
                    SendToRange(target, message, ref sentCount);
                    break;
                
                case TargetType.SingleId:
                    SendToSinglePlayer(long.Parse(target), message, ref sentCount);
                    break;
            }

            Console.WriteLine($"✓ Сообщение отправлено {sentCount} игрокам");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка: {ex.Message}");
        }
    }

    private enum TargetType { AllPlayers, PlayerTag, Range, SingleId }

    private static TargetType DetermineTargetType(string target)
    {
        if (target == "0") return TargetType.AllPlayers;
        if (target.Contains("-")) return TargetType.Range;
        if (target.StartsWith("#")) return TargetType.PlayerTag;
        return TargetType.SingleId;
    }

    #region Основные методы отправки
    private static void SendToAllPlayers(string message, ref int counter)
    {
        long maxId = Accounts.GetMaxAvatarId();
        Console.WriteLine($"Отправка всем игрокам (всего {maxId} аккаунтов)...");
        SendToRange(1, maxId, message, ref counter);
    }

    private static void SendToPlayerByTag(string tag, string message, ref int counter)
    {
        if (!tag.StartsWith("#") || tag.Length != 5)
            throw new ArgumentException("Неверный формат тега! Пример: #2ABCD");

        long accountId = LogicLongCodeGenerator.ToId(tag);
        SendToSinglePlayer(accountId, message, ref counter);
    }

    private static void SendToRange(string range, string message, ref int counter)
    {
        var parts = range.Split('-');
        if (parts.Length != 2 || !long.TryParse(parts[0], out long start) || 
           !long.TryParse(parts[1], out long end))
            throw new ArgumentException("Неверный формат диапазона! Пример: 100-200");

        SendToRange(Math.Min(start, end), Math.Max(start, end), message, ref counter);
    }

    private static void SendToRange(long startId, long endId, string message, ref int counter)
    {
        Console.WriteLine($"Отправка игрокам с ID {startId} по {endId}...");
        for (long id = startId; id <= endId; id++)
            SendToSinglePlayer(id, message, ref counter);
    }

    private static void SendToSinglePlayer(long accountId, string message, ref int counter)
    {
        try
        {
            Account account = Accounts.Load(accountId);
            if (account == null || account.Home == null)
            {
                Console.WriteLine($"Игрок {accountId} не найден");
                return;
            }

            var notification = new Notification
            {
                Id = 81,
                MessageEntry = FormatMessage(message)
            };

            account.Home.NotificationFactory.Add(notification);
            SendInstantNotification(account, notification);
            
            Console.WriteLine($"→ ID {accountId}: отправлено");
            counter++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки для ID {accountId}: {ex.Message}");
        }
    }
    #endregion

    #region Вспомогательные методы
    private static string FormatMessage(string rawMessage)
    {
        return $"<c5>[СИСТЕМНОЕ СООБЩЕНИЕ]</c>\n{rawMessage}";
    }

    private static void SendInstantNotification(Account account, Notification notification)
    {
        if (!Sessions.IsSessionActive(account.Avatar.AccountId)) 
            return;

        var session = Sessions.GetSession(account.Avatar.AccountId);
        session.GameListener.SendTCPMessage(new AvailableServerCommandMessage
        {
            Command = new LogicAddNotificationCommand { Notification = notification }
        });
    }
    #endregion





private static void ExecuteGiveSkin(string[] args)
{
    if (args.Length < 4)
    {
        Console.WriteLine("Usage: /skin [TAG] [SkinID] [Message]");
        return;
    }

    bool sc = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        id = long.Parse(args[1]);
    }

    if (!int.TryParse(args[2], out int skinID))
    {
        Console.WriteLine("Error: Invalid SkinID.");
        return;
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    string skinMessage = args[3] == "1"
        ? "<c6>Спасибо за поддержку сервера<3</c>"
        : $"<c6>{args[3]}</c>";

    Notification n = new Notification
    {
        Id = 90,
        DonationCount = 1,
        SkinID = 29000000 + skinID,
        MessageEntry = skinMessage
    };

    account.Home.NotificationFactory.Add(n);

    LogicAddNotificationCommand acm = new()
    {
        Notification = n
    };
    AvailableServerCommandMessage asm = new() { Command = acm };

    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(asm);
    }

    string tag = sc ? LogicLongCodeGenerator.ToCode(id) : args[1];
    Console.WriteLine($"Done: Added Skin {skinID} to {tag}!");
}


private static void ExecuteAddBrawlerBundle(string[] args)
{
    if (args.Length < 6)
    {
        Console.WriteLine("Usage: /addbrawlerbundle [TAG] [BrawlerID] [Donation1] [Donation2] [Message]");
        return;
    }

    bool sc = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        id = long.Parse(args[1]);
    }

    if (!int.TryParse(args[2], out int brawlerID))
    {
        Console.WriteLine("Error: Invalid BrawlerID.");
        return;
    }

    if (!int.TryParse(args[3], out int donation1))
    {
        Console.WriteLine("Error: Invalid DonationCount 1.");
        return;
    }

    if (!int.TryParse(args[4], out int donation2))
    {
        Console.WriteLine("Error: Invalid DonationCount 2.");
        return;
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    string bundleMessage = args[5] == "1"
        ? "<c6>Спасибо за поддержку сервера<3</c>"
        : $"<c6>{args[5]}</c>";

    List<Notification> notifications = new List<Notification>
    {
        new Notification
        {
            Id = 93,
            BrawlerID = brawlerID,
            MessageEntry = bundleMessage
        },
        new Notification
        {
            Id = 92,
            BrawlerID = brawlerID,
            DonationCount = donation1,
            MessageEntry = bundleMessage
        },
        new Notification
        {
            Id = 89,
            DonationCount = donation2,
            MessageEntry = bundleMessage
        }
    };

    foreach (var notification in notifications)
    {
        account.Home.NotificationFactory.Add(notification);
        LogicAddNotificationCommand acm = new() { Notification = notification };
        AvailableServerCommandMessage asm = new() { Command = acm };

        if (Sessions.IsSessionActive(id))
        {
            var session = Sessions.GetSession(id);
            session.GameListener.SendTCPMessage(asm);
        }
    }

    string tag = sc ? LogicLongCodeGenerator.ToCode(id) : args[1];
    Console.WriteLine($"Done: Added brawler bundle for {tag} (Brawler {brawlerID}, Donations: {donation1}, {donation2})!");
}



private static void ExecuteUpgradeBrawler(string[] args)
{
    if (args.Length < 5)
    {
        Console.WriteLine("Usage: /upgradebrawler [TAG] [BrawlerID] [DonationCount] [Message]");
        return;
    }

    bool sc = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        id = long.Parse(args[1]);
    }

    if (!int.TryParse(args[2], out int brawlerID))
    {
        Console.WriteLine("Error: Invalid BrawlerID.");
        return;
    }

    if (!int.TryParse(args[3], out int donationCount))
    {
        Console.WriteLine("Error: Invalid DonationCount.");
        return;
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    string upgradeMessage = args[4] == "1" 
        ? "<c6>Спасибо за поддержку сервера<3</c>" 
        : $"<c6>{args[4]}</c>";

    Notification n = new Notification
    {
        Id = 92,
        BrawlerID = brawlerID,
        DonationCount = donationCount,
        MessageEntry = upgradeMessage
    };

    account.Home.NotificationFactory.Add(n);

    LogicAddNotificationCommand acm = new()
    {
        Notification = n
    };
    AvailableServerCommandMessage asm = new() { Command = acm };

    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(asm);
    }

    string tag = sc ? LogicLongCodeGenerator.ToCode(id) : args[1];
    Console.WriteLine($"Done: Upgraded Brawler {brawlerID} with {donationCount} points for {tag}!");
}


private static void ExecuteGiveBattlePass(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /battlepass [TAG] [Type]");
        return;
    }

    bool sc = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        id = long.Parse(args[1]);
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    string gemsMessage = "<c6>Спасибо за поддержку сервера<3</c>";

    List<Notification> notifications = new List<Notification>
    {
        new Notification { Id = 73, MessageEntry = gemsMessage }
    };

    if (args[2] == "2")
    {
        notifications.AddRange(new[]
        {
            new Notification { Id = 90, DonationCount = 1000, MessageEntry = gemsMessage },
            new Notification { Id = 89, DonationCount = 100, MessageEntry = gemsMessage },
            new Notification { Id = 93, BrawlerID = 11, MessageEntry = gemsMessage }
        });
    }

    foreach (var n in notifications)
    {
        account.Home.NotificationFactory.Add(n);

        LogicAddNotificationCommand acm = new() { Notification = n };
        AvailableServerCommandMessage asm = new() { Command = acm };

        if (Sessions.IsSessionActive(id))
        {
            var session = Sessions.GetSession(id);
            session.GameListener.SendTCPMessage(asm);
        }
    }

    string tag = sc ? LogicLongCodeGenerator.ToCode(id) : args[1];
    Console.WriteLine($"Done: BattlePass reward sent to {tag}!");
}



private static void ExecuteAddEmote(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /addemote [TAG] [EmoteID] [Message]");
        return;
    }

    bool sc = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        id = long.Parse(args[1]);
    }

    if (!int.TryParse(args[2], out int emoteID))
    {
        Console.WriteLine("Error: Invalid EmoteID.");
        return;
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    string emoteMessage = args.Length > 3 && args[3] != "1" ? $"<c6>{args[3]}</c>" : "<c6>Спасибо за поддержку сервера<3</c>";

    Notification n = new Notification
    {
        Id = 72,
        BrawlerID = 52000000 + emoteID,
        MessageEntry = emoteMessage
    };

    account.Home.NotificationFactory.Add(n);

    LogicAddNotificationCommand acm = new()
    {
        Notification = n
    };
    AvailableServerCommandMessage asm = new AvailableServerCommandMessage();
    asm.Command = acm;

    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(asm);
    }

    string tag = sc ? LogicLongCodeGenerator.ToCode(id) : args[1];
    Console.WriteLine($"Done: added Emote {emoteID} to {tag}!");
}



private static void ExecuteAddBrawler(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /addbrawler [TAG] [BrawlerID] [Message]");
        return;
    }

    bool sc = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        id = long.Parse(args[1]);
    }

    if (!int.TryParse(args[2], out int brawlerID))
    {
        Console.WriteLine("Error: Invalid BrawlerID.");
        return;
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    string brawlerMessage = args.Length > 3 && args[3] != "1" ? $"<c6>{args[3]}</c>" : "<c6>Спасибо за поддержку сервера<3</c>";

    Notification n = new Notification
    {
        Id = 93,
        BrawlerID = brawlerID,
        MessageEntry = brawlerMessage
    };

    account.Home.NotificationFactory.Add(n);

    LogicAddNotificationCommand acm = new()
    {
        Notification = n
    };
    AvailableServerCommandMessage asm = new AvailableServerCommandMessage();
    asm.Command = acm;

    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(asm);
    }

    string tag = sc ? LogicLongCodeGenerator.ToCode(id) : args[1];
    Console.WriteLine($"Done: added Brawler {brawlerID} to {tag}!");
}


private static void ExecuteAddSkin(string[] args)
{
    if (args.Length < 4)
    {
        Console.WriteLine("Usage: /addskin [TAG] [SkinID] [Message]");
        return;
    }

    bool isCode = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        isCode = true;
        id = long.Parse(args[1]);
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    if (!int.TryParse(args[2], out int skinId))
    {
        Console.WriteLine("Error: Invalid SkinID.");
        return;
    }

    string message = args[3] == "1" ? "<c6>Cпасибо за поддержку сервера<3</c>" : $"<c6>{args[3]}</c>";

    Notification notification = new Notification
    {
        Id = 90,
        DonationCount = 1,
        SkinID = 29000000 + skinId,
        MessageEntry = message
    };

    account.Home.NotificationFactory.Add(notification);

    LogicAddNotificationCommand command = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverMessage = new AvailableServerCommandMessage
    {
        Command = command
    };

    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(serverMessage);
    }

    string tag = isCode ? LogicLongCodeGenerator.ToCode(id) : args[1];
    Console.WriteLine($"Done: added skin {skinId} to {tag}!");
}


private static void ExecuteAddGold(string[] args)
{
    if (args.Length != 3)
    {
        Console.WriteLine("Usage: /addgold [TAG] [Amount]");
        return;
    }

    bool sc = false;
    long id = 0;

    if (args[1].StartsWith('#'))
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        id = long.Parse(args[1]);
    }

    if (!int.TryParse(args[2], out int amount) || amount <= 0)
    {
        Console.WriteLine("Error: Invalid amount. Amount must be a positive number.");
        return;
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine($"Error: Invalid tag: {id}");
        return;
    }

    Notification n = new Notification
    {
        Id = 90,
        DonationCount = amount,
        MessageEntry = $"<c6>Thanks for donation!</c>"
    };

    account.Home.NotificationFactory.Add(n);

    LogicAddNotificationCommand acm = new()
    {
        Notification = n
    };
    AvailableServerCommandMessage asm = new AvailableServerCommandMessage();
    asm.Command = acm;

    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(asm);
    }

    string d = sc ? LogicLongCodeGenerator.ToCode(long.Parse(args[1])) : args[1];
    Console.WriteLine($"Done: added {amount} gold for {d}!");
}

private static void PlusTrophies(string[] args)
{
    if (args.Length != 3) // Увеличиваем количество аргументов
    {
        //Console.WriteLine("Usage: /dev [TAG] [TROPHIES]");
        return;
    }

    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);
    if (account == null)
    {
        //Console.WriteLine("Fail: account not found!");
        return;
    }

    // Устанавливаем режим разработчика
    account.Avatar.IsDev = true;

    // Пытаемся получить количество трофеев из аргументов
    if (!long.TryParse(args[2], out long trophies))
    {
        //Console.WriteLine("Fail: invalid trophy count!");
        return;
    }

    // Сбрасываем трофеи
    account.Avatar.PlusTrophies(trophies); // Передаем количество трофеев

    // Сохраняем изменения в базе данных
    Accounts.Save(account);

    // Проверяем активность сессии и уведомляем пользователя
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "УПС! Тебя заблокировали!"
        });
        Sessions.Remove(id);
    }
}



private static void Dev(string[] args)
{
    if (args.Length != 2)
    {
        //Console.WriteLine("Usage: /dev [TAG]");
        return;
    }

    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);
    if (account == null)
    {
        //Console.WriteLine("Fail: account not found!");
        return;
    }

    // Устанавливаем режим разработчика
    account.Avatar.IsDev = true;

    // Вызываем функцию для бана аккаунта
    BanAccount(account);

    // Сбрасываем трофеи
    account.Avatar.ResetTrophies();

    // Сохраняем изменения в базе данных
    Accounts.Save(account);

    // Проверяем активность сессии и уведомляем пользователя
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "УПС! Тебя заблокировали!"
        });
        Sessions.Remove(id);
    }
}

// Метод для бана аккаунта
private static void BanAccount(Account account)
{
    // Логика бана аккаунта, например:
    account.Avatar.Banned = true; // Устанавливаем флаг бана
    // Можно добавить дополнительные действия, например, запись в логи или уведомления
}



private static void ExecuteGiveMegaGemsToAccount(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /offer [TAG]");
        return;
    }

    bool sc = false;
    long targetAccountId = 0;
    if (args[1].StartsWith('#'))
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    else
    {
        sc = true;
        targetAccountId = long.Parse(args[1]);
    }

    Account targetAccount = Accounts.Load(targetAccountId);
    if (targetAccount == null)
    {
        Console.WriteLine("Error: Account not found!");
        return;
    }


    int donationAmount = 10;
    int itemPrice = 0;    
    ShopItem itemType = ShopItem.MegaBox;  

  
    Notification gemsNotification = new Notification
    {
        Id = 81,  
        MessageEntry = $"<c6>Ваши {donationAmount} Мега Ящиков, спасибо за поддержку сервера!</c>"
    };
    targetAccount.Home.NotificationFactory.Add(gemsNotification);

    LogicAddNotificationCommand gemsNotificationCommand = new()
    {
        Notification = gemsNotification
    };

    AvailableServerCommandMessage gemsCommandMessage = new AvailableServerCommandMessage
    {
        Command = gemsNotificationCommand
    };

    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(gemsCommandMessage);
    }

   
    string donationLogEntry = $"Игроку {args[1]} Выдано {donationAmount} Мега Ящиков";
    string donationFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "donate.txt");

    if (!File.Exists(donationFilePath))
    {
        File.Create(donationFilePath).Dispose();
    }

    File.AppendAllText(donationFilePath, donationLogEntry + Environment.NewLine);

   
    OfferBundle megaBoxBundle = new OfferBundle
    {
        Title = "АКЦИЯ",
        IsDailyDeals = false,
        EndTime = DateTime.UtcNow.AddDays(3),
        BackgroundExportName = "offer_special",
        Cost = itemPrice,
        OldCost = 80 * donationAmount, 
        Currency = 0 
    };

    megaBoxBundle.Items.Add(new Offer(itemType, donationAmount));
    targetAccount.Home.OfferBundles.Add(megaBoxBundle);
    Accounts.Save(targetAccount);

    string d = sc ? LogicLongCodeGenerator.ToCode(long.Parse(args[1])) : args[1];
    Console.WriteLine($"Done: {donationAmount} Мега Ящиков добавлено в аккаунт {d}!");
}



        private static void ExecuteUnlockAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unlockall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            for (int i = 0; i < HomeMode.UNLOCKABLE_HEROES_COUNT; i++)
            {
                if (!account.Avatar.HasHero(16000000 + i))
                {
                    CharacterData character = DataTables.Get(16).GetDataWithId<CharacterData>(i);
                    CardData card = DataTables.Get(23).GetData<CardData>(character.Name + "_unlock");

                    account.Avatar.UnlockHero(character.GetGlobalId(), card.GetGlobalId());
                }
            }

            Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteUnbanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = false;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }


        private static void ExecuteChangeNameForAccount(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [NewName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Name = args[2];
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGetFieldValue(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            int value = (int)field.GetValue(account.Avatar);
            Console.WriteLine($"LogicClientAvatar::{args[2]} = {value}");
        }

        private static void ExecuteChangeValueForAccount(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName] [Value]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            field.SetValue(account.Avatar, int.Parse(args[3]));
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGivePremiumToAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /giveprem [TAG]");
                return;
            }

            bool sc = false;
            long id = 0;
            if (args[1].StartsWith('#'))
            {
                id = LogicLongCodeGenerator.ToId(args[1]);
            }
            else
            {
                sc = true;
                id = long.Parse(args[1]);
            }

            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine($"Error: Invalid tag: {id}");
                return;
            }

            if (account.Home.PremiumEndTime < DateTime.UtcNow)
            {
                account.Home.PremiumEndTime = DateTime.UtcNow.AddMonths(12);
            }
            else
            {
                account.Home.PremiumEndTime = account.Home.PremiumEndTime.AddMonths(12);
            }

            account.Avatar.PremiumLevel = 1;
            Notification n = new Notification
            {
                Id = 89,
                DonationCount = 360,
                MessageEntry = $"<c6>Vip статус активирован/продлён до {account.Home.PremiumEndTime} UTC, спасибо за поддержку сервера!</c>"
            };
            account.Home.NotificationFactory.Add(n);
            LogicAddNotificationCommand acm = new()
            {
                Notification = n
            };
            AvailableServerCommandMessage asm = new AvailableServerCommandMessage();
            asm.Command = acm;

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(asm);
            }

            string d = sc ? LogicLongCodeGenerator.ToCode(long.Parse(args[1])) : args[1];
            Console.WriteLine($"Done: set vip status for {d} activated/extended to {account.Home.PremiumEndTime} UTC!");
        }

        private static void ExecuteGiveGemsToAccount(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /gems [TAG] [Amount]");
                return;
            }

            bool sc = false;
            long id = 0;
            if (args[1].StartsWith('#'))
            {
                id = LogicLongCodeGenerator.ToId(args[1]);
            }
            else
            {
                sc = true;
                id = long.Parse(args[1]);
            }

            if (!int.TryParse(args[2], out int amount) || amount <= 0)
            {
                Console.WriteLine("Error: Invalid amount. Amount must be a positive number.");
                return;
            }

            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine($"Error: Invalid tag: {id}");
                return;
            }

        

            
            Notification n = new Notification
            {
                Id = 89,  
                DonationCount = amount,
                MessageEntry = $"<c6>Вам было выдано {amount} гемов! Спасибо за поддержку сервера!</c>"
            };
            account.Home.NotificationFactory.Add(n);

            
            LogicAddNotificationCommand acm = new()
            {
                Notification = n
            };
            AvailableServerCommandMessage asm = new AvailableServerCommandMessage();
            asm.Command = acm;

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(asm);
            }

            string d = sc ? LogicLongCodeGenerator.ToCode(long.Parse(args[1])) : args[1];
            Console.WriteLine($"Done: {amount} gems added to account {d}!");
        }

        private static void ExecuteShutdown()
        {
            Sessions.StartShutdown();
            AccountCache.SaveAll();
            AllianceCache.SaveAll();

            AccountCache.Started = false;
            AllianceCache.Started = false;
        }
    }
}
