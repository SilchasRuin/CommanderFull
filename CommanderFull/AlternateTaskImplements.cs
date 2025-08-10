using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;

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
}