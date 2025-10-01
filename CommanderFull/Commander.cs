using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Encounters.Tutorial;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DawnniExpanded;
using Dawnsbury.ThirdParty.SteamApi;
using static CommanderFull.ModData;
using Color = Microsoft.Xna.Framework.Color;

namespace CommanderFull;

public abstract partial class Commander
{
    public static readonly Skill WarfareLore = ExplorationActivities.ModData.Skills.WarfareLore;
    public static readonly Trait WarfareLoreTrait = ExplorationActivities.ModData.Traits.WarfareLore;
    public static Dictionary<string, FeatName> TacticsDict { get; } = [];
    public static Dictionary<string, FeatName> PrereqsDict { get; } = [];
    public static Dictionary<FeatName, FeatName> PrereqsToTactics { get; } = [];
    public static IEnumerable<Feat> LoadAll() 
    { 
        foreach (Feat tactic in LoadTactics())
        {
            int level = tactic.Traits.Contains(MTraits.LegendaryTactic) ? 19 : tactic.Traits.Contains(MTraits.MasterTactic) ? 15 : tactic.Traits.Contains(MTraits.ExpertTactic) ? 7 : 1;
            Feat prereq = CreatePrereqTacticsBasic((ActionFeat)tactic, level);
            prereq.WithIllustration(tactic.Illustration);
            if (prereq.Traits.Any(trait => trait == MTraits.ExpertTactic)) 
                prereq.WithPrerequisite(values => values.HasFeat(MFeatNames.Commander) || values.HasFeat(MFeatNames.TacticalExcellence8), "You must be a Commander or have the level 8 Tactical Excellence feat to select this tactic.");
            if (prereq.Traits.Any(trait => trait == MTraits.MasterTactic || trait == MTraits.LegendaryTactic))
                prereq.WithPrerequisite(values => values.HasFeat(MFeatNames.Commander), "You must be a Commander to select this tactic.");
            tactic.WithPrerequisite(values => values.HasFeat(prereq),
                "You must have this tactic in your folio to select it.");
            tactic.WithOnCreature(cr =>
            {
                QEffect? first = null;
                foreach (QEffect effect in TacticsQFs(cr))
                {
                    if (effect.Tag == null || (FeatName)effect.Tag != tactic.FeatName) continue;
                    first = effect;
                    break;
                }
                if (first == null) return;
                QEffect tacticQf = first;
                cr.AddQEffect(tacticQf);
            });
            AddTags(tactic);
            tactic.FeatGroup = GetFeatGroupFromTraits(tactic);
            prereq.FeatGroup = GetFeatGroupFromTraits(prereq);
            TacticsDict.Add(tactic.Name, tactic.FeatName);
            PrereqsDict.Add(prereq.Name, prereq.FeatName);
            PrereqsToTactics.Add(prereq.FeatName, tactic.FeatName);
            
            yield return tactic;
            yield return prereq;
        }
        foreach (Feat feat in LoadFeats())
        {
            yield return feat;
        }

        Feat commander = new ClassSelectionFeat(MFeatNames.Commander,
                "You approach battle with the knowledge that tactics and strategy are every bit as crucial as brute strength or numbers. You may have trained in classical theories of warfare and strategy at a military school or you might have refined your techniques through hard-won experience as part of an army or mercenary company. Regardless of how you came by your knowledge, you have a gift for signaling your allies from across the battlefield and shouting commands to rout even the most desperate conflicts, allowing your squad to exceed their limits and claim victory.",
                MTraits.Commander, new EnforcedAbilityBoost(Ability.Intelligence), 8,
                [
                    Trait.Fortitude, Trait.Armor, Trait.UnarmoredDefense, Trait.Society, Trait.Simple, Trait.Martial,
                    Trait.Unarmed
                ],
                [Trait.Reflex, Trait.Will, Trait.Perception],
                2,
                $"{{b}}1. Commander's Banner{{/b}} A commander needs a battle standard so their allies can locate them on the field. You start play with a custom {CreateTooltips("banner", "Your banner is a special item you carry, either in your inventory or in hand. As long as your banner is visible and in your possession, it provides an aura that gives you and all allies in a 30-foot emanation a +1 status bonus to Will saves and DCs against fear effects. This effect is paused or resumed as part of any action you would typically use to stow or retrieve your banner.")} that you can use to signal allies when using tactics or to deploy specific abilities.\n\n" +
                $"{{b}}2. Tactics{{/b}} By studying and practicing the strategic arts of war, you can guide your allies to victory. You begin play with a folio containing five {CreateTooltips("tactics", "{i}Preparing and Changing Tactics{/i}\nYou may prepare three tactics from your folio as a precombat preparation. At the start of an encounter, you can instruct a total number of party members equal to 2 + your Intelligence modifier, enabling these allies to respond to your tactics in combat. These allies are your squadmates. A squadmate always has the option not to respond to your tactical signal if they do not wish to. You count as one of your squadmates for the purposes of participating in or benefiting from a tactic (though you do not count against your own maximum number of squadmates).\n\n{i}Gaining New Tactics{/i}\nYou add additional tactics to your folio and increase the number of tactics you can prepare when you gain the expert tactician, master tactician, and legendary tactician class features. You can also add tactics to your folio with the Tactical Expansion feat, though this does not change the number you can have prepared.")} though you may only prepare three as a precombat preparation to begin with. These are combat techniques and coordinated maneuvers you can instruct your allies in, enabling them to respond to your signals in combat. As you increase in level, you gain the ability to learn more potent tactics.\n\n" +
                "{b}3. Drilled Reactions{/b} Your time spent training with your allies allows them to respond quickly and instinctively to your commands. Once per round when you use a tactic, you can grant one ally of your choice benefiting from that tactic an extra reaction. This reaction has to be used for that tactic and is lost if not used.\n\n" +
                "{b}4. Shield Block {icon:Reaction}.{/b} You gain the Shield Block general feat.\n\n" +
                "{b}5. Commander feat.{/b}",
                null)
            .WithClassFeatures(features =>
            {
                features.AddFeature(3, CreateTooltips("warfare expertise",
                    "Your knowledge of war and strategy grows and guides your decisions in battle. You gain expert proficiency in Warfare Lore. As long as you are observing at least one opponent when initiative is rolled, you can use Warfare Lore for your initiative roll. If you have DawnniEx installed, you use Warfare Lore for all {b}Recall Weakness{/b} actions if your Warfare Lore is better than the original skill check."));
                features.AddFeature(5, CreateTooltips("military expertise",
                    "You’ve studied in a wide variety of weapons and learned to apply their principles in combat. Your proficiency rank for martial weapons, simple weapons, and unarmed attacks increases to expert. When you critically succeed at an attack roll with a weapon you are at least an expert with, you apply the weapon’s critical specialization effect."));
                features.AddFeature(7, CreateTooltips("expert tactician",
                    "Your time spent leading and training others on battlefield tactics has improved your combat acumen. Your proficiency rank for your commander class DC increases to expert, and you add two new tactics to your folio; these can be any mobility or offensive tactics you don’t already know, or you can choose from expert tactics you have access to. The total number of tactics you can have prepared increases to four. In addition, your proficiency rank in Warfare Lore increases to master."));
                features.AddFeature(7,
                    CreateTooltips("weapon specialization",
                        "You’ve learned how to inflict greater injuries with the weapons you know best. You deal 2 additional damage with weapons and unarmed attacks in which you’re an expert. This damage increases to 3 if you’re a master, and 4 if you’re legendary."));
                features.AddFeature(9, WellKnownClassFeature.ExpertInFortitude);
                features.AddFeature(11, CreateTooltips("armor expertise",
                    "You have spent so much time in armor that you know how to make the most of its protection. Your proficiency ranks for light, medium, and heavy armor, as well as for unarmored defense, increase to expert. You gain the armor specialization effects of medium and heavy armor."));
                features.AddFeature(11,
                    CreateTooltips("commanding will",
                        "You know that if you break, so too will those who follow you, and so you have cultivated a will that bends to no outside force. Your proficiency rank for Will saves increases to master. When you roll a success on a Will save, you get a critical success instead."));
                features.AddFeature(13, WellKnownClassFeature.MasterInPerception);
                features.AddFeature(13, CreateTooltips("weapon mastery",
                    "You’ve drilled extensively in your weapons. Your proficiency ranks for unarmed attacks, simple weapons, and martial weapons increase to master."));
                features.AddFeature(15, CreateTooltips("battlefield intuition",
                    "Your experience across a wide array of battlefields gives you a preternatural ability to predict and avoid damaging effects. Your proficiency rank for Reflex saves increases to master. When you roll a success on a Reflex save, you get a critical success instead."));
                features.AddFeature(15, CreateTooltips("master tactician",
                    "You are among the greatest tacticians to have ever led forces on the field of battle. Your proficiency rank for commander class DC increases to master, and you add two new tactics to your folio; these can be any mobility or offensive tactics you don’t already have in your folio, or you can choose from expert tactics or master tactics you have access to. The total number of tactics you can have prepared increases to five. In addition, you gain legendary proficiency in Warfare Lore."));
                features.AddFeature(17, CreateTooltips("armor mastery",
                    "Your skill with armor improves, helping you avoid more blows. Your proficiency ranks for light, medium, and heavy armor, as well as for unarmored defense, increase to master."));
                features.AddFeature(19, CreateTooltips("legendary tactician",
                    "You are an unrivaled legend in your use of battlefield tactics. Your proficiency rank for your commander class DC increases to legendary, and you add two new tactics to your folio; these can be any mobility or offensive tactics you don’t already have in your folio, or you can choose from expert tactics, master tactics, or legendary tactics you have access to. The total number of tactics you can have prepared increases to six."));
            })
            .WithOnSheet(values =>
            {
                int[] levels = values.Sheet.InventoriesByLevel.Keys.ToArray();
                Inventory campaignInventory = values.Sheet.CampaignInventory;
                AddBannerToInventory(campaignInventory, Items.CreateNew(BannerItem.Banner));
                foreach (int level in levels)
                {
                    Inventory inventory = values.Sheet.InventoriesByLevel[level];
                    if (level == 1 || inventory.LeftHand != null || inventory.RightHand != null ||
                        inventory.Backpack.Count != 0)
                    {
                        AddBannerToInventory(inventory, Items.CreateNew(BannerItem.Banner));
                    }
                }
                int prepTactics = values.Sheet.MaximumLevel >= 19 ? 6 :
                    values.Sheet.MaximumLevel >= 15 ? 5 :
                    values.Sheet.MaximumLevel >= 7 ? 4 : 3;
                values.Tags.Add("PreparedTactics", prepTactics);
                values.AddSelectionOption(new MultipleFeatSelectionOption("DefaultTargetMS", "Drilled Reactions Default Target", SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, feat => feat.Tag is "DefaultTarget", 1).WithIsOptional());
                values.AddSelectionOption(new MultipleFeatSelectionOption("CommanderTactics", "Prepared Tactics",
                    SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, feat => feat.HasTrait(MTraits.Tactic), prepTactics
                    ));
                values.SetProficiency(MTraits.Commander, Proficiency.Trained);
                values.GrantFeat(FeatName.ShieldBlock);
                if (!values.HasFeat(ExplorationActivities.ModData.FeatNames.AdditionalLoreWF))
                {
                    values.TrainInThisOrSubstitute(WarfareLore);
                }
                else if (ModManager.TryParse("Fount of Knowledge", out FeatName fountOfKnowledge))
                {
                    values.GrantFeat(fountOfKnowledge);
                }
                values.AddSelectionOption(new MultipleFeatSelectionOption("CommanderFolio", "Commander Folio", 1,
                    feat => feat.HasTrait(MTraits.BasicTacticPre), 5));
                values.AddSelectionOption(new SingleFeatSelectionOption("CommanderFeat1", "Commander feat", 1,
                    feat => feat.HasTrait(MTraits.Commander)));
                values.AddAtLevel(3, v3 => v3.GrantFeat(ExplorationActivities.ModData.FeatNames.WarfareLoreExpert));
                values.IncreaseProficiency(5, Trait.Martial, Proficiency.Expert);
                values.IncreaseProficiency(5, Trait.Simple, Proficiency.Expert);
                values.IncreaseProficiency(5, Trait.Unarmed, Proficiency.Expert);
                values.IncreaseProficiency(7, MTraits.Commander, Proficiency.Expert);
                values.AddAtLevel(7, v7 => v7.GrantFeat(ExplorationActivities.ModData.FeatNames.WarfareLoreMaster));
                values.AddAtLevel(7,
                    v7 => v7.AddSelectionOption(new MultipleFeatSelectionOption("ExpertTactics", "Expert Tactics", 7,
                        feat => feat.HasTrait(MTraits.ExpertTacticPre) || feat.HasTrait(MTraits.BasicTacticPre), 2)));
                values.IncreaseProficiency(9, Trait.Fortitude, Proficiency.Expert);
                values.IncreaseProficiency(11, Trait.Will, Proficiency.Master);
                values.IncreaseProficiency(11, Trait.UnarmoredDefense, Proficiency.Expert);
                values.IncreaseProficiency(11, Trait.LightArmor, Proficiency.Expert);
                values.IncreaseProficiency(11, Trait.MediumArmor, Proficiency.Expert);
                values.IncreaseProficiency(11, Trait.HeavyArmor, Proficiency.Expert);
                values.IncreaseProficiency(13, Trait.Perception, Proficiency.Master);
                values.IncreaseProficiency(13, Trait.Martial, Proficiency.Master);
                values.IncreaseProficiency(13, Trait.Simple, Proficiency.Master);
                values.IncreaseProficiency(13, Trait.Unarmed, Proficiency.Master);
                values.AddAtLevel(15, v15 => v15.GrantFeat(ExplorationActivities.ModData.FeatNames.WarfareLoreLegendary));
                values.IncreaseProficiency(15, Trait.Reflex, Proficiency.Master);
                values.IncreaseProficiency(15, MTraits.Commander, Proficiency.Master);
                values.AddAtLevel(15,
                    v15 => v15.AddSelectionOption(new MultipleFeatSelectionOption("MasterTactics", "Master Tactics", 15,
                        feat => feat.HasTrait(MTraits.ExpertTacticPre) || feat.HasTrait(MTraits.MasterTacticPre) ||
                                feat.HasTrait(MTraits.BasicTacticPre), 2)));
                values.IncreaseProficiency(17, Trait.UnarmoredDefense, Proficiency.Master);
                values.IncreaseProficiency(17, Trait.LightArmor, Proficiency.Master);
                values.IncreaseProficiency(17, Trait.MediumArmor, Proficiency.Master);
                values.IncreaseProficiency(17, Trait.HeavyArmor, Proficiency.Master);
                values.IncreaseProficiency(19, MTraits.Commander, Proficiency.Legendary);
                values.AddAtLevel(19,
                    v19 => v19.AddSelectionOption(new MultipleFeatSelectionOption("LegendaryTactics",
                        "Legendary Tactics", 19,
                        feat => feat.HasTrait(MTraits.ExpertTacticPre) || feat.HasTrait(MTraits.MasterTacticPre) ||
                                feat.HasTrait(MTraits.LegendaryTacticPre) || feat.HasTrait(MTraits.BasicTacticPre), 2)));
            })
            .WithOnCreature(cr =>
            {
                cr.AddQEffect(new QEffect("Drilled Reactions", "Once per round when you use a tactic, you can grant one ally of your choice benefiting from that tactic an extra reaction. This reaction has to be used for that tactic and is lost if not used.")
                {
                    ProvideMainAction = _ =>
                    {
                        SubmenuPossibility commanderMenu = new(MIllustrations.Toggle, "Tactics")
                        {
                            SubmenuId = MSubmenuIds.Commander
                        };
                        return commanderMenu;
                    }, Innate = true
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideActionIntoPossibilitySection = (_, section) =>
                    {
                        SubmenuPossibility signalToggle = new(new SideBySideIllustration(MIllustrations.Visual, MIllustrations.Auditory), "Signal Toggle")
                        {
                            SubmenuId = MSubmenuIds.SignalToggle
                        };
                        return section.PossibilitySectionId == MPossibilitySectionIds.Toggle ?  signalToggle : null;
                    },
                    StartOfCombat = _ =>
                    {
                        cr.AddQEffect(new QEffect("Audible Tactics", "Your tactics gain the Auditory trait.") { Id = MQEffectIds.AudibleTactics, DoNotShowUpOverhead = true});
                        return Task.CompletedTask;
                    }
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideSectionIntoSubmenu = (_, possibility) => possibility.SubmenuId == MSubmenuIds.SignalToggle
                        ? new PossibilitySection("Visual").WithPossibilitySectionId(MPossibilitySectionIds
                            .VisualTactics)
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideActionIntoPossibilitySection = (_, section) => section.PossibilitySectionId == MPossibilitySectionIds.VisualTactics
                        ? new ActionPossibility(VisualTactics(cr))
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideSectionIntoSubmenu = (_, possibility) => possibility.SubmenuId == MSubmenuIds.SignalToggle
                        ? new PossibilitySection("Audible").WithPossibilitySectionId(MPossibilitySectionIds
                            .AuditoryTactics)
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideActionIntoPossibilitySection = (_, section) => section.PossibilitySectionId == MPossibilitySectionIds.AuditoryTactics
                        ? new ActionPossibility(AudibleTactics(cr))
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideSectionIntoSubmenu = (_, possibility) => possibility.SubmenuId == MSubmenuIds.Commander
                        ? new PossibilitySection("Mobility").WithPossibilitySectionId(MPossibilitySectionIds
                            .MobilityTactics)
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideSectionIntoSubmenu = (_, possibility) => possibility.SubmenuId == MSubmenuIds.Commander
                        ? new PossibilitySection("Offensive").WithPossibilitySectionId(MPossibilitySectionIds
                            .OffensiveTactics)
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideSectionIntoSubmenu = (_, possibility) => possibility.SubmenuId == MSubmenuIds.Commander
                        ? new PossibilitySection("Expert").WithPossibilitySectionId(MPossibilitySectionIds
                            .ExpertTactics)
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideSectionIntoSubmenu = (_, possibility) => possibility.SubmenuId == MSubmenuIds.Commander
                        ? new PossibilitySection("Toggle").WithPossibilitySectionId(MPossibilitySectionIds
                            .Toggle)
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    ProvideActionIntoPossibilitySection = (_, section) => section.PossibilitySectionId == MPossibilitySectionIds.Toggle
                        ? new ActionPossibility(ChooseDrilledReactions(cr))
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    Id = MQEffectIds.BannerRadius,
                    Value = 6
                });
                cr.AddQEffect(new QEffect()
                {
                    StartOfCombat = _ =>
                    {
                        if (cr.PersistentCharacterSheet!.Calculated.Tags.Any(pair => pair.Value is List<Trait>))
                            return Task.CompletedTask;
                        cr.Battle.Log($"{cr.Name} has no prepared tactics! You should prepare some in the Precombat Preparations section.");
                        return Task.CompletedTask;
                    }
                });
                if (cr.Level >= 3)
                {
                    cr.AddQEffect(new QEffect("Warfare Expertise",
                        "As long as at least one enemy is visible at the start of an encounter, you can roll Warfare Lore in place of Perception for initiative. " +
                        "{i}Special{/i} If you have DawnniEx installed, you use Warfare Lore for all " +
                        CreateTooltips("Recall Weakness", "{b}Recall Weakness {icon:Action}{/b}"+"\n{i}Skill{/i}\n\n"+FeatRecallWeakness.RecallWeaknessAction(cr).Description) +
                        " actions if your Warfare Lore is better than the original skill check.")
                    {
                        StartOfCombat = _ =>
                        {
                            if (cr.Battle.AllCreatures.Any(enemy => enemy.EnemyOf(cr) && cr.CanSee(enemy)))
                                cr.AddQEffect(new QEffect
                                    { OfferAlternateSkillForInitiative = _ => WarfareLore });
                            return Task.CompletedTask;
                        },
                        YouBeginAction = (_, action) =>
                        {
                            if (action is
                                {
                                    Name: "Recall Weakness",
                                    ActiveRollSpecification.TaggedDetermineBonus.InvolvedSkill: not null
                                })
                            {
                                Skill original = action.ActiveRollSpecification.TaggedDetermineBonus.InvolvedSkill
                                    .Value;
                                action.WithActiveRollSpecification(new ActiveRollSpecification(
                                    TaggedChecks.SkillCheck(WarfareLore, original),
                                    action.ActiveRollSpecification.TaggedDetermineDC));
                            }

                            return Task.CompletedTask;
                        }
                    });
                }

                if (cr.Level >= 5)
                    cr.AddQEffect(new QEffect("Military Expertise",
                        "When you critically succeed at an attack roll with a weapon you are at least an expert with, you apply the weapon's {tooltip:criteffect}critical specialization effect{/tooltip}.")
                    {
                        YouHaveCriticalSpecialization = (_, item, _, self) =>
                            self.Proficiencies.Get(item.Traits) >= Proficiency.Expert
                    });
                if (cr.Level >= 7)
                    cr.AddQEffect(QEffect.WeaponSpecialization(cr.Level >= 15));
                if (cr.Level >= 11)
                {
                    CommonCharacterFeatures.AddEvasion(cr, "Commanding Will", Defense.Will);
                    CommonItemAbilities.ApplyArmorSpecializationEffects(cr);
                }

                if (cr.Level >= 15)
                    CommonCharacterFeatures.AddEvasion(cr, "Battlefield Intuition", Defense.Reflex);
            })
            .WithPermanentQEffect(null, qf =>
            {
                bool applied = false;
                qf.StartOfCombat = scQf =>
                {
                    Creature self = scQf.Owner;
                    if (self.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) || self.CarriedItems.Any(item => item.HasTrait(MTraits.Banner) && !item.HasTrait(Trait.Barding) && !item.HasTrait(Trait.Worn)))
                    {
                        self.AddQEffect(new QEffect
                        {
                            StateCheck = _ =>
                            {
                                if (self.FindQEffect(MQEffectIds.Banner) is { } banner &&
                                    !self.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) && !self.CarriedItems.Any(item => item.HasTrait(MTraits.Banner)))
                                    banner.ExpiresAt = ExpirationCondition.Immediately;
                                if (self.HasEffect(MQEffectIds.Banner) ||
                                    (!self.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) &&
                                     !self.CarriedItems.Any(item => item.HasTrait(MTraits.Banner))))
                                    return;
                                AuraAnimation animation =
                                    self.AnimationData.AddAuraAnimation(
                                        IllustrationName.BlessCircle, GetBannerRadius(self));
                                QEffect commandersBannerEffect =
                                    CommandersBannerEffect(animation, GetBannerRadius(self), self);
                                animation.Color = Color.Coral;
                                self.AddQEffect(commandersBannerEffect);
                            }
                        });
                    }
                    else if (self.CarriedItems.Any(item =>
                                 item.HasTrait(MTraits.Banner) && item.IsWorn && item.HasTrait(Trait.Shield)))
                    {
                        self.AddQEffect(new QEffect
                            {
                                StateCheck = _ =>
                                {
                                    if (self.HeldItems.FirstOrDefault(item =>
                                            item.HasTrait(Trait.Worn) && item.HasTrait(Trait.Shield)) is
                                        { } buckler)
                                    {
                                        self.HeldItems.Remove(buckler);
                                        self.CarriedItems.Add(buckler);
                                    }

                                    if (self.FindQEffect(MQEffectIds.Banner) is { } banner &&
                                        !(self.CarriedItems.Any(item =>
                                              item.HasTrait(MTraits.Banner) && item.IsWorn &&
                                              item.HasTrait(Trait.Shield)) &&
                                          self.HasFreeHand))
                                        banner.ExpiresAt = ExpirationCondition.Immediately;
                                    if (self.HasEffect(MQEffectIds.Banner) ||
                                        !(self.CarriedItems.Any(item =>
                                              item.HasTrait(MTraits.Banner) && item.IsWorn &&
                                              item.HasTrait(Trait.Shield)) &&
                                          self.HasFreeHand)) return;
                                    AuraAnimation animation =
                                        self.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle,
                                            GetBannerRadius(self));
                                    QEffect commandersBannerEffect =
                                        CommandersBannerEffect(animation, GetBannerRadius(self), self);
                                    animation.Color = Color.Coral;
                                    self.AddQEffect(commandersBannerEffect);
                                }
                            });
                    }
                    else if (self.HasFeat(MFeatNames.CommandersCompanion) &&
                             !self.PersistentUsedUpResources.AnimalCompanionIsDead && self.CarriedItems.Any(item =>
                                 item.HasTrait(MTraits.Banner) && item.HasTrait(Trait.Barding)))
                    {
                        self.AddQEffect(new QEffect
                        {
                            StateCheckWithVisibleChanges = effect =>
                            {
                                if (self.Battle.AllCreatures.Find(cr => IsMyAnimalCompanion(self, cr)) is
                                    not { } companion) return Task.CompletedTask;
                                QEffect? bannerVal = self.FindQEffect(MQEffectIds.BannerRadius);
                                if (bannerVal != null && self.HasFeat(MFeatNames.BattleTestedCompanion))
                                {
                                    bannerVal.Value += 2;
                                }
                                AuraAnimation auraAnimation =
                                    companion.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle,
                                        GetBannerRadius(self));
                                auraAnimation.Color = Color.Coral;
                                companion.AddQEffect(CommandersBannerEffect(auraAnimation, GetBannerRadius(self),
                                    self));
                                companion.AddQEffect(new QEffect
                                {
                                    WhenCreatureDiesAtStateCheckAsync = qEffect =>
                                    {
                                        if (bannerVal != null && self.HasFeat(MFeatNames.BattleTestedCompanion))
                                        {
                                            bannerVal.Value -= 2;
                                        }
                                        Item basicBanner = Items.CreateNew(ItemName.Club);
                                        basicBanner.Traits.Add(MTraits.Banner);
                                        basicBanner.Traits.Add(Trait.EncounterEphemeral);
                                        basicBanner.Name = "Simple Banner";
                                        basicBanner.Illustration = MIllustrations.SimpleBanner;
                                        qEffect.Owner.Space.CenterTile.DropItem(basicBanner);
                                        self.AddQEffect(new QEffect
                                        {
                                            StateCheck = _ =>
                                            {
                                                if (self.FindQEffect(MQEffectIds.Banner) is { } banner &&
                                                    !self.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) && !self.CarriedItems.Any(item => item.HasTrait(MTraits.Banner)))
                                                    banner.ExpiresAt = ExpirationCondition.Immediately;
                                                if (self.HasEffect(MQEffectIds.Banner) ||
                                                    (!self.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) &&
                                                     !self.CarriedItems.Any(item => item.HasTrait(MTraits.Banner))))
                                                    return;
                                                AuraAnimation animation =
                                                    self.AnimationData.AddAuraAnimation(
                                                        IllustrationName.BlessCircle, GetBannerRadius(self));
                                                QEffect commandersBannerEffect =
                                                    CommandersBannerEffect(animation, GetBannerRadius(self), self);
                                                animation.Color = Color.Coral;
                                                self.AddQEffect(commandersBannerEffect);
                                            }
                                        });
                                        return Task.CompletedTask;
                                    }
                                });
                                effect.ExpiresAt = ExpirationCondition.Immediately;
                                return Task.CompletedTask;
                            }
                        });
                    }
                    else
                    {
                        Item basicBanner = Items.CreateNew(ItemName.Club);
                        basicBanner.Traits.Add(MTraits.Banner);
                        basicBanner.Traits.Add(Trait.EncounterEphemeral);
                        basicBanner.Name = "Simple Banner";
                        basicBanner.Illustration = MIllustrations.SimpleBanner;
                        if (!self.CarriedItems.Any(item =>
                                item.HasTrait(MTraits.Banner) && item.HasTrait(Trait.Weapon)) &&
                            !self.Space.CenterTile.DroppedItems.Any(item => item.HasTrait(MTraits.Banner)))
                            self.CarriedItems.Add(basicBanner);
                        self.AddQEffect(new QEffect
                        {
                            StateCheck = _ =>
                            {
                                if (self.FindQEffect(MQEffectIds.Banner) is { } banner &&
                                    !self.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) && !self.CarriedItems.Any(item => item.HasTrait(MTraits.Banner)))
                                    banner.ExpiresAt = ExpirationCondition.Immediately;
                                if (self.HasEffect(MQEffectIds.Banner) ||
                                    (!self.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) &&
                                     !self.CarriedItems.Any(item => item.HasTrait(MTraits.Banner))))
                                    return;
                                AuraAnimation animation =
                                    self.AnimationData.AddAuraAnimation(
                                        IllustrationName.BlessCircle, GetBannerRadius(self));
                                QEffect commandersBannerEffect =
                                    CommandersBannerEffect(animation, GetBannerRadius(self), self);
                                animation.Color = Color.Coral;
                                self.AddQEffect(commandersBannerEffect);
                            }
                        });
                    }
                    applied = false;
                    return Task.CompletedTask;
                };
                qf.StartOfYourPrimaryTurn = async (_, self)=>
                {
                    if (applied) return;
                    self.AddQEffect(SquadmateQf(self));
                    if (self.HasFeat(MFeatNames.CommandersCompanion) &&
                        !self.PersistentUsedUpResources.AnimalCompanionIsDead)
                    {
                        if (self.Battle.AllCreatures.Find(cr => IsMyAnimalCompanion(self, cr)) is
                            { } companion)
                        {
                            companion.AddQEffect(SquadmateQf(companion));
                        }
                    }
                    List<Creature> potentialSquadmates = self.Battle.AllCreatures.Where(cr =>
                        cr.FriendOf(self) && (cr.PersistentCharacterSheet != null || cr.HasEffect(QEffectId.RangersCompanion) || cr.Traits.Any(t => t.HumanizeTitleCase2() == "Eidolon")) && !cr.QEffects.Any(effect => effect.Id == MQEffectIds.Squadmate && effect.Source == self)).ToList();
                    if (potentialSquadmates.Count <= self.Abilities.Intelligence + 2)
                    {
                        foreach (Creature creature in potentialSquadmates)
                            creature.AddQEffect(SquadmateQf(self));
                    }
                    else
                    {
                        CombatAction chooseSquadmate = CombatAction.CreateSimple(self, "Choose Squadmate",
                                Trait.DoesNotBreakStealth, Trait.DoNotShowInCombatLog,
                                Trait.DoNotShowOverheadOfActionName)
                            .WithActionCost(0).WithEffectOnEachTarget((_, _, target, _) =>
                            {
                                target.AddQEffect(SquadmateQf(self));
                                return Task.CompletedTask;
                            });
                        chooseSquadmate.Target = SquadmateTarget(self);
                        await self.Battle.GameLoop.FullCast(chooseSquadmate);
                    }
                };
            });
        string fulltext = (commander as ClassSelectionFeat)!.RulesText;
        if (fulltext.Contains(" and a number") && fulltext.IndexOf(" and a number", StringComparison.Ordinal) <
            (commander as ClassSelectionFeat)!.RulesText.Length)
        {
            string insert =
                (commander as ClassSelectionFeat)!.RulesText.Insert(
                    fulltext.IndexOf(" and a number", StringComparison.Ordinal), ", Warfare Lore");
            (commander as ClassSelectionFeat)!.RulesText = insert;
        }
        commander.RulesText = commander.RulesText.Replace("Key ability", "Key attribute");
        yield return commander;
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            Feat defaultTarget = new (ModManager.RegisterFeatName("DefaultTarget" + (i + 1),
                "Player Character " + (i + 1)), null, "", [], null);
            CreateDefaultTargetLogic(defaultTarget, index);
            yield return defaultTarget;
        }
        foreach (Feat feat in CommanderArchetype.LoadArchetypeFeats())
            yield return feat;
    }
    public static TrueFeat CreatePrereqTacticsBasic(ActionFeat feat, int level)
    {
        List<Trait> traits = [];
        traits.AddRange(feat.Traits);
        traits.Remove(MTraits.Tactic);
        if (traits.Contains(MTraits.ExpertTactic)) traits.Add(MTraits.ExpertTacticPre);
        else if (traits.Contains(MTraits.MasterTactic)) traits.Add(MTraits.MasterTacticPre);
        else if (traits.Contains(MTraits.LegendaryTactic)) traits.Add(MTraits.LegendaryTacticPre);
        else traits.Add(MTraits.BasicTacticPre);
        traits.Add(MTraits.TacticPre);
        return new TrueFeat(ModManager.RegisterFeatName(feat.Name + "PrereqTactics", feat.Name), level, feat.FlavorText,
            feat.RulesText, traits.ToArray());
    }
    private static void CreateDefaultTargetLogic(Feat defTarg, int index)
    {
        defTarg.WithNameCreator(_ =>
                GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL")
            .WithRulesTextCreator(_ =>
                $"{GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"} will be the default target for your Drilled Reactions feature.")
            .WithIllustrationCreator(_ =>
                GetCharacterSheetFromPartyMember(index)?.Illustration ?? IllustrationName.MagicHide)
            .WithTag("DefaultTarget")
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    qfFeat.StartOfCombat = qfThis =>
                    {
                        if (GetCharacterSheetFromPartyMember(index) is not { } hero
                            || qfThis.Owner.Battle.AllCreatures.FirstOrDefault(cr2 =>
                                    cr2.PersistentCharacterSheet == hero) is not
                                { } chosenCreature) return Task.CompletedTask;
                        QEffect defaultTarget = new("Default Target",
                                "You are the default target for drilled reactions.",
                                ExpirationCondition.Never,
                                qfFeat.Owner, MIllustrations.Toggle)
                            { Id = MQEffectIds.DrilledTarget, DoNotShowUpOverhead = true};
                        chosenCreature.AddQEffect(defaultTarget);
                        return Task.CompletedTask;
                    };
                });
    }

    private static readonly string RegisterReposition = CreateTooltips("Reposition", "{b}Reposition {icon:Action}{/b}" +
        "\n{i}Attack{/i}" +
        "\n\n{b}Requirements{/b} You either have at least one hand free, or you're grabbing or restraining the target." +
        "\n\nAttempt an Athletics check against an adjacent target's Fortitude DC." + S.FourDegreesOfSuccess(
            "You move the creature up to 10 feet. It must remain within your reach during this movement, and you can't move it into or through obstacles.",
            "You move the target up to 5 feet. It must remain within your reach during this movement, and you can't move it into or through obstacles.",
            null, "The target can move you up to 5 feet as though it successfully Repositioned you."));

    private static void AddTags(Feat feat)
    {
        feat.WithOnSheet(values => values.Tags.Add(feat.Name, feat.Traits));
    }

    private static FeatGroup GetFeatGroupFromTraits(Feat feat)
    {
        if (feat.Traits.Contains(MTraits.OffensiveTactic))
            return MFeatGroups.OffensiveTactics;
        if (feat.Traits.Contains(MTraits.MobilityTactic))
            return MFeatGroups.MobilityTactics;
        if (feat.Traits.Contains(MTraits.ExpertTactic))
            return MFeatGroups.ExpertTactics;
        return feat.Traits.Contains(MTraits.MasterTactic) ? MFeatGroups.MasterTactics : MFeatGroups.LegendaryTactics;
    }

    private static CombatAction ChooseDrilledReactions(Creature owner)
    {
        CombatAction choiceAction = new CombatAction(owner, MIllustrations.Toggle,
                "Default Drilled Reaction Target",
                [
                    Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName, Trait.Basic,
                    Trait.DoesNotBreakStealth
                ], "Use this toggle to determine who will be the default target for drilled reactions.",
                Target.Self())
            .WithActionCost(0)
            .WithEffectOnChosenTargets(async (self, _) =>
            {
                List<Creature> creatures = self.Battle.AllCreatures.Where(cr =>
                    cr.FindQEffect(MQEffectIds.Squadmate)?.Source == self).ToList();
                List<Option> options = [];
                foreach (Creature creature in creatures)
                {
                    if (creature.HasEffect(MQEffectIds.DrilledTarget))
                        creature.RemoveAllQEffects(qf => qf.Id == MQEffectIds.DrilledTarget);
                    options.Add(Option.ChooseCreature("drilled reactions target", creature,
                        async () =>
                        {
                            await creature.Battle.GameLoop.FullCast(CombatAction
                                .CreateSimple(creature, "choose self", Trait.DoNotShowInCombatLog,
                                    Trait.DoNotShowOverheadOfActionName, Trait.DoesNotBreakStealth).WithActionCost(0)
                                .WithEffectOnSelf(cr1 =>
                                    cr1.AddQEffect(new QEffect("Default Target",
                                            "You are the default target for drilled reactions.",
                                            ExpirationCondition.Never,
                                            self, MIllustrations.Toggle)
                                    {
                                        Id = MQEffectIds.DrilledTarget,
                                        DoNotShowUpOverhead = true
                                    })));
                        }, noConfirmation: true));
                }
                RequestResult defaultChoice = await self.Battle.SendRequest(new AdvancedRequest(self,
                    "Choose an ally to be the default target for drilled reactions.", options));
                await defaultChoice.ChosenOption.Action();
                if (!self.HasFeat(MFeatNames.DrilledReflexes)) return;
                options.Remove(defaultChoice.ChosenOption);
                RequestResult secondDefault = await self.Battle.SendRequest(new AdvancedRequest(self,
                    "Choose another ally to be a default target for drilled reactions.", options));
                await secondDefault.ChosenOption.Action();
            });
        return choiceAction;
    }

    private static CombatAction AudibleTactics(Creature owner)
    {
        return new CombatAction(owner,MIllustrations.Auditory, "Audible Tactics", [Trait.Basic, Trait.DoesNotBreakStealth, Trait.DoNotShowOverheadOfActionName],
            "You use audible signals to communicate your tactics to your allies.", Target.Self().WithAdditionalRestriction(cr => cr.HasEffect(MQEffectIds.AudibleTactics) ? "Your tactics are already audible." : null))
            .WithActionCost(0)
            .WithEffectOnSelf(self =>
            {
                self.RemoveAllQEffects(qf => qf.Id == MQEffectIds.VisualTactics);
                self.AddQEffect(new QEffect("Audible Tactics", "Your tactics gain the Auditory trait.") { Id = MQEffectIds.AudibleTactics });
            });
    }
    
    private static CombatAction VisualTactics(Creature owner)
    {
        return new CombatAction(owner,MIllustrations.Visual, "Visual Tactics", [Trait.Basic, Trait.DoesNotBreakStealth, Trait.DoNotShowOverheadOfActionName],
                "You use visual signals to communicate your tactics to your allies.", Target.Self().WithAdditionalRestriction(cr => cr.HasEffect(MQEffectIds.VisualTactics) ? "Your tactics are already visual." : null))
            .WithActionCost(0)
            .WithEffectOnSelf(self =>
            {
                self.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AudibleTactics);
                self.AddQEffect(new QEffect("Visual Tactics", "Your tactics gain the Visual trait.") { Id = MQEffectIds.VisualTactics });
            });
    }

    private static List<Creature?> DrilledTargets(ChosenTargets targets, Creature commander)
    {
        List<Creature?> drilledTargets =
        [
            targets.ChosenCreatures.FirstOrDefault(cr =>
                cr.QEffects.Any(qf => qf.Id == MQEffectIds.DrilledTarget && qf.Source == commander) &&
                !cr.HasEffect(MQEffectIds.AnimalReaction)) ??
            targets.ChosenCreatures.FirstOrDefault(cr => !cr.HasEffect(MQEffectIds.AnimalReaction))

        ];
        if (commander.HasFeat(MFeatNames.DrilledReflexes))
        {
            drilledTargets.Add(targets.ChosenCreatures.Find(cr => cr.QEffects.Any(qf => qf.Id == MQEffectIds.DrilledTarget && qf.Source == commander) && !cr.HasEffect(MQEffectIds.AnimalReaction) && !drilledTargets.Contains(cr)) ??
                                targets.ChosenCreatures.FirstOrDefault(cr => !cr.HasEffect(MQEffectIds.AnimalReaction) && !drilledTargets.Contains(cr)));
        }
        if (drilledTargets.Count == 0) drilledTargets.Add(targets.ChosenCreatures[0]);
        return drilledTargets;
    }

    private static Possibilities CreateSpells2(Creature target)
     {
         return AlternateTaskImplements.Filter(target.Possibilities, possibility =>
         {
             switch (possibility)
             {
                 case ActionPossibility ap when !SpellDealsDamage(ap.CombatAction):
                     return false;
                 case ActionPossibility ap:
                     ap.CombatAction.ActionCost = 0;
                     ap.RecalculateUsability();
                     return true;
                 case ChooseActionCostThenActionPossibility ap when !SpellDealsDamage(ap.CombatAction):
                     return false;
                 case  ChooseActionCostThenActionPossibility ap:
                     ap.CombatAction.ActionCost = 0;
                     ap.CombatAction.SpentActions = 2;
                     if (ap.CombatAction.Target is DependsOnActionsSpentTarget actionTarget)
                     {
                         ap.CombatAction.Target = actionTarget.IfTwoActions;
                     }
                     AlternateTaskImplements.RecalculateUsability(ap);
                     return true;
                 case ChooseVariantThenActionPossibility ap when !SpellDealsDamage(ap.CombatAction):
                     return false;
                 case ChooseVariantThenActionPossibility ap:
                     ap.CombatAction.ActionCost = 0;
                     AlternateTaskImplements.RecalculateUsability(ap);
                     return true;
                 default:
                     return false;
             }
         });
     }
     private static bool SpellDealsDamage(CombatAction action)
     {
         if (action.SpellcastingSource == null || action.ActionCost == 3 || action.ActionCost == -2) return false;
         if ((!action.Description.ContainsIgnoreCase("deal") &&
              !action.Description.ContainsIgnoreCase("attack") &&
              !action.Description.ContainsIgnoreCase("take") &&
              !action.Description.ContainsIgnoreCase("damage")) ||
             action.Description.ContainsIgnoreCase("battleform"))
             return false;
         // if (!action.WillBecomeHostileAction) return false;
         if (action.Target == Target.AdjacentCreature()) return false;
         return action.Description.Contains("d4") || action.Description.Contains("d6") ||
                action.Description.Contains("d8") || action.Description.Contains("d10") ||
                action.Description.Contains("d12") ||
                action.Description.Contains("Deal 40");
     }

    private static CombatAction? DetermineBestMeleeStrike(Creature target)
    {
        List<CombatAction> possibleStrikes = target.MeleeWeapons
            .Select(item => CreateReactiveAttackFromWeapon(item, target))
            .Where(atk => atk.CanBeginToUse(target)).ToList();
        CombatAction? bestStrike = possibleStrikes.MaxBy(combatAction =>
        {
            if (combatAction.Item?.WeaponProperties != null)
                return combatAction.Item != null ? combatAction.Item.WeaponProperties.ItemBonus : 0;
            return 0;
        });
        CombatAction? maxByStriking = possibleStrikes.MaxBy(combatAction =>
        {
            if (combatAction.Item?.WeaponProperties != null)
                return combatAction.Item != null ? combatAction.Item.WeaponProperties.DamageDieSize : 0;
            return 0;
        });
        if (maxByStriking != bestStrike && maxByStriking != null && bestStrike != null && maxByStriking.Item != null && bestStrike.Item != null && maxByStriking.Item.WeaponProperties != null && bestStrike.Item.WeaponProperties != null && maxByStriking.Item.WeaponProperties.ItemBonus == bestStrike.Item.WeaponProperties.ItemBonus)
        {
            bestStrike = maxByStriking;
        }
        return bestStrike;
    }

    //if size is added, update this

    #region miscellanious combat actions

    public static CombatAction Reposition(Creature owner)
    {
        return new CombatAction(owner, MIllustrations.Reposition, "Reposition",
                [Trait.Basic, Trait.Attack, Trait.AttackDoesNotTargetAC],
                "{i}You muscle a creature or object around.{/i}\n\n" +
                "{b}Requirement{/b} You must have a hand free or be grappling the target.\n\n" +
                "Attempt an Athletics check against an adjacent target's Fortitude DC." + S.FourDegreesOfSuccess(
                    "You move the creature up to 10 feet. It must remain within your reach during this movement, and you can't move it into or through obstacles.",
                    "You move the target up to 5 feet. It must remain within your reach during this movement, and you can't move it into or through obstacles.",
                    null, "The target can move you up to 5 feet as though it successfully Repositioned you."),
                Target.AdjacentCreature().WithAdditionalConditionOnTargetCreature((self, target) =>
                    self.HasFreeHand || self.HeldItems.Any(item => item.Name == target.Name)
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature(
                            "You must have a hand free or be grappling this creature.")))
            .WithActionCost(1).WithSoundEffect(SfxName.Shove).WithActionId(MActionIds.Reposition)
            .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Athletics),
                TaggedChecks.DefenseDC(Defense.Fortitude)))
            .WithEffectOnEachTarget(async (_, caster, target, result) =>
            {
                switch (result)
                {
                    case CheckResult.CriticalSuccess:
                        if (target.WeaknessAndResistance.ImmunityToForcedMovement)
                        {
                            target.Overhead("{i}immune{/i}", Color.White,
                                target + " is immune to forced movement and can't be repositioned.");
                        }
                        else
                        {
                            IEnumerable<Tile> tiles = caster.Battle.Map.AllTiles.Where(tile =>
                                tile.IsTrulyGenuinelyFreeTo(target) && tile.DistanceTo(target.Occupies) <= 2 &&
                                tile.IsAdjacentTo(caster.Occupies));
                            Tile moveTo = (await caster.Battle.AskToChooseATile(caster, tiles,
                                MIllustrations.Reposition,
                                "Choose where to reposition " + target.Name + ".", "", false, false))!;
                            await target.MoveTo(moveTo, null,
                                new MovementStyle()
                                {
                                    ForcedMovement = true, Shifting = true, ShortestPath = true,
                                    MaximumSquares = 100
                                });
                        }

                        break;
                    case CheckResult.Success:
                        if (target.WeaknessAndResistance.ImmunityToForcedMovement)
                        {
                            target.Overhead("{i}immune{/i}", Color.White,
                                target + " is immune to forced movement and can't be repositioned.");
                        }
                        else
                        {
                            IEnumerable<Tile> tile2 = caster.Battle.Map.AllTiles.Where(tile =>
                                tile.IsTrulyGenuinelyFreeTo(target) && tile.DistanceTo(target.Occupies) <= 1 &&
                                tile.IsAdjacentTo(caster.Occupies));
                            Tile moveTo2 = (await caster.Battle.AskToChooseATile(caster, tile2,
                                MIllustrations.Reposition,
                                "Choose where to reposition " + target.Name + ".", "", false, false))!;
                            await target.MoveTo(moveTo2, null,
                                new MovementStyle()
                                {
                                    ForcedMovement = true, Shifting = true, ShortestPath = true,
                                    MaximumSquares = 100
                                });
                        }

                        break;
                    case CheckResult.CriticalFailure:
                        if (caster.WeaknessAndResistance.ImmunityToForcedMovement)
                        {
                            caster.Overhead("{i}immune{/i}", Color.White,
                                caster + " is immune to forced movement and can't be repositioned.");
                        }
                        else
                        {
                            if (target.OwningFaction != caster.Battle.You)
                            {
                                IEnumerable<Tile> tiles2 = caster.Battle.Map.AllTiles.Where(tile =>
                                    tile.IsTrulyGenuinelyFreeTo(caster) && tile.DistanceTo(caster.Occupies) <= 1 &&
                                    tile.IsAdjacentTo(target.Occupies));
                                Tile[] enumerable = tiles2 as Tile[] ?? tiles2.ToArray();
                                Tile moveTo3 = (enumerable.ToList().GetRandomForAi() ?? enumerable.FirstOrDefault())!;
                                await caster.MoveTo(moveTo3, null,
                                    new MovementStyle()
                                    {
                                        ForcedMovement = true, Shifting = true, ShortestPath = true,
                                        MaximumSquares = 100
                                    });
                            }
                            else
                            {
                                IEnumerable<Tile> tiles2 = caster.Battle.Map.AllTiles.Where(tile =>
                                    tile.IsTrulyGenuinelyFreeTo(caster) && tile.DistanceTo(caster.Occupies) <= 1 &&
                                    tile.IsAdjacentTo(target.Occupies));
                                Tile moveTo4 = (await target.Battle.AskToChooseATile(caster, tiles2,
                                    MIllustrations.Reposition,
                                    "Choose where to reposition " + caster.Name + ".", "", false, false))!;
                                await caster.MoveTo(moveTo4, null,
                                    new MovementStyle()
                                    {
                                        ForcedMovement = true, Shifting = true, ShortestPath = true,
                                        MaximumSquares = 100
                                    });
                            }
                        }

                        break;
                    case CheckResult.Failure:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(result), result, null);
                }
            });
    }

    private static CombatAction CreateReactiveAttackFromWeapon(Item weapon, Creature target)
    {
        CombatAction attackFromWeapon = target.CreateStrike(weapon, 0).WithActionCost(0);
        attackFromWeapon.Traits.Add(Trait.ReactiveAttack);
        return attackFromWeapon;
    }

    private static CombatAction CreateParryForReaction(Creature owner, CombatAction action)
    {
        return new CombatAction(owner, action.Illustration, action.Name, action.Traits.ToArray(), action.Description,
            Target.Self()).WithActionCost(0).WithSoundEffect(SfxName.RaiseShield).WithEffectOnSelf(creature =>
        {
            var amount = 1;
            if (action.Name == "Parry (fist)" &&
                ModManager.TryParse("Archetype.SpiritWarrior.FlowingPalm", out FeatName flowingPalm) &&
                owner.HasFeat(flowingPalm))
                amount = 2;
            creature.AddQEffect(new QEffect("Parry",
                $"You gain a +{amount} circumstance bonus to AC until the start of your next turn.",
                ExpirationCondition.ExpiresAtStartOfYourTurn, owner, action.Illustration)
            {
                BonusToDefenses = (_, _, defense) =>
                    defense == Defense.AC ? new Bonus(amount, BonusType.Circumstance, action.Name) : null
            });
        });
    }

    #endregion

    #region QEffects

    internal static QEffect SquadmateQf(Creature self)
    {
        return new QEffect
        {
            Id = MQEffectIds.Squadmate,
            Source = self,
            DoNotShowUpOverhead = true
        };
    }

    private static QEffect DrilledReactionsExpended(Creature caster)
    {
        return new QEffect("Drilled Reactions Expended", "Drilled Reactions has already been used.",
            ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, MIllustrations.Toggle)
        {
            Id = MQEffectIds.ExpendedDrilled
        };
    }

    private static QEffect RespondedToTactic(Creature caster)
    {
        return new QEffect("Responded to Tactic", "You have responded to a Commander Tactic this round.",
            ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, MIllustrations.PincerAttack)
        {
            Id = MQEffectIds.TacticResponse
        };
    }

    private static QEffect AnimalReaction(Creature owner)
    {
        return new QEffect("Trained Reaction",
                "Your companion has a reaction it can only use in response to your tactics. This reaction is lost if not used by the end of your turn.",
                ExpirationCondition.ExpiresAtEndOfYourTurn, owner, IllustrationName.Reaction)
            { Id = MQEffectIds.AnimalReaction };
    }

    internal static QEffect CommandersBannerEffect(AuraAnimation auraAnimation, int radius, Creature source)
    {
        bool dedication = !source.HasFeat(MFeatNames.Commander);
        return new QEffect("Commander's Banner",
            $"You and all allies in a {5*radius}-foot emanation gain a +1 status bonus to Will saves and DCs against fear effects.")
        {
            StateCheck = qfBanner =>
            {
                foreach (Creature friend in qfBanner.Owner.Battle.AllCreatures.Where(cr =>
                             cr.DistanceTo(qfBanner.Owner) <= radius && cr.FriendOf(qfBanner.Owner) &&
                             !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object) &&
                             !cr.IsImmuneTo(Trait.Mental) && !cr.IsImmuneTo(Trait.Visual) &&
                             !cr.IsImmuneTo(Trait.Emotion)))
                {
                    if (dedication) return;
                    friend.AddQEffect(new QEffect("Commander's Banner",
                        "You gain a +1 status bonus to Will saves and DCs against fear effects.",
                        ExpirationCondition.Ephemeral, qfBanner.Owner, MIllustrations.Banner)
                    {
                        CountsAsABuff = true,
                        BonusToDefenses = (_, combatAction, defense) => combatAction != null && combatAction.HasTrait(Trait.Fear) &&
                                                                        defense == Defense.Will
                            ? new Bonus(1, BonusType.Status, "Commander's Banner", true)
                            : null
                    });
                }
            },
            WhenExpires = _ => auraAnimation.MoveTo(0.0f),
            Id = MQEffectIds.Banner,
            Source = source,
            Description = dedication ? $"Your banner's aura is a {5*radius}-foot emanation." : $"You and all allies in a {5*radius}-foot emanation gain a +1 status bonus to Will saves and DCs against fear effects."
        };
    }

    internal static TileQEffect CommandersBannerTileEffect(int radius, Creature source, Creature illusion)
    {
        bool dedication = !source.HasFeat(MFeatNames.Commander);
        return new TileQEffect
        {
            StateCheck = tileQf =>
            {
                foreach (Creature friend in source.Battle.AllCreatures.Where(cr =>
                             cr.DistanceTo(tileQf.Owner) <= radius && cr.FriendOf(source) &&
                             !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object) &&
                             !cr.IsImmuneTo(Trait.Mental) && !cr.IsImmuneTo(Trait.Visual) &&
                             !cr.IsImmuneTo(Trait.Emotion)))
                {
                    if (dedication) return;
                    friend.AddQEffect(new QEffect("Commander's Banner",
                        "You gain a +1 status bonus to Will saves and DCs against fear effects.",
                        ExpirationCondition.Ephemeral, source, MIllustrations.Banner)
                    {
                        CountsAsABuff = true,
                        BonusToDefenses = (_, combatAction, defense) =>
                        {
                            if (combatAction != null && combatAction.HasTrait(Trait.Fear) &&
                                defense == Defense.Will)
                            {
                                return new Bonus(1, BonusType.Status, "Commander's Banner", true);
                            }

                            return null;
                        }
                    });
                }
            },
            WhenExpires = _ => illusion.DieFastAndWithoutAnimation(),
            TileQEffectId = MTileQEffectIds.Banner,
            Illustration = MIllustrations.Banner,
            Name = source.Name + "'s Banner",
            VisibleDescription = dedication ? $"{source.Name}'s banner's aura is a {5*radius}-foot emanation." : $"{source.Name} and all their allies in a {5*radius}-foot emanation gain a +1 status bonus to Will saves and DCs against fear effects."
        };
    }

    #endregion

    public static string CreateTooltips(string caption, string description)
    {
        ModManager.RegisterInlineTooltip(caption + "commander.mod", description);
        return "{tooltip:" + caption + "commander.mod" + "}" + caption + "{/}";
    }
    public static string UseCreatedTooltip(string caption)
    {
        return "{tooltip:" + caption + "commander.mod" + "}" + caption + "{/}";
    }
    private static string? GetAnimalCompanionCommandRestriction(
        QEffect qfRanger,
        Creature animalCompanion)
    {
        if (qfRanger.UsedThisTurn)
            return "You already commanded your animal companion this turn.";
        if (animalCompanion.HasEffect(QEffectId.Paralyzed))
            return "Your animal companion is paralyzed.";
        return animalCompanion.Actions.ActionsLeft == 0 && (animalCompanion.Actions.QuickenedForActions == null || animalCompanion.Actions.UsedQuickenedAction) ? "You animal companion has no actions it could take." : null;
    }

    public static int GetBannerRadius(Creature self)
    {
        int? value = self.FindQEffect(MQEffectIds.BannerRadius)?.Value;
        return value ?? 0;
    }
    
    private static CharacterSheet? GetCharacterSheetFromPartyMember(int index)
    {
        CharacterSheet? hero = null;
        if (CampaignState.Instance is { } campaign)
            hero = campaign.Heroes[index].CharacterSheet;
        else if (CharacterLibrary.Instance is { } library)
            hero = library.SelectedRandomEncounterParty[index];
        return hero;
    }

    #region bools and targets

    public static Target SquadmateTarget(Creature owner)
    {
        return (Target.MultipleCreatureTargets(2 + owner.Abilities.Intelligence, () => Target.RangedFriend(100)
                .WithAdditionalConditionOnTargetCreature((self, cr) =>
                {
                    if (cr.QEffects.Any(effect =>
                            effect.Id == MQEffectIds.Squadmate && effect.Source == self))
                        return Usability.NotUsableOnThisCreature("This creature is already a squadmate.");
                    return cr.PersistentCharacterSheet != null || cr.HasEffect(QEffectId.RangersCompanion) ||
                           cr.Traits.Any(t => t.HumanizeTitleCase2() == "Eidolon")
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("Must be a party member, animal companion, or eidolon.");
                })) as
            MultipleCreatureTargetsTarget)!
            .WithMinimumTargets(
                1).WithMustBeDistinct();
    }

    public static Target AllSquadmateTarget(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        EmanationTarget emanationTarget = Target.Emanation(100);
        emanationTarget.WithAdditionalRequirementOnCaster(self =>
            squadmates.Any(cr => new TacticResponseRequirement().Satisfied(self, cr))
                ? Usability.Usable
                : Usability.NotUsable("There must be at least one squadmate who can respond to a tactic."));
        return emanationTarget.WithIncludeOnlyIf((_, cr) => IsSquadmate(owner, cr));
    }

    public static Target AllSquadmateInBannerTarget(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        return Target.Emanation(100).WithAdditionalRequirementOnCaster(self =>
                squadmates.Any(cr => new TacticResponseRequirement().Satisfied(self, cr))
                    ? Usability.Usable
                    : Usability.NotUsable("There must be at least one squadmate who can respond to a tactic."))
            .WithIncludeOnlyIf((_, cr) => IsSquadmate(owner, cr) && new InBannerAuraRequirement().Satisfied(owner, cr));
    }

    internal static bool IsSquadmate(Creature commander, Creature squadmate)
    {
        return squadmate.QEffects.Any(qf => qf.Id == MQEffectIds.Squadmate && qf.Source == commander);
    }

    internal static bool IsMyAnimalCompanion(Creature commander, Creature companion)
    {
        return companion.QEffects.Any(qf => qf.Id == QEffectId.RangersCompanion && qf.Source == commander);
    }

    internal static bool AnimalReactionAvailable(Creature source, Creature target)
    {
        return target.QEffects.Any(qf => qf.Id == MQEffectIds.AnimalReaction && qf.Source == source);
    }

    internal static bool IsMyBanner(Creature commander, Creature target)
    {
        return target.QEffects.Any(qf => qf.Id == MQEffectIds.Banner && qf.Source == commander);
    }

    internal static bool IsMyBanner(Creature commander, Tile tile)
    {
        return tile.TileQEffects.Any(t1 => t1.TileQEffectId == MTileQEffectIds.Banner && t1.Name ==
            commander.Name + "'s Banner");
    }

    internal static bool HasConsumableToToss(Creature target)
    {
        return (target.CarriedItems.Any(item => IsConsumable(item, target)) && target.HasFreeHand)
               || target.HeldItems.Any(item => IsConsumable(item, target));
    }

    internal static bool IsConsumable(Item item, Creature target)
    {
        return item.HasTrait(Trait.Consumable) && (item.WhenYouDrink != null ||
                                                   item.HasTrait(Trait.Bomb) ||
                                                   item.ScrollProperties?.Spell.CombatActionSpell.ActionCost < 2 ||
                                                   item.ProvidesItemAction?.Invoke(target, item) is ActionPossibility
                                                   {
                                                       CombatAction.ActionCost: < 2
                                                   });
    }

    internal static bool AddBannerToInventory(Inventory inventory, Item banner)
    {
        if (banner.BaseItemName == BannerItem.Banner &&
            ((inventory.LeftHand?.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName) ?? false) ||
             (inventory.RightHand?.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName) ?? false) ||
             inventory.Backpack.Any(item =>
                 item?.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName) ?? false) || 
             (inventory.Armor?.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName) ?? false)))
        {
        }
        else if (banner.BaseItemName == BannerItem.Banner &&
                 (inventory.LeftHand?.StoredItems.Any(item =>
                     item.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName)) ?? false) ||
                 (inventory.RightHand?.StoredItems.Any(item =>
                     item.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName)) ?? false) ||
                 inventory.Backpack.Any(item =>
                     item?.StoredItems.Any(item1 =>
                         item1.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName)) ?? false))
        {
        }
        else if ((inventory.LeftHand?.StoredItems.Any(item => item.BaseItemName == banner.BaseItemName) ?? false) ||
                 (inventory.RightHand?.StoredItems.Any(item => item.BaseItemName == banner.BaseItemName) ?? false) ||
                 inventory.Backpack.Any(item =>
                     item?.StoredItems.Any(item1 => item1.BaseItemName == banner.BaseItemName) ?? false))
        {
        }
        else if (CampaignState.Instance != null && CampaignState.Instance.CommonLoot.Any(item => item.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName)
                 || item.BaseItemName == banner.BaseItemName || item.StoredItems.Any(item1 => item1.BaseItemName == banner.BaseItemName) 
                 || item.StoredItems.Any(item1 =>
                     item1.Runes.Any(rune => rune.BaseItemName == banner.BaseItemName))))
        {
        }
        else if ((inventory.LeftHand == null || inventory.LeftHand.BaseItemName != banner.BaseItemName) &&
                 (inventory.RightHand == null || inventory.RightHand.BaseItemName != banner.BaseItemName) &&
                 !inventory.Backpack.Any(item => item != null && item.BaseItemName == banner.BaseItemName))
        {
            if (inventory.CanBackpackFit(banner, 0))
            {
                inventory.AddAtEndOfBackpack(banner);
            }
            return true;
        }
        return false;
    }

    internal static bool CanTakeReaction(bool useDrilledReactions, Creature target, List<Creature?> drilledTargets, Creature caster)
    {
        if (target.HasEffect(QEffectId.CannotTakeReactions)) return false;
        return (useDrilledReactions && IsDrilledTarget(drilledTargets, target)) ||
               target.Actions.CanTakeReaction() || AnimalReactionAvailable(caster, target);
    }
    internal static bool CanTakeReaction(bool useDrilledReactions, Creature target, Creature caster)
    {
        if (target.HasEffect(QEffectId.CannotTakeReactions)) return false;
        return useDrilledReactions ||
               target.Actions.CanTakeReaction() || AnimalReactionAvailable(caster, target);
    }

    internal static bool IsDrilledTarget(List<Creature?> drilledTargets, Creature target)
    {
        return drilledTargets.Count > 0 && drilledTargets.Any(cr => cr == target);
    }

    internal static bool UseDrilledReactions(Creature caster)
    {
        return caster.HasFeat(MFeatNames.DrilledReflexes) ? caster.QEffects.Count(qEffect => qEffect.Id == MQEffectIds.ExpendedDrilled) < 2 : caster.QEffects.All(qEffect => qEffect.Id != MQEffectIds.ExpendedDrilled);
    }

    internal static void RemoveDrilledExpended(Creature caster)
    {
        QEffect? expended = caster.QEffects.FirstOrDefault(qf => qf.Id == MQEffectIds.ExpendedDrilled);
        if (expended != null) expended.ExpiresAt = ExpirationCondition.Immediately;
    }
    #endregion

    #region Extra Targeting Requirement Classes

    public class ReactionRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (source.QEffects.Any(qEffect => qEffect.Id == MQEffectIds.ExpendedDrilled) &&
                !target.Actions.CanTakeReaction() && !AnimalReactionAvailable(source, target))
            {
                return Usability.NotUsableOnThisCreature(
                    "You have used your Drilled Reactions already, and your target doesn't have a reaction available.");
            }
            if (target.HasEffect(QEffectId.CannotTakeReactions))
            {
                return Usability.NotUsableOnThisCreature(
                    "This creature cannot take reactions.");
            }
            return Usability.Usable;
        }
    }

    public class TacticResponseRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (target.QEffects.Any(qEffect => qEffect.Id == MQEffectIds.TacticResponse))
                return Usability.NotUsableOnThisCreature(target.Name + " has already responded to a tactic this round.");
            if (target.Destroyed || !target.Alive)
                return Usability.NotUsableOnThisCreature("This creature is not capable of taking action.");
            return Usability.Usable;
        }
    }

    public class CanMakeStrikeWithPrimary : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (target.PrimaryWeaponIncludingRanged == null || !target.CreateStrike(target.PrimaryWeaponIncludingRanged)
                    .WithActionCost(0).CanBeginToUse(target) || target.PrimaryWeaponIncludingRanged.EphemeralItemProperties.NeedsReload
                || target.PrimaryWeaponIncludingRanged.EphemeralItemProperties.AmmunitionLeftInMagazine <= 0)
            {
                return Usability.NotUsableOnThisCreature(target.Name + " cannot make a strike.");
            }
            return Usability.Usable;
        }
    }

    public class InBannerAuraRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            Creature? bannerHolder = source.Battle.AllCreatures.FirstOrDefault(cr => IsMyBanner(source, cr));
            Tile? bannerTile = source.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(source, tile));
            Tile? banner = bannerHolder != null ? bannerHolder.Occupies : bannerTile;
            if (banner != null && target.DistanceTo(banner) <= GetBannerRadius(source))
            {
                return Usability.Usable;
            }
            return Usability.NotUsableOnThisCreature(target.Name + " is not within the banner's aura.");
        }
    }
    public class BrandishRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (!source.HeldItems.Any(item => item.HasTrait(MTraits.Banner)) &&
                (!source.CarriedItems.Any(item =>
                     item.HasTrait(MTraits.Banner) && item.HasTrait(Trait.Shield) && item.IsWorn) ||
                 !source.HasFreeHand)) return Usability.NotUsable("You must be holding a banner.");
            return !target.CanSee(source) ? Usability.NotUsableOnThisCreature("Your ally must be able to see you.") : Usability.Usable;
        }
    }
    public class SquadmateTargetRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (!IsSquadmate(source, target))
            {
                return Usability.NotUsableOnThisCreature(target.Name + " is not a squadmate.");
            }
            return Usability.Usable;
        }
    }
    public class CanTargetBeginToMoveRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            bool canUse = CommonCombatActions.StepByStepStride(target).WithActionCost(0).CanBeginToUse(target) && !target.HasEffect(QEffectId.Immobilized);
            if (!canUse)
            {
                return Usability.NotUsableOnThisCreature(target.Name + " cannot move.");
            }
            return Usability.Usable;
        }
    }
    public class CanTargetTripAndIsAdjacentRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            bool canUse = CombatManeuverPossibilities.CreateTripAction(target, target.MeleeWeapons.FirstOrDefault(item => item.HasTrait(Trait.Trip)) ?? Item.Fist()).WithActionCost(0).CanBeginToUse(target);
            bool adjacentTo = target.Battle.AllCreatures.Any(cr => cr.EnemyOf(target) && cr.IsAdjacentTo(target));
            if (!canUse)
            {
                return Usability.NotUsableOnThisCreature(target.Name + " cannot trip.");
            }
            return !adjacentTo ? Usability.NotUsableOnThisCreature(target.Name + " is not adjacent to an enemy.") : Usability.Usable;
        }
    }
    
    public class CanTargetCastDamageSpell : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            bool canUse = target.Spellcasting?.Sources.Any(list => list.Spells.Any(SpellDealsDamage) || list.Cantrips.Any(SpellDealsDamage)) ?? false;
            return !canUse ? Usability.NotUsableOnThisCreature(target.Name + " cannot cast a damaging spell.") : Usability.Usable;
        }
    }

    public class CanTargetThrowConsumable : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (IsSquadmate(source, target) && HasConsumableToToss(target))
                return Usability.Usable;
            return IsSquadmate(source, target) ? Usability.NotUsableOnThisCreature("Your squadmate does not have a legal item or does not have a free hand to toss.") : Usability.NotUsableOnThisCreature("This creature is not a squadmate.");
        }
    }
    public class AdditionalSquadmateInBannerAuraRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            Creature? bannerHolder = source.Battle.AllCreatures.FirstOrDefault(cr => IsMyBanner(source, cr));
            Tile? bannerTile = source.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(source, tile));
            Tile? banner = bannerHolder != null ? bannerHolder.Occupies : bannerTile;
            if (banner != null && target.Battle.AllCreatures.Any(cr => IsSquadmate(source, cr) && cr != target && cr.DistanceTo(banner) <= GetBannerRadius(source)))
            {
                return Usability.Usable;
            }
            return Usability.NotUsableOnThisCreature("There are no other squadmates within the banner's aura.");
        }
    }

    #endregion

    public class ActionFeat(
        FeatName featName,
        string? flavorText,
        string rulesText,
        List<Trait> traits,
        List<Feat>? subfeats = null) : Feat(featName, flavorText, rulesText, traits, subfeats)
    {
        public int? ActionCost { get; set; }

        protected override string Autoname => base.Autoname +
                                              (ActionCost.HasValue
                                                  ? " " + RulesBlock.GetIconTextFromNumberOfActions(ActionCost.Value)
                                                  : "");
        public ActionFeat WithActionCost(int actionCost)
        {
            ActionCost = actionCost;
            return this;
        }
    }
}