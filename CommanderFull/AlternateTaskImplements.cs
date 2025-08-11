using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;

namespace CommanderFull;

public abstract class AlternateTaskImplements
{
    public static async Task<bool> OfferOptions(Creature selectedCreature, List<Option> options, bool midSpell)
    {
        while (true)
        {
            if (midSpell)
            {
                if (options.Count == 0) return false;
                if (options.Count == 1 || options is [_, PassOption])
                {
                    int num = await options[0].Action() ? 1 : 0;
                    selectedCreature.Actions.WishesToEndTurn = false;
                    return num == 1;
                }
            }
            selectedCreature.Battle.MovementConfirmer = null;
            if (await (await selectedCreature.Battle.SendRequest(new AdvancedRequest(selectedCreature, midSpell ? "Choose what action to take." : selectedCreature + "'s turn.", options) { IsMainTurn = !midSpell, IsStandardMovementRequest = !midSpell })).ChosenOption.Action()) return true;
        }
    }
    
    public static List<ICombatAction> CreateActions(bool usableOnly, Possibilities possibilities)
    {
      return possibilities.Sections.SelectMany(section => CreateIActions(usableOnly, "Root", section)).ToList();
    }
    
    public static IEnumerable<ICombatAction> CreateIActions(bool usableOnly, string possibilityChain, PossibilitySection section)
  {
    string chain = $"{possibilityChain}:{(section.PossibilitySectionId == PossibilitySectionId.None ? section.Name : section.PossibilitySectionId.ToStringOrTechnical())}";
    int index = 0;
    foreach (Possibility possibility in section.Possibilities)
    {
      if (possibility is SubmenuPossibility submenuPossibility)
      {
        IEnumerator<ICombatAction>? enumerator = submenuPossibility.CreateOptions((usableOnly ? 1 : 0) != 0, $"{chain}:{(submenuPossibility.SubmenuId == SubmenuId.None ? submenuPossibility.Caption : submenuPossibility.SubmenuId.ToStringOrTechnical())}[{index.ToString()}]").GetEnumerator();
        using var enumerator1 = enumerator as IDisposable;
        while (enumerator.MoveNext())
          yield return enumerator.Current;
        enumerator = null;
      }
      else
      {
        string str = $"{chain}>{possibility.Caption}:{index.ToString()}";
        if (possibility is ActionPossibility actionPossibility3)
        {
          if (!usableOnly || possibility.Usable)
          {
            actionPossibility3.CombatAction.PossibilityChain = str;
            yield return actionPossibility3.CombatAction;
          }
        }
        else if (possibility is ChooseActionCostThenActionPossibility actionPossibility2)
        {
          if (!usableOnly || actionPossibility2.Usable)
            yield return new ChooseActionCostThenCombatAction(2, actionPossibility2.CombatAction)
            {
              PossibilityChain = str
            };
        }
        else if (possibility is ChooseVariantThenActionPossibility actionPossibility1 && (!usableOnly || actionPossibility1.Usable))
          yield return new ChooseVariantThenCombatAction(actionPossibility1.SpellVariant, actionPossibility1.CombatAction)
          {
            PossibilityChain = str
          };
      }
      ++index;
    }
  }
}