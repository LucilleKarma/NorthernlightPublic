using System.Numerics;
using Northernlight.Content.Knight.Enemies;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight;

public sealed class TargetSelectionInterface : BattleMenuBottomsideOverlay
{
    private int selectionIndex;

    private readonly Font font;

    private readonly Hero hero;

    private readonly Action idleUpdate;

    private readonly Action onCancel;

    private readonly Func<bool> onInteract;

    public override bool RequiresInput => true;

    public TargetSelectionInterface(Hero hero, BattleMenu menu, Action idleUpdate, Func<bool> onInteract, Action onCancel) : base(menu)
    {
        font = new Font("8bitoperator.ttf", 64f);
        this.hero = hero;
        this.idleUpdate = idleUpdate;
        this.onInteract = onInteract;
        this.onCancel = onCancel;
    }

    public override void Update()
    {
        idleUpdate();

        int previousSelectionIndex = selectionIndex;
        Enemy enemy = Menu.Scene.BattleHandler.Enemies[previousSelectionIndex];
        enemy.SelectionFlash = true;

        if (ActiveKeymap.Confirm.WasJustPressed)
        {
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));

            bool pop = true;
            if (!justShowedUp)
                pop = onInteract();

            if (pop)
            {
                Pop(moveToNextInterfaceIndex: true);
                enemy.SelectionFlash = false;
            }
        }

        if (ActiveKeymap.Cancel.WasJustPressed)
        {
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));
            onCancel?.Invoke();
            Pop();
            enemy.SelectionFlash = false;
        }

        if (ActiveKeymap.Up.WasJustPressed)
            selectionIndex--;
        if (ActiveKeymap.Down.WasJustPressed)
            selectionIndex++;
        selectionIndex = int.Clamp(selectionIndex, 0, Menu.Scene.BattleHandler.Enemies.Count - 1);

        if (selectionIndex != previousSelectionIndex)
        {
            enemy.SelectionFlash = false;
            enemy = Menu.Scene.BattleHandler.Enemies[selectionIndex];
            enemy.SelectionFlash = true;

            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));
        }

        if (justShowedUp)
            justShowedUp = false;
    }

    public override void PrepareRender(SpriteRenderer sprites, TextRenderer text, Vector3 center, Vector2 size)
    {
        for (int i = 0; i < Menu.Scene.BattleHandler.Enemies.Count; i++)
        {
            Enemy enemy = Menu.Scene.BattleHandler.Enemies[i];

            Vector2 soulScale = Vector2.One * 1.8f;
            Vector3 drawPosition = center + new Vector3(size * new Vector2(-0.3825f, -0.275f), 0f);

            if (Menu.Scene.BattleHandler.Enemies.Count >= 2)
                drawPosition.Y += i * size.Y * 0.275f;

            AtlasEntry soul = Atlases.KnightScene.Soul;
            if (i == selectionIndex)
            {
                sprites.Enqueue(soul, new SpriteData(soul)
                {
                    Position = drawPosition,
                    Scale = soulScale,
                    Tint = Color.White
                });
            }

            drawPosition.X += soulScale.X * soul.Area.Width - 10f;

            LanguageAgnosticText name = LocalizationEntries.Knight;
            LanguageAgnosticText mercyText = LocalizationEntries.Mercy;
            text.Enqueue(name.Value, font, drawPosition + Vector3.UnitY * -2f, Color.White, Vector2.One * 0.85f, new Vector2(0f, 0.5f));

            drawPosition.X = center.X + size.X * 0.185f;
            Vector2 barSize = new Vector2(size.X * 0.125f, size.X * 0.025f);
            AtlasEntry pixel = Atlases.KnightScene.Pixel;
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = drawPosition - Vector3.UnitZ,
                Scale = barSize,
                Tint = new Color(127, 0, 0)
            });
            float healthIndicatorRatio = float.Clamp(enemy.HP / (float)enemy.MaxHP, 0f, 1f);
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = drawPosition + Vector3.UnitX * (barSize.X * -0.5f + healthIndicatorRatio * barSize.X * 0.5f),
                Scale = barSize * new Vector2(healthIndicatorRatio, 1f),
                Tint = new Color(0, 255, 0)
            });

            AtlasEntry hp = Atlases.KnightScene.BattleMenuHPText;
            sprites.Enqueue(hp, new SpriteData(hp)
            {
                Position = drawPosition + new Vector3(-barSize.X * 0.275f, -barSize.Y, 0f),
                Scale = new Vector2(1.85f, 1f),
                Tint = Color.White
            });

            AtlasEntry questionMark = Atlases.KnightScene.QuestionMarkTimesThree;
            sprites.Enqueue(questionMark, new SpriteData(questionMark)
            {
                Position = drawPosition + new Vector3(-barSize.X * 0.15f, 0f, 5f),
                Scale = Vector2.One * 0.9f,
                Tint = Color.White
            });

            drawPosition.X = center.X + size.X * 0.3f;
            text.Enqueue(mercyText.Value, font, drawPosition + new Vector3(barSize.X * 0.05f, -barSize.Y, 0f), Color.White, new Vector2(1.5f, 0.7f) * 0.65f, new Vector2(0f, 0.5f));

            AtlasEntry mercyDenial = Atlases.KnightScene.MercyDenial;
            sprites.Enqueue(mercyDenial, new SpriteData(mercyDenial)
            {
                Position = drawPosition + Vector3.UnitX * barSize.X * 0.5f,
                Scale = barSize / 11f,
                Tint = Color.White
            });
        }
    }
}
