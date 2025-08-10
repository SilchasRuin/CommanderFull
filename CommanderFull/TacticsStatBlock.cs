using System.Text;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using static CommanderFull.ModData;

namespace CommanderFull;

public abstract class TacticsStatBlock
{
    public static string DescribePreparedTactics(Creature owner)
    {
        if (!owner.HasTrait(MTraits.Commander)) return "";
        Dictionary<string, object?>? tags = owner.PersistentCharacterSheet?.Calculated.Tags.Where(pair =>
                pair.Value is List<Trait> list &&
                list.Contains(MTraits.Tactic))
            .ToDictionary();
        if (tags == null)
            return "";
        int dc = owner.ClassDC(MTraits.Commander);
        string tacticsPrepared = string.Join("",
            tags
                .GroupBy(tactic => (tactic.Value as List<Trait>)!.FirstOrDefault(trait => trait == MTraits.MobilityTactic ||  trait == MTraits.OffensiveTactic || trait == MTraits.ExpertTactic || trait == MTraits.MasterTactic || trait == MTraits.LegendaryTactic))
                .OrderBy(rg => AssignValueByTrait(rg.Key))
                .Select(tg =>
                {
                    string type = "{b}" + tg.Key.GetTraitProperties().HumanizedName + ":{/b}";
                    string tactics = string.Join(", ",
                        tg.GroupBy(tn => tn.Key)
                            .OrderBy(lg => lg.Key[0])
                            .Select(tName =>
                            {
                                string tacticName = tName.Key.Remove(tName.Key.IndexOf(" {icon:", StringComparison.Ordinal));
                                return tacticName[..].ToLower();
                            }));
                    return type + " {i}" + tactics + "{/i}\n";
                })
        );
        return $"{{b}}DC{{/b}} {dc}"
               + $"\n{tacticsPrepared}";
    }
    public static int AssignValueByTrait(Trait trait)
    {
        if (trait == MTraits.MobilityTactic)
            return 0;
        if (trait == MTraits.OffensiveTactic)
            return 1;
        if (trait == MTraits.ExpertTactic)
            return 2;
        if (trait == MTraits.MasterTactic)
            return 3;
        return trait == MTraits.LegendaryTactic ? 4 : 0;
    }
}