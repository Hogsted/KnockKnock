using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnockKnockServer;

// Ansvar: acceptere TCP-forbindelser og starte en trÃ¥d pr. klient.
// Ved intet om vittigheder eller samtalens regler.
class KnockKnockTcpServer
{
    private readonly ServerOptions _options;
    private readonly IConversationFactory _conversationFactory;
    private readonly ILogger _logger;
    private readonly ILogger _sessionLogger;

    // Delt mellem hovedtrÃ¥den og klienttrÃ¥dene - beskyttes af _lock
    private readonly Dictionary<ClientSession, Thread> _activeSessions = new();
    private readonly object _lock = new();

    // Bruges kun af hovedtrÃ¥den, sÃ¥ der er ikke behov for synkronisering
    private int _nextClientId;

    public KnockKnockTcpServer(IOptions<ServerOptions> options, IConversationFactory conversationFactory, ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _conversationFactory = conversationFactory;
        _logger = loggerFactory.CreateLogger<KnockKnockTcpServer>();
        _sessionLogger = loggerFactory.CreateLogger<ClientSession>();
    }

    // KÃ¸rer pÃ¥ hovedtrÃ¥den og blokerer, indtil stoppingToken annulleres (Ctrl+C)
    public void Run(CancellationToken stoppingToken)
    {
        // Lytter pÃ¥ bÃ¥de IPv4 og IPv6, sÃ¥ "localhost" (::1) forbinder med det samme
        TcpListener listener = TcpListener.Create(_options.Port);
        listener.Start();
        _logger.LogInformation("Knock Knock Server lytter pÃ¥ port {Port} (maks. {MaxClients} klienter). Tryk Ctrl+C for at stoppe.",
            _options.Port, _options.MaxClients);

        // Stop() fÃ¥r det blokerende AcceptTcpClient() til at kaste en exception, sÃ¥ lÃ¸kken afsluttes
        using CancellationTokenRegistration registration = stoppingToken.Register(listener.Stop);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception ex) when (stoppingToken.IsCancellationRequested
                    && ex is SocketException or ObjectDisposedException or InvalidOperationException)
                {
                    break;
                }

                try
                {
                    StartSession(client);
                }
                catch (Exception ex)
                {
                    // En fejl ved Ã©n forbindelse mÃ¥ ikke stoppe hovedtrÃ¥den
                    _logger.LogWarning(ex, "Kunne ikke starte en session for ny klient");
                    client.Dispose();
                }
            }
        }
        finally
        {
            listener.Stop();
            CloseAllSessions();
        }
    }

    private void StartSession(TcpClient client)
    {
        ClientSession session = new ClientSession(++_nextClientId, client, _sessionLogger);

        // Tjek og tilfÃ¸j skal ske under samme lock, ellers kunne grÃ¦nsen overskrides
        lock (_lock)
        {
            if (_activeSessions.Count < _options.MaxClients)
            {
                Thread thread = new Thread(() => RunSession(session))
                {
                    Name = $"Klient {session.Id}",
                    IsBackground = true
                };
                _activeSessions.Add(session, thread);
                thread.Start();
                return;
            }
        }

        _logger.LogWarning("[Klient {Id}] Afvist - maks. {MaxClients} samtidige klienter er nÃ¥et", session.Id, _options.MaxClients);
        session.Close("Server is busy. Please try again later.");
    }

    // KÃ¸rer pÃ¥ klientens egen trÃ¥d
    private void RunSession(ClientSession session)
    {
        try
        {
            // Hver klient fÃ¥r sin egen samtale og dermed sin egen tilstand
            session.Run(_conversationFactory.Create());
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            _logger.LogInformation("[Klient {Id}] Forbindelsen blev afbrudt", session.Id);
        }
        catch (Exception ex)
        {
            // En uhÃ¥ndteret exception i en trÃ¥d ville stoppe hele processen
            _logger.LogError(ex, "[Klient {Id}] Uventet fejl", session.Id);
        }
        finally
        {
            session.Dispose();
            int activeCount;
            lock (_lock)
            {
                _activeSessions.Remove(session);
                activeCount = _activeSessions.Count;
            }
            _logger.LogInformation("[Klient {Id}] Lukket. Aktive klienter: {Count}", session.Id, activeCount);
        }
    }

    private void CloseAllSessions()
    {
        List<KeyValuePair<ClientSession, Thread>> sessions;
        lock (_lock)
        {
            sessions = _activeSessions.ToList();
        }

        _logger.LogInformation("Serveren lukker ned. Lukker {Count} aktive forbindelser...", sessions.Count);

        foreach ((ClientSession session, _) in sessions)
            session.Close("Server is shutting down.");

        // Vent pÃ¥ at klienttrÃ¥dene nÃ¥r at rydde op
        foreach ((_, Thread thread) in sessions)
            thread.Join(TimeSpan.FromSeconds(5));

        _logger.LogInformation("Serveren er stoppet.");
    }
}
