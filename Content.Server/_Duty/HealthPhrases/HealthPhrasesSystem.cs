using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.FixedPoint;
using Content.Shared._Duty.HealthPhrases;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Duty.HealthPhrases;

/// <summary>
/// Система атмосферы боли.
/// Раз в Update проверяет HP гуманоидов с компонентом HealthPhrasesComponent
/// и выдаёт реплики/попапы в зависимости от уровня здоровья.
/// </summary>
public sealed class HealthPhrasesSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Цвет popup-сообщений (#DC143C — красный)
    private static readonly Color PopupColor = Color.FromHex("#DC143C");

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HealthPhrasesComponent, DamageableComponent, HumanoidAppearanceComponent, MobStateComponent>();

        while (query.MoveNext(out var uid, out var phrases, out var damageable, out var humanoid, out var mobState))
        {
            // Только живые (не в крите, не мёртвые)
            if (mobState.CurrentState != MobState.Alive)
                continue;

            if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
                continue;

            // Получаем порог крита
            FixedPoint2 critThreshold = 0;
            foreach (var (threshold, state) in thresholds.Thresholds)
            {
                if (state == MobState.Critical)
                {
                    critThreshold = threshold;
                    break;
                }
            }

            if (critThreshold <= 0)
                continue;

            var totalDamage = (float) damageable.TotalDamage;
            var hpPercent = 1f - (float)(totalDamage / critThreshold);
            hpPercent = Math.Clamp(hpPercent, 0f, 1f);

            var level = GetHpLevel(hpPercent);
            if (level == HpLevel.None)
                continue;

            var raceKey = GetRaceKey(humanoid.Species.Id);

            // ── Popup (40–180 сек кд) ──────────────────────────────────────
            if (now >= phrases.NextPopupTime)
            {
                var popupText = PickPhrase(uid, phrases, level, PhraseType.Popup, raceKey);
                if (popupText != null)
                    _popup.PopupEntity(popupText, uid, uid, PopupType.SmallCaution);

                phrases.NextPopupTime = now + TimeSpan.FromSeconds(_random.NextDouble() * 140 + 40);
            }

            // ── Whisper/Say (60–300 сек кд) — только с уровня 25% ─────────
            if (level >= HpLevel.Level25 && now >= phrases.NextSpeechTime)
            {
                PhraseType speechType;
                if (level >= HpLevel.Level15)
                    speechType = _random.Prob(0.4f) ? PhraseType.Say : PhraseType.Whisper;
                else
                    speechType = PhraseType.Whisper;

                var speechText = PickPhrase(uid, phrases, level, speechType, raceKey);
                if (speechText != null)
                {
                    if (speechType == PhraseType.Whisper)
                        _chat.TrySendInGameICMessage(uid, speechText, InGameICChatType.Whisper, hideChat: true);
                    else
                        _chat.TrySendInGameICMessage(uid, speechText, InGameICChatType.Speak, hideChat: true);
                }

                phrases.NextSpeechTime = now + TimeSpan.FromSeconds(_random.NextDouble() * 240 + 60);
            }
        }
    }

    private string? PickPhrase(EntityUid uid, HealthPhrasesComponent comp, HpLevel level, PhraseType type, string raceKey)
    {
        // 1. Пользовательские фразы
        var customList = GetCustomList(comp, level);
        if (customList.Count > 0)
            return _random.Pick(customList);

        // 2. Расовые фразы из ftl
        var ftlKey = BuildFtlKey(raceKey, level, type);
        if (TryPickFtlPhrase(ftlKey, out var racialPhrase))
            return racialPhrase;

        // 3. Общие фразы из ftl (fallback)
        var generalKey = BuildFtlKey("general", level, type);
        if (TryPickFtlPhrase(generalKey, out var generalPhrase))
            return generalPhrase;

        return null;
    }

    private bool TryPickFtlPhrase(string baseKey, out string? result)
    {
        var variants = new List<string>();
        for (var i = 1; i <= 10; i++)
        {
            var key = $"{baseKey}-{i}";
            if (Loc.TryGetString(key, out var str))
                variants.Add(str);
        }

        if (variants.Count == 0)
        {
            result = null;
            return false;
        }

        result = _random.Pick(variants);
        return true;
    }

    private string BuildFtlKey(string race, HpLevel level, PhraseType type)
    {
        var levelStr = level switch
        {
            HpLevel.Level50 => "50",
            HpLevel.Level40 => "40",
            HpLevel.Level35 => "35",
            HpLevel.Level25 => "25",
            HpLevel.Level15 => "15",
            HpLevel.Level10 => "10",
            HpLevel.Level5  => "5",
            _ => "50"
        };

        var typeStr = type switch
        {
            PhraseType.Popup   => "popup",
            PhraseType.Whisper => "whisper",
            PhraseType.Say     => "say",
            _ => "popup"
        };

        return $"duty-health-phrases-{race}-{levelStr}-{typeStr}";
    }

    private List<string> GetCustomList(HealthPhrasesComponent comp, HpLevel level) => level switch
    {
        HpLevel.Level50 => comp.CustomPhrases50,
        HpLevel.Level40 => comp.CustomPhrases40,
        HpLevel.Level35 => comp.CustomPhrases35,
        HpLevel.Level25 => comp.CustomPhrases25,
        HpLevel.Level15 => comp.CustomPhrases15,
        HpLevel.Level10 => comp.CustomPhrases10,
        HpLevel.Level5  => comp.CustomPhrases5,
        _ => new List<string>()
    };

    private HpLevel GetHpLevel(float hpPercent) => hpPercent switch
    {
        >= 0.40f and < 0.50f => HpLevel.Level50,
        >= 0.35f and < 0.40f => HpLevel.Level40,
        >= 0.25f and < 0.35f => HpLevel.Level35,
        >= 0.15f and < 0.25f => HpLevel.Level25,
        >= 0.10f and < 0.15f => HpLevel.Level15,
        >= 0.05f and < 0.10f => HpLevel.Level10,
        >= 0.00f and < 0.05f => HpLevel.Level5,
        _ => HpLevel.None
    };

    /// <summary>
    /// Возвращает ключ расы для ftl по species ID.
    /// Если раса не особая — возвращает "general" (fallback).
    /// </summary>
    private string GetRaceKey(string speciesId) => speciesId switch
    {
        "MobReptilian" => "unath",
        "MobResomi"    => "rezomi",
        "MobMoth"      => "nian",
        "MobDrask"     => "drask",
        _ => "general"
    };
}

public enum HpLevel
{
    None,
    Level50, // 40–50%
    Level40, // 35–40%
    Level35, // 25–35%
    Level25, // 15–25%
    Level15, // 10–15%
    Level10, // 5–10%
    Level5,  // 0–5%
}

public enum PhraseType
{
    Popup,
    Whisper,
    Say,
}
