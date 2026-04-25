using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Mods.DawnniExpanded;

namespace CommanderFull;

public class DawnniRequired
{
    public static void CombatAssessmentLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(null,
            qf => qf.ProvideStrikeModifier = item =>
            {
                CombatAction strike = qf.Owner.CreateStrike(item);
                strike.Illustration = new SideBySideIllustration(strike.Illustration,
                    IllustrationName.NarratorBook);
                strike.Name = "Combat Assessment " + strike.Name;
                strike.Traits.Add(Trait.Basic);
                strike.ActionId = FeatRecallWeakness.CombatAssessmentActionID;
                strike.Description = StrikeRules.CreateBasicStrikeDescription2(strike.StrikeModifiers,
                    additionalSuccessText: "Recall Weakness against the target",
                    additionalCriticalSuccessText: "Gain a +2 circumstance bonus to the check to Recall Weakness.",
                    additionalAftertext: "The target is temporarily immune to Combat Assessment for 1 day.");
                strike.StrikeModifiers.OnEachTarget +=
                    (Func<Creature, Creature, CheckResult, Task>)(async (caster, target, checkResult) =>
                    {
                        target.AddQEffect(QEffect.ImmunityToTargeting(FeatRecallWeakness.CombatAssessmentActionID));
                        bool observed = target.FindQEffect(ModData.MQEffectIds.Observed)?.Source == caster;
                        QEffect crit = new(
                            (observed ? "Observational Analysis" : "Combat Assessment") + " (Critical Success)",
                            "",
                            ExpirationCondition.ExpiresAtEndOfAnyTurn, null)
                        {
                            BonusToSkillChecks =
                                ((Func<Skill, CombatAction, Creature, Bonus?>)((_, action, _) =>
                                    action.ActionId != FeatRecallWeakness.ActionID
                                        ? null
                                        : new Bonus(observed ? 4 : 2, BonusType.Circumstance,
                                            (observed ? "Observational Analysis" : "Combat Assessment") +
                                            " (Critical Success)")))!
                        };
                        QEffect analysis = new("Observational Analysis", "",
                            ExpirationCondition.ExpiresAtEndOfAnyTurn, null)
                        {
                            BonusToSkillChecks =
                                ((Func<Skill, CombatAction, Creature, Bonus?>)((_, action, _) =>
                                    action.ActionId != FeatRecallWeakness.ActionID
                                        ? null
                                        : new Bonus(2, BonusType.Circumstance,
                                            "Observational Analysis")))!
                        };
                        switch (checkResult)
                        {
                            case < CheckResult.Success:
                                return;
                            case CheckResult.Success:
                                if (observed)
                                {
                                    caster.AddQEffect(analysis);
                                }

                                break;
                            case CheckResult.CriticalSuccess:
                                strike.Owner.AddQEffect(crit);
                                break;
                        }

                        TBattle battle = strike.Owner.Battle;
                        CombatAction recall = FeatRecallWeakness.RecallWeaknessAction(strike.Owner);
                        recall.WithActionCost(0);
                        recall.Target = strike.Target;
                        bool done = await battle.GameLoop.FullCast(recall, ChosenTargets.CreateSingleTarget(target));
                        if (done)
                        {
                            crit.ExpiresAt = ExpirationCondition.Immediately;
                            analysis.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    });
                return item.HasTrait(Trait.Melee) ? strike : null;
            });
    }

    public static void ObservationalAnalysisLogic(TrueFeat feat)
    {
        feat.WithPrerequisite(ModData.MFeatNames.CombatAssessment, "Combat Assessment").WithPermanentQEffect(
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
            });
    }

    public static void ModCombatAssessment()
    {
        FeatRecallWeakness.CombatAssessment.Traits = [];
    }

    public static string RecallWeaknessDescription(Creature cr)
    {
        return FeatRecallWeakness.RecallWeaknessAction(cr).Description;
    }
}