using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RelayServer
{
    public class RelayServer
    {
        //Default port 9000
        private int serverport = 9000;
        private Thread listen;
        private Boolean exit;

        // Incoming data from the client.  
        public static string data = null;

        //List of connections
        private volatile List<Connection> connectionList = new List<Connection>();

        private int bufferSize = 2048;

        public RelayServer() { }
        
        public RelayServer(int port)
        {
            this.serverport = port;
        }

        public void start()
        {
            Console.WriteLine("Server started with port: " + serverport);
            runServer();
        }

        public void stop()
        {
            exit = true; //Set condition to true to exit thread
            listen.Abort(); //Abort listening thread
        }

        private void runServer()
        {
            //Start listening thread
            listen = new Thread(new ThreadStart(ListenToConnections));
            listen.Start();
        }

        private void ListenToConnections()
        {
            //Method to accurately get the local IP from machine - uses the correct network card 
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("94.124.143.109", 65530); //Try connecting to relayserver
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;//Get IP from connection
                localIP = endPoint.Address.ToString(); //Parse connection
            }
            IPAddress iPAddress = IPAddress.Parse(localIP);
            IPEndPoint localEndPoint = new IPEndPoint(iPAddress, serverport);


                // Create a TCP/IP socket.  
                Socket listener = new Socket(iPAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

            //Bind the socket to the local endpoint and
            //listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);//Bind to socket
                listener.Listen(100);

                // Start listening for connections.  
                while (!exit) //Condition to exit thread
                {
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.  
                    Socket handler = listener.Accept();
                    String ip = handler.RemoteEndPoint.ToString();
                    Console.WriteLine("Client with IP " + ip + " connected!");
                    Stream stream = new NetworkStream(handler);
                    Connection connection = new Connection(ip, stream, false, "");
                    connectionList.Add(connection);
                    ConnectedPeer(connection);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        
        private void ConnectedPeer(Connection connection)
        {
            Thread t = new Thread(() => {
                StreamReader reader = new StreamReader(connection.stream, Encoding.Default); //Read messages from peer
                StreamWriter writer = new StreamWriter(connection.stream, Encoding.ASCII, bufferSize);//Send messages to peer
                Connection targetConnection = null;
                writer.AutoFlush = true; //Set to automatically flush
                Boolean connected = true; //Condition to stop thread
                while (connected && !exit)
                {
                    try
                    {
                        string msg = "";
                        msg = reader.ReadLine();
                        String aliasMsg = connection.ipport + " : " + msg;
                        Console.WriteLine(aliasMsg);
                        if (connection.relay)
                        {
                            if (FindConnection(connection.connected))
                            {
                                if (targetConnection == null)
                                {

                                    connectionList.ForEach(connection2 =>
                                    {
                                        if (connection.connected.Equals(connection2.ipport))
                                        {
                                            targetConnection = connection2;
                                        }
                                    });
                                }
                                if (msg.Contains("<CLOSE>"))
                                {
                                    //zet verbinding uit
                                    connection.relay = false;
                                    targetConnection.relay = false;
                                    connection.connected = "";
                                    targetConnection.connected = "";
                                    SendMessage(writer, "Disconnected with " + targetConnection.ipport);
                                    StreamWriter targetWriter = new StreamWriter(targetConnection.stream, Encoding.ASCII, bufferSize);
                                    SendMessage(targetWriter, "Disconnected with " + connection.ipport);
                                }
                                else
                                {
                                    StreamWriter targetWriter = new StreamWriter(targetConnection.stream, Encoding.ASCII, bufferSize);
                                    SendMessage(targetWriter, aliasMsg);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Connected client disconnected!");
                                connection.relay = false;
                                connection.connected = "";
                            }
                        } else
                        {
                            if (msg.Contains("<CONNECT>"))
                            {
                                String targetIPPort = msg.Replace("<CONNECT>", "");

                                if (FindConnection(targetIPPort)) //Check if ip is connected
                                {
   
                                    targetConnection = GetConnection(targetIPPort);
                                    connection.relay = true;
                                    connection.connected = targetIPPort;
                                    targetConnection.relay = true;
                                    targetConnection.connected = connection.ipport;
                                    SendMessage(writer, "Connected with " + targetConnection.ipport);
                                    //Make streamwriter to send messages
                                    StreamWriter targetWriter = new StreamWriter(targetConnection.stream, Encoding.ASCII, bufferSize);
                                    SendMessage(targetWriter, "Connected with " + connection.ipport);
                                }
                                else
                                {
                                    SendMessage(writer, "Ip not found!");
                                }
                            }
                            if (msg.Contains("/ip"))
                            {
                                String ipList = "";
                                connectionList.ForEach(connect =>
                                {
                                    if ("".Equals(ipList))
                                    {
                                        ipList += connect.ipport;
                                    } else
                                    {
                                        ipList += ", " + connect.ipport;
                                    }
                                });
                                SendMessage(writer, "<IP>" + ipList);
                            }
                        }
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Lost connection with " + connection.ipport);
                        connected = false; //Shutdown thread
                        if (connection.relay)
                        {
                            if (targetConnection == null)
                            {
                                connectionList.ForEach(connection2 =>
                                {
                                    if (connection.connected.Equals(connection2.connected))
                                    {
                                        targetConnection = connection2;
                                    }
                                });
                            }
                            targetConnection.relay = false;
                            targetConnection.connected = "";
                        }
                        DeletePeer(connection);
                    }

                }
            });
            t.Start();
        }

        private void SendMessage(StreamWriter write, String message)
        {
            try
            {
                write.WriteLine(message + "<EOF>");
                write.Flush();
            }
            catch (IOException)
            {
                Console.WriteLine("Couldn't send message!");
            }
        }

        private void DeletePeer(Connection connection)
        {
            connectionList.Remove(connection);
        }

        private Boolean FindConnection(String targetIPPort)
        {
            Boolean result = false;
            foreach(Connection connection in connectionList)
            {
                if (targetIPPort.Equals(connection.ipport))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private Connection GetConnection(String targetIPPort)
        {
            Connection targetConnection = null;
            foreach (Connection connection in connectionList)
            {
                if (targetIPPort.Equals(connection.ipport))
                {
                    targetConnection = connection;
                }
            }

            return targetConnection;
        }

    }
}
