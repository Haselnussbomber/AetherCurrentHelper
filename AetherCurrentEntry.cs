using Lumina.Excel.GeneratedSheets;

namespace AetherCurrentHelper;

public enum AetherCurrentType
{
    Object,
    Quest
}

public record struct AetherCurrentEntry
{
    public AetherCurrent Entry { get; set; }
    public AetherCurrentType Type { get; set; }
    public Level? Level { get; set; }
    public string? ObjectName { get; set; }
    public string? QuestName { get; set; }
    public string? QuestIssuerName { get; set; }
    public bool IsUnlocked => Service.GameFunctions.IsAetherCurrentUnlocked(Entry.RowId);
}
