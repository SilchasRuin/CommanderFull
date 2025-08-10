using System.Reflection;
using Dawnsbury.Audio;
using Dawnsbury.Campaign.Encounters.Tutorial;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DawnniExpanded;
using Dawnsbury.ThirdParty.SteamApi;
using Microsoft.Xna.Framework;

namespace CommanderFull;

public abstract partial class Commander
{
    public static IEnumerable<Feat> LoadFeats()
    {
        yield return new TrueFeat(ModData.MFeatNames.OfficerMedic, 1,
                "You’re trained in battlefield triage and wound treatment.",
                "You are trained in Medicine and can use your Intelligence modifier in place of your Wisdom modifier for Medicine checks. You gain the Battle Medicine feat.",
                [ModData.MTraits.Commander])
            .WithOnSheet(sheet =>
            {
                sheet.TrainInThisOrSubstitute(Skill.Medicine);
                sheet.GrantFeat(FeatName.BattleMedicine);
            })
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect
                {
                    BonusToSkills = skill =>
                        skill == Skill.Medicine && creature.Abilities.Intelligence > creature.Abilities.Wisdom
                            ? new Bonus(creature.Abilities.Intelligence - creature.Abilities.Wisdom, BonusType.Untyped,
                                "Officer's Medical Training", true)
                            : null
                });
            })
            .WithPrerequisite(sheet =>
                    sheet.Tags.TryGetValue("PreparedTactics", out var value) && value is not null && (int)value >= 3,
                "You must be able to select at least 3 tactics.");
        yield return new TrueFeat(ModData.MFeatNames.CommandersCompanion, 1,
                "You gain the service of a young animal companion.",
                "You can affix your banner to your companion's saddle, barding, or simple harness, determining the effects of your commander's banner and other abilities that use your banner from your companion's space, even if you are not currently riding your companion. A companion granted by this feat always counts as one of your squadmates and does not count against your maximum number of squadmates." +
                "\n\n{b}Special{b} When you use Command an Animal to command the companion granted by this feat, it gains a reaction it can ony use in response to your tactics. This reaction is lost if not used by the end of your turn.",
                [ModData.MTraits.Commander],
                AnimalCompanionFeats.LoadAll().FirstOrDefault(feat => feat.FeatName == FeatName.AnimalCompanion)!
                    .Subfeats!.ToList())
            .WithPermanentQEffect(null, qf =>
            {
                qf.AfterYouTakeAction = (_, action) =>
                {
                    Creature owner = qf.Owner;
                    Creature? companion =
                        owner.Battle.AllCreatures.FirstOrDefault(cr => IsMyAnimalCompanion(owner, cr));
                    if (companion != null && action.Name == "Command your animal companion")
                    {
                        companion.AddQEffect(AnimalReaction(owner));
                    }

                    return Task.CompletedTask;
                };
            });
        yield return new TrueFeat(ModData.MFeatNames.DeceptiveTactics, 1,
                "Your training has taught you that the art of war is the art of deception.",
                "You can use your Warfare Lore modifier in place of your Deception modifier for Deception checks to Create a Diversion or Feint, you count as trained in deception for the purposes of making a feint, and can use your proficiency rank in Warfare Lore instead of your proficiency rank in Deception to meet the prerequisites of feats that modify the Create a Diversion or Feint actions (such as Lengthy Diversion).",
                [ModData.MTraits.Commander])
            .WithPermanentQEffect(qf =>
            {
                qf.YouBeginAction = (_, action) =>
                {
                    if (action.ActionId is ActionId.Feint && action.ActiveRollSpecification != null)
                        action.WithActiveRollSpecification(new ActiveRollSpecification(
                            TaggedChecks.SkillCheck(Skill.Deception, WarfareLore),
                            TaggedChecks.DefenseDC(Defense.Perception)));
                    return Task.CompletedTask;
                };
                qf.ProvideActionIntoPossibilitySection = (_, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.NonAttackManeuvers ||
                        qf.Owner.Proficiencies.Get(Trait.Deception) >= Proficiency.Trained) return null;
                    return new ActionPossibility(CombatManeuverPossibilities.CreateFeintAction(qf.Owner));
                };
            }).WithPrerequisite(sheet =>
                    sheet.Tags.TryGetValue("PreparedTactics", out var value) && value is not null && (int)value >= 3,
                "You must be able to select at least 3 tactics.");
        if (ModManager.TryParse("DawnniEx", out Trait _))
        {
            TrueFeat combatAssessment = new(ModData.MFeatNames.CombatAssessment, 1,
                "You make a telegraphed attack to learn about your foe.",
                $"Make a melee Strike. On a hit, you can immediately attempt a check to {UseCreatedTooltip("Recall Weakness")} about the target. On a critical hit, you gain a +2 circumstance bonus to the check to Recall Weakness. The target is temporarily immune to Combat Assessment for 1 day.",
                [ModData.MTraits.Commander, Trait.Fighter]);
            CombatAssessmentLogic(combatAssessment);
            yield return combatAssessment;
        }

        TrueFeat armoredRegiment = new(ModData.MFeatNames.ArmorRegiment, 1,
            "You've trained for grueling marches in full battle kit.",
            "You ignore the reduction to your Speed from any armor you wear and you can rest normally while wearing armor of any type.",
            [ModData.MTraits.Commander]);
        ArmorRegimentLogic(armoredRegiment);
        yield return armoredRegiment;

        TrueFeat plantBanner = new(ModData.MFeatNames.PlantBanner, 1,
            "You plant your banner to inspire your allies to hold the line.",
            "Plant your banner in a corner of your square. Each ally within a 30-foot burst centered on your banner immediately gains 4 temporary Hit Points, plus an additional 4 temporary Hit Points at 4th level and every 4 levels thereafter. " +
            "These temporary Hit Points last for 1 round; each time an ally starts their turn within the burst, their temporary Hit Points are renewed for another round. " +
            "If your banner is attached to a weapon, you cannot wield that weapon while your banner is planted. While your banner is planted, the emanation around your banner is a 35 foot emanation." +
            "\n\nYou can use an Interact action while adjacent to your banner to retrieve it. An enemy adjacent to the square you planted your banner in can remove your banner as an Interact action, ending this effect and preventing you and your allies from gaining any of your banner's other benefits until you have successfully retrieved it.",
            [ModData.MTraits.Commander, Trait.Manipulate]);
        PlantBannerLogic(plantBanner);
        yield return plantBanner;

        TrueFeat adaptiveStratagem = new(ModData.MFeatNames.AdaptiveStratagem, 2,
            "Your constant training and strong bond with your allies allow you to change tactics on the fly.",
            "At the start of combat you can replace one of your prepared expert, mobility, or offensive tactics with another tactic in your folio.",
            [ModData.MTraits.Commander]);
        AdaptiveStratagemLogic(adaptiveStratagem);
        yield return adaptiveStratagem;

        TrueFeat defensiveSwap = new(ModData.MFeatNames.DefensiveSwap, 2,
            "You and your allies work together selflessly to protect each other from harm.",
            "When you or an adjacent ally are the target of an attack, you may use a reaction to immediately swap positions with each other, and whichever of you was not the target of the triggering attack becomes the target instead.",
            [ModData.MTraits.Commander]);
        DefensiveSwapLogic(defensiveSwap);
        yield return defensiveSwap;

        TrueFeat guidingShot = new(ModData.MFeatNames.GuidingShot, 2,
            "Your ranged attack helps guide your allies into striking your enemy's weak point.",
            "Attempt a Strike with a ranged weapon. If the Strike hits, the next creature other than you to attack the same target before the start of your next turn gains a +1 circumstance bonus to their roll, or a +2 circumstance bonus if your Strike was a critical hit.",
            [ModData.MTraits.Commander, Trait.Flourish]);
        GuidingShotLogic(guidingShot);
        yield return guidingShot;

        TrueFeat setupStrike = new(ModData.MFeatNames.SetupStrike, 2,
            "Your attack makes it difficult for your enemy to defend themselves against your allies' attacks.",
            "Attempt a Strike against an enemy. If the Strike is successful, the target is off guard against the next attack that one of your allies attempts against it before the start of your next turn.",
            [ModData.MTraits.Commander, Trait.Flourish]);
        SetUpStrikeLogic(setupStrike);
        yield return setupStrike;

        if (ModManager.TryParse("DawnniEx", out Trait _))
        {
            TrueFeat rapidAssessment = new(ModData.MFeatNames.RapidAssessment, 2, "You quickly evaluate your enemies.",
                $"Attempt a check to {UseCreatedTooltip("Recall Weakness")} against one creature you are observing.",
                [ModData.MTraits.Commander]);
            RapidAssessmentLogic(rapidAssessment);
            yield return rapidAssessment;
        }

        TrueFeat tacticalExpansion = new(ModData.MFeatNames.TacticalExpansion, 2,
            "Your folio is filled with tactics and techniques you’ve devised based on study and experience.",
            "Add two additional tactics you qualify for to your folio.",
            [ModData.MTraits.Commander]);
        tacticalExpansion.WithOnSheet(values =>
            values.AddSelectionOption(new MultipleFeatSelectionOption("ExpandedFolio", "Tactical Expansion",
                values.CurrentLevel, feat => feat.HasTrait(ModData.MTraits.TacticPre), 2)));
        tacticalExpansion.WithMultipleSelection();
        yield return tacticalExpansion;

        TrueFeat bannerTwirl = new(ModData.MFeatNames.BannerTwirl, 4,
            "You spin your banner in an elaborate pattern that your enemies find inscrutable.",
            "You and any ally adjacent to you have concealment from ranged attacks until the start of your next turn.",
            [ModData.MTraits.Commander, ModData.MTraits.Banner, Trait.Manipulate]);
        BannerTwirlLogic(bannerTwirl);
        yield return bannerTwirl;

        TrueFeat bannerInspire = new(ModData.MFeatNames.BannersInspiration, 4,
            "You wave your banner, inspiring allies to throw off the shackles of fear.",
            "Each ally in your banner's aura reduces their frightened and stupefied conditions by 1, and can make a Will save against a standard level-based DC for your level, and on a success or better remove the Confused or Paralyzed condition. Regardless of the result, any ally that attempts this save is temporarily immune to Banner's Inspiration for 10 minutes.",
            [
                ModData.MTraits.Brandish, ModData.MTraits.Commander, Trait.Emotion, Trait.Flourish, Trait.Mental,
                Trait.Visual
            ]);
        BannersInspirationLogic(bannerInspire);
        yield return bannerInspire;

        if (ModManager.TryParse("DawnniEx", out Trait _))
        {
            TrueFeat observationalAnalysis = new(ModData.MFeatNames.ObservationalAnalysis, 4,
                "You are able to rapidly discern relevant details about your opponents in the heat of combat.",
                $"When you use Combat Assessment against a target that you or an ally has targeted with a Strike or spell since the start of your last turn, you get a +2 circumstance bonus to the {UseCreatedTooltip("Recall Weakness")} check (+4 if the Strike from Combat Assessment is a critical hit).",
                [ModData.MTraits.Commander]);
            ObservationalAnalysisLogic(observationalAnalysis);
            yield return observationalAnalysis;
        }

        TrueFeat unsteadyingStrike = new(ModData.MFeatNames.UnsteadyingStrike, 4,
            "Your attack makes your opponent more susceptible to follow-up maneuvers from your allies.",
            "Make a melee Strike against an enemy within your reach. If the Strike is successful, the enemy takes a –2 circumstance penalty to their Fortitude DC to resist being Grappled, Repositioned, or Shoved and a –2 circumstance penalty to their Reflex DC to resist being Disarmed. Both penalties last until the start of your next turn.",
            [ModData.MTraits.Commander, Trait.Flourish]);
        UnsteadyingStrikeLogic(unsteadyingStrike);
        yield return unsteadyingStrike;

        TrueFeat shieldedRecovery = new(ModData.MFeatNames.ShieldedRecovery, 4,
            "You can bandage wounds with the same hand you use to hold your shield.",
            "You can use the same hand you are using to wield a shield to use Battle Medicine. When you use Battle Medicine on an ally while wielding a shield, they gain a +1 circumstance bonus to AC and Reflex saves that lasts until the start of your next turn or until they are no longer adjacent to you, whichever comes first.",
            [ModData.MTraits.Commander]);
        ShieldedRecoveryLogic(shieldedRecovery);
        yield return shieldedRecovery;

        TrueFeat battleTestedCompanion = new(ModData.MFeatNames.BattleTestedCompanion, 6,
            "Your companion is a tried and tested ally of unshakable reliability.",
            "Your animal companion gains the following benefits:\r\n• It gets +1 to Strength, Dexterity, Constitution and Wisdom.\r\n• Its unarmed attack damage increases from one die to two dice (for example, from 1d8 to 2d8).\r\n• Its proficiency with Perception and all saving throws increases to Expert (an effective +2 to Perception and all saves).\r\n• Its proficiency in Intimidation, Stealth and Survival increases by one step (from untrained to trained; or from trained to expert).\r\n• While your banner is affixed to this companion, the banner's aura is 10 feet greater than it normally is (typically this means the banner's 30-foot aura becomes a 40-foot aura).",
            [ModData.MTraits.Commander]);
        BattleTestedCompanionLogic(battleTestedCompanion);
        yield return battleTestedCompanion;

        TrueFeat efficientPrep = new(ModData.MFeatNames.EfficientPreparation, 6,
                "You’ve developed techniques for drilling your allies on multiple tactics in a succinct and efficient manner.",
                "Increase the number of tactics you can have prepared by 1.", [ModData.MTraits.Commander]);
        EfficientPreparationLogic(efficientPrep);
        yield return efficientPrep;

        TrueFeat claimTheField = new(ModData.MFeatNames.ClaimTheField, 6,
            "You hurl your banner forward with precision, claiming the battlefield for yourself and your allies.",
            $"Your banner must be attached to a thrown weapon. You {CreateTooltips("Plant the Banner", plantBanner.RulesText)}, but you can place it at any corner within the required weapon's first range increment, rather than the corner of your square. The calculated confidence of this brash maneuver unnerves your enemies; any enemy who attempts to damage or remove your banner while it is planted in this way must succeed at a Will save against your class DC or the attempt fails. On a critical failure, the enemy is fleeing for 1 round. This is an incapacitation and mental effect.",
            [ModData.MTraits.Commander]);
        ClaimTheFieldLogic(claimTheField);
        yield return claimTheField;

        yield return new TrueFeat(ModData.MFeatNames.ReactiveStrike, 6, "You lash out at a foe that leaves an opening.",
                "{b}Trigger{/b} A creature within your reach uses a manipulate action or move action, makes a ranged attack, or leaves a square during a move action it's using.\n\nMake a melee Strike against the triggering creature. If your attack is a critical hit and the trigger was a manipulate action, you disrupt that action. This Strike doesn't count toward your multiple attack penalty, and your multiple attack penalty doesn't apply to this Strike.",
                [ModData.MTraits.Commander]).WithActionCost(-2).WithOnCreature(self =>
            {
                QEffect reactiveStrike = QEffect.AttackOfOpportunity();
                reactiveStrike.Name = reactiveStrike.Name?.Replace("Attack of Opportunity", "Reactive Strike");
                self.AddQEffect(reactiveStrike);
            })
            .WithEquivalent(values =>
                values.AllFeats.Any(ft =>
                    ft.BaseName is "Attack of Opportunity" or "Reactive Strike" or "Opportunist"));
        ;

        TrueFeat defiantBanner = new(ModData.MFeatNames.DefiantBanner, 8,
            "You vigorously wave your banner to remind yourself and your allies that you can and must endure.",
            "You and all allies within the aura of your commander's banner when you use this action gain resistance to bludgeoning, piercing, and slashing damage equal to your Intelligence modifier until the start of your next turn.",
            [ModData.MTraits.Commander, ModData.MTraits.Brandish, Trait.Flourish, Trait.Manipulate, Trait.Visual]);
        DefiantBannerLogic(defiantBanner);
        yield return defiantBanner;

        Feat education = new TrueFeat(ModData.MFeatNames.OfficersEducation, 8,
            "You know that a broad knowledge base is critical for a competent commander.",
            "You become trained in two skills you are not already trained in, become an expert in one skill you are currently trained in, and gain any one general feat that you meet the prerequisites for.",
            [ModData.MTraits.Commander]).WithMultipleSelection().WithOnSheet(values =>
        {
            values.AddSkillIncreaseOptionComplex("TrainInOne", "Officer's Education", Proficiency.Trained);
            values.AddSkillIncreaseOptionComplex("TrainInTwo", "Officer's Education", Proficiency.Trained);
            values.AddSkillIncreaseOptionComplex("ExpertInOne", "Officer's Education", Proficiency.Expert);
            values.AddSelectionOption(new SingleFeatSelectionOption("OE_GenFeat", "Officer's Education",
                values.CurrentLevel, feat => feat.HasTrait(Trait.General)));
        }).WithPrerequisite(
            sheet => sheet.AllFeatGrants.Count(fg => fg.GrantedFeat.FeatName == ModData.MFeatNames.OfficersEducation) <
                     3, "You can only take this feat twice.");
        string replace = education.RulesText.Replace("multiple times.", "twice.");
        education.RulesText = replace;
        yield return education;

        TrueFeat rallyingBanner = new(ModData.MFeatNames.RallyingBanner, 8,
            "Your banner waves high, reminding your allies that the fight can still be won.",
            "You restore 4d6 Hit Points to each ally within the aura of your commander's banner. This healing increases by an additional 1d6 at 10th level and every 2 levels thereafter. You may only use Rallying Banner once per encounter.",
            [
                ModData.MTraits.Brandish, ModData.MTraits.Commander, Trait.Emotion, Trait.Healing, Trait.Mental,
                Trait.Visual
            ]);
        RallyingBannerLogic(rallyingBanner);
        yield return rallyingBanner;

        yield return new TrueFeat(ModData.MFeatNames.UnrivaledAnalysis, 8,
            "Your experience allows you to derive even more information about your opponents from a mere glance.",
            "When you use Rapid Assessment, you can attempt up to four checks to Recall Knowledge about creatures you are observing.",
            [ModData.MTraits.Commander]).WithPrerequisite(ModData.MFeatNames.RapidAssessment, "Rapid Assessment");
    }

    internal static void LoadGenericFeats()
    {
        if (AllFeats.GetFeatByFeatName(FeatName.ShieldWarden) is not TrueFeat warden) return;
        warden.WithAllowsForAdditionalClassTrait(ModData.MTraits.Commander);
        warden.Prerequisites.RemoveAll(req =>
            req.Description.Contains("must have Shield Ally") || req.Description.Contains("must be a Fighter"));
        warden.WithPrerequisite(
            values => values.HasFeat(FeatName.Fighter) || values.HasFeat(ModData.MFeatNames.Commander) ||
                      values.HasFeat(Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion.Champion
                          .ShieldAllyFeatName),
            "You must be a Fighter, a Commander, or you must have Shield Ally as your divine ally.");
    }

    #region logics

    private static void CombatAssessmentLogic(TrueFeat feat)
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
                        target.AddQEffect(QEffect.ImmunityToTargeting(FeatRecallWeakness.CombatAssessmentActionID,
                            caster));
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

    private static void ArmorRegimentLogic(TrueFeat feat)
    {
        feat.WithPermanentQEffect(
            "You ignore the reduction to your Speed from any armor you wear and you can rest normally while wearing armor of any type.",
            qfFeat =>
            {
                qfFeat.StartOfCombatBeforeOpeningCutscene = qfThis =>
                {
                    qfThis.Tag = qfThis.Owner.BaseArmor;
                    return Task.CompletedTask;
                };
                qfFeat.StartOfCombat = qfThis =>
                {
                    if (qfThis.Owner.BaseArmor is null && qfThis.Tag is Item { ArmorProperties: not null } tagItem)
                    {
                        qfThis.Owner.BaseArmor = tagItem;
                        if (qfThis.Owner.FindQEffect(QEffectId.SpeakAboutMissingArmor) is { } s2e3_armor)
                            s2e3_armor.StartOfYourPrimaryTurn = async (effect, self) =>
                            {
                                if (!self.Actions.CanTakeActions())
                                    return;
                                effect.ExpiresAt = ExpirationCondition.Immediately;
                                self.Battle.Cinematics.TutorialBubble = new TutorialBubble(self.Illustration,
                                    SubtitleModification.Replace(
                                        "{Green}{b}Armored Regiment!{b}{/Green}\nIt's a good thing I can sleep in my armor. Now to pick up my weapons."),
                                    null);
                                await self.Battle.SendRequest(ModLoader.NewSleepRequest(5000));
                                self.Battle.Cinematics.TutorialBubble = null;
                            };
                    }

                    return Task.CompletedTask;
                };
                qfFeat.Id = ModData.MQEffectIds.ArmorRegiment;
            });
    }

    private static void PlantBannerLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(
            "You plant your banner, and allies in a 30 foot burst gain temporary hit points that are renewed as long as they remain within the area.",
            qf =>
            {
                CombatAction plantBanner = new CombatAction(qf.Owner, ModData.MIllustrations.PlantBanner,
                        "Plant Banner", [ModData.MTraits.Commander, Trait.Basic, Trait.Manipulate],
                        "Plant your banner in a corner of your square. Each ally within a 30-foot burst centered on your banner immediately gains 4 temporary Hit Points, plus an additional 4 temporary Hit Points at 4th level and every 4 levels thereafter. " +
                        "These temporary Hit Points last for 1 round; each time an ally starts their turn within the burst, their temporary Hit Points are renewed for another round. " +
                        "If your banner is attached to a weapon, you cannot wield that weapon while your banner is planted. While your banner is planted, the emanation around your banner is a 35 foot emanation." +
                        "\n\nYou can use an Interact action while adjacent to your banner to retrieve it. An enemy adjacent to the square you planted your banner in can remove your banner as an Interact action, ending this effect and preventing you and your allies from gaining any of your banner's other benefits until you have successfully retrieved it.",
                        Target.Burst(1, 6).WithAdditionalRequirementOnCaster(cr =>
                                (cr.HasFreeHand || cr.HeldItems.Any(item => item.HasTrait(ModData.MTraits.Banner))) &&
                                cr.HasEffect(ModData.MQEffectIds.Banner)
                                    ? Usability.Usable
                                    : Usability.NotUsable(
                                        "You must be carrying a banner in your hands or have a free hand to use plant banner."))
                            .WithIncludeOnlyIf((_, cr) => cr.FriendOf(qf.Owner)))
                    .WithActionCost(1).WithSoundEffect(ModData.Sfx.Drums)
                    .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                    {
                        Item? banner =
                            caster.HeldItems.FirstOrDefault(item => item.Traits.Contains(ModData.MTraits.Banner)) ??
                            caster.CarriedItems.FirstOrDefault(item =>
                                item.HasTrait(ModData.MTraits.Banner));
                        Creature illusory = Creature.CreateIndestructibleObject(IllustrationName.None,
                            "Banner", caster.Level).With(cr =>
                        {
                            cr.Traits.Add(ModData.MTraits.Banner);
                            AuraAnimation auraAnimation = cr.AnimationData.AddAuraAnimation(
                                IllustrationName.BlessCircle,
                                GetBannerRadius(caster));
                        });
                        QEffect? radius = caster.FindQEffect(ModData.MQEffectIds.BannerRadius);
                        int value = 6;
                        if (radius != null)
                            value = radius.Value;
                        QEffect planted = new()
                        {
                            StateCheckWithVisibleChanges = qff =>
                            {
                                Tile? bannerTile =
                                    qff.Owner.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(qff.Owner, tile));
                                if (bannerTile != null)
                                {
                                    if (radius != null)
                                        radius.Value = 7;
                                }
                                else
                                {
                                    qff.ExpiresAt = ExpirationCondition.Immediately;
                                }

                                return Task.CompletedTask;
                            },
                            ProvideContextualAction = ef =>
                            {
                                Tile bannerTile =
                                    ef.Owner.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(ef.Owner, tile))!;
                                CombatAction removeBanner = new CombatAction(caster, spell.Illustration,
                                        "Remove Banner", [Trait.Manipulate, Trait.Basic],
                                        "Removes your banner, returning it to where it was.",
                                        Target.Self().WithAdditionalRestriction(cr =>
                                            cr.HasFreeHand ? null : "You must have a free hand to remove the banner."))
                                    .WithActionCost(1).WithEffectOnChosenTargets(async (cr, _) =>
                                    {
                                        Item? bannerItem = ef.Tag as Item;
                                        TileQEffect? tileQEffect =
                                            bannerTile.TileQEffects.FirstOrDefault(tQ =>
                                                tQ.TileQEffectId == ModData.MTileQEffectIds.Banner);
                                        if (tileQEffect != null)
                                            tileQEffect.ExpiresAt = ExpirationCondition.Immediately;
                                        await cr.Battle.GameLoop.StateCheck();
                                        if (bannerItem is not null && !bannerItem.HasTrait(Trait.Worn))
                                            cr.AddHeldItem(bannerItem);
                                        else
                                        {
                                            if (bannerItem != null) cr.CarriedItems.Add(bannerItem);
                                            else
                                            {
                                                AuraAnimation animation =
                                                    cr.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle,
                                                        GetBannerRadius(cr));
                                                animation.Color = Color.Coral;
                                                QEffect commandersBannerEffect =
                                                    CommandersBannerEffect(animation, GetBannerRadius(cr), cr);
                                                cr.AddQEffect(commandersBannerEffect);
                                            }
                                        }
                                    });
                                return bannerTile.IsAdjacentTo(ef.Owner.Occupies) ||
                                       bannerTile.Equals(ef.Owner.Occupies)
                                    ? new ActionPossibility(removeBanner)
                                    : null;
                            },
                            WhenExpires = _ =>
                            {
                                if (radius != null) radius.Value = value;
                            }
                        };
                        planted.AddGrantingOfTechnical(cr => cr.EnemyOf(caster), qfTech =>
                        {
                            qfTech.ProvideContextualAction = _ =>
                            {
                                Tile? bannerTile =
                                    caster.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(caster, tile));
                                return bannerTile != null && (bannerTile.IsAdjacentTo(qfTech.Owner.Occupies) ||
                                                              bannerTile.Equals(qfTech.Owner.Occupies))
                                    ? new ActionPossibility(new CombatAction(qfTech.Owner, spell.Illustration,
                                            "Steal Banner", [Trait.Manipulate, Trait.Basic],
                                            "Steals the Commander's banner, frightening his allies.",
                                            Target.Self().WithAdditionalRestriction(cr =>
                                                cr.HasFreeHand
                                                    ? null
                                                    : "You must have a free hand to steal the banner."))
                                        .WithActionCost(1).WithGoodness((_, cr, _) =>
                                            cr.HasEffect(QEffect.Mindless()) ? int.MinValue : cr.Level * 6)
                                        .WithEffectOnChosenTargets(async (cr, _) =>
                                        {
                                            Item? bannerItem = planted.Tag as Item;
                                            TileQEffect? tileQEffect =
                                                bannerTile.TileQEffects.FirstOrDefault(tQ =>
                                                    tQ.TileQEffectId == ModData.MTileQEffectIds.Banner);
                                            if (tileQEffect != null)
                                                tileQEffect.ExpiresAt = ExpirationCondition.Immediately;
                                            await cr.Battle.GameLoop.StateCheck();
                                            foreach (Creature enemy in caster.Battle.AllCreatures.Where(enemy =>
                                                         enemy.FriendOfAndNotSelf(caster) &&
                                                         enemy.DistanceTo(bannerTile) <= 7))
                                            {
                                                if (enemy.IsImmuneTo(Trait.Mental) || enemy.IsImmuneTo(Trait.Visual) ||
                                                    enemy.IsImmuneTo(Trait.Fear) || enemy.IsImmuneTo(Trait.Emotion))
                                                    continue;
                                                enemy.AddQEffect(QEffect.Frightened(1));
                                            }

                                            cr.AddQEffect(new QEffect
                                            {
                                                WhenCreatureDiesAtStateCheckAsync = _ =>
                                                {
                                                    if (bannerItem != null)
                                                    {
                                                        cr.DropItem(bannerItem);
                                                    }
                                                    else
                                                    {
                                                        cr.Occupies.AddQEffect(new TileQEffect()
                                                        {
                                                            StateCheck = qfTile =>
                                                            {
                                                                if (qfTile.Owner.PrimaryOccupant != caster &&
                                                                    !qfTile.Owner.IsAdjacentTo(caster.Occupies)) return;
                                                                caster.AddQEffect(
                                                                    new QEffect(ExpirationCondition.Ephemeral)
                                                                    {
                                                                        ProvideContextualAction = armorBanner =>
                                                                        {
                                                                            CombatAction returnBanner =
                                                                                new CombatAction(
                                                                                        armorBanner.Owner,
                                                                                        spell.Illustration,
                                                                                        "Regain Banner",
                                                                                        [Trait.Manipulate, Trait.Basic],
                                                                                        "Returns your banner to its proper place.",
                                                                                        Target.Self())
                                                                                    .WithActionCost(1)
                                                                                    .WithEffectOnChosenTargets((
                                                                                        creature, _) =>
                                                                                    {
                                                                                        AuraAnimation animation =
                                                                                            creature.AnimationData
                                                                                                .AddAuraAnimation(
                                                                                                    IllustrationName
                                                                                                        .BlessCircle,
                                                                                                    GetBannerRadius(
                                                                                                        creature));
                                                                                        animation.Color = Color.Coral;
                                                                                        QEffect commandersBannerEffect =
                                                                                            CommandersBannerEffect(
                                                                                                animation,
                                                                                                GetBannerRadius(
                                                                                                    creature),
                                                                                                creature);
                                                                                        creature.AddQEffect(
                                                                                            commandersBannerEffect);
                                                                                        qfTile.ExpiresAt =
                                                                                            ExpirationCondition
                                                                                                .Immediately;
                                                                                        return Task.CompletedTask;
                                                                                    });
                                                                            return new ActionPossibility(returnBanner);
                                                                        }
                                                                    });
                                                            },
                                                            Illustration = ModData.MIllustrations.Banner
                                                        });
                                                    }

                                                    return Task.CompletedTask;
                                                }
                                            });
                                            if (bannerItem != null && !bannerItem.HasTrait(Trait.Worn))
                                                cr.AddHeldItem(bannerItem);
                                            else
                                            {
                                                if (bannerItem != null) cr.CarriedItems.Add(bannerItem);
                                            }
                                        }))
                                    : null;
                            };
                        });
                        if (banner != null)
                        {
                            planted.Tag = banner;
                            if (!banner.HasTrait(Trait.Worn))
                            {
                                caster.HeldItems.Remove(banner);
                            }
                            else
                            {
                                caster.CarriedItems.Remove(banner);
                            }
                        }

                        caster.Battle.Log(targets.ChosenTiles.Count.ToString());
                        caster.RemoveAllQEffects(effect => effect.Id == ModData.MQEffectIds.Banner);
                        caster.Occupies.AddQEffect(CommandersBannerTileEffect(7, caster, illusory));
                        caster.AddQEffect(planted);
                        await caster.Battle.GameLoop.StateCheck();
                        caster.Battle.SpawnIllusoryCreature(illusory, caster.Occupies);
                        var temp = (1 + caster.Level / 4) * 4;
                        foreach (Creature ally in targets.ChosenCreatures)
                        {
                            QEffect continuingTemp = new()
                            {
                                StateCheckWithVisibleChanges = qff =>
                                {
                                    Tile? bannerTile =
                                        qff.Owner.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(caster, tile));
                                    if (bannerTile == null)
                                        qff.ExpiresAt = ExpirationCondition.Immediately;
                                    return Task.CompletedTask;
                                },
                                StartOfYourPrimaryTurn = (qff, cr) =>
                                {
                                    Tile? bannerTile =
                                        qff.Owner.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(caster, tile));
                                    if (bannerTile == null)
                                    {
                                        qff.ExpiresAt = ExpirationCondition.Immediately;
                                        return Task.CompletedTask;
                                    }

                                    if (targets.ChosenTiles.Any(tile => Equals(tile, cr.Occupies)))
                                    {
                                        bool applied = false;
                                        cr.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                                        {
                                            StateCheckWithVisibleChanges = _ =>
                                            {
                                                if (applied) return Task.CompletedTask;
                                                ally.GainTemporaryHP(temp);
                                                applied = true;
                                                return Task.CompletedTask;
                                            },
                                            WhenExpires = _ => { ally.TemporaryHP = 0; }
                                        });
                                    }

                                    return Task.CompletedTask;
                                }
                            };
                            bool hasApplied = false;
                            QEffect newQf = new(ExpirationCondition.ExpiresAtStartOfYourTurn)
                            {
                                StateCheckWithVisibleChanges = _ =>
                                {
                                    if (hasApplied) return Task.CompletedTask;
                                    ally.GainTemporaryHP(temp);
                                    hasApplied = true;
                                    return Task.CompletedTask;
                                },
                                WhenExpires = _ =>
                                {
                                    ally.TemporaryHP = 0;
                                    ally.AddQEffect(continuingTemp);
                                }
                            };
                            ally.AddQEffect(newQf);
                        }
                    });
                qf.ProvideMainAction = _ => new ActionPossibility(plantBanner).WithPossibilityGroup("Abilities");
            });
    }

    private static void AdaptiveStratagemLogic(TrueFeat feat)
    {
        feat.WithActionCost(0).WithPermanentQEffect(
            "At the start of combat, you may replace one of your prepared expert, mobility, or offensive tactics with another tactic in your folio.",
            qf =>
            {
                Creature self = qf.Owner;
                List<string> preparedTactics = [];
                List<string> potentialTactics = [];
                CalculatedCharacterSheetValues? calculated = self.PersistentCharacterSheet?.Calculated;
                qf.StartOfCombat = async _ =>
                {
                    if (calculated?.AllFeatGrants is not null)
                    {
                        Dictionary<string, object?> tags = calculated.Tags.Where(pair =>
                                pair.Value is List<Trait> list &&
                                (list.Contains(ModData.MTraits.BasicTactic) ||
                                 list.Contains(ModData.MTraits.ExpertTactic)))
                            .ToDictionary();
                        preparedTactics.AddRange(tags.Keys);
                        potentialTactics.AddRange(calculated.AllFeatGrants
                            .Where(grant =>
                                grant.GrantedFeat.HasTrait(ModData.MTraits.TacticPre))
                            .Select(tactic1 => tactic1.GrantedFeat.Name));
                        foreach (string potentialTactic in potentialTactics
                                     .Where(tactic1 => preparedTactics.Contains(tactic1)).ToList())
                        {
                            potentialTactics.Remove(potentialTactic);
                        }

                        preparedTactics.Add("pass");
                        ChoiceButtonOption choice = await self.AskForChoiceAmongButtons(self.Illustration,
                            "Would you like to use Adaptive Stratagem to replace a prepared tactic?",
                            preparedTactics.ToArray()
                        );
                        if (preparedTactics[choice.Index] == "pass") return;
                        potentialTactics.Add("cancel");
                        ChoiceButtonOption choice2 = await self.AskForChoiceAmongButtons(self.Illustration,
                            "Which tactic would you like to add?", potentialTactics.ToArray()
                        );
                        if (potentialTactics[choice2.Index] == "cancel") return;
                        TacticsDict.TryGetValue(preparedTactics[choice.Index], out FeatName name);
                        self.RemoveAllQEffects(qff => (FeatName?)qff.Tag is { } ftName && ftName == name);
                        PrereqsDict.TryGetValue(potentialTactics[choice2.Index], out FeatName value);
                        PrereqsToTactics.TryGetValue(value, out FeatName tactic);
                        QEffect? tacticQf = TacticsQFs(self)
                            .FirstOrDefault(qff => (FeatName?)qff.Tag is { } here && here == tactic);
                        if (tacticQf != null) self.AddQEffect(tacticQf);
                    }
                };
            });
    }

    private static void DefensiveSwapLogic(TrueFeat feat)
    {
        feat.WithActionCost(-2).WithPermanentQEffect(
            "When you or an adjacent ally are the target of an attack, you may use a reaction to immediately swap positions with each other, and whichever of you was not the target of the triggering attack becomes the target instead.",
            qf =>
            {
                Creature self = qf.Owner;
                qf.AddGrantingOfTechnical(cr => cr.EnemyOf(self), qfTech =>
                {
                    qfTech.YouBeginAction = async (_, action) =>
                    {
                        if (!action.HasTrait(Trait.Attack) || action.ChosenTargets.ChosenCreatures.Count != 1) return;
                        if (action.ChosenTargets.ChosenCreature is { } ally && ally.FriendOfAndNotSelf(self) &&
                            ally.IsAdjacentTo(self) && CommonCombatActions.StepByStepStride(ally).WithActionCost(0)
                                .CanBeginToUse(ally) &&
                            CommonCombatActions.StepByStepStride(self).WithActionCost(0).CanBeginToUse(self)
                            && !self.HasEffect(QEffectId.Immobilized) && !ally.HasEffect(QEffectId.Immobilized))
                        {
                            bool confirm = await self.AskToUseReaction(
                                "Do you wish to use a reaction to swap positions with {Green}" + ally.Name +
                                "{/Green} and become the target of {b}" + action.Name + "{/b} from {Red}" +
                                action.Owner + "{/Red}.");
                            if (confirm)
                            {
                                Tile selfStart = self.Occupies;
                                Tile allyStart = ally.Occupies;
                                await self.SingleTileMove(allyStart, null);
                                await ally.SingleTileMove(selfStart, null);
                                action.ChosenTargets = ChosenTargets.CreateSingleTarget(self);
                                self.Overhead("Defensive Swap", Color.Black, self + " uses {b}Defensive Swap{/b}",
                                    "Defensive Swap {icon:Reaction}", qf.Description,
                                    new Traits([ModData.MTraits.Commander]));
                            }
                        }

                        if (action.ChosenTargets.ChosenCreature == self &&
                            CommonCombatActions.StepByStepStride(self).WithActionCost(0).CanBeginToUse(self)
                            && self.Battle.AllCreatures.Any(friend => friend.FriendOfAndNotSelf(self) &&
                                                                      friend.IsAdjacentTo(self) && CommonCombatActions
                                                                          .StepByStepStride(friend).WithActionCost(0)
                                                                          .CanBeginToUse(friend) &&
                                                                      !friend.HasEffect(QEffectId.Immobilized))
                            && !self.HasEffect(QEffectId.Immobilized))
                        {
                            bool confirm = await self.AskToUseReaction(
                                "Do you wish to use a reaction to swap positions with an adjacent ally and cause them to become the target of {b}" +
                                action.Name + "{/b} from {Red}" + action.Owner + "{/Red}.");
                            if (confirm)
                            {
                                Creature? friend = null;
                                IEnumerable<Creature?> allies = self.Battle.AllCreatures.Where(creature =>
                                    creature.FriendOfAndNotSelf(self) &&
                                    creature.IsAdjacentTo(self) && CommonCombatActions.StepByStepStride(creature)
                                        .WithActionCost(0)
                                        .CanBeginToUse(creature) && !creature.HasEffect(QEffectId.Immobilized));
                                IEnumerable<Creature?> enumerable = allies.ToList();
                                friend = enumerable.ToList().Count switch
                                {
                                    1 => enumerable.FirstOrDefault(),
                                    > 1 => await self.Battle.AskToChooseACreature(self, enumerable!, self.Illustration,
                                        "Choose an adjacent ally to swap with", "ally", "pass"),
                                    _ => friend
                                };
                                if (friend != null)
                                {
                                    Tile selfStart = self.Occupies;
                                    Tile allyStart = friend.Occupies;
                                    await self.SingleTileMove(allyStart, null);
                                    await friend.SingleTileMove(selfStart, null);
                                    action.ChosenTargets = ChosenTargets.CreateSingleTarget(friend);
                                    self.Overhead("Defensive Swap", Color.Black, self + " uses {b}Defensive Swap{/b}",
                                        "Defensive Swap {icon:Reaction}", qf.Description,
                                        new Traits([ModData.MTraits.Commander]));
                                }
                                else
                                {
                                    self.Actions.RefundReaction();
                                }
                            }
                        }
                    };
                });
            });
    }

    private static void GuidingShotLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            Creature self = qf.Owner;
            qf.ProvideStrikeModifier = item =>
            {
                CombatAction guidingShot = self.CreateStrike(item);
                guidingShot.Illustration = new SideBySideIllustration(item.Illustration, IllustrationName.TrueStrike);
                guidingShot.Traits.Add(Trait.Flourish);
                guidingShot.Traits.Add(ModData.MTraits.Commander);
                guidingShot.WithEffectOnEachTarget((shot, caster, target, result) =>
                {
                    int amount = result == CheckResult.CriticalSuccess ? 2 : 1;
                    if (result < CheckResult.Success) return Task.CompletedTask;
                    QEffect guide = new("Guiding Shot",
                        "The next attack made against this creature by anyone other than " + self.Name +
                        " will have a +" + amount + " circumstance bonus to hit.",
                        ExpirationCondition.ExpiresAtStartOfSourcesTurn, self, IllustrationName.TrueStrike)
                    {
                        AfterYouAreTargeted = (effect, action) =>
                        {
                            if (!action.HasTrait(Trait.Attack) || action == shot || action.Owner == caster)
                                return Task.CompletedTask;
                            effect.ExpiresAt = ExpirationCondition.Immediately;
                            return Task.CompletedTask;
                        },
                    };
                    guide.AddGrantingOfTechnical(cr => cr != caster, qfTech =>
                    {
                        qfTech.BonusToAttackRolls = (_, action, creature) =>
                        {
                            if (!action.HasTrait(Trait.Attack) || creature != target) return null;
                            return new Bonus(amount, BonusType.Circumstance, "Guiding Shot", true);
                        };
                    });
                    target.AddQEffect(guide);
                    return Task.CompletedTask;
                });
                guidingShot.Name = "Guiding Shot";
                guidingShot.Description = StrikeRules.CreateBasicStrikeDescription4(guidingShot.StrikeModifiers,
                    additionalSuccessText:
                    "The next creature other than you to attack the same target before the start of your next turn gains a +1 circumstance bonus to their roll, or a +2 circumstance bonus if your Strike was a critical hit.");
                return item.HasTrait(Trait.Ranged) ? guidingShot : null;
            };
        });
    }

    private static void SetUpStrikeLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            Creature self = qf.Owner;
            qf.ProvideStrikeModifier = item =>
            {
                CombatAction setupStrike = self.CreateStrike(item);
                setupStrike.Illustration =
                    new SideBySideIllustration(item.Illustration, IllustrationName.BigFlatfooted);
                setupStrike.Traits.Add(Trait.Flourish);
                setupStrike.Traits.Add(ModData.MTraits.Commander);
                setupStrike.WithEffectOnEachTarget((strike, caster, target, result) =>
                {
                    if (result < CheckResult.Success) return Task.CompletedTask;
                    QEffect setup = new("Set-up Strike",
                        "This creature will be off guard against the next attack made by allies of " + caster.Name +
                        ".", ExpirationCondition.ExpiresAtStartOfSourcesTurn, self, IllustrationName.Flatfooted)
                    {
                        AfterYouAreTargeted = (effect, action) =>
                        {
                            if (!action.HasTrait(Trait.Attack) || action == strike || action.Owner == caster)
                                return Task.CompletedTask;
                            effect.ExpiresAt = ExpirationCondition.Immediately;
                            return Task.CompletedTask;
                        },
                        IsFlatFootedTo = (_, creature, _) =>
                        {
                            if (creature != null && creature.FriendOfAndNotSelf(caster))
                                return "Set-up Strike";
                            return null;
                        }
                    };
                    target.AddQEffect(setup);
                    return Task.CompletedTask;
                });
                setupStrike.Name = "Set-up Strike";
                setupStrike.Description = StrikeRules.CreateBasicStrikeDescription4(setupStrike.StrikeModifiers,
                    additionalSuccessText:
                    "The target is off guard against the next attack that one of your allies attempts against it before the start of your next turn.");
                return setupStrike;
            };
        });
    }

    private static void RapidAssessmentLogic(TrueFeat feat)
    {
        feat.WithActionCost(0).WithPermanentQEffect(
            $"Attempt a check to {UseCreatedTooltip("Recall Weakness")} against one creature you are observing.",
            effect =>
            {
                Creature self = effect.Owner;
                effect.StartOfCombat = async _ =>
                {
                    if (Possibilities.Create(self).Filter(ap =>
                            {
                                if (!ap.CombatAction.Name.Contains("Recall Weakness"))
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.CombatAction.Target = Target.Distance(500);
                                ap.RecalculateUsability();
                                return true;
                            }).CreateActions(true)
                            .FirstOrDefault(pw => pw.Action.Name.Contains("Recall Weakness")) is CombatAction
                        investigateAction)
                    {
                        investigateAction.Name = "Rapid Assessment";
                        if (self.HasFeat(ModData.MFeatNames.UnrivaledAnalysis))
                        {
                            if (investigateAction.Target is CreatureTarget original)
                                investigateAction.Target =
                                    Target.MultipleCreatureTargets(original, original, original, original)
                                        .WithMinimumTargets(1).WithMustBeDistinct();
                            investigateAction.Name = "Rapid Assessment - Unrivaled Analysis";
                        }

                        if (self.Battle.AllCreatures.Any(cr => cr.EnemyOf(self) && cr.VisibleToHumanPlayer))
                            await self.Battle.GameLoop.FullCast(investigateAction);
                    }
                };
            });
    }

    private static void BannerTwirlLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(
            "You and any ally adjacent to you have concealment from ranged attacks until the start of your next turn",
            qf =>
            {
                qf.ProvideMainAction = effect =>
                {
                    CombatAction twirl = new CombatAction(effect.Owner, ModData.MIllustrations.BannerTwirl,
                            "Banner Twirl",
                            [ModData.MTraits.Brandish, ModData.MTraits.Commander, Trait.Manipulate, Trait.Basic],
                            "You and any ally adjacent to you have concealment from ranged attacks until the start of your next turn",
                            (Target.AlliesOnlyEmanation(1) as AreaTarget)!.WithAdditionalRequirementOnCaster(creature =>
                                new BrandishRequirement().Satisfied(creature, creature)))
                        .WithActionCost(1).WithSoundEffect(SfxName.ItemAction)
                        .WithEffectOnEachTarget((action, caster, target, _) =>
                        {
                            QEffect concealment = new()
                            {
                                ThisCreatureCannotBeMoreVisibleThan = DetectionStrength.ConcealedViaBlur,
                                Name = "Banner Conceal"
                            };
                            target.AddQEffect(new QEffect("Banner Twirl",
                                "You have concealment from ranged attacks until the start of " + caster.Name +
                                "'s next turn", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster,
                                action.Illustration)
                            {
                                YouAreTargeted = (qEffect, combatAction) =>
                                {
                                    if (combatAction.HasTrait(Trait.Ranged))
                                    {
                                        qEffect.Owner.AddQEffect(concealment);
                                    }

                                    return Task.CompletedTask;
                                },
                                AfterYouAreTargeted = (qEffect, _) =>
                                {
                                    if (qEffect.Owner.QEffects.FirstOrDefault(qff => qff.Name == concealment.Name) is
                                        { } conceal)
                                    {
                                        conceal.ExpiresAt = ExpirationCondition.Immediately;
                                    }

                                    return Task.CompletedTask;
                                }
                            });
                            return Task.CompletedTask;
                        });
                    return new ActionPossibility(twirl).WithPossibilityGroup("Abilities");
                };
            });
    }

    private static void BannersInspirationLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(
            "Each ally in your banner's aura reduces their frightened and stupefied conditions by 1. These allies can also attempt a saving throw to end the paralyzed or confused conditions.",
            qf =>
            {
                qf.ProvideMainAction = effect =>
                {
                    CombatAction inspiration = new CombatAction(effect.Owner, ModData.MIllustrations.InspiringBanner,
                            "Banner's Inspiration",
                            [
                                ModData.MTraits.Brandish, ModData.MTraits.Commander, Trait.Emotion, Trait.Flourish,
                                Trait.Mental, Trait.Visual, Trait.Basic
                            ],
                            "Each ally in your banner's aura reduces their frightened and stupefied conditions by 1, and can make a Will save against a standard level-based DC for your level, and on a success or better remove the Confused or Paralyzed condition. Regardless of the result, any ally that attempts this save is temporarily immune to Banner's Inspiration for 10 minutes.",
                            new EmanationTarget(100, false)
                                .WithAdditionalRequirementOnCaster(cr => new BrandishRequirement().Satisfied(cr, cr))
                                .WithIncludeOnlyIf((_, creature) =>
                                    new InBannerAuraRequirement().Satisfied(effect.Owner, creature)))
                        .WithActionCost(1).WithSoundEffect(SfxName.Drum)
                        .WithActionId(ModData.MActionIds.BannersInspiration)
                        .WithEffectOnEachTarget((spell, caster, target, _) =>
                        {
                            if (target.FindQEffect(QEffectId.Stupefied) is { } stupefied)
                            {
                                stupefied.Value -= 1;
                                if (stupefied.Value <= 0) stupefied.ExpiresAt = ExpirationCondition.Immediately;
                            }

                            if (target.FindQEffect(QEffectId.Frightened) is { } frightened)
                            {
                                frightened.Value -= 1;
                                if (frightened.Value <= 0) frightened.ExpiresAt = ExpirationCondition.Immediately;
                            }

                            if (!target.HasEffect(QEffectId.Confused) && !target.HasEffect(QEffectId.Paralyzed))
                                return Task.CompletedTask;
                            CheckResult save = CommonSpellEffects.RollSavingThrow(target, spell, Defense.Will,
                                Checks.LevelBasedDC(caster.Level));
                            if (save >= CheckResult.Success)
                            {
                                QEffectId? toRemove = target.QEffects.FirstOrDefault(qff =>
                                    qff.Id is QEffectId.Confused or QEffectId.Paralyzed)?.Id;
                                target.RemoveAllQEffects(qff => qff.Id == toRemove);
                            }

                            target.AddQEffect(QEffect.ImmunityToTargeting(ModData.MActionIds.BannersInspiration));
                            return Task.CompletedTask;
                        });
                    return new ActionPossibility(inspiration).WithPossibilityGroup("Abilities");
                };
            });
    }

    private static void ObservationalAnalysisLogic(TrueFeat feat)
    {
        feat.WithPrerequisite(ModData.MFeatNames.CombatAssessment, "Combat Assessment").WithPermanentQEffect(
            $"When you use Combat Assessment against a target that you or an ally has targeted with a Strike or spell since the start of your last turn, you get a +2 circumstance bonus to the {UseCreatedTooltip("Recall Weakness")} check (+4 if the Strike from Combat Assessment is a critical hit).",
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
                            if (action.SpellInformation == null || !action.HasTrait(Trait.Strike))
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

    private static void UnsteadyingStrikeLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            Creature self = qf.Owner;
            qf.ProvideStrikeModifier = item =>
            {
                CombatAction unsteady = self.CreateStrike(item);
                unsteady.Illustration = new SideBySideIllustration(item.Illustration, IllustrationName.Shove);
                unsteady.Name = "Unsteadying Strike";
                unsteady.Description = StrikeRules.CreateBasicStrikeDescription4(unsteady.StrikeModifiers,
                    additionalSuccessText:
                    "The enemy takes a –2 circumstance penalty to their Fortitude DC to resist being Grappled, Repositioned, or Shoved and a –2 circumstance penalty to their Reflex DC to resist being Disarmed. Both penalties last until the start of your next turn.");
                unsteady.Traits.Add(Trait.Flourish);
                unsteady.Traits.Add(ModData.MTraits.Commander);
                unsteady.WithEffectOnEachTarget((_, caster, target, result) =>
                {
                    if (result <= CheckResult.Failure) return Task.CompletedTask;
                    target.AddQEffect(new QEffect("Unsteadying Strike",
                        "This creature takes a –2 circumstance penalty to their Fortitude DC to resist being Grappled, Repositioned, or Shoved and a –2 circumstance penalty to their Reflex DC to resist being Disarmed.",
                        ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, IllustrationName.Shove)
                    {
                        BonusToDefenses = (_, action, defense) =>
                        {
                            if (action == null) return null;
                            if (((action is { ActionId: ActionId.Shove or ActionId.Grapple } ||
                                  action.ActionId == ModData.MActionIds.Reposition) && defense == Defense.Fortitude) ||
                                (action.ActionId == ActionId.Disarm && defense == Defense.Reflex))
                            {
                                return new Bonus(-2, BonusType.Circumstance, "Unsteadying Strike");
                            }

                            return null;
                        }
                    });
                    return Task.CompletedTask;
                });
                return unsteady;
            };
        });
    }

    private static void ShieldedRecoveryLogic(TrueFeat feat)
    {
        feat.WithPrerequisite(ModData.MFeatNames.OfficerMedic, "Officer's Medical Training").WithPermanentQEffect(
            "You can use the same hand you are using to wield a shield to Treat Wounds or use Battle Medicine, and you are considered to have a hand free for other uses of Medicine as long as the only thing you are holding or wielding in that hand is a shield. When you use Battle Medicine on an ally while wielding a shield, they gain a +1 circumstance bonus to AC and Reflex saves that lasts until the start of your next turn or until they are no longer adjacent to you, whichever comes first.",
            qf =>
            {
                Creature self = qf.Owner;
                qf.AfterYouTakeActionAgainstTarget = (_, action, ally, _) =>
                {
                    if (!ally.FriendOfAndNotSelf(self) || !action.Name.Contains("Battle Medicine"))
                        return Task.CompletedTask;
                    ally.AddQEffect(new QEffect("Shielded Recovery",
                        "You gain a +1 circumstance bonus to AC and Reflex saves as long as you are adjacent to " +
                        self.Name + ".",
                        ExpirationCondition.ExpiresAtStartOfSourcesTurn, self, IllustrationName.HealersTools)
                    {
                        BonusToDefenses = (_, _, defense) =>
                            defense is not (Defense.AC or Defense.Reflex)
                                ? null
                                : new Bonus(1, BonusType.Circumstance, "Shielded Recovery"),
                        StateCheck = effect =>
                        {
                            if (!ally.IsAdjacentTo(self))
                                effect.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    });
                    return Task.CompletedTask;
                };
            });
    }

    private static void ClaimTheFieldLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPrerequisite(ModData.MFeatNames.PlantBanner, "Plant Banner").WithPermanentQEffect(
            "You can use a thrown weapon to use Plant Banner at range.", qf =>
            {
                qf.ProvideStrikeModifier = item =>
                {
                    int? distance = ModManager.TryParse("Thrown30Feet", out Trait thrown30) && item.HasTrait(thrown30)
                        ?
                        6
                        : item.HasTrait(Trait.Thrown20Feet)
                            ? 4
                            : item.HasTrait(Trait.Thrown10Feet)
                                ? 2
                                : null;
                    if (distance == null) return null;
                    CombatAction claimTheField = new CombatAction(qf.Owner,
                            new SideBySideIllustration(item.Illustration, ModData.MIllustrations.PlantBanner),
                            "Claim the Field", [ModData.MTraits.Commander, Trait.Basic, Trait.Manipulate],
                            "You can plant your banner at any corner within your weapon's first range increment. Each ally within a 30-foot burst centered on your banner immediately gains 4 temporary Hit Points, plus an additional 4 temporary Hit Points at 4th level and every 4 levels thereafter. " +
                            "These temporary Hit Points last for 1 round; each time an ally starts their turn within the burst, their temporary Hit Points are renewed for another round. " +
                            "You cannot wield your banner while it is planted. While your banner is planted, the emanation around your banner is a 35 foot emanation." +
                            "\n\nYou can use an Interact action while adjacent to your banner to retrieve it. An enemy adjacent to the square you planted your banner in can remove your banner as an Interact action, ending this effect and preventing you and your allies from gaining any of your banner's other benefits until you have successfully retrieved it." +
                            "\nAny enemy who attempts to remove your banner while it is planted in this way must succeed at a Will save against your class DC or the attempt fails. On a critical failure, the enemy is fleeing for 1 round. This is an incapacitation and mental effect.",
                            Target.Burst(distance.Value, 6)
                                .WithIncludeOnlyIf((_, cr) => cr.FriendOf(qf.Owner)))
                        .WithActionCost(1).WithSoundEffect(ModData.Sfx.Drums)
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            Creature illusory = Creature.CreateIndestructibleObject(
                                IllustrationName.None,
                                "Banner", caster.Level);
                            illusory.Traits.Add(ModData.MTraits.Banner);
                            QEffect? radius = caster.FindQEffect(ModData.MQEffectIds.BannerRadius);
                            Tile? thrownTo = caster.Battle.Map.GetTile(targets.ChosenPointOfOrigin.X,
                                targets.ChosenPointOfOrigin.Y);
                            if (thrownTo == null) return;
                            int value = 6;
                            if (radius != null)
                                value = radius.Value;
                            QEffect planted = new()
                            {
                                StateCheckWithVisibleChanges = qff =>
                                {
                                    Tile? bannerTile =
                                        qff.Owner.Battle.Map.AllTiles.FirstOrDefault(tile =>
                                            IsMyBanner(qff.Owner, tile));
                                    if (bannerTile != null)
                                    {
                                        if (radius != null)
                                            radius.Value = 7;
                                    }
                                    else
                                    {
                                        qff.ExpiresAt = ExpirationCondition.Immediately;
                                    }

                                    return Task.CompletedTask;
                                },
                                ProvideContextualAction = ef =>
                                {
                                    Tile bannerTile =
                                        ef.Owner.Battle.Map.AllTiles.FirstOrDefault(tile =>
                                            IsMyBanner(ef.Owner, tile))!;
                                    CombatAction removeBanner = new CombatAction(caster,
                                            ModData.MIllustrations.SimpleBanner,
                                            "Remove Banner", [Trait.Manipulate, Trait.Basic],
                                            "Removes your banner, returning it to where it was.",
                                            Target.Self().WithAdditionalRestriction(cr =>
                                                cr.HasFreeHand
                                                    ? null
                                                    : "You must have a free hand to remove the banner."))
                                        .WithActionCost(1).WithEffectOnChosenTargets(async (cr, _) =>
                                        {
                                            Item? bannerItem = ef.Tag as Item;
                                            TileQEffect? tileQEffect =
                                                bannerTile.TileQEffects.FirstOrDefault(tQ =>
                                                    tQ.TileQEffectId == ModData.MTileQEffectIds.Banner);
                                            if (tileQEffect != null)
                                                tileQEffect.ExpiresAt = ExpirationCondition.Immediately;
                                            await cr.Battle.GameLoop.StateCheck();
                                            if (bannerItem is not null && !bannerItem.HasTrait(Trait.Worn))
                                                cr.AddHeldItem(bannerItem);
                                            else
                                            {
                                                if (bannerItem != null) cr.CarriedItems.Add(bannerItem);
                                                else
                                                {
                                                    AuraAnimation animation =
                                                        cr.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle,
                                                            GetBannerRadius(cr));
                                                    animation.Color = Color.Coral;
                                                    QEffect commandersBannerEffect =
                                                        CommandersBannerEffect(animation, GetBannerRadius(cr), cr);
                                                    cr.AddQEffect(commandersBannerEffect);
                                                }
                                            }
                                        });
                                    return bannerTile.IsAdjacentTo(ef.Owner.Occupies) ||
                                           bannerTile.Equals(ef.Owner.Occupies)
                                        ? new ActionPossibility(removeBanner)
                                        : null;
                                },
                                WhenExpires = _ =>
                                {
                                    if (radius != null) radius.Value = value;
                                }
                            };
                            planted.AddGrantingOfTechnical(cr => cr.EnemyOf(caster), qfTech =>
                            {
                                qfTech.ProvideContextualAction = _ =>
                                {
                                    Tile? bannerTile =
                                        caster.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(caster, tile));
                                    return bannerTile != null && (bannerTile.IsAdjacentTo(qfTech.Owner.Occupies) ||
                                                                  bannerTile.Equals(qfTech.Owner.Occupies))
                                        ? new ActionPossibility(new CombatAction(qfTech.Owner,
                                                ModData.MIllustrations.SimpleBanner,
                                                "Steal Banner", [Trait.Manipulate, Trait.Basic],
                                                "Steals the Commander's banner, frightening his allies.",
                                                Target.Self().WithAdditionalRestriction(cr =>
                                                    cr.HasFreeHand
                                                        ? null
                                                        : "You must have a free hand to steal the banner."))
                                            .WithActionCost(1).WithGoodness((_, cr, _) =>
                                                cr.HasEffect(QEffect.Mindless()) ? int.MinValue : cr.Level * 6)
                                            .WithEffectOnChosenTargets(async (combatAction, cr, _) =>
                                            {
                                                Item? bannerItem = planted.Tag as Item;
                                                TileQEffect? tileQEffect =
                                                    bannerTile.TileQEffects.FirstOrDefault(tQ =>
                                                        tQ.TileQEffectId == ModData.MTileQEffectIds.Banner);
                                                CheckResult save = CommonSpellEffects.RollSavingThrow(
                                                    combatAction.Owner, spell, Defense.Will,
                                                    caster.ClassDC(ModData.MTraits.Commander));
                                                if (cr.Level > caster.Level) save.ImproveByOneStep();
                                                switch (save)
                                                {
                                                    case CheckResult.CriticalFailure when !cr.IsImmuneTo(Trait.Mental):
                                                        cr.AddQEffect(QEffect.Fleeing(caster)
                                                            .WithExpirationAtStartOfOwnerTurn());
                                                        combatAction.Disrupted = true;
                                                        return;
                                                    case <= CheckResult.Failure when !cr.IsImmuneTo(Trait.Mental):
                                                        combatAction.Disrupted = true;
                                                        return;
                                                }

                                                if (tileQEffect != null)
                                                    tileQEffect.ExpiresAt = ExpirationCondition.Immediately;
                                                await cr.Battle.GameLoop.StateCheck();
                                                foreach (Creature enemy in caster.Battle.AllCreatures.Where(enemy =>
                                                             enemy.FriendOfAndNotSelf(caster) &&
                                                             enemy.DistanceTo(bannerTile) <= 7))
                                                {
                                                    if (enemy.IsImmuneTo(Trait.Mental) ||
                                                        enemy.IsImmuneTo(Trait.Visual) ||
                                                        enemy.IsImmuneTo(Trait.Fear) || enemy.IsImmuneTo(Trait.Emotion))
                                                        continue;
                                                    enemy.AddQEffect(QEffect.Frightened(1));
                                                }

                                                cr.AddQEffect(new QEffect
                                                {
                                                    WhenCreatureDiesAtStateCheckAsync = _ =>
                                                    {
                                                        if (bannerItem != null)
                                                        {
                                                            cr.DropItem(bannerItem);
                                                        }
                                                        else
                                                        {
                                                            cr.Occupies.AddQEffect(new TileQEffect()
                                                            {
                                                                StateCheck = qfTile =>
                                                                {
                                                                    if (qfTile.Owner.PrimaryOccupant != caster &&
                                                                        !qfTile.Owner.IsAdjacentTo(caster.Occupies))
                                                                        return;
                                                                    caster.AddQEffect(
                                                                        new QEffect(ExpirationCondition.Ephemeral)
                                                                        {
                                                                            ProvideContextualAction = armorBanner =>
                                                                            {
                                                                                CombatAction returnBanner =
                                                                                    new CombatAction(
                                                                                            armorBanner.Owner,
                                                                                            ModData.MIllustrations
                                                                                                .Banner,
                                                                                            "Regain Banner",
                                                                                            [
                                                                                                Trait.Manipulate,
                                                                                                Trait.Basic
                                                                                            ],
                                                                                            "Returns your banner to its proper place.",
                                                                                            Target.Self())
                                                                                        .WithActionCost(1)
                                                                                        .WithEffectOnChosenTargets((
                                                                                            creature, _) =>
                                                                                        {
                                                                                            AuraAnimation animation =
                                                                                                creature.AnimationData
                                                                                                    .AddAuraAnimation(
                                                                                                        IllustrationName
                                                                                                            .BlessCircle,
                                                                                                        GetBannerRadius(
                                                                                                            creature));
                                                                                            animation.Color =
                                                                                                Color.Coral;
                                                                                            QEffect
                                                                                                commandersBannerEffect =
                                                                                                    CommandersBannerEffect(
                                                                                                        animation,
                                                                                                        GetBannerRadius(
                                                                                                            creature),
                                                                                                        creature);
                                                                                            creature.AddQEffect(
                                                                                                commandersBannerEffect);
                                                                                            qfTile.ExpiresAt =
                                                                                                ExpirationCondition
                                                                                                    .Immediately;
                                                                                            return Task.CompletedTask;
                                                                                        });
                                                                                return new ActionPossibility(
                                                                                    returnBanner);
                                                                            }
                                                                        });
                                                                },
                                                                Illustration = ModData.MIllustrations.Banner
                                                            });
                                                        }

                                                        return Task.CompletedTask;
                                                    }
                                                });
                                                if (bannerItem != null && !bannerItem.HasTrait(Trait.Worn))
                                                    cr.AddHeldItem(bannerItem);
                                                else
                                                {
                                                    if (bannerItem != null) cr.CarriedItems.Add(bannerItem);
                                                }
                                            }))
                                        : null;
                                };
                            });
                            planted.Tag = item;
                            caster.HeldItems.Remove(item);
                            caster.Battle.Log(targets.ChosenTiles.Count.ToString());
                            caster.RemoveAllQEffects(effect => effect.Id == ModData.MQEffectIds.Banner);
                            thrownTo.AddQEffect(CommandersBannerTileEffect(7, caster, illusory));
                            caster.AddQEffect(planted);
                            illusory.DescriptionFulltext = "This is " + caster.Name + "'s banner.";
                            await caster.Battle.GameLoop.StateCheck();
                            caster.Battle.SpawnIllusoryCreature(illusory, thrownTo);
                            AuraAnimation auraAnimation = illusory.AnimationData.AddAuraAnimation(
                                IllustrationName.BlessCircle,
                                GetBannerRadius(caster));
                            auraAnimation.Color = Color.DarkKhaki;
                            var temp = (1 + caster.Level / 4) * 4;
                            foreach (Creature ally in targets.ChosenCreatures)
                            {
                                QEffect continuingTemp = new()
                                {
                                    StateCheckWithVisibleChanges = qff =>
                                    {
                                        Tile? bannerTile =
                                            qff.Owner.Battle.Map.AllTiles.FirstOrDefault(tile =>
                                                IsMyBanner(caster, tile));
                                        if (bannerTile == null)
                                            qff.ExpiresAt = ExpirationCondition.Immediately;
                                        return Task.CompletedTask;
                                    },
                                    StartOfYourPrimaryTurn = (qff, cr) =>
                                    {
                                        Tile? bannerTile =
                                            qff.Owner.Battle.Map.AllTiles.FirstOrDefault(tile =>
                                                IsMyBanner(caster, tile));
                                        if (bannerTile == null)
                                        {
                                            qff.ExpiresAt = ExpirationCondition.Immediately;
                                            return Task.CompletedTask;
                                        }

                                        if (targets.ChosenTiles.Any(tile => Equals(tile, cr.Occupies)))
                                        {
                                            bool applied = false;
                                            cr.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                                            {
                                                StateCheckWithVisibleChanges = _ =>
                                                {
                                                    if (applied) return Task.CompletedTask;
                                                    ally.GainTemporaryHP(temp);
                                                    applied = true;
                                                    return Task.CompletedTask;
                                                },
                                                WhenExpires = _ => { ally.TemporaryHP = 0; }
                                            });
                                        }

                                        return Task.CompletedTask;
                                    }
                                };
                                bool hasApplied = false;
                                QEffect newQf = new(ExpirationCondition.ExpiresAtStartOfYourTurn)
                                {
                                    StateCheckWithVisibleChanges = _ =>
                                    {
                                        if (hasApplied) return Task.CompletedTask;
                                        ally.GainTemporaryHP(temp);
                                        hasApplied = true;
                                        return Task.CompletedTask;
                                    },
                                    WhenExpires = _ =>
                                    {
                                        ally.TemporaryHP = 0;
                                        ally.AddQEffect(continuingTemp);
                                    }
                                };
                                ally.AddQEffect(newQf);
                            }
                        });
                    return item.HasTrait(ModData.MTraits.Banner) && (item.HasTrait(Trait.Thrown20Feet) ||
                                                                     item.HasTrait(Trait.Thrown10Feet) ||
                                                                     item.HasTrait(thrown30))
                        ? claimTheField
                        : null;
                };
            });
    }

    private static void EfficientPreparationLogic(TrueFeat feat)
    {
        feat.WithOnSheet(values =>
        {
            int prepTactics = values.Sheet.MaximumLevel >= 19 ? 7 :
                values.Sheet.MaximumLevel >= 15 ? 6 :
                values.Sheet.MaximumLevel >= 7 ? 5 : 4;
            values.Tags.Remove("PreparedTactics");
            values.Tags.Add("PreparedTactics", prepTactics);
            var myOption = values.SelectionOptions
                .FirstOrDefault(option => option.Name == "Prepared Tactics") as MultipleFeatSelectionOption;
            if (myOption == null) return;
            FieldInfo? maxOptions = typeof(MultipleFeatSelectionOption)
                .GetField("<MaximumNumberOfOptions>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (maxOptions == null) return;
            maxOptions.SetValue(myOption, prepTactics);
        });
    }

    private static void BattleTestedCompanionLogic(TrueFeat feat)
    {
        feat.WithPrerequisite(ModData.MFeatNames.CommandersCompanion, "Commander's Companion").WithPermanentQEffect(
            "Your animal companion is stronger.", qf =>
            {
                qf.Id = QEffectId.MatureAnimalCompanion;
                qf.StartOfCombat = _ =>
                {
                    if (qf.Owner.HasFeat(ModData.MFeatNames.BattleHardenedCompanion) ||
                        qf.Owner.HasFeat(FeatName.MatureAnimalCompanionRanger) ||
                        qf.Owner.HasFeat(FeatName.MatureAnimalCompanionDruid) ||
                        qf.Owner.HasFeat(FeatName.LoyalCompanion)) return Task.CompletedTask;
                    qf.Owner.RemoveAllQEffects(qf1 => qf1 == qf1.Owner.QEffects.FirstOrDefault(qff =>
                        (qff.ProvideMainAction?.Invoke(qff) as ActionPossibility)?.CombatAction.Name ==
                        "Act on your own"));
                    qf.Owner.RemoveAllQEffects(qf1 => qf1 == qf1.Owner.QEffects.FirstOrDefault(qff =>
                        (qff.ProvideMainAction?.Invoke(qff) as ActionPossibility)?.CombatAction.Name ==
                        "Command your animal companion"));
                    qf.Owner.AddQEffect(new QEffect()
                    {
                        ProvideMainAction = qfPointless =>
                        {
                            Creature owner = qfPointless.Owner;
                            QEffect? controller = owner.FindQEffect(QEffectId.AnimalCompanionController);
                            if (controller == null)
                                return null;
                            Creature? animalCompanion = Ranger.GetAnimalCompanion(owner);
                            if (animalCompanion == null || !animalCompanion.Actions.CanTakeActions())
                                return null;
                            bool flag = owner.HasEffect(QEffectId.CompanionsCry);
                            int num = flag ? 1 : 0;
                            if (!flag)
                                return new ActionPossibility(CreateCommandAnAnimal(-1));
                            SubmenuPossibility submenuPossibility = new(animalCompanion.Illustration,
                                "Command your animal companion");
                            List<PossibilitySection> subsections = submenuPossibility.Subsections;
                            PossibilitySection possibilitySection = new("Command your animal companion");
                            List<Possibility> possibilities1 = possibilitySection.Possibilities;
                            possibilities1.Add(new ActionPossibility(CreateCommandAnAnimal(1))
                            {
                                Caption = "One action"
                            });
                            List<Possibility> possibilities2 = possibilitySection.Possibilities;
                            possibilities2.Add(new ActionPossibility(CreateCommandAnAnimal(2))
                            {
                                Caption = "Two actions"
                            });
                            subsections.Add(possibilitySection);
                            return submenuPossibility;

                            CombatAction CreateCommandAnAnimal(int numberOfActions)
                            {
                                int actionCost = numberOfActions <= 1 ? 1 : numberOfActions;
                                CombatAction combatAction = new(owner,
                                    numberOfActions > 0
                                        ? new SideBySideIllustration(animalCompanion.Illustration,
                                            numberOfActions == 2
                                                ? IllustrationName.TwoActions
                                                : IllustrationName.Action)
                                        : animalCompanion.Illustration, "Command your animal companion",
                                    [Trait.Auditory],
                                    $"Take {actionCost + 1} actions as your animal companion.\n\nYou can only command your animal companion once per turn.",
                                    Target.Self().WithAdditionalRestriction(_ =>
                                        GetAnimalCompanionCommandRestriction(controller, animalCompanion)))
                                {
                                    ShortDescription = $"Take {actionCost + 1} actions as your animal companion."
                                };
                                return combatAction.WithActionCost(actionCost).WithEffectOnSelf(async _ =>
                                {
                                    Steam.CollectAchievement("RANGER");
                                    controller.UsedThisTurn = true;
                                    if (numberOfActions == 2)
                                    {
                                        animalCompanion.Actions.AnimateActionUsedTo(0, ActionDisplayStyle.Available);
                                        ++animalCompanion.Actions.ActionsLeft;
                                    }

                                    await CommonSpellEffects.YourMinionActs(animalCompanion);
                                });
                            }
                        }
                    });
                    return Task.CompletedTask;
                };
                // qf.AfterYouTakeAction = (_, action) =>
                // {
                //     Creature owner = qf.Owner;
                //     Creature? companion =
                //         owner.Battle.AllCreatures.FirstOrDefault(cr => IsMyAnimalCompanion(owner, cr));
                //     if (companion != null && action.Name == "Act on your own")
                //     {
                //         companion.AddQEffect(new QEffect("Trained Reaction",
                //                 "Your companion has a reaction it can only use in response to your tactics. This reaction is lost if not used by the end of your turn.",
                //                 ExpirationCondition.ExpiresAtEndOfYourTurn, owner, IllustrationName.Reaction)
                //             { Id = MQEffectIds.AnimalReaction });
                //     }
                //     return Task.CompletedTask;
                //     
                // };
            });
    }

    private static void DefiantBannerLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect("", qf =>
        {
            Creature owner = qf.Owner;
            qf.Description +=
                $"You and all allies within the aura of your commander's banner when you use this action gain resistance {owner.Abilities.Intelligence} to bludgeoning, piercing, and slashing damage until the start of your next turn.";
            qf.ProvideMainAction = _ =>
            {
                CombatAction defiant = new CombatAction(owner, ModData.MIllustrations.DefiantBanner, "Defiant Banner",
                        [
                            ModData.MTraits.Brandish, ModData.MTraits.Commander, Trait.Flourish, Trait.Manipulate,
                            Trait.Visual, Trait.Basic
                        ],
                        $"You and all allies within the aura of your commander's banner when you use this action gain resistance {owner.Abilities.Intelligence} to bludgeoning, piercing, and slashing damage until the start of your next turn.",
                        Target.Emanation(GetBannerRadius(owner))
                            .WithAdditionalRequirementOnCaster(cr => new BrandishRequirement().Satisfied(cr, cr))
                            .WithIncludeOnlyIf((_, cr) =>
                                new InBannerAuraRequirement().Satisfied(owner, cr) && cr.FriendOf(owner)))
                    .WithActionCost(1).WithSoundEffect(SfxName.BeastRoar)
                    .WithEffectOnEachTarget((spell, caster, target, _) =>
                    {
                        int value = caster.Abilities.Intelligence;
                        target.AddQEffect(QEffect.DamageResistance(DamageKind.Piercing, value)
                            .WithExpirationAtStartOfSourcesTurn(caster, 1));
                        target.AddQEffect(QEffect.DamageResistance(DamageKind.Slashing, value)
                            .WithExpirationAtStartOfSourcesTurn(caster, 1));
                        target.AddQEffect(QEffect.DamageResistance(DamageKind.Bludgeoning, value)
                            .WithExpirationAtStartOfSourcesTurn(caster, 1));
                        target.AddQEffect(new QEffect("Defiant Banner",
                            $"You gain resistance {value} to bludgeoning, piercing, and slashing damage.",
                            ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, spell.Illustration));
                        return Task.CompletedTask;
                    });
                return new ActionPossibility(defiant).WithPossibilityGroup("Abilities");
            };
        });
    }

    private static void RallyingBannerLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect("", qf =>
        {
            Creature owner = qf.Owner;
            qf.Description +=
                $"You restore {4 + (owner.Level - 8) / 2}d6 Hit Points to each ally within the aura of your commander's banner. You may only use Rallying Banner once per encounter.";
            qf.ProvideMainAction = _ =>
            {
                CombatAction rally = new CombatAction(owner, ModData.MIllustrations.RallyingBanner, "Rallying Banner",
                        [
                            ModData.MTraits.Brandish, ModData.MTraits.Commander, Trait.Emotion, Trait.Healing,
                            Trait.Mental, Trait.Visual, Trait.Basic
                        ],
                        $"You restore {4 + (owner.Level - 8) / 2}d6 Hit Points to each ally within the aura of your commander's banner. You may only use Rallying Banner once per encounter.",
                        Target.Emanation(GetBannerRadius(owner))
                            .WithAdditionalRequirementOnCaster(cr => new BrandishRequirement().Satisfied(cr, cr))
                            .WithIncludeOnlyIf((_, cr) =>
                                new InBannerAuraRequirement().Satisfied(owner, cr) && cr.FriendOf(owner)))
                    .WithActionCost(1).WithSoundEffect(SfxName.Healing).WithActionId(ModData.MActionIds.RallyBanner)
                    .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                    {
                        foreach (Creature target in targets.ChosenCreatures)
                        {
                            await target.HealAsync($"{4 + (owner.Level - 8) / 2}d6", spell);
                        }

                        caster.AddQEffect(new QEffect
                        {
                            PreventTakingAction = action =>
                                action.ActionId == ModData.MActionIds.RallyBanner
                                    ? "You can only use Rally Banner once per encounter."
                                    : null
                        });
                    });
                return new ActionPossibility(rally).WithPossibilityGroup("Abilities");
            };
        });
    }

    #endregion
}