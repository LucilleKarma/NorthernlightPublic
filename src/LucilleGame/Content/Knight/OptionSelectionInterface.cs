using System.Numerics;
using Northernlight.Core.Audio;
using Northernlight.Core.Localization;
using Northernlight.Core.Rendering;
using Northernlight.Core.Resources;

namespace Northernlight.Content.Knight;

public sealed class OptionSelectionInterface : BattleMenuBottomsideOverlay
{
    private int previouslyHoveredOption;
    private int hoveredOptionIndex;

    private int pageIndex;

    private readonly Font font;

    private readonly Option[] options;

    private readonly Action onCancel;

    /// <summary>
    ///     The action responsible for this interface.
    /// </summary>
    public QueuedHeroAction Action
    {
        get;
        private set;
    }

    /// <summary>
    ///     The hero making the option.
    /// </summary>
    public Hero Hero
    {
        get;
        private set;
    }

    /// <summary>
    ///     The button that initiated this interface.
    /// </summary>
    public BattleMenuActionInterface.Button Button
    {
        get;
        private set;
    }

    /// <summary>
    ///     The maximum amount of options that can
    ///     fit in a horizontal option.
    /// </summary>
    public static int MaxHorizontalOptions => 2;

    /// <summary>
    ///     The maximum amount of options that can
    ///     fit in a vertical option.
    /// </summary>
    public static int MaxVerticalOptions => 3;

    public record OptionDescriptionLine(LanguageAgnosticText Line, Func<Color> Color)
    {
        public OptionDescriptionLine(LanguageAgnosticText line, Color color) : this(line, () => color)
        { }
    }

    public record Option(
        Func<OptionSelectionInterface, bool> CanBeSelected,
        Action<OptionSelectionInterface> OnHover,
        Action<OptionSelectionInterface> OnCancelledHover,
        Action<OptionSelectionInterface> OnSelect,
        OptionDescriptionLine Title,
        OptionDescriptionLine[] Description
    );

    public override bool RequiresInput => true;

    public OptionSelectionInterface(Hero hero, BattleMenuActionInterface.Button button, QueuedHeroAction action, BattleMenu menu, Action onCancel, Func<OptionSelectionInterface, QueuedHeroAction, Option[]> options) : base(menu)
    {
        font = new Font("8bitoperator.ttf", 64f);
        Button = button;
        Hero = hero;
        Action = action;
        this.onCancel = onCancel;
        this.options = options(this, action);
    }

    public override void Update()
    {
        Option? option = options.Length <= 0 ? null : options[hoveredOptionIndex];
        bool canBeSelected = option?.CanBeSelected(this) ?? false;
        if (canBeSelected && option is not null)
        {
            previouslyHoveredOption = hoveredOptionIndex;
            option.OnHover?.Invoke(this);
            if (ActiveKeymap.Confirm.WasJustPressed)
            {
                Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuSelect.wav")));

                bool pop = true;
                if (!justShowedUp)
                    option.OnSelect(this);

                if (pop)
                    Pop(moveToNextInterfaceIndex: true);
            }
        }
        else if (hoveredOptionIndex != previouslyHoveredOption || !canBeSelected)
        {
            option?.OnCancelledHover?.Invoke(this);
        }

        if (ActiveKeymap.Cancel.WasJustPressed)
        {
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));
            onCancel?.Invoke();
            Pop();
        }

        if (justShowedUp)
            justShowedUp = false;
        else
            HandleNavigation();
    }

    private void HandleNavigation()
    {
        if (options.Length <= 0)
            return;

        int previousOptionIndex = hoveredOptionIndex;

        int maxIndicesPerPage = MaxHorizontalOptions * MaxVerticalOptions;
        int startIndex = int.Min(maxIndicesPerPage * pageIndex, options.Length - 1);
        int relativeIndex = hoveredOptionIndex - startIndex;
        int horizontalOffset = relativeIndex % MaxHorizontalOptions;
        int verticalOffset = relativeIndex / MaxHorizontalOptions;

        int absoluteVerticalOffset = hoveredOptionIndex / MaxHorizontalOptions;
        int totalVerticalOptions = (int)float.Ceiling(options.Length / (float)MaxHorizontalOptions);

        if (ActiveKeymap.Left.WasJustPressed)
            horizontalOffset--;
        if (ActiveKeymap.Right.WasJustPressed)
            horizontalOffset++;

        if (horizontalOffset < 0)
            horizontalOffset = MaxHorizontalOptions - 1;
        if (horizontalOffset >= MaxHorizontalOptions)
            horizontalOffset = 0;

        if (ActiveKeymap.Up.WasJustPressed && absoluteVerticalOffset >= 1)
            verticalOffset--;
        if (ActiveKeymap.Down.WasJustPressed && absoluteVerticalOffset < totalVerticalOptions - 1)
            verticalOffset++;

        hoveredOptionIndex = startIndex + horizontalOffset + verticalOffset * MaxHorizontalOptions;
        if (hoveredOptionIndex < 0 || hoveredOptionIndex >= options.Length)
            hoveredOptionIndex = previousOptionIndex;

        pageIndex = hoveredOptionIndex / maxIndicesPerPage;

        if (hoveredOptionIndex != previousOptionIndex)
            Menu.Scene.Audio.Play(new Sound(AudioBuffer.FromWav("MenuMove.wav")));
    }

    public override void PrepareRender(SpriteRenderer sprites, TextRenderer text, Vector3 center, Vector2 size)
    {
        // What?
        if (options.Length <= 0)
            return;

        float scale = 0.85f;
        float soulScale = 1.65f;
        float verticalSpacing = font.Size * scale;
        Vector2 start = center.AsVector2() + new Vector2(-0.4f, -0.3f) * size;

        int localIndex = 0;
        int maxIndicesPerPage = MaxHorizontalOptions * MaxVerticalOptions;
        int startIndex = int.Min(maxIndicesPerPage * pageIndex, options.Length - 1);
        int endIndex = int.Min(maxIndicesPerPage * (pageIndex + 1), options.Length - 1);
        for (int i = startIndex; i <= endIndex; i++)
        {
            int relativeIndex = i - startIndex;
            int horizontalOffset = relativeIndex % MaxHorizontalOptions;
            int verticalOffset = relativeIndex / MaxHorizontalOptions;
            float horizontalProgress = horizontalOffset / (float)MaxHorizontalOptions;
            Option option = options[i];

            Vector2 drawPosition = start + new Vector2(size.X * horizontalProgress * 0.765f, verticalOffset * verticalSpacing);

            text.Enqueue(option.Title.Line.Value, font, new Vector3(drawPosition, center.Z), option.Title.Color(), Vector2.One * scale, new Vector2(0f, 0.5f));
            if (hoveredOptionIndex == i)
            {
                AtlasEntry soul = Atlases.KnightScene.Soul;
                sprites.Enqueue(soul, new SpriteData(soul)
                {
                    Position = new Vector3(drawPosition + new Vector2(-20f, 2f) * soulScale, center.Z),
                    Tint = Color.White,
                    Scale = Vector2.One * soulScale
                }, BlendState.NonPremultiplied);
            }
            localIndex++;
            if (localIndex >= MaxHorizontalOptions * MaxVerticalOptions)
                break;
        }

        Option activeOption = options[hoveredOptionIndex];
        Vector2 descriptionDrawPosition = start + Vector2.UnitX * size.X * 0.65f;
        foreach (OptionDescriptionLine line in activeOption.Description)
        {
            text.Enqueue(line.Line.Value, font, new Vector3(descriptionDrawPosition, center.Z), line.Color(), Vector2.One * scale, new Vector2(0f, 0.5f));
            descriptionDrawPosition.Y += verticalSpacing;
        }
    }
}
