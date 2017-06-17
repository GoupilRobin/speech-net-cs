using System;

namespace Marvin
{
    // TODO: read port from args so game can randomly pick one
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Server server = new Server();

            server.Start();
        }
    }
}
