using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Mods.LoresAndWeaknesses;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace CommanderFull;

public class AnaseRequired
{
    public static void ObservationalAnalysisLogic(TrueFeat feat)
    {
        feat.WithPrerequisite(RecallWeakness.FNCombatAssessment, "Combat Assessment").WithPermanentQEffect(
            $"When you use Combat Assessment against a target that you or an ally has targeted with a Strike or spell since the start of your last turn, you get a +2 circumstance bonus to the {Commander.UseCreatedTooltip("Recall Weakness")} check (+4 if the Strike from Combat Assessment is a critical hit).",
            qf =>
            {
                Creature self = qf.Owner;
                var apply = true;
                qf.StartOfYourPrimaryTurn = (_, _) =>
                {
                    if (!apply) return Task.CompletedTask;
                    qf.AddGrantingOfTechnical(cr => cr.EnemyOf(self), qfTech =>
                    {
                        qfTech.YouAreTargeted = (_, action) =>
                        {
                            if (action.SpellInformation == null && !action.HasTrait(Trait.Strike))
                                return Task.CompletedTask;
                            if (action.Owner == self ||
                                self.Battle.AllCreatures.Any(cr =>
                                    cr.FriendOfAndNotSelf(self) && action.Owner == cr))
                            {
                                qfTech.Owner.AddQEffect(
                                    new QEffect(ExpirationCondition.CountsDownAtStartOfSourcesTurn)
                                    {
                                        Value = 2,
                                        Id = ModData.MQEffectIds.Observed,
                                        Source = self
                                    });
                            }

                            return Task.CompletedTask;
                        };
                    });
                    apply = false;
                    return Task.CompletedTask;
                };
                qf.YouBeginAction = async (_, action) =>
                {
                    if (action.ChosenTargets.ChosenCreature == null || action.ActionId != RecallWeakness.CombatAssessment || !action.ChosenTargets.ChosenCreature.HasEffect(ModData.MQEffectIds.Observed))
                        return;
                    self.AddQEffect(new QEffect()
                    {
                        BonusToSkillChecks = (_, combatAction, _) =>
                        {
                            if (combatAction.ActionId != RecallWeakness.RWActionId)
                                return null;
                            return new Bonus(action.CheckResult == CheckResult.CriticalSuccess ? 4 : 2,
                                BonusType.Circumstance, "Observational Analysis");
                        },
                        AfterYouTakeAction = async (qfThis, combatAction) =>
                        {
                            if (combatAction == action && action.CheckResult <= CheckResult.Failure)
                                qfThis.ExpiresAt = ExpirationCondition.Immediately;
                            if (combatAction.ActionId != RecallWeakness.RWActionId)
                                return;
                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                        },
                        Illustration = IllustrationName.Acorn
                    });

                };
            });
    }

    public static string RecallWeaknessDescription()
    {
        return RecallWeakness.DefaultActionDescription;
    }
}