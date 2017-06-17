using System;
using System.Collections.Generic;
using System.Text;

namespace Marvin
{
    public class Configuration
    {
        // port to be used by the server
        public static readonly int Port = 9856;

        // maximum size of the packet sent by the Server to the Client when speech has been recognized
        public static readonly int MaxPayloadLength = 4096;

        // current API version
        // should be updated when changes are made to almost anything in the MarvinInterface project
        public static readonly int ApiVersion = 1;

        // oldest compatible API version
        // should be updated when changing ApiVersion and that the changes are breaking
        public static readonly int MinApiVersion = 1;

    }
}
