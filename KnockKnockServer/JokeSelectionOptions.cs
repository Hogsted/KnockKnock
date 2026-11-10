using System.ComponentModel.DataAnnotations;

namespace KnockKnockServer;

// Stærkt typet udgave af "JokeSelection"-sektionen i appsettings.json
class JokeSelectionOptions
{
    public const string SectionName = "JokeSelection";

    // Navnet som strategien er registreret under i Program.cs, fx "Random" eller "Sequential"
    [Required(ErrorMessage = "JokeSelection:Strategy skal angives.")]
    public string Strategy { get; set; } = "";
}
