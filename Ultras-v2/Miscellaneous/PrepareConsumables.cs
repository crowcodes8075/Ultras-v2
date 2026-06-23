/*
name: PrepareConsumables
description: Farms recommended Ultra potions for the current class and crafts Scroll of Enrage.
tags: Miscellaneous, consumables, potions, enrage
*/

//cs_include Scripts/Ultras-v2/Dependencies/CoreEngine.cs
//cs_include Scripts/Ultras-v2/Dependencies/CoreUltra.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraPotions.cs
//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreStory.cs

using System;
using Skua.Core.Interfaces;
using Skua.Core.Options;

public class PrepareConsumables
{
    private CoreBots C => CoreBots.Instance;
    public IScriptInterface Bot => IScriptInterface.Instance;
    public CoreEngine Core = new();
    public CoreUltra Ultra = new();
    public UltraPotions Pots = new();

    public bool DontPreconfigure = true;
    public string OptionsStorage = "PrepareConsumables";
    public List<IOption> Options = new()
    {
        new Option<int>("PotionQuantity", "Potion Quantity", "How many of each recommended potion to keep stocked.", 10),
        new Option<int>("ScrollQuantity", "Scroll Quantity", "How many Scroll of Enrage to keep stocked.", 10),
        CoreBots.Instance.SkipOptions,
    };

    public void ScriptMain(IScriptInterface bot)
    {
        C.SetOptions();
        Core.Boot();

        try
        {
            PrepConsumables();
        }
        finally
        {
            C.SetOptions(false);
            Bot.StopSync();
        }
    }

    private void PrepConsumables()
    {
        int potionQuant = Bot.Config!.Get<int>("PotionQuantity");
        int scrollQuant = Bot.Config!.Get<int>("ScrollQuantity");

        string[] recommended = Pots.GetRecommendedPotions();
        if (recommended.Length == 0)
        {
            C.Logger("No recommended potions were detected for the current class.");
        }
        else
        {
            C.Logger($"Recommended potions: {string.Join(", ", recommended)}");
            Pots.PreparePotions(potionQuant);
        }

        EnsureScrollOfEnrage(scrollQuant);
    }

    private void EnsureScrollOfEnrage(int desiredCount)
    {
        const string scroll = "Scroll of Enrage";

        if (desiredCount <= 0)
        {
            C.Logger("Scroll of Enrage quantity is set to 0 or less, skipping craft.");
            return;
        }

        if (!Core.Faction("SpellCrafting", 5))
        {
            C.Logger("SpellCrafting rank 5 is required to craft Scroll of Enrage.");
            return;
        }

        while (!Bot.ShouldExit && !C.CheckInventory(scroll, desiredCount))
        {
            int current = Bot.Inventory.GetQuantity(scroll);
            if (current >= desiredCount)
                break;

            C.Logger($"Crafting Scroll of Enrage ({current}/{desiredCount})...");
            Core.ForItem("Undead Infantry", "underworld", "Mystic Parchment", 2);
            Core.BuyItem("Zealous Ink", 549, "dragonrune", 5, calculateRemaining: false);

            Core.Join("spellcraft");
            Bot.Drops.Add(scroll);
            Bot.Send.Packet("%xt%zm%crafting%1%spellOnStart%7%1555%Spell%");
            Bot.Sleep(5000);
            Bot.Send.Packet("%xt%zm%crafting%1%spellComplete%7%2330%Enrage%");
            Core.WaitForDrop(scroll, 10000);
            Core.Pickup(scroll);
            Bot.Drops.Remove(scroll);

            if (Bot.ShouldExit)
                break;
        }

        int finalCount = Bot.Inventory.GetQuantity(scroll);
        C.Logger($"Scroll of Enrage stock: {finalCount}/{desiredCount}.");

        if (finalCount < desiredCount)
            C.Logger("Could not craft the requested number of Scroll of Enrage.", stopBot: false);
    }
}
