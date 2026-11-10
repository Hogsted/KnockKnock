namespace KnockKnockServer;

// Strategy Pattern: fælles interface for alle måder at vælge en vittighed på.
// Implementeringer skal være trådsikre, da flere klienter kan kalde SelectJoke() samtidig.
interface IJokeSelector
{
    KnockKnockJoke SelectJoke();
}
