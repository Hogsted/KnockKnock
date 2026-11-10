using System.Net;
using System.Net.Sockets;
using System.Text;
using KnockKnockServer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnockKnockServer.Tests;

// Integrationstest: starter den rigtige TCP-server på en tråd og forbinder rigtige klienter
public class KnockKnockTcpServerTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _stop = new();
    private readonly Thread _serverThread;
    private readonly int _port = GetFreePort();

    public KnockKnockTcpServerTests() : this(maxClients: 10) { }

    private KnockKnockTcpServerTests(int maxClients)
    {
        IOptions<JokeOptions> jokes = Options.Create(new JokeOptions
        {
            Jokes = new() { new KnockKnockJoke { Who = "Tank", Punchline = "You're welcome!" } }
        });
        KnockKnockTcpServer server = new KnockKnockTcpServer(
            Options.Create(new ServerOptions { Port = _port, MaxClients = maxClients }),
            new KnockKnockConversationFactory(new SequentialJokeSelector(jokes)),
            NullLoggerFactory.Instance);

        _serverThread = new Thread(() => server.Run(_stop.Token)) { IsBackground = true };
        _serverThread.Start();
    }

    [Fact]
    public void ThreeClients_CanTalkAtTheSameTime()
    {
        using TestClient a = Connect(), b = Connect(), c = Connect();

        // Samtalerne flettes ind i hinanden - hver klient har sin egen tilstand
        foreach (TestClient client in new[] { a, b, c })
            Assert.Equal("Knock! Knock!", client.Receive());
        foreach (TestClient client in new[] { a, b, c })
            Assert.Equal("Tank", client.SendAndReceive("Who's there?"));

        Assert.Equal("You are supposed to say: Tank who?", b.SendAndReceive("Hello?"));

        foreach (TestClient client in new[] { a, b, c })
        {
            Assert.Equal("You're welcome!", client.SendAndReceive("Tank who?"));
            Assert.Equal("Bye!", client.Receive());
        }
    }

    [Fact]
    public void ClientDisconnecting_DoesNotAffectOthers()
    {
        using TestClient survivor = Connect();
        TestClient quitter = Connect();
        Assert.Equal("Knock! Knock!", quitter.Receive());
        quitter.Dispose();

        Assert.Equal("Knock! Knock!", survivor.Receive());
        Assert.Equal("Tank", survivor.SendAndReceive("Who's there?"));

        // Serveren accepterer stadig nye klienter
        using TestClient newcomer = Connect();
        Assert.Equal("Knock! Knock!", newcomer.Receive());
    }

    [Fact]
    public void MaxClientsReached_NewClientIsRejected()
    {
        using KnockKnockTcpServerTests fixture = new KnockKnockTcpServerTests(maxClients: 2);
        using TestClient a = fixture.Connect(), b = fixture.Connect();
        Assert.Equal("Knock! Knock!", a.Receive());
        Assert.Equal("Knock! Knock!", b.Receive());

        using TestClient rejected = fixture.Connect();
        Assert.Equal("Server is busy. Please try again later.", rejected.Receive());
        Assert.Equal("Bye!", rejected.Receive());
        Assert.Null(rejected.Receive());

        // Når en plads bliver ledig, kan en ny klient komme ind
        a.Dispose();
        Assert.True(SpinWait.SpinUntil(() =>
        {
            using TestClient retry = fixture.Connect();
            return retry.Receive() == "Knock! Knock!";
        }, Timeout));
    }

    [Fact]
    public void ServerStopping_ClosesConnectionsAndThread()
    {
        using TestClient client = Connect();
        Assert.Equal("Knock! Knock!", client.Receive());

        _stop.Cancel();

        Assert.Equal("Server is shutting down.", client.Receive());
        Assert.Equal("Bye!", client.Receive());
        Assert.Null(client.Receive());
        Assert.True(_serverThread.Join(Timeout));
    }

    public void Dispose()
    {
        _stop.Cancel();
        _serverThread.Join(Timeout);
    }

    private TestClient Connect() => new TestClient(_port);

    private static int GetFreePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class TestClient : IDisposable
    {
        private readonly TcpClient _client;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public TestClient(int port)
        {
            _client = new TcpClient("localhost", port) { ReceiveTimeout = (int)Timeout.TotalMilliseconds };
            NetworkStream stream = _client.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        }

        public string? Receive() => _reader.ReadLine();

        public string? SendAndReceive(string message)
        {
            _writer.WriteLine(message);
            return Receive();
        }

        public void Dispose() => _client.Dispose();
    }
}
