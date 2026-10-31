using System.Numerics;
using Northernlight.Content.Knight.AttackPatterns;
using Northernlight.Content.Knight.Dialogue;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Content.Knight.Particles;
using Northernlight.Content.SaveData;
using Northernlight.Core;
using Northernlight.Core.Audio;
using Northernlight.Core.InputHandling;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

public sealed class KnightFightScene(NorthernlightGame game) : Scene(game)
{
    private float screenshake;

    internal bool initializedCamera;

    internal BattleMenu battleMenu;

    /// <summary>
    ///     Whether the Knight fight is being performed in
    ///     the weird route, after aborting at the lake in Chapter 5.
    /// </summary>
    public bool SideB => SaveData.AbortedChapter5WeirdRoute;

    /// <summary>
    ///     The maintainer of hero data.
    /// </summary>
    public HeroesInitializer Heroes
    {
        get;
        set;
    }

    /// <summary>
    ///     The visual battle backdrop.
    /// </summary>
    public KnightBattleBackdrop Backdrop
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of screen darkening currently going on.
    /// </summary>
    public float ScreenDarkening
    {
        get;
        set;
    }

    /// <summary>
    ///     The start-up countdown.
    /// </summary>
    public float StartupCountdown
    {
        get;
        set;
    } = 1.8f;

    /// <summary>
    ///     The blackening overlay countdown.
    /// </summary>
    public float BlackCountdown
    {
        get;
        set;
    }

    /// <summary>
    ///     The position that the camera should pan towards.
    /// </summary>
    public Vector3 CameraPanPosition
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount of darkening to apply behind attack visuals.
    /// </summary>
    public float BattleBackBackdropOpacity
    {
        get;
        set;
    }

    /// <summary>
    ///     This game's death screen handler.
    /// </summary>
    public SpookyDeathScreen DeathScreen
    {
        get;
        set;
    }

    /// <summary>
    ///     This game's battle handler system.
    /// </summary>
    public BattleHandler BattleHandler
    {
        get;
        internal set;
    }

    /// <summary>
    ///     The system responsible for screen shatter visuals.
    /// </summary>
    public ScreenShatterSystem ShatterSystem
    {
        get;
        private set;
    }

    /// <summary>
    ///     The global hero stat manager.
    /// </summary>
    public HeroStats HeroStats
    {
        get;
        private set;
    }

    /// <summary>
    ///     The roaring knight.
    /// </summary>
    public RoaringKnight RoaringKnight
    {
        get;
        private set;
    }

    /// <summary>
    ///     The battle render layer for this game.
    /// </summary>
    public RenderLayer BattleLayer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The soul render layer for this game.
    /// </summary>
    public RenderLayer SoulLayer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The save data associated with this game
    ///     scene.
    /// </summary>
    public NorthernlightSaveData SaveData
    {
        get;
        private set;
    }

    /// <summary>
    ///     The active dialogue box. <see langword="null"/> if currently unused.
    /// </summary>
    public LargeDialogueBoxUI? DialogueBox
    {
        get;
        set;
    }

    /// <summary>
    ///     The special image render layer for this game.
    /// </summary>
    public RenderLayer SpecialImageLayer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The screen shatter image render layer for this game.
    /// </summary>
    public RenderLayer ShatterLayer
    {
        get;
        private set;
    }

    /// <summary>
    ///     The set of all hit impacts in the game
    ///     scene.
    /// </summary>
    public readonly List<HitImpact> HitImpacts = [];

    /// <summary>
    ///     The set of all damage indicators in the
    ///     game scene.
    /// </summary>
    public readonly List<DamageIndicator> DamageIndicators = [];

    /// <summary>
    ///     The set of all rude buster shots in the game
    ///     scene.
    /// </summary>
    public readonly List<RudeBusterShot> RudeBusterShots = [];

    /// <summary>
    ///     The set of all rude buster impacts in the game
    ///     scene.
    /// </summary>
    public readonly List<RudeBusterImpact> RudeBusterImpacts = [];

    /// <summary>
    ///     The set of all heal particles in the game
    ///     scene.
    /// </summary>
    public readonly List<HealParticle> HealParticles = [];

    /// <summary>
    ///     The set of all critical strike particles in the game
    ///     scene.
    /// </summary>
    public readonly List<CriticalParticle> CriticalStrikeParticles = [];

    /// <summary>
    ///     The set of all ice shock particles in the game
    ///     scene.
    /// </summary>
    public readonly List<IceShockParticle> IceShockParticles = [];

    /// <summary>
    ///     The set of all clash particles in the game
    ///     scene.
    /// </summary>
    public readonly List<ClashParticle> ClashParticles = [];

    public KnightFightScene(NorthernlightGame game, int slot) : this(game) => SaveData = NorthernlightSaveData.Load(slot, string.Empty);

    public override void Initialize()
    {
        base.Initialize();
        Backdrop = new KnightBattleBackdrop(this)
        {
            GameResolution = NorthernlightGame.KnightFightResolution,
        };

        HeroStats = new HeroStats()
        {
            KrisBaseDamage = (int)StatsRepository.KrisBaseDamage,
            SusieBaseDamage = (int)StatsRepository.SusieBaseDamage,
            NoelleBaseDamage = (int)StatsRepository.NoelleBaseDamage,

            CrossSlashTensionPointCost = (int)StatsRepository.CrossSlashTensionPointCost,

            RudeBusterTensionPointCost = StatsRepository.RudeBusterTensionPointCost,
            GoodHealTensionPointCost = StatsRepository.GoodHealTensionPointCost,
            GoodHealHealQuantity = (int)StatsRepository.GoodHealHealQuantity,

            AngelsBlessingTensionPointCost = StatsRepository.AngelsBlessingTensionPointCost,
            AngelsBlessingHealAmount_Revive = (int)StatsRepository.AngelsBlessingHealAmount_Revive,
            AngelsBlessingHealAmount_NotRevive = (int)StatsRepository.AngelsBlessingHealAmount_NotRevive,
            AngelsBlessingHealAmount_SideB = (int)StatsRepository.AngelsBlessingHealAmount_SideB,

            IceShockTensionPointCost = StatsRepository.IceShockTensionPointCost,

            GeneralDamageFactor = StatsRepository.GeneralDamageFactor,
            CrossSlashDamageFactor = StatsRepository.CrossSlashDamageFactor,
            RudeBusterDamageFactor = StatsRepository.RudeBusterDamageFactor,
            IceShockDamageFactor = SideB ? StatsRepository.IceShockDamageFactorSideB : StatsRepository.IceShockDamageFactor,
        };

        Heroes = new HeroesInitializer(this);
        Heroes.Initialize();

        RespawnKnight();

        BattleHandler = new BattleHandler(this, [RoaringKnight]);
        DeathScreen = new SpookyDeathScreen(this);
        battleMenu = new BattleMenu(this)
        {
            GameResolution = NorthernlightGame.KnightFightResolution,
        };

        BattleLayer = Game.CreateSimpleLayer();
        BattleLayer.OutputRenderer.Add(new MeshRenderer(Device, Window));

        BattleLayer.Preparations += (frame, layer) =>
        {
            SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();
            TextRenderer text = layer.OutputRenderer.Get<TextRenderer>();
            MeshRenderer trails = layer.OutputRenderer.Get<MeshRenderer>();
            BattleHandler.PrepareRender(layer.OutputRenderer, sprites, text, trails, frame);

            foreach (IceShockParticle particle in IceShockParticles)
                particle.PrepareRender(sprites);
            foreach (ClashParticle particle in ClashParticles)
                particle.PrepareRender(sprites);
        };

        SoulLayer = Game.CreateSimpleLayer();
        SoulLayer.Preparations += (frame, layer) =>
        {
            SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();
            TextRenderer text = layer.OutputRenderer.Get<TextRenderer>();
            DeathScreen.PrepareRender(sprites, text);
            if (!DeathScreen.PlayerWon)
                BattleHandler.Soul.PrepareRender(sprites);

            foreach (DamageIndicator damageIndicator in DamageIndicators)
                damageIndicator.PrepareRender(sprites, text);

            BattleHandler.Attack?.ExtraPrepareRender_SoulLayer(sprites, frame);
            DialogueBox?.Render(sprites, text);
        };

        SpecialImageLayer = Game.CreateSimpleLayer();
        SpecialImageLayer.OutputRenderer.Add(new MeshRenderer(Device, Window));

        ShatterSystem = new ScreenShatterSystem(this);
        ShatterLayer = Game.CreateSimpleLayer();
        ShatterLayer.OutputRenderer.Add(new MeshRenderer(Device, Window));
        ShatterLayer.Preparations += ShatterSystem.PrepareRender;

        BoxSplit.LoadEarly(this, Device);

        SaveData.InitializePlaySessionTimer(this);
    }

    public override void Show()
    {
        base.Show();

        MainCamera.Width = (int)NorthernlightGame.KnightFightResolution.X;
        MainCamera.Height = (int)NorthernlightGame.KnightFightResolution.Y;
        MainCamera.Zoom = 1f;
        UICamera.Width = (int)NorthernlightGame.KnightFightResolution.X;
        UICamera.Height = (int)NorthernlightGame.KnightFightResolution.Y;
        UICamera.Zoom = 1f;

        DefaultLayer.Preparations += RenderGame;
        UILayer.Preparations += RenderUI;
    }

    public override void Hide()
    {
        base.Hide();

        DefaultLayer.Preparations -= RenderGame;
        UILayer.Preparations -= RenderUI;
    }

    internal void RespawnKnight()
    {
        int knightHp = (int)float.Round(StatsRepository.KnightMaxHP);
        if (SideB)
            knightHp = (int)float.Round(StatsRepository.KnightMaxHPSideB);

        Vector2 knightRestingPosition = NorthernlightGame.KnightFightResolution * new Vector2(0.76f, 0.405f) + Backdrop.ForegroundVerticalOffset;
        RoaringKnight = new RoaringKnight(this, new Vector3(knightRestingPosition, 8f), maxHp: knightHp);

        Vector2 knightSpawnPosition = knightRestingPosition + Vector2.UnitX * NorthernlightGame.KnightFightResolution * 2.4f;
        RoaringKnight.Position = new Vector3(knightSpawnPosition, RoaringKnight.Position.Z);
    }

    private void RenderUI(FrameContext frame, RenderLayer layer)
    {
        if (DeathScreen.OverrideGameScreen)
            return;

        SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();
        TextRenderer text = layer.OutputRenderer.Get<TextRenderer>();

        if (BattleBackBackdropOpacity < 1f)
            battleMenu.PrepareRender(sprites, text, 1f - BattleBackBackdropOpacity);
    }

    private void RenderGame(FrameContext frame, RenderLayer layer)
    {
        SpriteRenderer sprites = layer.OutputRenderer.Get<SpriteRenderer>();

        sprites.Enqueue(Atlases.KnightScene.Pixel, new SpriteData(Atlases.KnightScene.Pixel)
        {
            Position = new Vector3(0f, 0f, -100f),
            Scale = Vector2.One * 99999f,
            Tint = BlackCountdown > 0f ? Color.Black : Color.Lerp(new Color(12, 19, 36), Color.Black, BattleBackBackdropOpacity)
        });

        if (BlackCountdown > 0f)
            return;

        Backdrop.PrepareRender(sprites);
        PrepareHeroes(sprites);

        foreach (Enemy enemy in BattleHandler.Enemies)
            enemy.PrepareRender(sprites);

        foreach (RudeBusterShot shot in RudeBusterShots)
            shot.PrepareRender(sprites);
        foreach (RudeBusterImpact impact in RudeBusterImpacts)
            impact.PrepareRender(sprites);
        foreach (HealParticle particle in HealParticles)
            particle.PrepareRender(sprites);
        foreach (CriticalParticle particle in CriticalStrikeParticles)
            particle.PrepareRender(sprites);

        foreach (HitImpact hitImpact in HitImpacts)
            hitImpact.PrepareRender(sprites);

        if (ScreenDarkening < 0f)
        {
            sprites.Enqueue(Atlases.KnightScene.Pixel, new SpriteData(Atlases.KnightScene.Pixel)
            {
                Position = new Vector3(0f, 0f, 600f),
                Scale = Vector2.One * 99999f,
                Tint = Color.White * -ScreenDarkening
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }
        if (ScreenDarkening > 0f)
        {
            sprites.Enqueue(Atlases.KnightScene.Pixel, new SpriteData(Atlases.KnightScene.Pixel)
            {
                Position = new Vector3(0f, 0f, 600f),
                Scale = Vector2.One * 99999f,
                Tint = new Color(0, 0, 12) * ScreenDarkening
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }
        if (BattleBackBackdropOpacity > 0f || StartupCountdown > 0f)
        {
            sprites.Enqueue(Atlases.KnightScene.Pixel, new SpriteData(Atlases.KnightScene.Pixel)
            {
                Position = new Vector3(0f, 0f, 600f),
                Scale = Vector2.One * 99999f,
                Tint = Color.Black * float.Max(StartupCountdown, BattleBackBackdropOpacity)
            }, BlendState.NonPremultiplied, drawToDepthBuffer: false);
        }
    }

    private void PrepareHeroes(SpriteRenderer sprites)
    {
        foreach (Hero hero in Heroes.All)
            hero.PrepareRender(sprites);
    }

    /// <summary>
    ///     Starts the Black Knife track.
    /// </summary>
    public void StartBlackKnifeTrack()
    {
        Music.Stop();
        Music.Add(new Sound(AudioBuffer.FromWav("BlackKnife.wav", musicDirectory: true)));
    }

    /// <summary>
    ///     Adds a quantity of screen shake to the game.
    /// </summary>
    public void AddScreenShake(float shakeIncrease) => screenshake += shakeIncrease;

    public override void Update()
    {
        base.Update();

        Vector3 centeringOffset = Vector3.Zero;
        if (MainCamera is RestrictedOrthographicCamera c)
        {
            MainCamera.Position = CameraPanPosition;

            centeringOffset = new Vector3(c.Width, c.Height, 0f) / MainCamera.Zoom * 0.5f;
            MainCamera.Position -= centeringOffset;
        }
        CameraPanPosition = Vector3.LerpDt(CameraPanPosition, centeringOffset, 16f, Time);
        if (!initializedCamera)
        {
            initializedCamera = true;
            CameraPanPosition = centeringOffset;
        }

        if (Time.GlobalSpeed > 0f)
            MainCamera.Position += new Vector3(Rng.NextCircular(screenshake * 0.5f), 0f);

        screenshake = float.Clamp(screenshake - Time.DeltaTime * 30f, 0f, 20.5f);

        if (Matrix4x4.Invert(UICamera.LetterboxTranslation, out Matrix4x4 inverseTranslation))
        {
            Vector2 scale = UICamera.Window.Size / new Vector2(UICamera.EffectiveWidth, UICamera.EffectiveHeight);
            MouseScreen = Vector2.Transform(Input.RawMousePosition / scale, inverseTranslation);
            MouseWorld = MouseScreen + MainCamera.Position.AsVector2();
        }

        Time.GlobalSpeed = 1f;
        StartupCountdown = float.Max(0f, StartupCountdown - Game.Time.DeltaTime * 6.1f);

        DeathScreen.Update();
        if (DeathScreen.OverrideGameScreen)
        {
            ScreenDarkening = 0f;
            HitImpacts.Clear();
            DamageIndicators.Clear();
            return;
        }

        Backdrop.Update();
        battleMenu.Update();
        Heroes.Update();

        BattleHandler.Update();

        foreach (Enemy enemy in BattleHandler.Enemies)
            enemy.Update();

        for (int i = 0; i < RudeBusterShots.Count; i++)
            RudeBusterShots[i].Update();
        for (int i = 0; i < RudeBusterImpacts.Count; i++)
            RudeBusterImpacts[i].Update();
        for (int i = 0; i < HealParticles.Count; i++)
            HealParticles[i].Update(Game);
        for (int i = 0; i < CriticalStrikeParticles.Count; i++)
            CriticalStrikeParticles[i].Update(Game);
        for (int i = 0; i < IceShockParticles.Count; i++)
            IceShockParticles[i].Update(this);
        for (int i = 0; i < ClashParticles.Count; i++)
            ClashParticles[i].Update(Game);
        HealParticles.RemoveAll(p => p.Opacity <= 0f);
        CriticalStrikeParticles.RemoveAll(p => p.Opacity <= 0f);
        IceShockParticles.RemoveAll(p => p.Dead);
        ClashParticles.RemoveAll(p => p.Dead);

        foreach (DamageIndicator damageIndicator in DamageIndicators)
            damageIndicator.Update(Game);
        foreach (HitImpact hitImpact in HitImpacts)
            hitImpact.Update(Game);
        HitImpacts.RemoveAll(i => i.Frame >= i.TotalFrames);
        DamageIndicators.RemoveAll(d => d.LifetimeRatio >= 1f);
        DialogueBox?.Update();

        ShatterSystem.Update();

        ScreenDarkening = float.LerpDt(ScreenDarkening, 0f, 3f, Time);
        BlackCountdown = float.Max(0f, BlackCountdown - Time.DeltaTime);
    }

    public override void Render(FrameContext frame)
    {
        Vector2 shatterCameraPosition = Vector2.Zero;
        if (MainCamera is RestrictedOrthographicCamera c)
        {
            Vector3 baseDisplacement = new Vector3(Window.Width - c.EffectiveWidth, Window.Height - c.EffectiveHeight, 0f) * 0.25f;
            shatterCameraPosition = -baseDisplacement.AsVector2();
        }

        OrthographicCamera shatterCamera = new OrthographicCamera(NorthernlightGame.KnightFightResolution, shatterCameraPosition.AsVector3(), Vector3.UnitZ, Vector3.UnitY, -1000f, 1000f);
        RenderLayer.RenderSet[] set =
        [
            new RenderLayer.RenderSet(DefaultLayer, MainCamera),
            new RenderLayer.RenderSet(BattleLayer, MainCamera),
            new RenderLayer.RenderSet(SoulLayer, MainCamera),
            new RenderLayer.RenderSet(UILayer, UICamera),
            new RenderLayer.RenderSet(SpecialImageLayer, MainCamera),
            new RenderLayer.RenderSet(ShatterLayer, shatterCamera),
        ];
        if (DeathScreen.OverrideGameScreen)
        {
            set =
            [
                new RenderLayer.RenderSet(SoulLayer, MainCamera),
                new RenderLayer.RenderSet(UILayer, UICamera),
            ];
        }

        RenderLayer.Render(set, frame, Time, Color.Black);
    }
}
