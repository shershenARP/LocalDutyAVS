using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Server._Duty.Weapons;

[RegisterComponent]
public sealed partial class BoomerangItemComponent : Component
{
    // ───────────────── Настраиваемые параметры ─────────────────

    /// <summary>Скорость возврата, тайлов/сек.</summary>
    [DataField]
    public float ReturnSpeed = 10f;

    /// <summary>Задержка перед возвратом, сек.</summary>
    [DataField]
    public float ReturnDelay = 1.2f;

    /// <summary>Урон за один тик касания.</summary>
    [DataField]
    public DamageSpecifier ContactDamage = new();

    /// <summary>Частота урона (атак/сек), аналог AttackRate у MeleeWeaponComponent.</summary>
    [DataField]
    public float DamageRate = 1.5f;

    /// <summary>Угловая скорость вращения в полёте, градусов/сек.</summary>
    [DataField]
    public float SpinStrength = 720f;

    /// <summary>Зацикленный звук во время полёта.</summary>
    [DataField]
    public SoundSpecifier? FlightSound = null;

    /// <summary>Звук при каждом тике контактного урона.</summary>
    [DataField]
    public SoundSpecifier? HitSound = null;

    // ───────────────── Runtime-состояние ───────────────────────

    [DataField]
    public EntityUid? Thrower = null;

    /// <summary>Момент времени, когда нужно начать обратный полёт.</summary>
    [DataField]
    public TimeSpan ReturnAt = TimeSpan.Zero;

    /// <summary>Ждём момента возврата (первый бросок завершён, ещё не летим обратно).</summary>
    [DataField]
    public bool WaitingForReturn = false;

    /// <summary>Активна фаза возврата к бросателю.</summary>
    [DataField]
    public bool IsReturning = false;

    [DataField]
    public float DamageCooldown = 0f;

    [DataField]
    public EntityUid? FlightSoundEntity = null;
}
