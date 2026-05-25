using Content.Shared._Duty.HealthPhrases;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    [DataField]
    public HealthPhrasesData HealthPhrases { get; private set; } = new();

    public HealthPhrasesData GetHealthPhrases() => HealthPhrases;

    public HumanoidCharacterProfile WithHealthPhrases(HealthPhrasesData data)
    {
        return new(this) { HealthPhrases = data.Clone() };
    }
}
