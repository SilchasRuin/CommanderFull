using Dawnsbury.Mods.DawnniExpanded;
using HarmonyLib;

namespace CommanderFull;
[HarmonyPatch(typeof(FeatBattleMedicine), nameof(FeatBattleMedicine.ExpertBattleMedAction))]
public class PatchEx
{
    
}