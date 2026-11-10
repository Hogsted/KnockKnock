using System.Net.Sockets;
using System.Text;

// Brug: KnockKnockClient [--auto] [--wrong] [--port 5000]
//   --auto   svarer automatisk (bruges til at demonstrere flere samtidige klienter)
//   --wrong  sender først et forkert svar (kun sammen med --auto)
bool auto = args.Contains("--auto");
bool sendWrongAnswer = args.Contains("--wrong");
int portIndex = Array.IndexOf(args, "--port");
int port = portIndex >= 0 ? int.Parse(args[portIndex + 1]) : 5000;

using TcpClient client = new TcpClient("localhost", port);
NetworkStream stream = client.GetStream();

// Én UTF-8-kodet besked pr. linje
StreamReader reader = new StreamReader(stream, Encoding.UTF8);
StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
Console.WriteLine("Connected to Knock Knock Server!\n");

if (!auto)
{
    // Brugerens input læses på en baggrundstråd, så hovedtråden altid kan modtage beskeder.
    // Baggrundstråden stopper automatisk, når hovedtråden er færdig.
    new Thread(() =>
    {
        while (Console.ReadLine() is string answer)
        {
            if (!string.IsNullOrWhiteSpace(answer))
                writer.WriteLine(answer);
        }
    }) { IsBackground = true }.Start();
}

string? lastAnswer = null;
try
{
    while (reader.ReadLine() is string message)
    {
        Console.WriteLine($"SERVER: {message}");

        // Serveren afslutter altid med "Bye!"
        if (message == "Bye!")
            break;

        if (auto)
        {
            string? answer = message switch
            {
                _ when sendWrongAnswer => "Hello?",
                "Knock! Knock!" or "You are supposed to say: Who's there?" => "Who's there?",
                _ when message.StartsWith("You are supposed to say: ") => message["You are supposed to say: ".Length..],
                _ when lastAnswer == "Who's there?" => $"{message} who?",
                _ => null
            };
            sendWrongAnswer = false;

            if (answer != null)
            {
                // Lille pause, så det er tydeligt at flere samtaler kører samtidig
                Thread.Sleep(Random.Shared.Next(500, 1500));
                Console.WriteLine($"CLIENT: {answer}");
                writer.WriteLine(answer);
                lastAnswer = answer;
            }
        }
    }
}
catch (IOException)
{
    // Forbindelsen blev lukket uventet
}

Console.WriteLine("\nConnection closed.");
