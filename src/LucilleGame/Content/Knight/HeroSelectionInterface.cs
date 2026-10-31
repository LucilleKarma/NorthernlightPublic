using System.Numerics;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight;

public sealed class HeroSelectionInterface : BattleMenuBottomsideOverlay
{
    /// <summary>
    ///     The selection index of this interface.
    ///     Indicates which hero has been chosen by
    ///     the player.
    /// </summary>
    public int SelectionIndex
    {
        get;
        private set;
    }

    private readonly Font font;

    private readonly Hero heroMakingSelection;

    private readonly Action onCancel;

    private readonly Action<HeroSelectionInterface> idleUpdate;

    private readonly Func<HeroSelectionInterface, bool> onInteract;

    public override bool RequiresInput => true;

    public HeroSelectionInterface(Hero hero, BattleMenu menu, Action<HeroSelectionInterface> idleUpdate, Action onCancel, Func<HeroSelectionInterface, bool> onInteract) : base(menu)
    {
        font = new Font("8bitoperator.ttf", 64f);
        this.heroMakingSelection = hero;
        this.idleUpdate = idleUpdate;
        this.onInteract = onInteract;
        this.onCancel = onCancel;
    }

    public override void Update()
    {
        idleUpdate(this);

        if (ActiveKeymap.Confirm.WasJustPressed)
        {
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));

            bool pop = true;
            if (!justShowedUp)
                pop = onInteract(this);

            if (pop)
                Pop(moveToNextInterfaceIndex: true);
        }

        if (ActiveKeymap.Cancel.WasJustPressed)
        {
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));
            Pop();
            onCancel?.Invoke();
        }

        int previousSelectionIndex = SelectionIndex;
        RespondToArrowInput();
        if (SelectionIndex != previousSelectionIndex)
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));

        if (justShowedUp)
            justShowedUp = false;
    }

    private void RespondToArrowInput()
    {
        int selectionDirection = 0;
        ReadOnlySpan<Hero> heroes = Menu.Scene.Heroes.All;
        if (ActiveKeymap.Up.WasJustPressed)
            selectionDirection = -1;
        if (ActiveKeymap.Down.WasJustPressed)
            selectionDirection = 1;

        SelectionIndex = int.Clamp(SelectionIndex + selectionDirection, 0, heroes.Length - 1);
    }

    public override void PrepareRender(SpriteRenderer sprites, TextRenderer text, Vector3 center, Vector2 size)
    {
        for (int i = 0; i < Menu.Scene.Heroes.All.Length; i++)
        {
            Hero hero = Menu.Scene.Heroes.All[i];

            Vector2 soulScale = Vector2.One * 1.8f;
            Vector3 drawPosition = center + new Vector3(size * new Vector2(-0.3825f, -0.3f), 0f);
            drawPosition.Y += size.Y * i / 2f * 0.6f;

            AtlasEntry soul = Atlases.KnightScene.Soul;
            if (i == SelectionIndex)
            {
                sprites.Enqueue(soul, new SpriteData(soul)
                {
                    Position = drawPosition,
                    Scale = soulScale,
                    Tint = Color.White
                });
            }

            drawPosition.X += soulScale.X * soul.Area.Width - 10f;

            text.Enqueue(hero.Name.Value, font, drawPosition + Vector3.UnitY * -2f, Color.White, Vector2.One * 0.85f, new Vector2(0f, 0.5f));

            drawPosition.X = center.X + size.X * 0.185f;
            Vector2 barSize = new Vector2(size.X * 0.125f, size.X * 0.025f);
            AtlasEntry pixel = Atlases.KnightScene.Pixel;
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = drawPosition - Vector3.UnitZ,
                Scale = barSize,
                Tint = new Color(127, 0, 0)
            });
            float healthIndicatorRatio = float.Clamp(hero.HP / (float)hero.MaxHP, 0f, 1f);
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = drawPosition + Vector3.UnitX * (barSize.X * -0.5f + healthIndicatorRatio * barSize.X * 0.5f),
                Scale = barSize * new Vector2(healthIndicatorRatio, 1f),
                Tint = new Color(0, 255, 0)
            });
        }
    }
}
