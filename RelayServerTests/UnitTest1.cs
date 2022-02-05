using NUnit.Framework;
using RelayServer = RelayServer.RelayServer;
using Socket = relayPeer.Socket;

namespace RelayServerTests
{
    public class Tests
    {

        private global::RelayServer.RelayServer sut;

        private global::relayPeer.IRelaySocket iRelaySocket;

        [SetUp]
        public void Setup()
        {
            sut = new global::RelayServer.RelayServer();
        }

        [Test]
        public void TestStartServerAndConnectWithPeer()
        {
            sut.start();
            Assert.Pass();
        }
    }
}