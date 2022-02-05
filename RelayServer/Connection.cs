using System;
using System.IO;

namespace RelayServer
{
    class Connection
    {
        public String ipport;
        public Stream stream;
        public Boolean relay;
        public String connected;


        public Connection() { }
        public Connection(String ipport, Stream stream, Boolean relay, String connected)
        {
            this.ipport = ipport;
            this.stream = stream;
            this.relay = relay;
            this.connected = connected;
        }

    }
}
