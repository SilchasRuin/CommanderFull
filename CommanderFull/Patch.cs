using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Controls;
using Dawnsbury.ThirdParty.SteamApi;
using HarmonyLib;

namespace CommanderFull;
[HarmonyPatch(typeof(Creature), nameof(Creature.RecalculateLandSpeedAndInitiative))]
internal static class ArmorRegiment
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        //match: ldloc. to ldfld bool unburdenedIron to brtrue/brtrue.s
        matcher.MatchStartForward(
            new CodeMatch(ci => ci.opcode.Name != null && ci.opcode.Name.StartsWith("ldloc")),
            new CodeMatch(ci => ci.opcode == OpCodes.Ldfld && IsUnburdenedIronField(ci.operand)),
            new CodeMatch(ci => ci.opcode == OpCodes.Brtrue || ci.opcode == OpCodes.Brtrue_S)
        );
        if (!matcher.IsValid) return matcher.InstructionEnumeration();
        MethodInfo shouldSkipMethod = AccessTools.Method(typeof(MyConditionPatch), nameof(MyConditionPatch.ShouldSkip));
        //advance to before the branch
        matcher.Advance(2);
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, shouldSkipMethod),
            new CodeInstruction(OpCodes.Or)
        );
        return matcher.InstructionEnumeration();
    }

    private static bool IsUnburdenedIronField(object operand)
    {
        if (operand is FieldInfo fieldInfo)
            return fieldInfo.FieldType == typeof(bool) && fieldInfo.Name == "unburdenedIron";
        //fallback to string check
        string? opStr = operand.ToString();
        return opStr != null && opStr.Contains("unburdenedIron") && opStr.Contains("Boolean");
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
        if (rune.RuneProperties == null || rune.RuneProperties.RuneKind != ModData.MRuneKinds.MagicalBanner) return true;
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
        if (runestone.RuneProperties == null || runestone.RuneProperties.RuneKind != ModData.MRuneKinds.MagicalBanner || runestone.RuneProperties.RuneKind != ModData.MRuneKinds.Banner || equipment == null || !equipment.HasTrait(Trait.SpecificMagicWeapon)) return true;
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
        if (runestone.RuneProperties == null || runestone.RuneProperties.RuneKind != ModData.MRuneKinds.MagicalBanner || runestone.RuneProperties.RuneKind != ModData.MRuneKinds.Banner || !equipment.HasTrait(Trait.SpecificMagicWeapon)) return true;
        AltAttach.Attach(runestone, equipment);
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
        runestone.RuneProperties!.ModifyItem(equipment);
        if (!string.IsNullOrWhiteSpace(equipment.Description))
            equipment.Description += "\n";
        equipment.Description = $"{equipment.Description}{{b}}{runestone.RuneProperties.Prefix.Capitalize()}.{{/b}} {runestone.RuneProperties.RulesText}";
        equipment.Name = itemTemplate.Name;
        List<Item> list2 = equipment.Runes
            .OrderByDescending(rune => rune.RuneProperties!.RuneKind)
            .ToList();
        equipment.Runes.Add(runestone);
        foreach (Item obj3 in list2)
        {
            equipment.Name =
                $"{obj3.RuneProperties!.Prefix} {equipment.Name}";
        }
    }
}

public static class MyConditionPatch
{
    public static bool ShouldSkip(Creature creature)
    {
        return creature.HasEffect(ModData.MQEffectIds.ArmorRegiment);
    }
}