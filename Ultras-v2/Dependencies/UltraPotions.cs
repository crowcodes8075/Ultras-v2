/*
name: UltraPotions
description: Ultra potion helper/presets with BuyReagents enabled
tags: ultra,potions
*/

//cs_include Scripts/Ultras-v2/Dependencies/CoreEngine.cs
//cs_include Scripts/Ultras-v2/Dependencies/CoreUltra.cs
//cs_include Scripts/CoreStory.cs
//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreFarms.cs
//cs_include Scripts/CoreAdvanced.cs
//cs_include Scripts/Ultras-v2/Dependencies/GetPotions.cs

using System;
using System.Linq;
using System.Collections.Generic;
using Skua.Core.Interfaces;

public class UltraPotions
{
    public IScriptInterface Bot => IScriptInterface.Instance;
    public CoreBots Core => CoreBots.Instance;

    public CoreEngine C = new();

    private static CoreUltra Ultra
    {
        get => _Ultra ??= new CoreUltra();
        set => _Ultra = value;
    }
    private static CoreUltra _Ultra;

    private static PotionBuyer Pots
    {
        get => _Pots ??= new PotionBuyer();
        set => _Pots = value;
    }
    private static PotionBuyer _Pots;

    #region Class Detection

    public string NormalizeString(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? string.Empty
            : s.Replace(" ", "").ToLowerInvariant();

    public bool HasAssignedClass(string assignedClass) =>
        NormalizeString(Bot.Player.CurrentClass?.Name)
        == NormalizeString(assignedClass);

    #endregion

    #region Presets

    public string[] GetRecommendedPotions(string context = "")
    {
        if (context.Equals("Dage", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Fate Tonic",
                "Potent Destruction Elixir",
                "Potent Honor Potion"
            };

            // fallthrough to default presets if no Dage-specific override for this class
        }

        if (HasAssignedClass("ArchPaladin"))
            return new[]
            {
                "Fate Tonic",
                "Potent Destruction Elixir",
                "Potent Honor Potion"
            };

        if (HasAssignedClass("StoneCrusher"))
            return new[]
            {
                "Body Tonic",
                "Potent Malevolence Elixir",
                "Potent Honor Potion"
            };

        if (HasAssignedClass("Lord of Order"))
            return new[]
            {
                "Body Tonic",
                "Potent Malevolence Elixir",
                "Potent Honor Potion"
            };

        if (HasAssignedClass("King's Echo"))
            return new[]
            {
                "Sage Tonic",
                "Potent Destruction Elixir",
                "Potent Honor Potion"
            };

        if (HasAssignedClass("Shaman"))
            return new[]
            {
                "Sage Tonic",
                "Potent Malevolence Elixir",
                "Potent Honor Potion"
            };

        if (HasAssignedClass("Dragon of Time"))
            return new[]
            {
                "Sage Tonic",
                "Potent Malevolence Elixir",
                "Potent Honor Potion"
            };
        // Default fallback when the current class does not match any preset
        return new[]
        {
            "Body Tonic",
            "Potent Destruction Elixir",
            "Potent Honor Potion"
        };
    }

    #endregion

    #region Potion Usage

    /// <summary>
    /// Activates the currently equipped consumable potion (skill slot 5) if its aura is not already active.
    /// Covers: Potent Honor Potion, Potent Life Potion, Felicitous Philtre.
    /// Call this once per combat loop iteration.
    /// </summary>
    public void ActivateEquippedPotion()
    {
        string? aura = null;

        if (Bot.Inventory.IsEquipped("Potent Honor Potion"))
            aura = "Potent Honor Malice";
        else if (Bot.Inventory.IsEquipped("Potent Life Potion"))
            aura = "Righteous";
        else if (Bot.Inventory.IsEquipped("Felicitous Philtre"))
            aura = "Felicitous Philtre";
        else if (Bot.Inventory.IsEquipped("Endurance Draught"))
            aura = "Endurance Draught";

        if (string.IsNullOrWhiteSpace(aura) || C.HasAura(aura, true))
            return;

        while (!Bot.ShouldExit && !C.HasAura(aura, true))
        {
            if (!C.Cast(5))
                break;

            Bot.Sleep(100);
        }
    }

    public void UseRecommendedPotions(int desiredQuant = 10, bool skipThird = false, string context = "", bool ensureStock = false)
    {
        string[] potions = GetRecommendedPotions(context);

        if (potions.Length == 0)
            return;

        if (skipThird && potions.Length >= 3)
            potions = potions[..2];

        if (ensureStock)
            EnsurePotions(desiredQuant, skipThird, context);

        foreach (string potion in potions)
        {
            // Already active — aura name matches potion name for most consumables
            if (Bot.Self.Auras.Any(a => a != null && a.Name == potion))
            {
                Core.Logger($"{potion} aura already active.");
                continue;
            }

            Core.Logger($"Equipping {potion}...");

            C.EquipConsumable(potion);

            Bot.Sleep(500);

            // Some consumables auto-use when equipped
            if (Bot.Self.Auras.Any(a => a != null && a.Name == potion))
            {
                Core.Logger($"{potion} successfully applied.");
                continue;
            }

            // If not equipped somehow, stop here
            if (!Bot.Inventory.IsEquipped(potion))
            {
                Core.Logger($"Failed to equip {potion}.");
                Bot.Sleep(200);
                continue;
            }

            Core.Logger($"Using {potion}...");

            Core.UsePotion();

            Bot.Sleep(500);
        }
    }

    #endregion

    #region Potion Purchase

    public void EnsurePotions(int desiredQuant = 10, bool skipThird = false, string context = "")
    {
        string[] potions = GetRecommendedPotions(context);

        if (potions.Length == 0)
            return;

        if (skipThird && potions.Length >= 3)
            potions = potions[..2];

        List<string> missing = new();

        foreach (string potion in potions)
        {
            int current = Bot.Inventory.GetQuantity(potion);

            if (current < desiredQuant)
            {
                int needed = desiredQuant - current;

                Core.Logger($"Need {needed}x more {potion}.");

                missing.Add(potion);
            }
        }

        if (missing.Count == 0)
        {
            Core.Logger("All recommended potions already stocked.");
            return;
        }

        Pots.INeedYourStrongestPotions(
            Potions: missing.ToArray(),
            PotionQuant: desiredQuant,
            Seperate: true,
            BuyReagents: true
        );

        missing = missing
            .Where(potion => Bot.Inventory.GetQuantity(potion) < desiredQuant)
            .ToList();

        if (missing.Count > 0)
        {
            Core.Logger(
                $"Potion purchase ended with missing items: {string.Join(", ", missing)}. " +
                "No further purchase retries are performed to avoid infinite loops.",
                messageBox: true,
                stopBot: false
            );
            return;
        }

        Core.Logger("Potion purchase complete.");
    }

    public void EnsureRecommendedPotions(int desiredQuant = 10, bool skipThird = false, string context = "")
        => EnsurePotions(desiredQuant, skipThird, context);

    #endregion
}
