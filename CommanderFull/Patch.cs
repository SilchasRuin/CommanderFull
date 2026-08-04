using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Controls;
using Dawnsbury.ThirdParty.SteamApi;
using HarmonyLib;
using static CommanderFull.ModData;

namespace CommanderFull;
[HarmonyPatch(typeof(Creature), nameof(Creature.DetermineLandSpeed))]
internal static class ArmorRegiment
{
    internal static void Postfix(Creature __instance, ref (int, string) __result)
    {
        if (__instance.HasEffect(QEffectId.UnburdenedIron) || !__instance.HasEffect(MQEffectIds.ArmorRegiment))
            return;
        if (__instance.Armor.SpeedBonus >= 0)
            return;
        int original = __result.Item1;
        __result.Item1 +=  Math.Abs(__instance.Armor.SpeedBonus);
        if (__result.Item2 == "") return;
        List<Bonus> armorBonus = [new(__instance.Armor.SpeedBonus, BonusType.Untyped, __instance.Armor.Item?.Name.Capitalize() ?? "")];
        (int bonusTotal, string bonusDescription) = Bonus.CalculateBestNonNull(armorBonus, true, true);
        __result.Item2 = __result.Item2.Replace("\n" + bonusDescription, "");
        __result.Item2 = __result.Item2.Replace($"{{b}}{original * 5} feet{{/b}} Final speed", $"{{b}}{__result.Item1 * 5} feet{{/b}} Final speed");
    }
}

[HarmonyPatch(typeof(RunestoneRules), nameof(RunestoneRules.DetachSubItem))]
internal static class PatchRuneCost
{
    internal static bool Prefix(int priceOfDetaching,
        InventoryItemSlot itemSlot,
        Item rune,
        Item slotItem,
        Action onSuccessfulDetach)
    {
        if (rune.RuneProperties == null || rune.RuneProperties.RuneKind != MRuneKinds.MagicalBanner) return true;
        AltDetach.Detach(itemSlot, onSuccessfulDetach, slotItem, rune);
        return false;
    }
}

[HarmonyPatch(typeof(Item), nameof(Item.WithModification))]
internal static class PatchModification
{
    internal static bool Prefix(ItemModification itemModification, Item __instance)
    {
        if (itemModification.Kind != ItemModificationKind.Rune || BannerItem.LoadBanners().All(name => name != itemModification.ItemName)
            || !__instance.HasTrait(Trait.SpecificMagicWeapon)) return true;
        RunestoneRules.AddRuneTo(Items.CreateNew(itemModification.ItemName), __instance);
        __instance.ItemModifications.Add(itemModification);
        Action<Item>? modifyItem = itemModification.ModifyItem;
        if (modifyItem != null)
            modifyItem(__instance);
        Action<Item, ItemModification>? withModification = __instance.AfterModifiedWithModification;
        if (withModification != null)
            withModification(__instance, itemModification);
        return false;
    }
}

[HarmonyPatch(typeof(RunestoneRules), nameof(RunestoneRules.AttachSubitem))]
internal static class PatchRuneAttach
{
    internal static bool Prefix(Item runestone, Item? equipment, ref RunestoneRules.SubitemAttachmentResult __result)
    {
        if (runestone.RuneProperties == null || runestone.RuneProperties.RuneKind != MRuneKinds.MagicalBanner || runestone.RuneProperties.RuneKind != MRuneKinds.Banner || equipment == null || !equipment.HasTrait(Trait.SpecificMagicWeapon)) return true;
        if (equipment.StoresItem != null) return true;
        RuneProperties rune = runestone.RuneProperties;
        int num1 = equipment.Runes.Count(itm => itm.RuneProperties?.RuneKind == rune.RuneKind);
        __result = num1 switch
        {
            > 0 when equipment.Runes.Any(rn => rn.ItemName == runestone.ItemName) => new
                RunestoneRules.SubitemAttachmentResult(RunestoneRules.SubitemAttachmentResultKind.Unallowed,
                    "This item already has that banner."),
            <= 0 => new RunestoneRules.SubitemAttachmentResult(
                RunestoneRules.SubitemAttachmentResultKind.PlaceAsSubitem, ActionIfItGoesThrough: (Action)(() =>
                {
                    equipment.WithModification(new ItemModification(ItemModificationKind.Rune)
                    {
                        ItemName = runestone.ItemName
                    });
                    Steam.CollectAchievement("CRAFTING");
                    Sfxs.Play(SfxName.AttachRune);
                })),
            _ => new RunestoneRules.SubitemAttachmentResult(RunestoneRules.SubitemAttachmentResultKind.SwapOrUpgradeRune)
        };
        return false;
    }
}

[HarmonyPatch(typeof(RunestoneRules), nameof(RunestoneRules.AddRuneTo))]
internal static class PatchRuneAdd
{
    internal static bool Prefix(Item runestone, Item equipment)
    {
        if (runestone.RuneProperties == null || runestone.RuneProperties.RuneKind != MRuneKinds.MagicalBanner || runestone.RuneProperties.RuneKind != MRuneKinds.Banner || !equipment.HasTrait(Trait.SpecificMagicWeapon)) return true;
        AltAttach.Attach(runestone, equipment);
        return false;
    }
}

[HarmonyPatch(typeof(TargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement),
    nameof(TargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement.Satisfied))]
internal static class PatchTargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement
{
    internal static bool Prefix(Creature source, Creature target, ref Usability __result)
    {
        if (source.FindQEffect(MQEffectIds.TheBiggerTheyAre) is not {} qEffect || target.HasTrait(Trait.Object))
            return true;
        int num = target.Space.SizeCategory - source.Space.SizeCategory;
        int wrestler = target.HasEffect(QEffectId.TitanWrestlerLegendary) ? 3 :
            target.HasEffect(QEffectId.TitanWrestler) ? 2 : 1;
        int num2 = qEffect.Value + wrestler ;
        if (num2 >= num)
        {
            __result = Usability.Usable;
            return false;
        }
        __result = Usability.CommonReasons.TargetTooLarge;
        return false;
    }
}

internal static class AltDetach
{
    internal static void Detach(InventoryItemSlot itemSlot, Action onSuccessfulDetach, Item slotItem, Item rune)
    {
        Sfxs.Play(SfxName.PutDown);
        itemSlot.ReplaceSelf(RunestoneRules.RecreateWithUnattachedSubitem(slotItem, rune, slotItem.StoresItem == null));
        onSuccessfulDetach();
    }
}

internal static class AltAttach
{
    internal static void Attach(Item runestone, Item equipment)
    {
        Item itemTemplate = Items.GetItemTemplate(equipment.ItemName);
        if (equipment.Runes.Count == 0)
            equipment.Price = itemTemplate.Price > runestone.Price ? itemTemplate.Price : 0;
        equipment.Price += runestone.Price;
        if (runestone.Level > equipment.Level)
            equipment.Level = runestone.Level;
        runestone.RuneProperties!.ApplyRuneOntoItem(runestone, equipment);
        if (!string.IsNullOrWhiteSpace(equipment.Description))
            equipment.Description += "\n";
        equipment.Description = $"{equipment.Description}{{b}}{runestone.RuneProperties.Prefix.Capitalize()}.{{/b}} {runestone.RuneProperties.RulesText}";
        equipment.ProsaicName = itemTemplate.Name;
        List<Item> list2 = equipment.Runes
            .OrderByDescending(rune => rune.RuneProperties!.RuneKind)
            .ToList();
        equipment.Runes.Add(runestone);
        foreach (Item obj3 in list2)
        {
            equipment.ProsaicName =
                $"{obj3.RuneProperties!.Prefix} {equipment.Name}";
        }
    }
}

public static class MyConditionPatch
{
    public static bool ShouldSkip(Creature creature)
    {
        return creature.HasEffect(MQEffectIds.ArmorRegiment);
    }
}