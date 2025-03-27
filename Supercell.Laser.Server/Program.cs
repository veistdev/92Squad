namespace Supercell.Laser.Server
{
    using Supercell.Laser.Server.Handler;
    using Supercell.Laser.Server.Settings;
    using System.Drawing;

    static class Program
    {
        public const string SERVER_VERSION = "1.2";
        public const string BUILD_TYPE = "Beta";

        private static void Main(string[] args)
        {
            Console.Title = "92Squad - server emulator";
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            Colorful.Console.WriteWithGradient(
                @"

░█████╗░██████╗░  ░██████╗░██████╗░██╗░░░██╗░█████╗░██████╗░
██╔══██╗╚════██╗  ██╔════╝██╔═══██╗██║░░░██║██╔══██╗██╔══██╗
╚██████║░░███╔═╝  ╚█████╗░██║██╗██║██║░░░██║███████║██║░░██║
░╚═══██║██╔══╝░░  ░╚═══██╗╚██████╔╝██║░░░██║██╔══██║██║░░██║
░█████╔╝███████╗  ██████╔╝░╚═██╔═╝░╚██████╔╝██║░░██║██████╔╝
░╚════╝░╚══════╝  ╚═════╝░░░░╚═╝░░░░╚═════╝░╚═╝░░╚═╝╚═════╝░                                           " + "\n\n\n", Color.Fuchsia, Color.Cyan, 8);

            Logger.Print("Server By: 92Squad");

            Logger.Init();
            Configuration.Instance = Configuration.LoadFromFile("config.json");

            Resources.InitDatabase();
            Resources.InitLogic();
            Resources.InitNetwork();

            Logger.Print("Server started!");

            ExitHandler.Init();
            CmdHandler.Start();
        }
    }
}