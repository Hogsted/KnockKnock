using KnockKnockServer;

namespace KnockKnockServer.Tests;

// Tester samtalelogikken uden TCP-forbindelser
public class KnockKnockProtocolTests
{
    private static KnockKnockProtocol CreateProtocol() =>
        new KnockKnockProtocol(new KnockKnockJoke { Who = "Lettuce", Punchline = "Lettuce in, it's cold out here!" });

    [Fact]
    public void Start_ReturnsKnockKnock()
    {
        Assert.Equal("Knock! Knock!", CreateProtocol().Start());
    }

    [Fact]
    public void CorrectAnswers_FinishTheJoke()
    {
        KnockKnockProtocol protocol = CreateProtocol();
        protocol.Start();

        Assert.Equal("Lettuce", protocol.HandleInput("Who's there?"));
        Assert.Equal("Lettuce in, it's cold out here!", protocol.HandleInput("Lettuce who?"));
        Assert.True(protocol.IsFinished);
    }

    [Fact]
    public void WrongAnswer_KeepsState()
    {
        KnockKnockProtocol protocol = CreateProtocol();
        protocol.Start();

        Assert.Equal("You are supposed to say: Who's there?", protocol.HandleInput("Hello?"));
        Assert.False(protocol.IsFinished);

        // Stadig i samme tilstand, så det korrekte svar virker bagefter
        Assert.Equal("Lettuce", protocol.HandleInput("Who's there?"));
        Assert.Equal("You are supposed to say: Lettuce who?", protocol.HandleInput("Who?"));
        Assert.Equal("Lettuce in, it's cold out here!", protocol.HandleInput("Lettuce who?"));
    }

    [Fact]
    public void Answers_IgnoreCaseAndWhitespace()
    {
        KnockKnockProtocol protocol = CreateProtocol();
        protocol.Start();

        Assert.Equal("Lettuce", protocol.HandleInput("  who's THERE?  "));
    }

    [Fact]
    public void TwoProtocols_HaveIndependentState()
    {
        KnockKnockProtocol first = CreateProtocol();
        KnockKnockProtocol second = CreateProtocol();

        first.HandleInput("Who's there?");
        first.HandleInput("Lettuce who?");

        Assert.True(first.IsFinished);
        Assert.False(second.IsFinished);
    }
}
