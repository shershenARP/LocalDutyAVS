using Content.Server._Duty.Weapons;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Throwing;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Duty.Weapons;

public sealed class BoomerangItemSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    // Имя throwing fixture из ThrownItemSystem
    private const string ThrowingFixture = "throw-fixture";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BoomerangItemComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<BoomerangItemComponent, StartCollideEvent>(OnCollide);
    }

    // ── Бросок ───────────────────────────────────────────────────────────────

    private void OnThrown(EntityUid uid, BoomerangItemComponent comp, ref ThrownEvent args)
    {
        if (comp.IsReturning)
            return;

        comp.Thrower = args.User;
        comp.ReturnAt = _timing.CurTime + TimeSpan.FromSeconds(comp.ReturnDelay);
        comp.DamageCooldown = 0f;
        comp.IsReturning = false;
        comp.WaitingForReturn = true;

        if (TryComp<PhysicsComponent>(uid, out var body))
            _physics.SetAngularVelocity(uid, MathHelper.DegreesToRadians(comp.SpinStrength), body: body);

        if (comp.FlightSound != null)
        {
            comp.FlightSoundEntity = _audio.PlayPvs(
                comp.FlightSound, uid,
                AudioParams.Default.WithLoop(true).WithVolume(-3f))?.Entity;
        }
    }

    // ── Коллизия ─────────────────────────────────────────────────────────────

    private void OnCollide(EntityUid uid, BoomerangItemComponent comp, ref StartCollideEvent args)
    {
        // Нас интересует только throwing fixture — проверяем через FixtureSystem
        if (!TryComp<FixturesComponent>(uid, out var fixturesComp))
            return;
        var throwingFixture = _fixtures.GetFixtureOrNull(uid, ThrowingFixture, manager: fixturesComp);
        if (throwingFixture == null || args.OurFixture != throwingFixture)
            return;

        if (!comp.WaitingForReturn && !comp.IsReturning)
            return;

        var other = args.OtherEntity;

        // ── Поимка на обратном пути ──────────────────────────────────────────
        if (comp.IsReturning && other == comp.Thrower)
        {
            // Останавливаем бросок корректно через ThrownItemSystem
            if (TryComp<ThrownItemComponent>(uid, out var thrownComp))
                _thrownItem.StopThrow(uid, thrownComp);

            // Кладём в руку без проверки action blocker (игрок может быть в стане)
            _hands.TryPickupAnyHand(other, uid, checkActionBlocker: false, animate: false);

            ResetBoomerang(uid, comp);
            return;
        }

        // ── Не бить бросателя на пути «туда» ────────────────────────────────
        if (other == comp.Thrower)
            return;

        // ── Кулдаун урона ────────────────────────────────────────────────────
        if (comp.DamageCooldown > 0f)
            return;

        comp.DamageCooldown = 1f / comp.DamageRate;

        if (comp.ContactDamage.AnyPositive())
            _damageable.TryChangeDamage(other, comp.ContactDamage, origin: comp.Thrower);

        if (comp.HitSound != null)
            _audio.PlayPvs(comp.HitSound, uid);
    }

    // ── Update: ждём момента возврата и ведём предмет ────────────────────────

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BoomerangItemComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DamageCooldown > 0f)
                comp.DamageCooldown -= frameTime;

            if (!comp.WaitingForReturn && !comp.IsReturning)
                continue;

            if (comp.Thrower == null || TerminatingOrDeleted(comp.Thrower.Value))
            {
                ResetBoomerang(uid, comp);
                continue;
            }

            if (comp.WaitingForReturn && _timing.CurTime >= comp.ReturnAt)
                StartReturn(uid, comp);

            if (comp.IsReturning && TryComp<PhysicsComponent>(uid, out var body))
            {
                var targetPos = _transform.GetWorldPosition(comp.Thrower.Value);
                var currentPos = _transform.GetWorldPosition(uid);
                var delta = targetPos - currentPos;

                // Страховочная поимка по дистанции
                if (delta.Length() < 0.35f)
                {
                    _hands.TryPickupAnyHand(comp.Thrower.Value, uid, checkActionBlocker: false, animate: false);
                    ResetBoomerang(uid, comp);
                    continue;
                }

                _physics.SetLinearVelocity(uid, delta.Normalized() * comp.ReturnSpeed, body: body);
            }
        }
    }

    // ── Запуск обратного броска ───────────────────────────────────────────────

    private void StartReturn(EntityUid uid, BoomerangItemComponent comp)
    {
        comp.WaitingForReturn = false;
        comp.IsReturning = true;

        if (comp.Thrower == null)
            return;

        var targetPos = _transform.GetWorldPosition(comp.Thrower.Value);
        var currentPos = _transform.GetWorldPosition(uid);
        var direction = targetPos - currentPos;

        if (direction == System.Numerics.Vector2.Zero)
            return;

        _throwing.TryThrow(uid, direction, comp.ReturnSpeed, comp.Thrower, doSpin: false);
    }

    // ── Сброс ────────────────────────────────────────────────────────────────

    private void ResetBoomerang(EntityUid uid, BoomerangItemComponent comp)
    {
        comp.Thrower = null;
        comp.WaitingForReturn = false;
        comp.IsReturning = false;
        comp.DamageCooldown = 0f;

        if (TryComp<PhysicsComponent>(uid, out var body))
            _physics.SetAngularVelocity(uid, 0f, body: body);

        if (comp.FlightSoundEntity != null)
        {
            if (!TerminatingOrDeleted(comp.FlightSoundEntity.Value))
                QueueDel(comp.FlightSoundEntity.Value);
            comp.FlightSoundEntity = null;
        }
    }
}
