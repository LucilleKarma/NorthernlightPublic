using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Northernlight.Core.Localization;

namespace Northernlight.Content.Knight.Items;

/// <summary>
///     A global registry of items usable in battle
///     that may be ported over from Deltarune save files.
/// </summary>
/// <remarks>
///     In the interest of system unification, this registry
///     also takes into account our own imagined items.
/// </remarks>
public static class VanillaGameItemRegistry
{
    // Do not change these numerical values unless
    // strictly necessary, they correspond to Deltarune's
    // save file IDs.

    /// <summary>
    ///     A neat enumeration mapping from name
    ///     to ID.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ID
    {
        DarkerCandy = 1,
        ReviveMint = 2,
        Glowshard = 3,
        Manual = 4,
        BrokenCake = 5,
        TopCake = 6,
        Spincake = 7,
        Darkburger = 8,
        LancerCookie = 9,
        GigaSalad = 10,
        ClubsSandwich = 11,
        HeartsDonut = 12,
        ChocoDiamond = 13,
        Favwich = 14,
        RouxlsRoux = 15,

        CDBagel = 16,
        Mannequin = 17,
        RottenTea_Kris = 18,
        RottenTea_Noelle = 19,
        RottenTea_Ralsei = 20,
        RottenTea_Susie = 21,
        DDBurger = 22,
        LightCandy = 23,
        ButlerJuice = 24,
        SpagettiCode = 25,
        JavaCookie = 26,
        TensionBit = 27,
        TensionGem = 28,
        TensionMax = 29,
        ReviveDust = 30,
        ReviveBrite = 31,
        SPoison = 32,
        DogDollar = 33,

        TVDinner = 34,
        Pipis = 35,
        FlatSoda = 36,
        TVSlop = 37,
        ExecutiveBuffet = 38,
        DeluxeDinner = 39,

        AncientSweet = 60,
        Rhapsotea = 61,
        Scarlixer = 62,
        BitterTear = 63,

        PunchBowl = 40,
        Flavigne = 41,
        GreenTea = 42,
        OrangeJuice = 43,
        Schadenbrot = 64,
        TreeCake = 65,
        SPotion = 66,
        RawMoon = 67,
        Phanta = 68,
        FlowerySoda = 69,
        Shikacola = 70,

        // Custom, added by us.
        DarkCandyCane = 50000,
        PieCone = 50001,
        IceCube = 50002,
        SnowwomanPiece = 50003,
        Nutmegnog = 50004,
        GrandDinner = 50005,
        GingerbreadMonster = 50006,
    }

    private static KnightFightScene? scene => game.CurrentScene as KnightFightScene;

    private static NorthernlightGame game;

    /// <summary>
    ///     The set of all item generators mapped by the
    ///     internal IDs used for them in Deltarune's save
    ///     file data.
    /// </summary>
    private static readonly Dictionary<ID, Func<Item>> itemGeneratorsById = [];

    /// <summary>
    ///     Initializes the registry.
    /// </summary>
    public static void Initialize(NorthernlightGame game)
    {
        if (VanillaGameItemRegistry.game is not null)
            return;

        VanillaGameItemRegistry.game = game;

        LoadChapter1Items();
        LoadChapter2Items();
        LoadChapter3Items();
        LoadChapter4Items();
        LoadChapter5Items();
        LoadChapter6Items();
    }

    /// <summary>
    ///     Filters raw numbers into corresponding item IDs.
    /// </summary>
    public static List<ID> FilterValidIds(ReadOnlySpan<int> ids)
    {
        List<ID> result = [];
        for (int i = 0; i < ids.Length; i++)
        {
            if (itemGeneratorsById.ContainsKey((ID)ids[i]))
                result.Add((ID)ids[i]);
        }

        return result;
    }

    /// <summary>
    ///     Tries to convert a given internal Deltarune item ID into
    ///     a usable item in Northernlight.
    /// </summary>
    public static bool TryMakeOfId(ID id, [NotNullWhen(true)] out Item? item)
    {
        item = null;
        if (itemGeneratorsById.TryGetValue(id, out Func<Item>? generator))
        {
            item = generator();
            return true;
        }

        return false;
    }

    private static void LoadChapter1Items()
    {
        // Darker Candy (Known as Dark Candy in Chapter 3 and prior, but
        // gets converted thusly in Chapter 4).
        itemGeneratorsById[ID.DarkerCandy] = MakeBasicHealingItem(LocalizationEntries.DarkerCandyName, 120);

        // Revive Mint.
        itemGeneratorsById[ID.ReviveMint] = () => new Item()
        {
            Name = LocalizationEntries.ReviveMintName,
            DescriptionLines = [LocalizationEntries.Heal, LocalizationEntries.Downed, LocalizationEntries.Ally],
            OnUse = h =>
            {
                if (h.HP >= 1)
                    h.Heal((int)float.Ceiling(h.MaxHP * 0.5f));
                else
                    h.Revive(h.MaxHP);
            }
        };

        // Broken Cake (not normally meant to be
        // edible, but is supposed by save data as a technically
        // usable item).
        itemGeneratorsById[ID.BrokenCake] = MakeBasicHealingItem(LocalizationEntries.BrokenCakeName, 20);

        // Top Cake.
        itemGeneratorsById[ID.TopCake] = MakeBasicHealingItem(LocalizationEntries.TopCakeName, 160, affectsEveryone: true);

        // Spin Cake.
        itemGeneratorsById[ID.Spincake] = MakeBasicHealingItem(LocalizationEntries.SpinCakeName, 200, affectsEveryone: true);

        // Darkburger.
        itemGeneratorsById[ID.Darkburger] = MakeBasicHealingItem(LocalizationEntries.DarkburgerName, 70);

        // Lancer cookie (Expired in this chapter specifically).
        itemGeneratorsById[ID.LancerCookie] = MakeBasicHealingItem(LocalizationEntries.LancerCookieName, 4, overridingDescriptionLines: [LocalizationEntries.LancerCookieDescription]);

        // GigaSalad (Another unused chapter one item that
        // heals 4 HP for most members of the team but interestingly
        // heals 90 HP for Noelle specifically).
        itemGeneratorsById[ID.GigaSalad] = () =>
        {
            int baseHealQuantity = 4;
            int noelleHealQuantity = 90;
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.Heals, LocalizationEntries.ArbitraryHP.WithArgs(baseHealQuantity.ToString())];

            return new Item()
            {
                Name = LocalizationEntries.GigaSaladName,
                DescriptionLines = descriptionLines,
                OnUse = h => h.Heal(h == scene?.Heroes.Noelle ? noelleHealQuantity : baseHealQuantity),
                AffectsEveryone = false
            };
        };

        // Clubs Sandwich.
        itemGeneratorsById[ID.ClubsSandwich] = MakeBasicHealingItem(LocalizationEntries.ClubsSandwichName, 70, affectsEveryone: true);

        // Hearts Donut. Applies a variable
        // quantity of healing.
        itemGeneratorsById[ID.HeartsDonut] = MakeVariableHealingItem(LocalizationEntries.HeartsDonutName, healQuantityMin: 10, healQuantityMax: 90);

        // Choco Diamond. Applies a variable
        // quantity of healing.
        itemGeneratorsById[ID.ChocoDiamond] = MakeVariableHealingItem(LocalizationEntries.ChocoDiamondName, healQuantityMin: 20, healQuantityMax: 80);

        // Favwich (Yet another unused item that
        // restores 500HP. Possibly a debug item?
        // Anyway, it's part of the game's save readback, so
        // it's included here too)
        itemGeneratorsById[ID.Favwich] = MakeBasicHealingItem(LocalizationEntries.FavwichName, 500, overridingDescriptionLines: [LocalizationEntries.Heals, LocalizationEntries.AllHPScreaming]);

        // Rouxls Roux.
        itemGeneratorsById[ID.RouxlsRoux] = MakeBasicHealingItem(LocalizationEntries.RouxlsRouxName, 50);
    }

    private static void LoadChapter2Items()
    {
        // CD Bagel.
        itemGeneratorsById[ID.CDBagel] = MakeBasicHealingItem(LocalizationEntries.CDBagelName, 80);

        // The following four items correspond to the
        // Kris Tea, Noelle Tea, Ralsei Tea, and Susie Tea
        // respectively. However, since chapter 3 they have
        // all been afflicted with a mysterious curse
        // that makes them all rotten, much to my chagrin.
        var rottenTeas = MakeBasicHealingItem(LocalizationEntries.RottenTeaName, 10);
        itemGeneratorsById[ID.RottenTea_Kris] = rottenTeas;
        itemGeneratorsById[ID.RottenTea_Noelle] = rottenTeas;
        itemGeneratorsById[ID.RottenTea_Ralsei] = rottenTeas;
        itemGeneratorsById[ID.RottenTea_Susie] = rottenTeas;

        // DD (or Double-Dark) Burger. Acts similar to the
        // Bisicle from Undertale in that consuming it causes
        // the item to turn into a normal dark burger
        // afterwards, allowing for double-usage across two turns.
        itemGeneratorsById[ID.DDBurger] = () =>
        {
            int healQuantity = 60;
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.Heals, LocalizationEntries.ArbitraryHPDouble.WithArgs(healQuantity.ToString())];

            return new Item()
            {
                Name = LocalizationEntries.DDBurgerName,
                DescriptionLines = descriptionLines,
                OnUse = h =>
                {
                    h.Heal(healQuantity);

                    Item newDarkBurger = itemGeneratorsById[ID.Darkburger]();
                    scene?.BattleHandler.Inventory.Add(newDarkBurger);
                },
                AffectsEveryone = false
            };
        };

        // Light Candy.
        itemGeneratorsById[ID.LightCandy] = MakeBasicHealingItem(LocalizationEntries.LightCandyName, 120);

        // Butler Juice.
        itemGeneratorsById[ID.ButlerJuice] = MakeBasicHealingItem(LocalizationEntries.ButJuiceName, 100);

        // Spagetti Code (Not Spaghetti, Spagetti. There's a difference...)
        itemGeneratorsById[ID.SpagettiCode] = MakeBasicHealingItem(LocalizationEntries.SpagettiCodeName, 30, affectsEveryone: true);

        // Java Cookie (Unused again, heals 100HP to Kris and 90HP to everyone else.)
        itemGeneratorsById[ID.JavaCookie] = () =>
        {
            int baseHealQuantity = 90;
            int krisHealQuantity = 100;
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.Healing, LocalizationEntries.Varies];

            return new Item()
            {
                Name = LocalizationEntries.JavaCookieName,
                DescriptionLines = descriptionLines,
                OnUse = h => h.Heal(h == scene?.Heroes.Kris ? krisHealQuantity : baseHealQuantity),
                AffectsEveryone = false
            };
        };

        // Tension Bit.
        itemGeneratorsById[ID.TensionBit] = MakeBasicTPItem(LocalizationEntries.TensionBitName, 32);

        // Tension Gem.
        itemGeneratorsById[ID.TensionGem] = MakeBasicTPItem(LocalizationEntries.TensionGemName, 50);

        // Tension Max.
        itemGeneratorsById[ID.TensionMax] = MakeBasicTPItem(LocalizationEntries.TensionMaxName, 100);

        // Revive Dust.
        itemGeneratorsById[ID.ReviveDust] = () => new Item()
        {
            Name = LocalizationEntries.ReviveDustName,
            DescriptionLines = [LocalizationEntries.Revives, LocalizationEntries.TeamLowercase, LocalizationEntries.ArbitraryPercentage.WithArgs("25")],
            OnUse = h =>
            {
                if (h.HP >= 1)
                    h.Heal(10);
                else
                    h.Revive((int)float.Ceiling(h.MaxHP * 0.25f));
            }
        };

        // Revive Brite (Another unused item).
        itemGeneratorsById[ID.ReviveBrite] = () => new Item()
        {
            Name = LocalizationEntries.ReviveBriteName,
            DescriptionLines = [LocalizationEntries.Revives, LocalizationEntries.TeamLowercase, LocalizationEntries.ArbitraryPercentage.WithArgs("100")],
            OnUse = h =>
            {
                if (h.HP >= 1)
                    h.Heal(10);
                else
                    h.Revive(h.MaxHP);
            }
        };
    }

    private static void LoadChapter3Items()
    {
        // TV Dinner.
        itemGeneratorsById[ID.TVDinner] = MakeBasicHealingItem(LocalizationEntries.TVDinnerName, 100);

        // 35 = Pipis.

        // Flat Soda.
        itemGeneratorsById[ID.FlatSoda] = MakeBasicHealingItem(LocalizationEntries.FlatSodaName, 20);

        // TV Slop.
        itemGeneratorsById[ID.TVSlop] = MakeBasicHealingItem(LocalizationEntries.TVSlopName, 80);

        // Executive Buffet.
        itemGeneratorsById[ID.ExecutiveBuffet] = MakeBasicHealingItem(LocalizationEntries.ExecBuffetName, 100, affectsEveryone: true);

        // Deluxe Dinner.
        itemGeneratorsById[ID.DeluxeDinner] = MakeBasicHealingItem(LocalizationEntries.DeluxeDinnerName, 140);
    }

    private static void LoadChapter4Items()
    {
        // Ancient Sweet. Claims that only
        // Kris can use it ("Kris only") but in reality
        // anyone can use it in battle, but Kris receives
        // a 400 HP boost, whereas everyone else only receives
        // 40 HP.
        itemGeneratorsById[ID.AncientSweet] = () =>
        {
            int baseHealQuantity = 40;
            int krisHealQuantity = 400;
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.AncientSweetDescription1, LocalizationEntries.AncientSweetDescription2];

            return new Item()
            {
                Name = LocalizationEntries.AncientSweetName,
                DescriptionLines = descriptionLines,
                OnUse = h => h.Heal(h == scene?.Heroes.Kris ? krisHealQuantity : baseHealQuantity),
                AffectsEveryone = false
            };
        };

        // Rhapsotea.
        itemGeneratorsById[ID.Rhapsotea] = MakeBasicHealingItem(LocalizationEntries.RhapsoteaName, 115);

        // Scarlixir.
        itemGeneratorsById[ID.Scarlixer] = MakeBasicHealingItem(LocalizationEntries.ScarlixirName, 160);

        // Bitter Tear.
        itemGeneratorsById[ID.BitterTear] = () =>
        {
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.Heals, LocalizationEntries.AllHP];

            return new Item()
            {
                Name = LocalizationEntries.BitterTearName,
                DescriptionLines = descriptionLines,
                OnUse = h => h.Heal(h.MaxHP - h.HP + 999),
                AffectsEveryone = false
            };
        };
    }

    private static void LoadChapter5Items()
    {
        // Punch Bowl.
        itemGeneratorsById[ID.PunchBowl] = MakeBasicHealingItem(LocalizationEntries.PunchBowlName, 200, affectsEveryone: true);

        // Flavigne.
        itemGeneratorsById[ID.Flavigne] = MakeBasicHealingItem(LocalizationEntries.FlavigneName, 130);

        // Green Tea.
        itemGeneratorsById[ID.GreenTea] = MakeBasicHealingItem(LocalizationEntries.GreenTeaName, 180);

        // Orange Juice.
        itemGeneratorsById[ID.OrangeJuice] = MakeBasicHealingItem(LocalizationEntries.OrangeJuiceName, 80);

        // Schadenbrot.
        itemGeneratorsById[ID.Schadenbrot] = MakeBasicHealingItem(LocalizationEntries.SchadenbrotName, 200, affectsEveryone: true);

        // Tree Cake.
        itemGeneratorsById[ID.TreeCake] = MakeBasicHealingItem(LocalizationEntries.TreeCakeName, 160, affectsEveryone: true);

        // S. POTION.
        itemGeneratorsById[ID.SPotion] = MakeBasicHealingItem(LocalizationEntries.SPotionName, 200, overridingDescriptionLines: [LocalizationEntries.Heals, LocalizationEntries.PartyLowercase, LocalizationEntries.MemberLowercase]);

        // Raw moon.
        itemGeneratorsById[ID.RawMoon] = () =>
        {
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.RawMoonDescription1, LocalizationEntries.RawMoonDescription2, LocalizationEntries.RawMoonDescription3];

            return new Item()
            {
                Name = LocalizationEntries.RawMoonName,
                DescriptionLines = descriptionLines,
                OnUse = h =>
                {
                    h.Heal(100);
                    scene?.BattleHandler.TensionPoints = float.Clamp(scene.BattleHandler.TensionPoints + 16f, 0f, 100f);
                },
                AffectsEveryone = false
            };
        };

        // Phanta.
        itemGeneratorsById[ID.Phanta] = () =>
        {
            // Exact same as Raw Moon.
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.RawMoonDescription1, LocalizationEntries.RawMoonDescription2, LocalizationEntries.RawMoonDescription3];

            return new Item()
            {
                Name = LocalizationEntries.PhantaName,
                DescriptionLines = descriptionLines,
                OnUse = h =>
                {
                    h.Heal(100);
                    scene?.BattleHandler.TensionPoints = float.Clamp(scene.BattleHandler.TensionPoints + 16f, 0f, 100f);
                },
                AffectsEveryone = false
            };
        };

        // Flowery Soda.
        itemGeneratorsById[ID.FlowerySoda] = () =>
        {
            // Same as Raw Moon except for the third line.
            LanguageAgnosticText[] descriptionLines = [LocalizationEntries.RawMoonDescription1, LocalizationEntries.RawMoonDescription2, LocalizationEntries.FlowerySodaDescription3];

            return new Item()
            {
                Name = LocalizationEntries.FlowerySodaName,
                DescriptionLines = descriptionLines,
                OnUse = h =>
                {
                    h.Heal(50);
                    scene?.BattleHandler.TensionPoints = float.Clamp(scene.BattleHandler.TensionPoints + 16f, 0f, 100f);
                },
                AffectsEveryone = false
            };
        };

        // Shikacola.
        itemGeneratorsById[ID.Shikacola] = MakeBasicHealingItem(LocalizationEntries.ShikacolaName, 80, affectsEveryone: true);
    }

    private static void LoadChapter6Items()
    {
        // Dark Candy Cane.
        itemGeneratorsById[ID.DarkCandyCane] = MakeBasicHealingItem(LocalizationEntries.DarkCandyCaneName, 220);

        // Pie Cone.
        itemGeneratorsById[ID.PieCone] = MakeBasicHealingItem(LocalizationEntries.PieConeName, 180, affectsEveryone: true);

        // Ice Cube.
        itemGeneratorsById[ID.IceCube] = MakeBasicHealingItem(LocalizationEntries.IceCubeName, 240, overridingDescriptionLines:
            [LocalizationEntries.IceCubeDescription1, LocalizationEntries.IceCubeDescription2, LocalizationEntries.IceCubeDescription3]);

        // Snowwoman Piece.
        itemGeneratorsById[ID.SnowwomanPiece] = MakeBasicHealingItem(LocalizationEntries.SnowwomanPieceName, 300);

        // Nutmegnog.
        itemGeneratorsById[ID.Nutmegnog] = MakeBasicHealingItem(LocalizationEntries.NutmegnogName, 220);

        // Grand Dinner.
        itemGeneratorsById[ID.GrandDinner] = MakeBasicHealingItem(LocalizationEntries.GrandDinnerName, 200, affectsEveryone: true);

        // Gingerbread Monster.
        itemGeneratorsById[ID.GingerbreadMonster] = MakeBasicHealingItem(LocalizationEntries.GingerbreadMonsterName, 220);
    }

    /// <summary>
    ///     Makes a basic healing item.
    /// </summary>
    private static Func<Item> MakeBasicHealingItem(LanguageAgnosticText name, int healQuantity, bool affectsEveryone = false, LanguageAgnosticText[]? overridingDescriptionLines = null)
    {
        LanguageAgnosticText[] descriptionLines = [LocalizationEntries.Heals, LocalizationEntries.ArbitraryHP.WithArgs(healQuantity.ToString())];
        if (affectsEveryone)
            descriptionLines = [LocalizationEntries.Heals, LocalizationEntries.TeamLowercase, LocalizationEntries.ArbitraryHP.WithArgs(healQuantity.ToString())];

        return () => new Item()
        {
            Name = name,
            DescriptionLines = overridingDescriptionLines ?? descriptionLines,
            OnUse = h => h.Heal(healQuantity, playHealSound: h == scene?.Heroes.Kris || !affectsEveryone),
            AffectsEveryone = affectsEveryone
        };
    }

    /// <summary>
    ///     Makes a basic healing item with variable
    ///     healing quantities.
    /// </summary>
    private static Func<Item> MakeVariableHealingItem(LanguageAgnosticText name, int healQuantityMin, int healQuantityMax)
    {
        LanguageAgnosticText[] descriptionLines = [LocalizationEntries.Healing, LocalizationEntries.Varies];
        return () => new Item()
        {
            Name = name,
            DescriptionLines = descriptionLines,
            OnUse = h => h.Heal(scene?.Rng.Next(healQuantityMin, healQuantityMax + 1) ?? healQuantityMin),
            AffectsEveryone = false
        };
    }

    /// <summary>
    ///     Makes a basic tension point granting item.
    /// </summary>
    private static Func<Item> MakeBasicTPItem(LanguageAgnosticText name, int tpPercentage)
    {
        LanguageAgnosticText valueDescriptor = LocalizationEntries.ArbitraryPercentage.WithArgs(tpPercentage.ToString());
        if (tpPercentage >= 100)
            valueDescriptor = LocalizationEntries.Max;

        return () => new Item()
        {
            Name = name,
            DescriptionLines = [LocalizationEntries.Raises, LocalizationEntries.TP, valueDescriptor],
            OnUse = h => { },
            AffectsEveryone = false,
            TensionPointBonusPercentage = tpPercentage
        };
    }
}
