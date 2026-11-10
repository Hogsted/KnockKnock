using System.Collections.Concurrent;
using KnockKnockServer;
using Microsoft.Extensions.Options;

namespace KnockKnockServer.Tests;

public class JokeSelectorTests
{
    private static IOptions<JokeOptions> CreateJokes(int count) => Options.Create(new JokeOptions
    {
        Jokes = Enumerable.Range(1, count)
            .Select(i => new KnockKnockJoke { Who = $"Who {i}", Punchline = $"Punchline {i}" })
            .ToList()
    });

    [Fact]
    public void Sequential_SelectsInOrderAndStartsOver()
    {
        SequentialJokeSelector selector = new SequentialJokeSelector(CreateJokes(3));

        string[] selected = Enumerable.Range(0, 5).Select(_ => selector.SelectJoke().Who).ToArray();

        Assert.Equal(new[] { "Who 1", "Who 2", "Who 3", "Who 1", "Who 2" }, selected);
    }

    [Fact]
    public void Sequential_IsThreadSafe()
    {
        SequentialJokeSelector selector = new SequentialJokeSelector(CreateJokes(5));
        ConcurrentBag<string> selected = new();

        // 5000 samtidige valg skal fordele sig præcis 1000 på hver vittighed.
        // Uden lock i SelectJoke() fejler testen typisk, fordi tråde læser samme index.
        Parallel.For(0, 5000, _ => selected.Add(selector.SelectJoke().Who));

        Assert.All(selected.GroupBy(who => who), group => Assert.Equal(1000, group.Count()));
    }

    [Fact]
    public void Random_OnlySelectsConfiguredJokes()
    {
        IOptions<JokeOptions> jokes = CreateJokes(3);
        RandomJokeSelector selector = new RandomJokeSelector(jokes);

        for (int i = 0; i < 100; i++)
            Assert.Contains(selector.SelectJoke(), jokes.Value.Jokes);
    }
}
