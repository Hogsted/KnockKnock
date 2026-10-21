using System.Net.Sockets;
using System.Text;

using TcpClient client = new TcpClient("localhost", 5000);
using NetworkStream stream = client.GetStream();
Console.WriteLine("Connected to Knock Knock Server!\n");

while (true)
{
    byte[] buffer = new byte[1024];
    int bytesRead = stream.Read(buffer, 0, buffer.Length);
    if (bytesRead == 0)
        break;

    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    Console.WriteLine($"SERVER: {message}");

    // Serveren afslutter joken med "Bye!"
    if (message.EndsWith("Bye!"))
        break;

    string answer = "";
    while (string.IsNullOrWhiteSpace(answer))
    {
        Console.Write("Your answer: ");
        answer = Console.ReadLine() ?? throw new InvalidOperationException("No input");
    }
    byte[] data = Encoding.UTF8.GetBytes(answer);
    stream.Write(data, 0, data.Length);
}

Console.WriteLine("\nConnection closed.");
