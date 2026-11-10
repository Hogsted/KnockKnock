namespace KnockKnockServer;

// Opretter en ny, uafhængig samtale til hver klient
interface IConversationFactory
{
    IConversation Create();
}
