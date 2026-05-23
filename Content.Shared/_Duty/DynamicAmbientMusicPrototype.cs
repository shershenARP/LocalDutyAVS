    // SPDX-FileCopyrightText: 2025 LocalDuty <https://github.com/Bebranot/LocalDuty_Reserve>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Duty.Audio;

/// <summary>Трек для крит. состояния с явно заданной длительностью.</summary>
[DataDefinition]
public sealed partial class DutyCritTrack
{
    /// <summary>Путь к аудиофайлу.</summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>Длительность трека в секундах.</summary>
    [DataField(required: true)]
    public float Duration;
}

[Prototype("dynamicAmbientMusic")]
public sealed class DynamicAmbientMusicPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>Очень хорошее состояние: 90–100% HP.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> TracksVeryGood = new();

    /// <summary>Хорошее состояние: 70–90% HP.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> TracksGood = new();

    /// <summary>Среднее состояние: 40–70% HP.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> TracksMedium = new();

    /// <summary>Ниже среднего: 25–40% HP.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> TracksBelowMedium = new();

    /// <summary>Ужасное состояние: 5–25% HP.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> TracksAwful = new();

    /// <summary>Критическое состояние по HP: менее 5% HP.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> TracksCritical = new();

    /// <summary>MobState.Critical — персонаж лежит без сознания. Треки с длительностью для плавных переходов.</summary>
    [DataField(required: true)]
    public List<DutyCritTrack> TracksMobCritical = new();

    /// <summary>Усиление громкости для TracksMobCritical (в dB, положительное = громче).</summary>
    [DataField]
    public float MobCritVolumeBoost = 6f;

    /// <summary>Длительность fade-out и fade-in при переходе между крит. треками (сек).</summary>
    [DataField]
    public float MobCritCrossfadeDuration = 10f;

    /// <summary>Боевая музыка — играет в петле при боевом режиме.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> CombatTracks = new();

    /// <summary>Боевая музыка при низком HP — играет в петле при боевом режиме + HP меньше порога.</summary>
    [DataField(required: true)]
    public List<SoundSpecifier> CombatLowTracks = new();

    /// <summary>Порог HP (%) для переключения на CombatLowTracks в боевом режиме.</summary>
    [DataField]
    public float CombatLowHpThreshold = 10f;

    /// <summary>Одиночный звук при смерти персонажа.</summary>
    [DataField]
    public SoundSpecifier? DeathSound = null;

    [DataField] public float CalmMinInterval = 5f;
    [DataField] public float CalmMaxInterval = 50f;
    [DataField] public float CalmFadeInDuration = 2.5f;
    [DataField] public float CalmFadeOutDuration = 3.5f;
    [DataField] public float StateTransitionPause = 1.5f;
    [DataField] public float CombatFadeOutDuration = 1.5f;
    [DataField] public float CombatFadeInDuration = 0.5f;
}
