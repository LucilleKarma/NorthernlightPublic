using System.Numerics;
using Northernlight.Core.Audio;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;
using Northernlight.Core.Utilities;

namespace Northernlight.Content.Knight;

/// <summary>
///     Represents an instance of a per-hero action
///     interface.
/// </summary>
public sealed class BattleMenuActionInterface
{
    private int buttonIndex;

    private float popupTime;

    private float popupReverseTime = 0.001f;

    private readonly Button[] buttons;

    public sealed record Button(AtlasEntry Texture, AtlasEntry? Icon, Func<Hero, Button, QueuedHeroAction> OnPress);

    /// <summary>
    ///     How far along this interface is in its
    ///     popup animation.
    /// </summary>
    public float PopupProgress => float.InverseLerp(0f, PopupDuration, popupTime) * float.InverseLerp(PopupDuration * 0.2f, 0f, popupReverseTime);

    /// <summary>
    ///     How much this interface should cause the
    ///     portrait to ascend, in pixels.
    /// </summary>
    public float PopupAscent => float.Smoothstep(float.Sqrt(PopupProgress)) * MaxPopupAscent;

    /// <summary>
    ///     Whether this interface is active or not.
    /// </summary>
    public bool Active
    {
        get;
        set;
    }

    /// <summary>
    ///     The menu responsible for this
    ///     interface.
    /// </summary>
    public BattleMenu Menu
    {
        get;
        private set;
    }

    /// <summary>
    ///     The hero that owns this action interface.
    /// </summary>
    public Hero Hero
    {
        get;
        private set;
    }

    /// <summary>
    ///     The maximum amount by which the popup
    ///     can cause the underlying portrait to ascend.
    /// </summary>
    public static float MaxPopupAscent => 56f;

    /// <summary>
    ///     How long this interface spends popping up.
    /// </summary>
    public static float PopupDuration => 0.175f;

    public BattleMenuActionInterface(BattleMenu menu, Hero hero, Button[] buttons)
    {
        Menu = menu;
        Hero = hero;
        this.buttons = buttons;
    }

    /// <summary>
    ///     Shifts the active button index.
    /// </summary>
    public void ShiftButtonIndex(int shiftDirection)
    {
        int previousButtonIndex = buttonIndex;
        buttonIndex += shiftDirection;
        if (buttonIndex < 0)
            buttonIndex = buttons.Length - 1;
        if (buttonIndex >= buttons.Length)
            buttonIndex = 0;

        if (buttonIndex != previousButtonIndex)
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));
    }

    /// <summary>
    ///     Clicks the active button.
    /// </summary>
    public void ClickButton()
    {
        Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));

        Menu.PlayerActionQueue.AddNew(Menu.Scene, buttons[buttonIndex].OnPress(Hero, buttons[buttonIndex]));

        // If the latest menu requires additional input, wait
        // for that before moving the interface index.
        bool waitForPlayerInput = Menu.OverridingBottomsideOverlay?.RequiresInput ?? false;
        if (!waitForPlayerInput)
            Menu.AdvanceInterfaceIndex();
    }

    /// <summary>
    ///     Updates this interface.
    /// </summary>
    public void Update()
    {
        if (!Active)
        {
            buttonIndex = 0;
            popupReverseTime += Menu.Scene.Time.DeltaTime;
        }
        else
        {
            popupTime += Menu.Scene.Time.DeltaTime;
            popupReverseTime = 0f;
        }
    }

    /// <summary>
    ///     Forces this interface to pop up immediately.
    /// </summary>
    public void PopUpImmediately()
    {
        popupReverseTime = 0f;
        if (popupTime < PopupDuration)
            popupTime = PopupDuration;
    }

    /// <summary>
    ///     Opposite of <see cref="PopUpImmediately"/>.
    /// </summary>
    public void ShoveDown()
    {
        popupReverseTime = 0f;
        popupTime = 0f;
    }

    /// <summary>
    ///     Prepares this interface for rendering.
    /// </summary>
    public void PrepareRender(SpriteRenderer sprites, Vector3 drawPosition, Vector3 bottom, Vector3 portraitEnd, float scale, float opacity, int index)
    {
        if (PopupProgress <= 0.75f && popupReverseTime > 0f)
            return;

        float top = drawPosition.Y - scale * 36f;
        float verticalHeight = top - bottom.Y + 0.5f;
        float left = drawPosition.X - scale * 60f;
        float right = portraitEnd.X + scale * 18f;
        float horizontalSpacing = float.Abs(right - left);
        float centerX = (left + right) * 0.5f;
        float horizontalWidth = float.Abs(right - left);
        float barWidth = 3f;
        float separatorY = drawPosition.Y + 28f;
        float barHeight = float.Abs(separatorY - bottom.Y);
        float upperBarHeight = float.Abs(verticalHeight) - barHeight;
        AtlasEntry pixel = Atlases.KnightScene.Pixel;

        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(centerX, bottom.Y + verticalHeight * 0.5f, bottom.Z),
            Scale = new Vector2(horizontalWidth, verticalHeight) * scale,
            Tint = Color.Black * opacity
        });
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(centerX, top + upperBarHeight * 0.5f, bottom.Z + 1f),
            Scale = new Vector2(horizontalWidth, upperBarHeight) * scale,
            Tint = Color.Black * opacity
        });

        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(left, bottom.Y + verticalHeight * 0.5f, bottom.Z + 5f),
            Scale = new Vector2(barWidth, verticalHeight) * scale,
            Tint = Hero.BattleMenuBorderColor * opacity
        });
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(right, bottom.Y + verticalHeight * 0.5f, bottom.Z + 5f),
            Scale = new Vector2(barWidth, verticalHeight) * scale,
            Tint = Hero.BattleMenuBorderColor * opacity
        });

        for (int i = 0; i < 4; i++)
        {
            float localOpacity = (i / 4f - popupTime * 0.6f).Modulo(1f) * opacity;
            float horizontalOffset = (1f - localOpacity).Squared() * horizontalSpacing * 0.2f;

            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = new Vector3(left + horizontalOffset, separatorY + barHeight * 0.5f, bottom.Z + 0.2f + index * 0.1f),
                Scale = new Vector2(barWidth, barHeight) * scale,
                Tint = Hero.BattleMenuBorderColor * localOpacity
            });
            sprites.Enqueue(pixel, new SpriteData(pixel)
            {
                Position = new Vector3(right - horizontalOffset, separatorY + barHeight * 0.5f, bottom.Z + 0.2f + index * 0.1f),
                Scale = new Vector2(barWidth, barHeight) * scale,
                Tint = Hero.BattleMenuBorderColor * localOpacity
            });
        }

        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(centerX, top + 2f, bottom.Z + 2f),
            Scale = new Vector2(horizontalWidth, barWidth) * scale,
            Tint = Hero.BattleMenuBorderColor * opacity
        });
        sprites.Enqueue(pixel, new SpriteData(pixel)
        {
            Position = new Vector3(centerX, separatorY, bottom.Z + 2f),
            Scale = new Vector2(horizontalWidth, barWidth) * scale,
            Tint = Hero.BattleMenuBorderColor * opacity
        });

        float squishInterpolant = 0.2f;
        float squishedLeft = float.Lerp(left, centerX, squishInterpolant);
        float squishedRight = float.Lerp(right, centerX, squishInterpolant);

        if (popupReverseTime <= 0f)
            PrepareButtons(sprites, squishedLeft, squishedRight, bottom.Y, bottom.Z + 0.1f, opacity);
    }

    private void PrepareButtons(SpriteRenderer sprites, float left, float right, float bottom, float z, float opacity)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            float positionInterpolant = i / (float)buttons.Length;
            float nextPositionInterpolant = (i + 1f) / (float)buttons.Length;
            if (buttons.Length == 1)
            {
                positionInterpolant = 0.5f;
                nextPositionInterpolant = 1f;
            }

            float spacingInterpolant = float.Min(nextPositionInterpolant - positionInterpolant, 0.18f);
            float localLeft = float.Lerp(left, right, positionInterpolant);
            float localRight = float.Lerp(left, right, positionInterpolant + spacingInterpolant);
            float size = localRight - localLeft;

            Button button = buttons[i];
            Vector2 scale = Vector2.One * size / button.Texture.Area.Width;
            sprites.Enqueue(button.Texture, new SpriteData(button.Texture, 0, buttonIndex == i ? 1 : 0, 1, 2)
            {
                Position = new Vector3((localLeft + localRight) * 0.5f, bottom - button.Texture.Area.Height * scale.Y * 0.25f, z),
                Tint = Color.White * opacity,
                Scale = scale
            });
        }
    }
}
