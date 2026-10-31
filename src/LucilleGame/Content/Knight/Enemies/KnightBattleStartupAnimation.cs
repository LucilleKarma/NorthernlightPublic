using System.Numerics;
using Northernlight.Content.Borders;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Enemies;

/// <summary>
///     Represents a kind of custom startup
///     animation that can be played before
///     a battle properly begins.
/// </summary>
public sealed class KnightBattleStartupAnimation : BattleStartupAnimation<RoaringKnight>
{
    public enum State
    {
        WaitingForKnightToShowUp,
        WaitingForKnightToRaiseUpTheirBat
    }

    private bool playedDashSound;

    /// <summary>
    ///     The current stsate of this animation.
    /// </summary>
    public State CurrentState
    {
        get;
        private set;
    }

    /// <summary>
    ///     The timer responsible for this animation's states.
    /// </summary>
    public OwnedTimer StateTimer
    {
        get;
        private set;
    }

    public KnightBattleStartupAnimation(RoaringKnight owner) : base(owner)
    {
    }

    private void MakeKnightShowUp(BattleHandler battle, Scene scene)
    {
        if (!playedDashSound)
        {
            playedDashSound = true;
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("KnightAppearanceDash.wav")));
        }

        battle.Soul.StayDormant(battle.DormantSoulPosition);

        Vector3 idealPosition = Owner.RestingPosition;
        Vector3 idealVelocity = (idealPosition - Owner.Position) * 2f;
        Owner.Velocity = Vector3.LerpDt(Owner.Velocity, idealVelocity, 3f, scene.Time);
        Owner.RepositionDelay = 0.2f;
        Owner.EmitsAfterimages = false;

        if (Vector3.Distance(idealPosition, Owner.Position) < 700f)
            Owner.SetTexture(Atlases.KnightScene.KnightFlyingTransition, 17, loops: false, animatesInReverse: true);
        else
            Owner.SetTexture(Atlases.KnightScene.KnightFlyingAnimation, 4);

        bool flewPastDestination = Vector3.Dot(Owner.Velocity, idealPosition - Owner.Position) < 0f;
        if (flewPastDestination)
        {
            Owner.Velocity *= 0.05f;
            StateTimer.Reset();
            CurrentState = State.WaitingForKnightToRaiseUpTheirBat;
        }
    }

    private bool MakeKnightRaiseUpTheirBat(BattleHandler battle, Scene scene)
    {
        Vector3 idealPosition = Owner.RestingPosition;
        Vector3 idealVelocity = (idealPosition - Owner.Position) * 3f;
        Owner.Velocity = Vector3.LerpDt(Owner.Velocity, idealVelocity, 4f, scene.Time);
        Owner.RepositionDelay = 0.2f;
        Owner.EmitsAfterimages = false;

        battle.Soul.StayDormant(battle.DormantSoulPosition);

        if (Owner.RenderState.Texture == Atlases.KnightScene.KnightFlyingAnimation)
            Owner.SetTexture(Atlases.KnightScene.KnightFlyingTransition, 17, loops: false, animatesInReverse: true);
        if (Owner.RenderState.Texture == Atlases.KnightScene.KnightFlyingTransition && Owner.RenderState.CurrentFrame == 0 && Owner.AnimationTimer >= 0.1f)
            Owner.SetTexture(Atlases.KnightScene.KnightSwordMaterializationAnimation, 19, loops: false);
        if (Owner.RenderState.Texture == Atlases.KnightScene.KnightSwordMaterializationAnimation && Owner.RenderState.CurrentFrame == Owner.RenderState.TotalFrames - 4)
        {
            StateTimer.Reset();
            Owner.Velocity = Vector3.Zero;

            KnightBorder.Instance.ShineIntensityFactor = 4f;
            KnightBorder.Instance.TwinkleSpeedupFactor = 5f;
            return true;
        }

        return false;
    }

    public override bool Update(BattleHandler battle, Scene scene)
    {
        StateTimer ??= scene.Time.Rent();

        switch (CurrentState)
        {
            case State.WaitingForKnightToShowUp:
                MakeKnightShowUp(battle, scene);
                break;
            case State.WaitingForKnightToRaiseUpTheirBat:
                return MakeKnightRaiseUpTheirBat(battle, scene);
        }
        return false;
    }
}
