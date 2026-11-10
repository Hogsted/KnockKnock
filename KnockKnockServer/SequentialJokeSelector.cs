using Microsoft.Extensions.Options;

namespace KnockKnockServer;

// Strategi: vælger vittighederne i rækkefølge og starter forfra efter den sidste
class SequentialJokeSelector : IJokeSelector
{
    private readonly KnockKnockJoke[] _jokes;
    private readonly object _lock = new();
    private int _nextIndex;

    public SequentialJokeSelector(IOptions<JokeOptions> options)
    {
        _jokes = options.Value.Jokes.ToArray();
    }

    public KnockKnockJoke SelectJoke()
    {
        // _nextIndex deles mellem alle klienttråde. Uden lock kunne to klienter
        // læse samme værdi og få samme vittighed (race condition).
        lock (_lock)
        {
            KnockKnockJoke joke = _jokes[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _jokes.Length;
            return joke;
        }
    }
}
