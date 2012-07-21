using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrickEmulator.IO;
using System.Windows.Forms;
using System.Security.Permissions;

using BrickEmulator.Storage;
using BrickEmulator.Security;
using BrickEmulator.HabboHotel.Users;
using BrickEmulator.HabboHotel.Shop;
using BrickEmulator.HabboHotel.Furni;
using BrickEmulator.HabboHotel.Users.Handlers.Membership;
using BrickEmulator.HabboHotel.Rooms.Navigator;
using BrickEmulator.HabboHotel.Rooms;
using BrickEmulator.HabboHotel.Missions.Achievements;
using BrickEmulator.HabboHotel.Users.Handlers.Clothes;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units.Commands;
using BrickEmulator.HabboHotel.Users.Handlers.Badges;
using BrickEmulator.HabboHotel.Users.Handlers.Messenger;
using BrickEmulator.HabboHotel.Tools;
using BrickEmulator.HabboHotel.Processing;
using BrickEmulator.HabboHotel.Users.Handlers.Messenger.Streaming;
using BrickEmulator.HabboHotel.Rooms.Chatlogs;
using BrickEmulator.Network;
using BrickEmulator.HabboHotel.Users.Handlers.Effects;
using BrickEmulator.HabboHotel.Shop.Marketplace;
using BrickEmulator.HabboHotel.Shop.Ecotron;
using BrickEmulator.HabboHotel.Pets;
using BrickEmulator.Network.Site;
using BrickEmulator.HabboHotel.Rooms.Virtual.Units.Chatting;
using BrickEmulator.HabboHotel.Missions.Quests;

namespace BrickEmulator
{
    /// <summary>
    /// Diplomated Environment created for all classes that has been coded.
    /// </summary>
    /// <author>Breakz0ne</author>
    static class BrickEngine
    {
        #region IO
        private static ScreenWriter ScreenWriter = new ScreenWriter();
        private static MemoryWriter MemoryWriter = new MemoryWriter();

        public static DateTime Started = DateTime.Now;
        public static Random Random = new Random();
        #endregion

        #region Engine
        private static ConfigureFile       ConfigureFile;
        private static SocketShield        SocketShield;
        private static SiteSocketListener  SiteSocketListener;
        private static DatabaseEngine      DatabaseEngine;
        private static SecurityConvertor   Convertor = new SecurityConvertor();
        private static ProgressReactor     ProgressReactor = new ProgressReactor();
        #endregion

        #region Reactors
        private static ItemReactor         ItemReactor;
        private static UserReactor         UserReactor;
        private static ShopReactor         ShopReactor;
        private static FurniReactor        FurniReactor;
        private static AchievementReactor  AchievementReactor;
        private static NavigatorReactor    NavigatorReactor;
        private static RoomReactor         RoomReactor;
        private static ToolReactor         ToolReactor;
        private static MarketplaceReactor  MarketplaceReactor;
        private static EcotronReactor      EcotronReactor;
        private static PetReactor          PetReactor;
        private static QuestReactor        QuestReactor;
        #endregion

        #region Handlers
        private static PacketHandler      PacketHandler;
        private static MembershipHandler  MembershipHandler;
        private static ClothesHandler     ClothesHandler;
        private static BadgeHandler       BadgeHandler;
        private static CommandHandler     CommandHandler;
        private static MessengerHandler   MessengerHandler;
        private static StreamHandler      StreamHandler;
        private static ChatlogHandler     ChatlogHandler;
        private static EffectsHandler     EffectsHandler;
        private static WordFilterHandler  WordFilterHandler;
        #endregion

        #region Constructors
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        [MTAThread]
        public static void Main()
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);

                GetScreenWriter().ScretchTitle(" [" + System.Diagnostics.Process.GetCurrentProcess().Id + "] [" + Environment.UserName + "] [" + (int)(DateTime.Now - Started).TotalHours + "] Hours alive.");

                GetScreenWriter().ScretchStandardLine(string.Empty);

                GetScreenWriter().ScretchStandardLine("       ______      _      _    _____                _       _                  ");
                GetScreenWriter().ScretchStandardLine("       | ___ \\    (_)    | |  |  ___|              | |     | |                 ");
                GetScreenWriter().ScretchStandardLine("       | |_/ /_ __ _  ___| | _| |__ _ __ ___  _   _| | __ _| |_ ___  _ __      ");
                GetScreenWriter().ScretchStandardLine("       | ___ \\ '__| |/ __| |/ /  __| '_ ` _ \\| | | | |/ _` | __/ _ \\| '__|     ");
                GetScreenWriter().ScretchStandardLine("       | |_/ / |  | | (__|   <| |__| | | | | | |_| | | (_| | || (_) | |        ");
                GetScreenWriter().ScretchStandardLine("       \\____/|_|  |_|\\___|_|\\_\\____/_| |_| |_|\\__,_|_|\\__,_|\\__\\___/|_|        ");

                GetScreenWriter().ScretchStandardLine(string.Empty);

                GetScreenWriter().ScretchStandardLine("         x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x");
                GetScreenWriter().ScretchStandardLine("         x Breakz0ne Productions © and BrickPHP all rights reserved. x");
                GetScreenWriter().ScretchStandardLine("         x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x-x");

                GetScreenWriter().ScretchStandardLine(string.Empty);

                ConfigureFile = new ConfigureFile();

                if (ConfigureFile.HandleFiles())
                {
                    if (GetConfigureFile().CallBooleanKey("writer.enabled"))
                    {
                        GetScreenWriter().ScretchLine("Writer running on interval(sec) : " + MemoryWriter.StartLogging(), WriteType.Outgoing);
                    }

                    SocketShield = new SocketShield();
                    SocketShield.Start();

                    SiteSocketListener = new SiteSocketListener();

                    DatabaseEngine = new DatabaseEngine();
                    DatabaseEngine.Initialize();

                    using (QueryReactor Reactor = GetQueryReactor())
                    {
                        Reactor.SetQuery("UPDATE users SET credits = '50' WHERE id = '1' LIMIT 1");
                        Reactor.ExcuteQuery();
                    }

                    GetScreenWriter().ScretchStandardLine(string.Empty);

                    GetScreenWriter().ScretchLine("Starting caching items...", WriteType.Outgoing);

                    PacketHandler = new PacketHandler();
                    PacketHandler.LoadInteractions();

                    UserReactor = new UserReactor();
                    UserReactor.LoadSettings();

                    ShopReactor = new ShopReactor();
                    ShopReactor.Prepare();

                    FurniReactor = new FurniReactor();
                    FurniReactor.Prepare();

                    MembershipHandler = new MembershipHandler();
                    ClothesHandler = new ClothesHandler();

                    NavigatorReactor = new NavigatorReactor();

                    RoomReactor = new RoomReactor();
                    RoomReactor.Prepare();

                    AchievementReactor = new AchievementReactor();
                    AchievementReactor.Prepare();

                    CommandHandler = new CommandHandler();
                    CommandHandler.Prepare();

                    ItemReactor = new ItemReactor();
                    ItemReactor.Prepare();

                    BadgeHandler = new BadgeHandler();

                    MessengerHandler = new MessengerHandler();
                    StreamHandler = new StreamHandler();

                    ToolReactor = new ToolReactor();
                    ChatlogHandler = new ChatlogHandler();
                    EffectsHandler = new EffectsHandler();

                    MarketplaceReactor = new MarketplaceReactor();
                    EcotronReactor = new EcotronReactor();
                    PetReactor = new PetReactor();
                    QuestReactor = new QuestReactor();

                    WordFilterHandler = new WordFilterHandler();

                    GetScreenWriter().ScretchStandardLine(string.Empty);
                    GetScreenWriter().ScretchLine("BrickEmulator is booted up succesfully.", WriteType.Outgoing);
                    GetScreenWriter().ScretchStandardLine(string.Empty);

                    Console.Beep();
                }

                GetScreenWriter().KeepAlive();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                GetScreenWriter().KeepAlive();
            }
        }
        #endregion

        #region Methods
        static void ExceptionHandler(object From, UnhandledExceptionEventArgs Args)
        {
            Exception e = (Exception)Args.ExceptionObject;
            
            GetScreenWriter().PaintScreen(ConsoleColor.Red, PaintType.ForeColor);
            GetScreenWriter().ScretchLine("Exception " + e.ToString(), WriteType.Incoming);

            GetScreenWriter().KeepAlive();
        }

        public static string CleanString(string Raw)
        {
            Raw = Raw.Replace(Convert.ToChar(1), ' ');
            Raw = Raw.Replace(Convert.ToChar(2), ' ');
            Raw = Raw.Replace(Convert.ToChar(3), ' ');
            Raw = Raw.Replace(Convert.ToChar(9), ' ');

            Raw = Raw.Replace("\'","'");

            return Raw;
        }
        #endregion

        #region Quick Returners
        public static ConfigureFile GetConfigureFile()
        {
            return ConfigureFile;
        }

        public static SocketShield GetSocketShield()
        {
            return SocketShield;
        }

        public static SiteSocketListener GetSiteSocketListener()
        {
            return SiteSocketListener;
        }

        public static MemoryWriter GetMemoryWriter()
        {
            return MemoryWriter;
        }

        public static ScreenWriter GetScreenWriter()
        {
            return ScreenWriter;
        }

        public static PacketHandler GetPacketHandler()
        {
            return PacketHandler;
        }

        public static UserReactor GetUserReactor()
        {
            return UserReactor;
        }

        public static ShopReactor GetShopReactor()
        {
            return ShopReactor;
        }

        public static FurniReactor GetFurniReactor()
        {
            return FurniReactor;
        }

        public static MembershipHandler GetMembershipHandler()
        {
            return MembershipHandler;
        }

        public static ClothesHandler GetClothesHandler()
        {
            return ClothesHandler;
        }

        public static NavigatorReactor GetNavigatorManager()
        {
            return NavigatorReactor;
        }

        public static RoomReactor GetRoomReactor()
        {
            return RoomReactor;
        }

        public static AchievementReactor GetAchievementReactor()
        {
            return AchievementReactor;
        }

        public static ProgressReactor GetProgressReactor()
        {
            return ProgressReactor;
        }

        public static CommandHandler GetCommandHandler()
        {
            return CommandHandler;
        }

        public static ItemReactor GetItemReactor()
        {
            return ItemReactor;
        }

        public static BadgeHandler GetBadgeHandler()
        {
            return BadgeHandler;
        }

        public static MessengerHandler GetMessengerHandler()
        {
            return MessengerHandler;
        }

        public static StreamHandler GetStreamHandler()
        {
            return StreamHandler;
        }

        public static ToolReactor GetToolReactor()
        {
            return ToolReactor;
        }

        public static ChatlogHandler GetChatlogHandler()
        {
            return ChatlogHandler;
        }

        public static EffectsHandler GetEffectsHandler()
        {
            return EffectsHandler;
        }

        public static MarketplaceReactor GetMarketplaceReactor()
        {
            return MarketplaceReactor;
        }

        public static EcotronReactor GetEcotronReactor()
        {
            return EcotronReactor;
        }

        public static PetReactor GetPetReactor()
        {
            return PetReactor;
        }

        public static WordFilterHandler GetWordFilterHandler()
        {
            return WordFilterHandler;
        }

        public static QuestReactor GetQuestReactor()
        {
            return QuestReactor;
        }

        public static QueryReactor GetQueryReactor()
        {
            return DatabaseEngine.GetAvailableReactor();
        }

        public static Encoding GetEncoding()
        {
            return Encoding.ASCII;
        }

        public static SecurityConvertor GetConvertor()
        {
            return Convertor;
        }
        #endregion
    }
}
