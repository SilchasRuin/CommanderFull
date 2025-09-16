using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Dawnsbury.Audio;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Controls;
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
internal static class AltDetach
{
    internal static void Detach(InventoryItemSlot itemSlot, Action onSuccessfulDetach, Item slotItem, Item rune)
    {
        Sfxs.Play(SfxName.PutDown);
        itemSlot.ReplaceSelf(RunestoneRules.RecreateWithUnattachedSubitem(slotItem, rune, slotItem.StoresItem == null));
        onSuccessfulDetach();
    }
}
public static class MyConditionPatch
{
    public static bool ShouldSkip(Creature creature)
    {
        return creature.HasEffect(ModData.MQEffectIds.ArmorRegiment);
    }
}