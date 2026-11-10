namespace KnockKnockServer;

// Stærkt typet udgave af "Jokes"-listen i appsettings.json
class JokeOptions
{
    public List<KnockKnockJoke> Jokes { get; set; } = new();
}
