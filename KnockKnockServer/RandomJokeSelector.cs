using Microsoft.Extensions.Options;

namespace KnockKnockServer;

// Strategi: vælger en tilfældig vittighed
class RandomJokeSelector : IJokeSelector
{
    private readonly KnockKnockJoke[] _jokes;

    public RandomJokeSelector(IOptions<JokeOptions> options)
    {
        _jokes = options.Value.Jokes.ToArray();
    }

    // Random.Shared er trådsikker, så der er ikke behov for en lock
    public KnockKnockJoke SelectJoke() => _jokes[Random.Shared.Next(_jokes.Length)];
}
