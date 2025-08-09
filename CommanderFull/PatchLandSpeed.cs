using System.Reflection.Emit;
using Dawnsbury.Core.Creatures;
using HarmonyLib;

namespace CommanderFull;

public class PatchLandSpeed
{
    [HarmonyPatch(typeof(Creature), nameof(Creature.RecalculateLandSpeedAndInitiative))]
    internal static class ArmorRegiment
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var shouldSkipMethod = AccessTools.Method(typeof(MyConditionPatch), nameof(MyConditionPatch.ShouldSkip));
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode != OpCodes.Ldfld || codes[i + 1].opcode != OpCodes.Brtrue_S) continue;
                object? branchTarget = codes[i + 1].operand;
                codes.RemoveAt(i + 1);
                codes.InsertRange(i + 1, new[]
                {
                    new CodeInstruction(OpCodes.Or),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, shouldSkipMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, branchTarget)
                });
                break;
            }
            return codes;
        }
    }
    public static class MyConditionPatch
    {
        public static bool ShouldSkip(Creature creature)
        {
            return creature.HasEffect(ModData.MQEffectIds.ArmorRegiment);
        }
    }
}