namespace KnockKnockServer;

// Indeholder reglerne for samtalen - ved intet om TCP.
// Hver klient får sin egen instans, så tilstanden deles ikke mellem klienter.
class KnockKnockProtocol : IConversation
{
    private readonly KnockKnockJoke _joke;
    private ConversationState _state = ConversationState.WaitingForWhosThere;

    public KnockKnockProtocol(KnockKnockJoke joke)
    {
        _joke = joke;
    }

    public bool IsFinished => _state == ConversationState.Finished;

    public string Start() => "Knock! Knock!";

    public string HandleInput(string input)
    {
        input = input.Trim();

        switch (_state)
        {
            case ConversationState.WaitingForWhosThere:
                if (input.Equals("Who's there?", StringComparison.OrdinalIgnoreCase))
                {
                    _state = ConversationState.WaitingForWho;
                    return _joke.Who;
                }
                // Forkert svar: tilstanden ændres ikke
                return "You are supposed to say: Who's there?";

            case ConversationState.WaitingForWho:
                string expected = $"{_joke.Who} who?";
                if (input.Equals(expected, StringComparison.OrdinalIgnoreCase))
                {
                    _state = ConversationState.Finished;
                    return _joke.Punchline;
                }
                return $"You are supposed to say: {expected}";

            case ConversationState.Finished:
            default:
                return "The joke is over.";
        }
    }
}
