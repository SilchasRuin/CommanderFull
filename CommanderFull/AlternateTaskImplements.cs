using System.Reflection;
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
    public static Possibilities Filter(Possibilities possibilities0, Func<Possibility, bool> keepOnlyWhat)
    {
        Possibilities possibilities = possibilities0.Filter(_ => false);
        foreach (PossibilitySection section in possibilities0.Sections)
        {
            PossibilitySection? possibilitySection = Filter(keepOnlyWhat, section);
            if (possibilitySection != null)
                possibilities.Sections.Add(possibilitySection);
        }
        return possibilities;
    }
    public static PossibilitySection? Filter(Func<Possibility, bool> keepOnlyWhat, PossibilitySection possibilitySectionO)
    {
        PossibilitySection possibilitySection = new(possibilitySectionO.Name);
        foreach (Possibility possibility in possibilitySectionO.Possibilities)
        {
            if (possibility is SubmenuPossibility submenuPossibility1)
            {
                SubmenuPossibility? submenuPossibility = Filter(keepOnlyWhat, submenuPossibility1);
                if (submenuPossibility != null)
                    possibilitySection.Possibilities.Add(submenuPossibility);
            }
            else if (keepOnlyWhat(possibility))
                possibilitySection.Possibilities.Add(possibility);
        }
        return possibilitySection.Possibilities.Count == 0 ? null : possibilitySection;
    }
    
    public static SubmenuPossibility? Filter(Func<Possibility, bool> keepOnlyWhat, SubmenuPossibility possibility)
    {
        SubmenuPossibility submenuPossibility = new(possibility.Illustration, possibility.Caption, possibility.PossibilitySize);
        foreach (PossibilitySection subsection in possibility.Subsections)
        {
            PossibilitySection? possibilitySection = Filter(keepOnlyWhat, subsection);
            if (possibilitySection != null)
                submenuPossibility.Subsections.Add(possibilitySection);
        }
        return submenuPossibility.Subsections.Count != 0 ? submenuPossibility : null;
    }
    
    public static void RecalculateUsability(Possibility possibility)
    {
        CombatAction action = possibility switch
        {
            ActionPossibility ap => ap.CombatAction,
            ChooseActionCostThenActionPossibility acap => acap.CombatAction,
            ChooseVariantThenActionPossibility vap => vap.CombatAction,
            _ => throw new ArgumentOutOfRangeException(nameof(possibility), possibility, null)
        };
        Usability use = action.Target.CanBeginToUse(action.Owner);
        ForceSet(possibility, "Usable", use.CanBeUsed);
        ForceSet(possibility, "UnusableWhy", use.UnusableReason);
    }
    
    public static void ForceSet(object obj, string propertyName, object? value)
    {
        Type type = obj.GetType();
        FieldInfo? backing = type.GetField($"<{propertyName}>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (backing != null)
        {
            backing.SetValue(obj, value);
            return;
        }
        FieldInfo? field = type.GetField(propertyName.ToLower(),
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
}