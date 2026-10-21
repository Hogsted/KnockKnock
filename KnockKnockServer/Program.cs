using System.Net;
using System.Net.Sockets;
using System.Text;
using KnockKnockServer;

List<KnockKnockJoke> jokes = new()
{
    new KnockKnockJoke { Who = "Lettuce", Punchline = "Lettuce in, it's cold out here!" },
    new KnockKnockJoke { Who = "Tank", Punchline = "You're welcome!" },
    new KnockKnockJoke { Who = "Boo", Punchline = "Don't cry, it's only a joke!" },
    new KnockKnockJoke { Who = "Olive", Punchline = "Olive you and I don't care who knows it!" }
};

TcpListener listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();
Console.WriteLine("Knock Knock Server started...");

while (true)
{
    Console.WriteLine("Waiting for client...");
    using TcpClient client = listener.AcceptTcpClient();
    using NetworkStream stream = client.GetStream();
    Console.WriteLine("Client connected!");

    // Vælg en tilfældig joke når en ny samtale starter
    KnockKnockJoke joke = jokes[Random.Shared.Next(jokes.Count)];
    KnockKnockProtocol protocol = new KnockKnockProtocol(joke);

    Send(stream, protocol.Start());

    while (!protocol.IsFinished)
    {
        string? message = Receive(stream);
        if (message == null)
        {
            Console.WriteLine("Client disconnected.");
            break;
        }

        Console.WriteLine($"CLIENT: {message}");
        Send(stream, protocol.HandleInput(message));
    }

    Console.WriteLine("Conversation finished.\n");
}

static void Send(NetworkStream stream, string message)
{
    Console.WriteLine($"SERVER: {message}");
    byte[] data = Encoding.UTF8.GetBytes(message);
    stream.Write(data, 0, data.Length);
}

static string? Receive(NetworkStream stream)
{
    byte[] buffer = new byte[1024];
    int bytesRead = stream.Read(buffer, 0, buffer.Length);
    if (bytesRead == 0)
        return null;

    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
}
