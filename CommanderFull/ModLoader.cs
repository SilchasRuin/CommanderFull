using System.Runtime.CompilerServices;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DawnniExpanded;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace CommanderFull;

public class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        var harmony = new Harmony("commanderMod");
        harmony.PatchAll();
        ItemName banner = BannerItem.Banner;
        ItemName harness = BannerItem.Harness;
        foreach (Feat feat in Commander.LoadAll())
        {
            ModManager.AddFeat(feat);
        }
        Commander.LoadGenericFeats();
        AllFeats.GetFeatByFeatName(FeatName.LengthyDiversion).Prerequisites
            .RemoveAll(req => req.Description.Contains("trained in Deception"));
        AllFeats.GetFeatByFeatName(FeatName.LengthyDiversion).WithPrerequisite(values => values.GetProficiency(Trait.Deception) >= Proficiency.Trained || (values.HasFeat(ModData.MFeatNames.DeceptiveTactics) && values.GetProficiency(ExplorationActivities.ModData.Traits.WarfareLore) >= Proficiency.Trained), "You must be trained in Deception.");
        AllFeats.GetFeatByFeatName(FeatName.Confabulator).Prerequisites.Remove(AllFeats.GetFeatByFeatName(FeatName.Confabulator).Prerequisites.FirstOrDefault(pre => pre.Description == "You must be expert in Deception.")!);
        AllFeats.GetFeatByFeatName(FeatName.Confabulator).WithPrerequisite(values => values.GetProficiency(Trait.Deception) >= Proficiency.Expert || (values.HasFeat(ModData.MFeatNames.DeceptiveTactics) && values.GetProficiency(ExplorationActivities.ModData.Traits.WarfareLore) >= Proficiency.Expert), "You must be expert in Deception.");
        ModManager.RegisterActionOnEachActionPossibility(action =>
        {
            if (!action.Owner.HasEffect(ModData.MQEffectIds.ProtectiveScreenQf)) return;
            if ((action.HasTrait(Trait.Ranged) && action.HasTrait(Trait.Attack)) || action.SpellInformation != null)
            {
                action.Traits.Add(Trait.DoesNotProvoke);
            }
        });
        if (!ModManager.TryParse("Reposition", out ActionId _))
        {
            ModManager.RegisterActionOnEachCreature(cr =>
            {
                cr.AddQEffect(new QEffect()
                {
                    ProvideActionIntoPossibilitySection = (qf, possibility) => possibility.PossibilitySectionId == PossibilitySectionId.AttackManeuvers ? new ActionPossibility(Commander.Reposition(qf.Owner), PossibilitySize.Half) : null
                });
            });
        }
        ModManager.RegisterActionOnEachActionPossibility(action =>
        {
            if (!action.Owner.HasFeat(ModData.MFeatNames.DeceptiveTactics) ||
                action.ActionId != ActionId.CreateADiversion) return;
            Creature self = action.Owner;
            action.Description =
                $"Choose any number of enemy creatures you can see.\r\n\r\nMake one Warfare Lore check ({S.SkillBonus(self, ExplorationActivities.ModData.Skills.WarfareLore)}) against the Perception DC of all those creatures. On a success, you become Hidden to those creatures until end of turn{(self.HasEffect(QEffectId.LengthyDiversion) ? " {Blue}(indefinitely on a critical success){/Blue}" : "")} or until you do anything other than Step, Hide or Sneak.\r\n\r\nWhether or not you succeed, creatures you attempt to divert gain a +4 circumstance bonus to their Perception DCs against your attempts to Create a Diversion for the rest of the encounter.";
            action.EffectOnChosenTargets = null;
            action.WithEffectOnChosenTargets((caster, targets) =>
            {
                int roll = R.NextD20();
                bool flag1 = caster.HasEffect(QEffectId.LengthyDiversion);
                foreach (Creature chosenCreature in targets.ChosenCreatures)
                {
                    CheckBreakdown breakdown = CombatActionExecution.BreakdownAttack(new CombatAction(self,
                            null!,
                            "Create a diversion", [
                                Trait.Basic
                            ], "[this condition has no description]", (Target)Target.Self())
                        .WithActionId(ActionId.CreateADiversion)
                        .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Deception, ExplorationActivities.ModData.Skills.WarfareLore),
                            TaggedChecks.DefenseDC(Defense.Perception))), chosenCreature);
                    CheckBreakdownResult breakdownResult = new CheckBreakdownResult(breakdown, roll);
                    string str1 = breakdown.DescribeWithFinalRollTotal(breakdownResult);
                    DefaultInterpolatedStringHandler interpolatedStringHandler;
                    int d20Roll;
                    if (breakdownResult.CheckResult >= CheckResult.Success)
                    {
                        self.DetectionStatus.HiddenTo.Add(chosenCreature);
                        bool flag2 = breakdownResult.CheckResult == CheckResult.CriticalSuccess & flag1;
                        Creature creature = chosenCreature;
                        Color lime = Color.Lime;
                        string str2 = flag2 ? "{b}{Green}Hidden{/}{/b}" : "{Green}Hidden{/}";
                        string? str3 = chosenCreature?.ToString();
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 3);
                        interpolatedStringHandler.AppendLiteral(" (");
                        ref DefaultInterpolatedStringHandler local = ref interpolatedStringHandler;
                        d20Roll = breakdownResult.D20Roll;
                        string str4 = d20Roll.ToString() + breakdown.TotalCheckBonus.WithPlus();
                        local.AppendFormatted(str4);
                        interpolatedStringHandler.AppendLiteral("=");
                        interpolatedStringHandler.AppendFormatted<int>(breakdownResult.D20Roll + breakdown.TotalCheckBonus);
                        interpolatedStringHandler.AppendLiteral(" vs. ");
                        interpolatedStringHandler.AppendFormatted<int>(breakdown.TotalDC);
                        interpolatedStringHandler.AppendLiteral(").");
                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                        string log = $"{str2} from {str3}{stringAndClear}";
                        string logDetails = str1;
                        creature.Overhead("hidden from", lime, log, "Create a diversion", logDetails);
                        self.AddQEffect(new QEffect(flag2 ? "Lengthy diversion" : "Diversion",
                            $"You'll continue to be hidden from {chosenCreature} even in plain sight until you take an action that breaks stealth.",
                            (ExpirationCondition)(flag2 ? 0 : 8), chosenCreature,
                            flag2 ? (Illustration)IllustrationName.CreateADiversion : null)
                        {
                            DoNotShowUpOverhead = true,
                            Id = QEffectId.CreateADiversion,
                            YouBeginAction = (Func<QEffect, CombatAction, Task>)((effect, combatAction) =>
                            {
                                if (combatAction.ActionId == ActionId.Step || combatAction.ActionId == ActionId.Sneak ||
                                    combatAction.ActionId == ActionId.Hide)
                                    return Task.CompletedTask;
                                effect.ExpiresAt = ExpirationCondition.Immediately;
                                return Task.CompletedTask;
                            })
                        });
                    }
                    else
                    {
                        Creature creature = chosenCreature;
                        Color red = Color.Red;
                        string? str5 = chosenCreature?.ToString();
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 3);
                        interpolatedStringHandler.AppendLiteral(" (");
                        ref DefaultInterpolatedStringHandler local = ref interpolatedStringHandler;
                        d20Roll = breakdownResult.D20Roll;
                        string str6 = d20Roll.ToString() + breakdown.TotalCheckBonus.WithPlus();
                        local.AppendFormatted(str6);
                        interpolatedStringHandler.AppendLiteral("=");
                        interpolatedStringHandler.AppendFormatted<int>(breakdownResult.D20Roll + breakdown.TotalCheckBonus);
                        interpolatedStringHandler.AppendLiteral(" vs. ");
                        interpolatedStringHandler.AppendFormatted<int>(breakdown.TotalDC);
                        interpolatedStringHandler.AppendLiteral(").");
                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                        string log = $"{{Red}}Failure{{/}} vs. {str5}{stringAndClear}";
                        string logDetails = str1;
                        creature.Overhead("diversion failed", red, log, "Create a diversion", logDetails);
                    }
                    chosenCreature?.AddQEffect(new QEffect()
                    {
                        BonusToDefenses = ((Func<QEffect, CombatAction, Defense, Bonus?>)((effect, combatAction, defense) =>
                        {
                            if (defense != Defense.Perception || combatAction is not { ActionId: ActionId.CreateADiversion } ||
                                combatAction.Owner != caster)
                                return null;
                            if (caster.HasEffect(QEffectId.ConfabulatorLegendary))
                                return null;
                            if (caster.HasEffect(QEffectId.ConfabulatorMaster))
                                return new Bonus(1, BonusType.Circumstance, "Fool me twice... (Confabulator master)");
                            return caster.HasEffect(QEffectId.ConfabulatorExpert)
                                ? new Bonus(2, BonusType.Circumstance, "Fool me twice... (Confabulator)")
                                : new Bonus(4, BonusType.Circumstance, "Fool me twice...");
                        }))!
                    });
                }
                return Task.CompletedTask;
            });
        });
        ModManager.RegisterActionOnEachActionPossibility(action =>
        {
            if (action.HasTrait(ModData.MTraits.Brandish)) action.Traits.Add(Trait.Visual);
            if (action.Name != "Recall Weakness") return;
            action.Illustration= IllustrationName.NarratorBook;
        });
        LoadOrder.WhenFeatsBecomeLoaded += () =>
        {
            if (ModManager.TryParse("Goading Feint", out FeatName goadingFeat))
            {
                //AllFeats.GetFeatByFeatName(goadingFeat).Prerequisites.Remove(AllFeats.GetFeatByFeatName(goadingFeat).Prerequisites.FirstOrDefault(pre => pre.Description == "You must be trained in Deception.")!);
                AllFeats.GetFeatByFeatName(goadingFeat).WithPrerequisite(
                    values => values.GetProficiency(Trait.Deception) >= Proficiency.Trained ||
                              (values.HasFeat(ModData.MFeatNames.DeceptiveTactics) &&
                               values.GetProficiency(ExplorationActivities.ModData.Traits.WarfareLore) >=
                               Proficiency.Trained), "You must be trained in Deception.");
            }
            if (ModManager.TryParse("DawnniEx", out Trait _))
            {
                FeatRecallWeakness.CombatAssessment.Traits = [];
            }
        };
        ModManager.RegisterActionOnEachActionPossibility(action =>
        {
            if (!action.Name.Contains("Battle Medicine")) return;
            if (!action.Owner.HasFeat(ModData.MFeatNames.ShieldedRecovery)) return;
            Creature self = action.Owner;
            bool medic = self.HasEffect(QEffectId.Medic);
            action.Target = Target.AdjacentFriendOrSelf().WithAdditionalConditionOnTargetCreature(
                (a, d) =>
                {
                    if (!a.HasFreeHand && !a.HeldItems.Any(item => item.HasTrait(Trait.Shield)))
                        return Usability.CommonReasons.NoFreeHandForManeuver;
                    if (d.Damage == 0)
                        return Usability.NotUsableOnThisCreature("healthy");
                    return d.PersistentUsedUpResources.UsedUpActions.Contains("BattleMedicineFrom:" + self.Name) &&
                           (!medic || a.HasEffect(QEffectId.BattleMedicineImmunityBypassUsedThisEncounter) ||
                            self.Proficiencies.Get(Trait.Medicine) < Proficiency.Master &&
                            a.PersistentUsedUpResources.UsedUpActions.Contains("BattleMedicineImmunityBypassUsed"))
                        ? Usability.NotUsableOnThisCreature("immune")
                        : Usability.Usable;
                });
            action.Description = action.Description.Insert(action.Description.IndexOf('.'), " or be wielding a shield");
        }); 
        int abilitiesIndex = CreatureStatblock.CreatureStatblockSectionGenerators.FindIndex(gen => gen.Name == "Abilities");
        CreatureStatblock.CreatureStatblockSectionGenerators.Insert(abilitiesIndex,
            new CreatureStatblockSectionGenerator("Prepared tactics", TacticsStatBlock.DescribePreparedTactics));
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            Feat? commanderClass = AllFeats.All.FirstOrDefault(ft => ft.FeatName == ModData.MFeatNames.Commander);
            commanderClass!.RulesText = commanderClass.RulesText.Replace("Ability boosts", "Attribute boosts");
            Feat? battlePlanner = AllFeats.All.FirstOrDefault(feat => feat.FeatName == ExplorationActivities.ModData.FeatNames.BattlePlanner);
            if (battlePlanner != null)
                battlePlanner.RulesText +=
                    $"\n\n{{b}}Special{{/b}} If you are a Commander with the level 3 {Commander.UseCreatedTooltip("warfare expertise")} class feature, you instead gain this effect: If you or one of your allies has taken the scout exploration activity, you reroll your initiative and take the higher value.";
        };
    }
    public static AdvancedRequest NewSleepRequest(int sleepTime)
    {
        Type? sleepRequest = typeof(AdvancedRequest).Assembly.GetType("Dawnsbury.Core.Coroutines.Requests.SleepRequest");
        var constructor = sleepRequest?.GetConstructor([typeof(int)]);
        var sleep = constructor?.Invoke([sleepTime]);
        sleep?.GetType().GetProperty("CanBeClickedThrough")?.SetMethod?.Invoke(sleep, [false]);
        return (AdvancedRequest)sleep!;
    }
}