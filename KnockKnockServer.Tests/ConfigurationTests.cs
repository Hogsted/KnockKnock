using KnockKnockServer;

namespace KnockKnockServer.Tests;

// Kører serverens rigtige Main() med ugyldige værdier, der overskriver appsettings.json
public class ConfigurationTests
{
    [Theory]
    [InlineData("--Server:Port=0", "Server:Port skal være et tal mellem 1 og 65535.")]
    [InlineData("--Server:MaxClients=-1", "Server:MaxClients skal være et tal mellem 1 og 1000.")]
    [InlineData("--JokeSelection:Strategy=Foo", "JokeSelection:Strategy er ukendt.")]
    [InlineData("--Server:Port=abc", "Failed to convert configuration value at 'Server:Port'")]
    public void InvalidConfiguration_StopsWithReadableError(string argument, string expectedError)
    {
        StringWriter error = new StringWriter();
        TextWriter originalError = Console.Error;
        Console.SetError(error);
        try
        {
            int exitCode = RunServerMain(argument);

            Assert.Equal(1, exitCode);
            Assert.Contains("Ugyldig konfiguration i appsettings.json", error.ToString());
            Assert.Contains(expectedError, error.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    private static int RunServerMain(params string[] args)
    {
        var entryPoint = typeof(ServerOptions).Assembly.EntryPoint!;
        return (int)entryPoint.Invoke(null, new object[] { args })!;
    }
}
