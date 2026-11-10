namespace KnockKnockServer;

// Kobler valg af vittighed (IJokeSelector) sammen med samtalens regler (KnockKnockProtocol).
// Hvilken strategi der bruges, bestemmes af konfigurationen - ikke af denne klasse.
class KnockKnockConversationFactory : IConversationFactory
{
    private readonly IJokeSelector _jokeSelector;

    public KnockKnockConversationFactory(IJokeSelector jokeSelector)
    {
        _jokeSelector = jokeSelector;
    }

    public IConversation Create() => new KnockKnockProtocol(_jokeSelector.SelectJoke());
}
