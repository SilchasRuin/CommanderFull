using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Dawnsbury.Core.Creatures;
using HarmonyLib;

namespace CommanderFull;
[HarmonyPatch(typeof(Creature), nameof(Creature.RecalculateLandSpeedAndInitiative))]
internal static class ArmorRegiment
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        IEnumerable<CodeInstruction> enumerable = instructions.ToList();
        IEnumerable<CodeInstruction> codeInstructions = instructions as CodeInstruction[] ?? enumerable.ToArray();
        try
        {
            // 1) Find the closure/display-class type that contains the 'unburdenedIron' field.
            string closureTypeName = "Dawnsbury.Core.Creatures.Creature+<>c__DisplayClass340_0";
            Type? closureType = AccessTools.TypeByName(closureTypeName);
            FieldInfo? targetField = null;
            if (closureType != null)
                targetField = closureType.GetField("unburdenedIron", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField == null)
            {
                File.AppendAllText("HarmonyFailLog.txt", $"[ArmorRegiment] Could not locate closure field 'unburdenedIron'. closureType: {closureType?.FullName}\n");
                return enumerable;
            }
            MethodInfo shouldSkipMethod = AccessTools.Method(typeof(MyConditionPatch), nameof(MyConditionPatch.ShouldSkip));
            var matcher = new CodeMatcher(codeInstructions, generator);
            // Attempt to find the exact sequence: ldfld <closureType>::unburdenedIron  followed by brtrue.s or brtrue
            var matched = matcher.MatchStartForward(
                new CodeMatch(ci => ci.opcode == OpCodes.Ldfld && OperandIsFieldEqual(ci.operand, targetField)),
                new CodeMatch(ci => ci.opcode == OpCodes.Brtrue_S || ci.opcode == OpCodes.Brtrue)
            );
            if (matched.IsInvalid || matcher.IsInvalid)
            {
                File.AppendAllText("HarmonyFailLog.txt", "[ArmorRegiment] Pattern (ldfld unburdenedIron + brtrue) not found.\n");
                return codeInstructions;
            }
            // Position currently at the start of the match (ldfld).
            // Advance to the branch instruction (index + 1)
            matcher.Advance(1);
            // Save branch opcode/operand (preserve either brtrue.s or brtrue)
            var branchInstr = matcher.Instruction;
            var branchOp = branchInstr.opcode;
            matcher.Insert(
                new CodeInstruction(OpCodes.Or),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, shouldSkipMethod)
            );
            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            File.AppendAllText("HarmonyFailLog.txt", $"[ArmorRegiment] Exception in transpiler: {ex}\n");
            return codeInstructions;
        }
    }

    // Helper: compares a CodeInstruction.operand to a FieldInfo in a safe way.
    private static bool OperandIsFieldEqual(object? operand, FieldInfo? field)
    {
        if (operand == null || field == null) return false;
        try
        {
            if (operand is FieldInfo fi)
            {
                return fi.Name == field.Name && fi.DeclaringType?.FullName == field.DeclaringType?.FullName;
            }
            // Mono.Cecil.FieldReference case
            var opType = operand.GetType();
            var nameProp = opType.GetProperty("Name");
            var declaringTypeProp = opType.GetProperty("DeclaringType");
            if (nameProp != null && declaringTypeProp != null)
            {
                var opName = nameProp.GetValue(operand) as string;
                var declaringTypeRef = declaringTypeProp.GetValue(operand);
                string? declaringTypeFullName = null;
                if (declaringTypeRef != null)
                {
                    var dtNameProp = declaringTypeRef.GetType().GetProperty("FullName");
                    if (dtNameProp != null)
                        declaringTypeFullName = dtNameProp.GetValue(declaringTypeRef) as string;
                }

                if (opName != null && declaringTypeFullName != null)
                {
                    return opName == field.Name && declaringTypeFullName == field.DeclaringType?.FullName;
                }
            }
            // Fallback: string compare
            var s = operand.ToString();
            return s != null && s.Contains(field.Name) && field.DeclaringType?.Name != null && s.Contains(field.DeclaringType?.Name!);
        }
        catch
        {
            return false;
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