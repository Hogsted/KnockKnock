using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace KnockKnockServer;

// Ansvar: linjebaseret TCP-kommunikation med én klient.
// Samtalens regler ligger i IConversation, så klassen ved intet om Knock Knock.
class ClientSession : IDisposable
{
    public const string Goodbye = "Bye!";

    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly ILogger _logger;

    // Klienttråden og hovedtråden (ved nedlukning) kan begge skrive til klienten
    private readonly object _writeLock = new();

    public int Id { get; }

    public ClientSession(int id, TcpClient client, ILogger logger)
    {
        Id = id;
        _client = client;
        _logger = logger;

        // Én UTF-8-kodet besked pr. linje
        NetworkStream stream = client.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
    }

    public void Run(IConversation conversation)
    {
        _logger.LogInformation("[Klient {Id}] Forbundet", Id);
        Send(conversation.Start());

        while (!conversation.IsFinished)
        {
            string? message = _reader.ReadLine();
            if (message == null)
            {
                _logger.LogInformation("[Klient {Id}] Afbrød forbindelsen", Id);
                return;
            }

            _logger.LogInformation("[Klient {Id}] CLIENT: {Message}", Id, message);
            Send(conversation.HandleInput(message));
        }

        Send(Goodbye);
        _logger.LogInformation("[Klient {Id}] Samtalen er færdig", Id);
    }

    // Sender en sidste besked og lukker forbindelsen, fx når serveren er fuld eller lukker ned
    public void Close(string reason)
    {
        try
        {
            Send(reason);
            Send(Goodbye);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // Klienten har allerede lukket forbindelsen
        }
        Dispose();
    }

    // Lukker socket'en. En blokeret ReadLine() i klienttråden kaster da en IOException.
    public void Dispose() => _client.Dispose();

    private void Send(string message)
    {
        lock (_writeLock)
        {
            _writer.WriteLine(message);
        }
        _logger.LogInformation("[Klient {Id}] SERVER: {Message}", Id, message);
    }
}
