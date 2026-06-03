/*
name: UltraEnhancements
description: Centralised enhancement presets for all Ultra boss scripts.
tags: ultra, enhancements
*/

//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreFarms.cs
//cs_include Scripts/CoreAdvanced.cs

using Skua.Core.Interfaces;
using Skua.Core.Models.Items;

public class UltraEnhancements
{
    public IScriptInterface Bot => IScriptInterface.Instance;
    private CoreBots C => CoreBots.Instance;

    private static CoreAdvanced Adv
    {
        get => _Adv ??= new CoreAdvanced();
        set => _Adv = value;
    }
    private static CoreAdvanced _Adv;

    /// <summary>
    /// Applies the recommended enhancement preset for the currently equipped class.
    /// Falls back to SmartEnhance if the class has no explicit preset.
    /// </summary>
    public void Apply()
    {
        string className = Bot.Player?.CurrentClass?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(className))
            return;

        C.Logger($"[UltraEnhancements] Enhancing for: {className}");

        switch (className)
        {
            // ── Taunters / Support ────────────────────────────────────────────

            case "ArchPaladin":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Lacerate,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Lord Of Order":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Awe_Blast,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "StoneCrusher":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Praxis,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Void Highlord":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Anima,
                    wSpecial: WeaponSpecial.Valiance,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            // ── DPS ───────────────────────────────────────────────────────────

            case "Legion Revenant":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Wizard,
                    hSpecial: HelmSpecial.Pneuma,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Verus DoomKnight":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Anima,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Chrono ShadowSlayer":
            case "Chrono ShadowHunter":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Vim,
                    wSpecial: WeaponSpecial.Valiance,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Chaos Avenger":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Anima,
                    wSpecial: WeaponSpecial.Valiance,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "King's Echo":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    hSpecial: HelmSpecial.Examen,
                    wSpecial: WeaponSpecial.Spiral_Carve,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "ArchFiend":
            case "Archfiend":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Legendary Hero":
            case "Dark Legendary Hero":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Anima,
                    wSpecial: WeaponSpecial.Valiance,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Great Thief":
                Adv.EnhanceEquipped(
                    hSpecial: Adv.uVim() ? HelmSpecial.Vim : HelmSpecial.Forge,
                    type: EnhancementType.Lucky,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Arachnomancer":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Vim,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Sentinel":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Anima,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Lich":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Examen,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Penitence
                );
                break;

            case "Arcana Invoker":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Examen,
                    wSpecial: WeaponSpecial.Elysium,
                    cSpecial: CapeSpecial.Absolution
                );
                break;

            case "Light Caster":
            case "LightCaster":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Pneuma,
                    wSpecial: Adv.uDauntless() ? WeaponSpecial.Ravenous : WeaponSpecial.Praxis,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Shaman":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    wSpecial: WeaponSpecial.Mana_Vamp,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Master Ranger":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Anima,
                    wSpecial: Adv.uDauntless() ? WeaponSpecial.Arcanas_Concerto : WeaponSpecial.Praxis,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Quantum Chronomancer":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Anima,
                    wSpecial: WeaponSpecial.Dauntless,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Phantom Chronomancer":
            case "Phantasm Chronomancer":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Wizard,
                    hSpecial: HelmSpecial.Examen,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Infinity Knight":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Wizard,
                    hSpecial: HelmSpecial.Pneuma,
                    wSpecial: WeaponSpecial.Dauntless,
                    cSpecial: CapeSpecial.Forge
                );
                break;

            case "Hollowborn Vindicator":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Dauntless,
                    cSpecial: CapeSpecial.Penitence
                );
                break;

            case "Chaos Slayer":
            case "Chaos Slayer Berserker":
            case "Chaos Slayer Cleric":
            case "Chaos Slayer Mystic":
            case "Chaos Slayer Thief":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Alpha Omega":
            case "Alpha DOOMmega":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Vim,
                    wSpecial: WeaponSpecial.Praxis,
                    cSpecial: CapeSpecial.Avarice
                );
                break;

            case "Guardian":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Ravenous,
                    cSpecial: CapeSpecial.Penitence
                );
                break;

            case "Paladin Chronomancer":
            case "Obsidian Paladin Chronomancer":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    hSpecial: HelmSpecial.Pneuma,
                    wSpecial: WeaponSpecial.Mana_Vamp,
                    cSpecial: CapeSpecial.Absolution
                );
                break;

            // ── Mage / Healer ─────────────────────────────────────────────────

            case "Dragon of Time":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Wizard,
                    hSpecial: HelmSpecial.Examen,
                    wSpecial: WeaponSpecial.Awe_Blast,
                    cSpecial: CapeSpecial.Vainglory
                );
            break;

            case "Blaze Binder":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Wizard,
                    hSpecial: HelmSpecial.Pneuma,
                    wSpecial: WeaponSpecial.Elysium,
                    cSpecial: CapeSpecial.Lament
                );
                break;

            case "Healer":
            case "Healer (Rare)":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    wSpecial: WeaponSpecial.Elysium
                );
                break;

            // ── Fallback ──────────────────────────────────────────────────────

            default:
                C.Logger($"[UltraEnhancements] No preset for '{className}', using SmartEnhance.");
                Adv.SmartEnhance(className);
                break;
        }
    }

    /// <summary>
    /// Applies the Ultra Dage enhancement strategy for all classes.
    /// Uses fixed Lucky across all gear with Health Vamp on weapon — no Forge anywhere,
    /// since healing is detrimental in the Dage fight.
    /// </summary>
    public void ApplyDage()
    {
        string className = Bot.Player?.CurrentClass?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(className))
            return;

        C.Logger($"[UltraEnhancements] Dage enhancing for: {className} using fixed Lucky preset");

        switch (className)
        {
            case "King's Echo":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    hSpecial: HelmSpecial.Examen,
                    wSpecial: WeaponSpecial.Health_Vamp,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "ArchPaladin":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    wSpecial: WeaponSpecial.Health_Vamp,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Lord Of Order":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    wSpecial: WeaponSpecial.Health_Vamp,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "StoneCrusher":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Healer,
                    wSpecial: WeaponSpecial.Health_Vamp,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            default:
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    wSpecial: WeaponSpecial.Health_Vamp
                );
                break;
        }
    }

    public void ApplyGramiel()
    {
        string className = Bot.Player?.CurrentClass?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(className))
            return;

        C.Logger($"[UltraEnhancements] Gramiel enhancing for: {className}");

        switch (className)
        {

            default:
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Health_Vamp,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;
        }
    }

    /// <summary>
    /// Applies the Ultra Darkon enhancement strategy for all classes.
    /// Uses fixed Lucky across all gear with Health Vamp on weapon, similar to the Dage flow.
    /// </summary>
    public void ApplyDarkon()
    {
        string className = Bot.Player?.CurrentClass?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(className))
            return;

        C.Logger($"[UltraEnhancements] Darkon enhancing for: {className} using fixed Lucky preset");

        switch (className)
        {
            case "King's Echo":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Examen,
                    wSpecial: WeaponSpecial.Spiral_Carve,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "ArchPaladin":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Lacerate,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Lord Of Order":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Awe_Blast,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "StoneCrusher":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    hSpecial: HelmSpecial.Forge,
                    wSpecial: WeaponSpecial.Praxis,
                    cSpecial: CapeSpecial.Vainglory
                );
                break;

            case "Shaman":
                Adv.EnhanceEquipped(
                    type: EnhancementType.Lucky,
                    wSpecial: WeaponSpecial.Mana_Vamp,
                    cSpecial: CapeSpecial.Lament
                );
                break;
        }
    }

    /// <summary>
    /// Applies the Ultra No Vainglory enhancement strategy for all classes.
    /// Runs the Darkon enhancement preset first, then forces the equipped cape to Forge.
    /// </summary>
    public void ApplyNoVainglory()
    {
        // Apply the Darkon class preset first.
        Apply();

        string className = Bot.Player?.CurrentClass?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(className))
            return;

        InventoryItem? cape = Bot.Inventory.Items.Find(i => i.Equipped && i.Category == ItemCategory.Cape);
        if (cape == null)
        {
            C.Logger($"[UltraEnhancements] No equipped cape found for Nulgath override ({className}).");
            return;
        }

        C.Logger($"[UltraEnhancements] NoVainglory enhancing for: {className} and forcing Forge cape");
        Adv.EnhanceItem(cape.Name, EnhancementType.Lucky, cSpecial: CapeSpecial.Absolution);
    }
}

