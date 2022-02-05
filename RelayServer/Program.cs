using System;

namespace RelayServer
{
    class Program
    {
        static void Main(string[] args)
        {
            RelayServer relayServer = new RelayServer();
            if (args.Length > 1)
            {
                relayServer = new RelayServer(Int32.Parse(args[0]));
            }
            relayServer.start();
        }
    }
}
