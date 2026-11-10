namespace KnockKnockServer;

// Det ClientSession har brug for at kende til en samtale - hverken mere eller mindre
interface IConversation
{
    bool IsFinished { get; }
    string Start();
    string HandleInput(string input);
}
