using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Duty.HealthPhrases;

/// <summary>
/// Компонент системы атмосферы боли.
/// Хранит пользовательские фразы и состояние таймеров.
/// Добавляется на сущность при спавне через humanoid profiles.
/// </summary>
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class HealthPhrasesComponent : Component
{
    // ─── Пользовательские фразы (из персонализации) ───────────────────────────

    /// <summary>
    /// Фразы при 40–50% HP. Только popup.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<string> CustomPhrases50 = new();

    /// <summary>
    /// Фразы при 35–40% HP. Только popup.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<string> CustomPhrases40 = new();

    /// <summary>
    /// Фразы при 25–35% HP. Только popup.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<string> CustomPhrases35 = new();

    /// <summary>
    /// Фразы при 15–25% HP. Popup + whisper.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<string> CustomPhrases25 = new();

    /// <summary>
    /// Фразы при 10–15% HP. Popup + whisper + say.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<string> CustomPhrases15 = new();

    /// <summary>
    /// Фразы при 5–10% HP. Popup + whisper + say.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<string> CustomPhrases10 = new();

    /// <summary>
    /// Фразы при 0–5% HP. Popup + whisper + say.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<string> CustomPhrases5 = new();

    // ─── Состояние таймеров ────────────────────────────────────────────────────

    /// <summary>
    /// Когда можно следующий раз выдать popup-фразу.
    /// Рандом 40–180 сек.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextPopupTime = TimeSpan.Zero;

    /// <summary>
    /// Когда можно следующий раз выдать whisper/say фразу.
    /// Рандом 60–300 сек.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextSpeechTime = TimeSpan.Zero;
}

/// <summary>
/// Данные пользовательских фраз — передаются из лобби на сервер при спавне.
/// Хранятся в HumanoidCharacterProfile.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthPhrasesData
{
    public List<string> Phrases50 = new();
    public List<string> Phrases40 = new();
    public List<string> Phrases35 = new();
    public List<string> Phrases25 = new();
    public List<string> Phrases15 = new();
    public List<string> Phrases10 = new();
    public List<string> Phrases5  = new();
}
