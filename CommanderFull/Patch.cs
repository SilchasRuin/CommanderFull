using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Dawnsbury.Core.Creatures;
using HarmonyLib;

namespace CommanderFull;
[HarmonyLib.HarmonyPatch(typeof(Creature), nameof(Creature.RecalculateLandSpeedAndInitiative))]
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
public static class MyConditionPatch
{
    public static bool ShouldSkip(Creature creature)
    {
        return creature.HasEffect(ModData.MQEffectIds.ArmorRegiment);
    }
}