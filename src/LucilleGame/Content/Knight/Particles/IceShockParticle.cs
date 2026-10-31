using System.Numerics;
using Northernlight.Content.Borders;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.Mathematics;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight.Particles;

/// <summary>
///     A visual hit particle that displays in the world
///     when the Knight is attacked with ice shock.
/// </summary>
public sealed class IceShockParticle
{
    private sealed class SubParticle
    {
        private readonly OwnedTimer timer;

        /// <summary>
        ///     This particle's relative offset.
        /// </summary>
        public Vector3 RelativeOffset
        {
            get;
            set;
        }

        /// <summary>
        ///     This particle's velocity.
        /// </summary>
        public Vector2 Velocity
        {
            get;
            set;
        }

        /// <summary>
        ///     The owner of this subparticle.
        /// </summary>
        public IceShockParticle Owner
        {
            get;
            set;
        }

        /// <summary>
        ///     The scale of this particle.
        /// </summary>
        public float Scale
        {
            get;
            set;
        }

        /// <summary>
        ///     The opacity of this particle.
        /// </summary>
        public float Opacity
        {
            get;
            set;
        } = 1f;

        /// <summary>
        ///     How long it takes before this particle
        ///     starts fading away.
        /// </summary>
        public float FadeOutTimeThreshold
        {
            get;
            set;
        }

        /// <summary>
        ///     The angular velocity of this particle.
        /// </summary>
        public Angle AngularVelocity
        {
            get;
            set;
        }

        /// <summary>
        ///     The rotation of this particle.
        /// </summary>
        public Angle Rotation
        {
            get;
            set;
        }

        public SubParticle(Game game) => timer = game.Time.Rent();

        /// <summary>
        ///     Updates this particle.
        /// </summary>
        public void Update(KnightFightScene scene)
        {
            Velocity = Velocity.RotatedBy(AngularVelocity * scene.Time.DeltaTime * 1.5f);
            Rotation += AngularVelocity * scene.Time.DeltaTime;

            RelativeOffset += new Vector3(Velocity, 0f) * scene.Time.DeltaTime;

            if (FadeOutTimeThreshold > 0f && timer.Elapsed(FadeOutTimeThreshold))
                Opacity -= scene.Time.DeltaTime * 3f;
        }

        /// <summary>
        ///     Prepares this sub-particle for rendering.
        /// </summary>
        public void PrepareRender(SpriteRenderer sprites)
        {
            AtlasEntry texture = Atlases.KnightScene.IceShockSnowflake;
            sprites.Enqueue(texture, new SpriteData(texture)
            {
                Position = Owner.Position + RelativeOffset,
                Tint = Color.White * Opacity,
                Scale = Vector2.One * Scale
            });
        }
    }

    private OwnedTimer timer;

    private bool spawnedInitialParticleA;

    private bool spawnedInitialParticleB;

    private bool spawnedInitialParticleC;

    private bool splitInitialParticles;

    private bool hitTarget;

    private readonly int Damage;

    private readonly List<SubParticle> subParticles = [];

    /// <summary>
    ///     Whether this particle is dead or not.
    /// </summary>
    public bool Dead => timer >= 1f;

    /// <summary>
    ///     This particle's position in the world.
    /// </summary>
    public Vector3 Position
    {
        get;
        set;
    }

    /// <summary>
    ///     Whether this particle's damage is strong or not.
    /// </summary>
    public bool Strong
    {
        get;
        set;
    }

    public IceShockParticle(int damage) => Damage = damage;

    /// <summary>
    ///     Updates this particle.
    /// </summary>
    public void Update(KnightFightScene scene)
    {
        timer ??= scene.Time.Rent();

        if (!spawnedInitialParticleA && timer.Elapsed(1f / 60f))
        {
            spawnedInitialParticleA = true;
            SpawnMainParticleWithOffset(scene, new Vector2(-50f, -40f));
        }
        if (!spawnedInitialParticleB && timer.Elapsed(4f / 60f))
        {
            spawnedInitialParticleB = true;
            SpawnMainParticleWithOffset(scene, new Vector2(50f, -40f));
        }
        if (!spawnedInitialParticleC && timer.Elapsed(7f / 60f))
        {
            spawnedInitialParticleC = true;
            SpawnMainParticleWithOffset(scene, new Vector2(0f, 40f));
        }

        if (!splitInitialParticles && timer.Elapsed(10f / 60f))
        {
            splitInitialParticles = true;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    Angle theta = Angle.Tau * j / 6f;
                    subParticles.Add(new SubParticle(scene.Game)
                    {
                        RelativeOffset = subParticles[i].RelativeOffset,
                        Owner = this,
                        Velocity = theta.Unit * 240f,
                        AngularVelocity = Angle.Tau * -0.667f,
                        Rotation = theta,
                        Scale = subParticles[i].Scale * 0.75f,
                        FadeOutTimeThreshold = 0.3333f
                    });
                }
            }

            for (int i = 0; i < 3; i++)
                subParticles.RemoveAt(0);
        }

        if (!hitTarget && timer.Elapsed(15f / 60f))
        {
            hitTarget = true;

            scene.AddScreenShake(10f);
            scene.Audio.Play(new Sound(AudioBuffer.FromWav("BasicKnightHit.wav")));
            scene.RoaringKnight.AddShake(20.5f);
            scene.RoaringKnight.Hurt(Damage, Strong);

            if (Strong)
            {
                KnightBorder.Instance.ShineIntensityFactor = 5f;
                KnightBorder.Instance.TwinkleSpeedupFactor = 2.25f;
            }

            scene.RoaringKnight.CreateHurtTextParticle(Damage, scene.Heroes.Noelle.DamageColor);
        }

        foreach (SubParticle particle in subParticles)
            particle.Update(scene);
        subParticles.RemoveAll(p => p.Opacity <= 0f);
    }

    private void SpawnMainParticleWithOffset(KnightFightScene scene, Vector2 offset)
    {
        subParticles.Add(new SubParticle(scene.Game)
        {
            RelativeOffset = new Vector3(offset, 0f),
            Owner = this,
            Scale = 1f
        });
    }

    /// <summary>
    ///     Prepares this particle for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites)
    {
        float shockwaveProgress = float.InverseLerp(0.3333f, 0.766f, timer);
        if (shockwaveProgress > 0f && shockwaveProgress < 1f)
        {
            for (int i = 0; i < 3; i++)
            {
                float radius = shockwaveProgress * 250f - i * 2f;

                AtlasEntry shockwave = Atlases.KnightScene.Shockwave;
                sprites.Enqueue(shockwave, new SpriteData(shockwave)
                {
                    Position = Position,
                    Scale = Vector2.One * radius / 300f,
                    Tint = Color.White * (1f - shockwaveProgress) * 1.2f
                }, drawToDepthBuffer: false);
            }
        }

        foreach (SubParticle particle in subParticles)
            particle.PrepareRender(sprites);
    }
}
