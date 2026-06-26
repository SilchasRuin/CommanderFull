using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using static CommanderFull.ModData;

namespace CommanderFull;

public class HighLevelFeats : Commander
{
    public static IEnumerable<Feat> Load()
    {
        if (Dawnni || LoreWeak)
        {
            yield return new TrueFeat(ModManager.RegisterFeatName("FC_PerfectedEvaluations", "Perfected Evaluations"),
                    12,
                    "You instantly assess the strengths and weaknesses of the enemy forces.",
                    "When you use Rapid Assessment, you can attempt up to six Recall Knowledge checks against enemies you are observing.",
                    [MTraits.Commander])
                .WithPrerequisite(MFeatNames.UnrivaledAnalysis, "Unrivaled Analysis")
                .WithPermanentQEffectAndSameRulesText(qf => qf.Id = MQEffectIds.PerfectedEvaluations);
        }
        yield return new TrueFeat(ModManager.RegisterFeatName("FC_ContactWithTheEnemy", "Contact with the Enemy"), 14,
            "You know that even the best-laid plans rarely survive contact with the enemy, and you have prepared your allies to adapt with a wide array of contingencies.",
            "When you use Adaptive Stratagem, you can replace any master tactics or legendary tactics you have prepared with any other tactics in your folio.",
            [MTraits.Commander])
            .WithPrerequisite(MFeatNames.AdaptiveStratagem, "Adaptive Stratagem")
            .WithPermanentQEffectAndSameRulesText(qf => qf.Id = MQEffectIds.ContactWithTheEnemy);
        yield return new TrueFeat(ModManager.RegisterFeatName("FC_QuickeningBanner", "Quickening Banner"), 14,
                "The sight of your banner urges your allies to strike now.",
                "Each ally within the aura of your commander's banner is quickened for 1 round and can use this extra action to Strike or Stride. You may use this action once per encounter.",
                [MTraits.Commander, MTraits.Brandish, Trait.Visual])
            .WithActionCost(1)
            .WithPermanentQEffectAndSameRulesText(qf =>
            {
                qf.ProvideMainAction = effect =>
                {
                    Creature self = effect.Owner;
                    int bannerRadius = GetBannerRadius(self);
                    Target allyInBanner = Target.Emanation(bannerRadius).WithAdditionalRequirementOnCaster(cr => new BrandishRequirement().Satisfied(cr, cr)).WithIncludeOnlyIf((_, cr) =>
                        cr.FriendOfAndNotSelf(self) && new InBannerAuraRequirement().Satisfied(self, cr) && new BrandishRequirement().Satisfied(self, cr));
                    CombatAction quicken = new CombatAction(self, MIllustrations.CreateIllustration("QuickBanner"),
                        "Quickening Banner",
                        [MTraits.Commander, MTraits.Brandish, Trait.Visual, Trait.Basic, Trait.DoesNotRequireAttackRollOrSavingThrow],
                        "Each ally within the aura of your commander's banner is quickened for 1 round and can use this extra action to Strike or Stride.",
                        self.HasEffect(MQEffectIds.QuickeningBanner) ? Target.Uncastable("You may only use Quickening Banner once per encounter.") : allyInBanner)
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.Abjuration)
                        .WithEffectOnChosenTargets(async (caster, _) => caster.AddQEffect(new QEffect { Id = MQEffectIds.QuickeningBanner }))
                        .WithEffectOnEachTarget(async (_, caster, target, _) =>
                        {
                            target.AddQEffect(QEffect.Quickened(action =>
                            {
                                if (action.ActionId is ActionId.Stride or ActionId.StepByStepStride)
                                    return true;
                                return action.HasTrait(Trait.Strike) && (action.Name.StartsWith("Strike") || action.Name == "Throw") && action.ActionCost == 1;
                            }).WithExpirationInOneRound(caster));
                        });
                    return new ActionPossibility(quicken).WithPossibilityGroup("Abilities");
                };
            });
    }
}