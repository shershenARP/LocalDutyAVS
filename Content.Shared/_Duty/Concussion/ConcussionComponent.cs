using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Duty.Concussion;

/// <summary>
/// _Duty: «шкала контузии» сущности. Накапливается от близких выстрелов и взрывов,
/// плавно затухает со временем. Высокая шкала = звон в ушах + затемнение экрана.
/// Текущее значение считается лениво из <see cref="Level"/> и <see cref="LastUpdate"/>
/// (см. <see cref="SharedConcussionSystem.GetCurrentLevel"/>), поэтому компонент НЕ
/// дёргается каждый тик — Dirty() вызывается только когда что-то добавилось.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConcussionComponent : Component
{
    /// <summary>Значение шкалы на момент <see cref="LastUpdate"/> (до затухания).</summary>
    [DataField, AutoNetworkedField]
    public float Level;

    /// <summary>Момент, на который было зафиксировано <see cref="Level"/>.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LastUpdate;

    /// <summary>Потолок шкалы.</summary>
    [DataField]
    public float MaxLevel = 100f;

    /// <summary>Скорость затухания шкалы (единиц в секунду).</summary>
    [DataField]
    public float DecayPerSecond = 3f;

    // ── Детекция выстрелов ──────────────────────────────────────────────────
    /// <summary>Базовый прирост шкалы от одного выстрела рядом (в эпицентре).</summary>
    [DataField]
    public float ShotAmount = 2.5f;

    /// <summary>Радиус (тайлы), в котором выстрел вообще влияет.</summary>
    [DataField]
    public float ShotRange = 5f;

    /// <summary>Минимальный множитель прироста на краю радиуса (фолл-офф не падает в ноль).</summary>
    [DataField]
    public float ShotMinFalloff = 0.25f;

    // ── Детекция взрывов ────────────────────────────────────────────────────
    /// <summary>Базовый прирост от взрыва (при эталонном уроне).</summary>
    [DataField]
    public float BlastAmount = 30f;

    /// <summary>Урон взрыва по сущности, дающий полный <see cref="BlastAmount"/>.</summary>
    [DataField]
    public float BlastReferenceDamage = 40f;

    /// <summary>Кап прироста от одного взрыва.</summary>
    [DataField]
    public float BlastMaxAmount = 60f;

    // ── Головокружение (dizzy, качание/мутность экрана) ─────────────────────
    /// <summary>Урон взрыва, при котором головокружение гарантированно (большой взрыв).</summary>
    [DataField]
    public float DizzyBlastDamage = 35f;

    /// <summary>Доля заполнения шкалы (0..1), при которой ЛЮБОЙ взрыв рядом вызывает головокружение.</summary>
    [DataField]
    public float DizzyNearbyLevelFraction = 0.65f;

    // ── Представление ───────────────────────────────────────────────────────
    /// <summary>Ниже этого значения шкала считается «нулевой» (бар скрывается через 5с).</summary>
    [DataField]
    public float MinVisibleLevel = 1f;

    /// <summary>Алерт-полоска справа.</summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "Concussion";

    /// <summary>Звон в ушах (зацикленный). Меняй на свой ассет в Audio/_Duty/Concussion.</summary>
    [DataField]
    public SoundSpecifier? RingSound = new SoundPathSpecifier("/Audio/_Duty/Effects/Tinnitus/tinnitus.ogg");

    /// <summary>С какого уровня шкалы начинает звенеть в ушах.</summary>
    [DataField]
    public float RingStartLevel = 25f;

    // ── «Ватность» звука (low-pass) ──────────────────────────────────────────
    /// <summary>С какого уровня шкалы звук начинает глохнуть (low-pass).</summary>
    [DataField]
    public float MuffleStartLevel = 15f;

    /// <summary>
    /// Максимальная окклюзия (low-pass) на пике шкалы. Движок мапит её в фильтр как
    /// cutoff = exp(-occlusion): ~1.5 — заметно «ватно», ~2.5 — как вата в ушах.
    /// </summary>
    [DataField]
    public float MuffleMaxOcclusion = 2.5f;

    // ── Серверная служебка (не сетится) ─────────────────────────────────────
    /// <summary>Последняя показанная стадия алерта (серверный кэш, чтобы не спамить ShowAlert).</summary>
    [ViewVariables]
    public short? ShownSeverity;

    /// <summary>С какого момента шкала держится на нуле (для скрытия бара через 5с).</summary>
    [ViewVariables]
    public TimeSpan? ZeroSince;
}
