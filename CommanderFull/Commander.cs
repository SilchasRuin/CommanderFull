using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
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
using Dawnsbury.Modding;
using Dawnsbury.Mods.DawnniExpanded;
using Dawnsbury.ThirdParty.SteamApi;
using static CommanderFull.ModData;
using Color = Microsoft.Xna.Framework.Color;

namespace CommanderFull;

public abstract class Commander
{
    public static readonly Skill WarfareLore = ExplorationActivities.ModData.Skills.WarfareLore;
    public static Dictionary<string, FeatName> TacticsDict { get; } = [];
    public static Dictionary<string, FeatName> PrereqsDict { get; } = [];
    public static Dictionary<FeatName, FeatName> PrereqsToTactics { get; } = [];
    public static IEnumerable<Feat> LoadAll()
    { 
        foreach (Feat tactic in LoadTactics())
        {
            int level = tactic.Traits.Contains(MTraits.LegendaryTactic) ? 19 : tactic.Traits.Contains(MTraits.MasterTactic) ? 15 : tactic.Traits.Contains(MTraits.ExpertTactic) ? 7 : 1;
            Feat prereq = CreatePrereqTacticsBasic((ActionFeat)tactic, level);
            tactic.WithPrerequisite(values => values.HasFeat(prereq),
                "You must have this tactic in your folio to select it.");
            QEffect? first = null;
            foreach (QEffect effect in TacticsQFs())
            {
                if (effect.Tag == null || (FeatName)effect.Tag != tactic.FeatName) continue;
                first = effect;
                break;
            }
            if (first != null)
            {
                QEffect tacticQf = first;
                tactic.WithOnCreature(cr => cr.AddQEffect(tacticQf));
            }
            AddTags(tactic);
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
                $"{{b}}2. Tactics{{/b}} By studying and practicing the strategic arts of war, you can guide your allies to victory. You begin play with a folio containing five {CreateTooltips("tactics", "{i}Preparing and Changing Tactics{/i}\nYou may prepare three tactics from your folio as a precombat preparation. At the start of an encounter, you can instruct a total number of party members equal to 2 + your Intelligence modifier, enabling these allies to respond to your tactics in combat. These allies are your squadmates. A squadmate always has the option not to respond to your tactical signal if they do not wish to. You count as one of your squadmates for the purposes of participating in or benefiting from a tactic (though you do not count against your own maximum number of squadmates).\n\n{i}Gaining New Tactics{/i}\nYou add additional tactics to your folio and increase the number of tactics you can prepare when you gain the expert tactician, master tactician, and legendary tactician class features. You can also add tactics to your folio with the Tactical Expansion feat, though this does not change the number you can have prepared.")}. These are combat techniques and coordinated maneuvers you can instruct your allies in, enabling them to respond to your signals in combat. As you increase in level, you gain the ability to learn more potent tactics.\n\n" +
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
                values.AddSelectionOption(new SingleFeatSelectionOption("DefaultTarget", "Drilled Reactions Default Target", SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, feat => feat.Tag is "DefaultTarget").WithIsOptional());
                values.AddSelectionOption(new MultipleFeatSelectionOption("CommanderTactics", "Commander Tactics",
                    SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, feat => feat.HasTrait(MTraits.Tactic), prepTactics
                    ));
                values.SetProficiency(MTraits.Commander, Proficiency.Trained);
                values.GrantFeat(FeatName.ShieldBlock);
                values.GrantFeat(ExplorationActivities.ModData.FeatNames.WarfareLore);
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
                values.AddAtLevel(15,
                    v15 => v15.GrantFeat(ExplorationActivities.ModData.FeatNames.WarfareLoreLegendary));
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
                cr.AddQEffect(new QEffect()
                {
                    ProvideMainAction = _ =>
                    {
                        SubmenuPossibility commanderMenu = new(MIllustrations.Toggle, "Tactics")
                        {
                            SubmenuId = MSubmenuIds.Commander
                        };
                        return commanderMenu;
                    }
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
                        ? new PossibilitySection("Expert Tactics").WithPossibilitySectionId(MPossibilitySectionIds
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
                    ProvideActionIntoPossibilitySection = (_, section) => section.PossibilitySectionId ==
                                                                          MPossibilitySectionIds
                                                                              .Toggle
                        ? new ActionPossibility(ChooseDrilledReactions(cr))
                        : null
                });
                cr.AddQEffect(new QEffect()
                {
                    Id = MQEffectIds.BannerRadius,
                    Value = 6
                });
                if (cr.Level >= 3)
                {
                    cr.AddQEffect(new QEffect("Warfare Expertise",
                        "As long as at least one enemy is visible at the start of an encounter, you can roll Warfare Lore in place of Perception for initiative. " +
                        "{i}Special{/i} If you have DawnniEx installed, you use Warfare Lore for all " +
                        CreateTooltips("Recall Weakness", FeatRecallWeakness.RecallWeaknessAction(cr).Description) +
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
                qf.StartOfCombat = async effectQ =>
                {
                    effectQ.StateCheckLayer = 1;
                    Creature self = qf.Owner;
                    self.AddQEffect(new QEffect
                    {
                        Id = MQEffectIds.Squadmate,
                        Source = self
                    });
                    List<Creature> potentialSquadmates = self.Battle.AllCreatures.Where(cr =>
                        cr.FriendOf(self) && cr.PersistentCharacterSheet != null && cr != self).ToList();
                    if (potentialSquadmates.Count <= self.Abilities.Intelligence + 2)
                    {
                        foreach (Creature creature in potentialSquadmates)
                            creature.AddQEffect(new QEffect
                            {
                                Id = MQEffectIds.Squadmate,
                                Source = self
                            });
                        if (self.HasFeat(MFeatNames.CommandersCompanion) &&
                            !self.PersistentUsedUpResources.AnimalCompanionIsDead)
                        {
                            self.AddQEffect(new QEffect()
                            {
                                StateCheckWithVisibleChanges = effect =>
                                {
                                    if (self.Battle.AllCreatures.Find(cr => IsMyAnimalCompanion(self, cr)) is
                                        not { } companion) return Task.CompletedTask;
                                    companion.AddQEffect(new QEffect()
                                    {
                                        Id = MQEffectIds.Squadmate,
                                        Source = self
                                    });
                                    effect.ExpiresAt = ExpirationCondition.Immediately;

                                    return Task.CompletedTask;
                                }
                            });
                        }
                    }
                    else
                    {
                        CombatAction chooseSquadmate = CombatAction.CreateSimple(self, "Choose Squadmate",
                                Trait.DoesNotBreakStealth, Trait.DoNotShowInCombatLog,
                                Trait.DoNotShowOverheadOfActionName)
                            .WithActionCost(0).WithEffectOnEachTarget((_, _, target, _) =>
                            {
                                target.AddQEffect(new QEffect()
                                {
                                    Id = MQEffectIds.Squadmate,
                                    Source = self
                                });
                                return Task.CompletedTask;
                            });
                        chooseSquadmate.Target = SquadmateTarget(self);
                        await self.Battle.GameLoop.FullCast(chooseSquadmate);
                        if (self.HasFeat(MFeatNames.CommandersCompanion) &&
                            !self.PersistentUsedUpResources.AnimalCompanionIsDead)
                        {
                            self.AddQEffect(new QEffect()
                            {
                                StateCheckWithVisibleChanges = effect =>
                                {
                                    if (self.Battle.AllCreatures.Find(cr => IsMyAnimalCompanion(self, cr)) is
                                        { } companion)
                                    {
                                        companion.AddQEffect(new QEffect()
                                        {
                                            Id = MQEffectIds.Squadmate,
                                            Source = self
                                        });
                                        effect.ExpiresAt = ExpirationCondition.Immediately;
                                    }

                                    return Task.CompletedTask;
                                }
                            });
                        }
                    }

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
        yield return commander;
        
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            Feat defaultTarget = new (ModManager.RegisterFeatName("DefaultTarget" + (i + 1),
                "Player Character " + (i + 1)), null, "", [], null);
            CreateDefaultTargetLogic(defaultTarget, index);
            yield return defaultTarget;
        }
        //s
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
    private static void CreateDefaultTargetLogic(Feat root, int index)
    {
        root.WithNameCreator(_ =>
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
                            { Id = MQEffectIds.DrilledTarget };
                        chosenCreature.AddQEffect(defaultTarget);
                        return Task.CompletedTask;
                    };
                });
    }

    #region feats
    public static IEnumerable<Feat> LoadFeats()
    {
        yield return new TrueFeat(MFeatNames.OfficerMedic, 1,
                "You’re trained in battlefield triage and wound treatment.",
                "You are trained in Medicine and can use your Intelligence modifier in place of your Wisdom modifier for Medicine checks. You gain the Battle Medicine feat.",
                [MTraits.Commander])
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
        yield return new TrueFeat(MFeatNames.CommandersCompanion, 1,
                "You gain the service of a young animal companion.",
                "You can affix your banner to your companion's saddle, barding, or simple harness, determining the effects of your commander's banner and other abilities that use your banner from your companion's space, even if you are not currently riding your companion. A companion granted by this feat always counts as one of your squadmates and does not count against your maximum number of squadmates." +
                "\n\n{b}Special{b} When you use Command an Animal to command the companion granted by this feat, it gains a reaction it can ony use in response to your tactics. This reaction is lost if not used by the end of your turn.",
                [MTraits.Commander],
                AnimalCompanionFeats.LoadAll().FirstOrDefault(feat => feat.FeatName == FeatName.AnimalCompanion)!
                    .Subfeats!.ToList()).WithPrerequisite(values => values.Sheet.Class?.ClassTrait == MTraits.Commander,
                "You must be a commander.")
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
        yield return new TrueFeat(MFeatNames.DeceptiveTactics, 1,
                "Your training has taught you that the art of war is the art of deception.",
                "You can use your Warfare Lore modifier in place of your Deception modifier for Deception checks to Create a Diversion or Feint, you count as trained in deception for the purposes of making a feint, and can use your proficiency rank in Warfare Lore instead of your proficiency rank in Deception to meet the prerequisites of feats that modify the Create a Diversion or Feint actions (such as Lengthy Diversion).",
                [MTraits.Commander])
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
            TrueFeat combatAssessment = new(MFeatNames.CombatAssessment, 1,
                "You make a telegraphed attack to learn about your foe.",
                $"Make a melee Strike. On a hit, you can immediately attempt a check to {UseCreatedTooltip("Recall Weakness")} about the target. On a critical hit, you gain a +2 circumstance bonus to the check to Recall Weakness. The target is temporarily immune to Combat Assessment for 1 day.",
                [MTraits.Commander, Trait.Fighter]);
            CombatAssessmentLogic(combatAssessment);
            yield return combatAssessment;
        }

        TrueFeat armoredRegiment = new(MFeatNames.ArmorRegiment, 1,
            "You've trained for grueling marches in full battle kit.",
            "You ignore the reduction to your Speed from any armor you wear and you can rest normally while wearing armor of any type.",
            [MTraits.Commander]);
        ArmorRegimentLogic(armoredRegiment);
        yield return armoredRegiment;

        TrueFeat plantBanner = new(MFeatNames.PlantBanner, 1,
            "You plant your banner to inspire your allies to hold the line.",
            "Plant your banner in a corner of your square. Each ally within a 30-foot burst centered on your banner immediately gains 4 temporary Hit Points, plus an additional 4 temporary Hit Points at 4th level and every 4 levels thereafter. " +
            "These temporary Hit Points last for 1 round; each time an ally starts their turn within the burst, their temporary Hit Points are renewed for another round. " +
            "If your banner is attached to a weapon, you cannot wield that weapon while your banner is planted. While your banner is planted, the emanation around your banner is a 35 foot emanation.",
            [MTraits.Commander, Trait.Manipulate]);
        PlantBannerLogic(plantBanner);
        yield return plantBanner;

        TrueFeat adaptiveStratagem = new(MFeatNames.AdaptiveStratagem, 2,
            "Your constant training and strong bond with your allies allow you to change tactics on the fly.",
            "At the start of combat you can replace one of your prepared expert, mobility, or offensive tactics with another tactic in your folio.",
            [MTraits.Commander]);
        AdaptiveStratagemLogic(adaptiveStratagem);
        yield return adaptiveStratagem;
        
        TrueFeat defensiveSwap = new(MFeatNames.DefensiveSwap, 2, "You and your allies work together selflessly to protect each other from harm.",
            "When you or an adjacent ally are the target of an attack, you may use a reaction to immediately swap positions with each other, and whichever of you was not the target of the triggering attack becomes the target instead.",
            [MTraits.Commander]);
        DefensiveSwapLogic(defensiveSwap);
        yield return defensiveSwap;
        
        TrueFeat guidingShot = new(MFeatNames.GuidingShot, 2, "Your ranged attack helps guide your allies into striking your enemy's weak point.",
            "Attempt a Strike with a ranged weapon. If the Strike hits, the next creature other than you to attack the same target before the start of your next turn gains a +1 circumstance bonus to their roll, or a +2 circumstance bonus if your Strike was a critical hit.",
            [MTraits.Commander, Trait.Flourish]);
        GuidingShotLogic(guidingShot);
        yield return guidingShot;
        
        TrueFeat setupStrike = new(MFeatNames.SetupStrike, 2, "Your attack makes it difficult for your enemy to defend themselves against your allies' attacks.",
            "Attempt a Strike against an enemy. If the Strike is successful, the target is off guard against the next attack that one of your allies attempts against it before the start of your next turn.",
            [MTraits.Commander, Trait.Flourish]);
        SetUpStrikeLogic(setupStrike);
        yield return setupStrike;

        if (ModManager.TryParse("DawnniEx", out Trait _))
        {
            TrueFeat rapidAssessment = new(MFeatNames.RapidAssessment, 2, "You quickly evaluate your enemies.",
                $"Attempt a check to {UseCreatedTooltip("Recall Weakness")} against one creature you are observing.", [MTraits.Commander]);
            RapidAssessmentLogic(rapidAssessment);
            yield return rapidAssessment;
        }

        TrueFeat tacticalExpansion = new(MFeatNames.TacticalExpansion, 2, "Your folio is filled with tactics and techniques you’ve devised based on study and experience.", 
            "Add two additional tactics you qualify for to your folio.", 
            [MTraits.Commander]);
        tacticalExpansion.WithOnSheet(values =>
            values.AddSelectionOption(new MultipleFeatSelectionOption("ExpandedFolio", "Tactical Expansion",
                values.CurrentLevel, feat => feat.HasTrait(MTraits.TacticPre), 2)));
        tacticalExpansion.WithMultipleSelection();
        yield return tacticalExpansion;
        
        TrueFeat bannerTwirl = new(MFeatNames.BannerTwirl, 4, "You spin your banner in an elaborate pattern that your enemies find inscrutable.",
            "You and any ally adjacent to you have concealment from ranged attacks until the start of your next turn.", [MTraits.Commander, MTraits.Banner, Trait.Manipulate]);
        BannerTwirlLogic(bannerTwirl);
        yield return bannerTwirl;
        
        TrueFeat bannerInspire = new(MFeatNames.BannersInspiration, 4, "You wave your banner, inspiring allies to throw off the shackles of fear.",
            "Each ally in your banner's aura reduces their frightened and stupefied conditions by 1, and can make a Will save against a standard level-based DC for your level, and on a success or better remove the Confused or Paralyzed condition. Regardless of the result, any ally that attempts this save is temporarily immune to Banner's Inspiration for 10 minutes.",
            [MTraits.Brandish, MTraits.Commander, Trait.Emotion, Trait.Flourish, Trait.Mental, Trait.Visual]);
        BannersInspirationLogic(bannerInspire);
        yield return bannerInspire;
        
        if (ModManager.TryParse("DawnniEx", out Trait _))
        {
            TrueFeat observationalAnalysis = new(MFeatNames.ObservationalAnalysis, 4, "You are able to rapidly discern relevant details about your opponents in the heat of combat.",
                $"When you use Combat Assessment against a target that you or an ally has targeted with a Strike or spell since the start of your last turn, you get a +2 circumstance bonus to the {UseCreatedTooltip("Recall Weakness")} check (+4 if the Strike from Combat Assessment is a critical hit).", [MTraits.Commander]);
            ObservationalAnalysisLogic(observationalAnalysis);
            yield return observationalAnalysis;
        }
        
        TrueFeat unsteadyingStrike = new(MFeatNames.UnsteadyingStrike, 4, "Your attack makes your opponent more susceptible to follow-up maneuvers from your allies.",
            "Make a melee Strike against an enemy within your reach. If the Strike is successful, the enemy takes a –2 circumstance penalty to their Fortitude DC to resist being Grappled, Repositioned, or Shoved and a –2 circumstance penalty to their Reflex DC to resist being Disarmed. Both penalties last until the start of your next turn.",
            [MTraits.Commander, Trait.Flourish]);
        UnsteadyingStrikeLogic(unsteadyingStrike);
        yield return unsteadyingStrike; 
        
        TrueFeat shieldedRecovery = new(MFeatNames.ShieldedRecovery, 4, "You can bandage wounds with the same hand you use to hold your shield.",
            "You can use the same hand you are using to wield a shield to use Battle Medicine. When you use Battle Medicine on an ally while wielding a shield, they gain a +1 circumstance bonus to AC and Reflex saves that lasts until the start of your next turn or until they are no longer adjacent to you, whichever comes first.",
            [MTraits.Commander]);
        ShieldedRecoveryLogic(shieldedRecovery);
        yield return shieldedRecovery;
        
        TrueFeat battleTestedCompanion = new(MFeatNames.BattleTestedCompanion, 6, "Your companion is a tried and tested ally of unshakable reliability.", 
            "Your animal companion gains the following benefits:\r\n• It gets +1 to Strength, Dexterity, Constitution and Wisdom.\r\n• Its unarmed attack damage increases from one die to two dice (for example, from 1d8 to 2d8).\r\n• Its proficiency with Perception and all saving throws increases to Expert (an effective +2 to Perception and all saves).\r\n• Its proficiency in Intimidation, Stealth and Survival increases by one step (from untrained to trained; or from trained to expert).\r\n• While your banner is affixed to this companion, the banner's aura is 10 feet greater than it normally is (typically this means the banner's 30-foot aura becomes a 40-foot aura).",
            [MTraits.Commander]);
        BattleTestedCompanionLogic(battleTestedCompanion);
        yield return battleTestedCompanion;
        
        yield return new TrueFeat(MFeatNames.EfficientPreparation, 6, "You’ve developed techniques for drilling your allies on multiple tactics in a succinct and efficient manner.", "Increase the number of tactics you can have prepared by 1.", [MTraits.Commander])
            .WithOnSheet(values => {
                values.AddSelectionOption(new SingleFeatSelectionOption("EfficientPreparation", "Efficient Preparation",
                    SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, feat => feat.HasTrait(MTraits.Tactic)));
            });
        
        TrueFeat claimTheField = new(MFeatNames.ClaimTheField, 6, "You hurl your banner forward with precision, claiming the battlefield for yourself and your allies.",
            $"Your banner must be attached to a thrown weapon. You {CreateTooltips("Plant the Banner", plantBanner.RulesText)}, but you can place it at any corner within the required weapon's first range increment, rather than the corner of your square. The calculated confidence of this brash maneuver unnerves your enemies; any enemy who attempts to damage or remove your banner while it is planted in this way must succeed at a Will save against your class DC or the attempt fails. On a critical failure, the enemy is fleeing for 1 round. This is an incapacitation and mental effect.",
            [MTraits.Commander]);
        ClaimTheFieldLogic(claimTheField);
        yield return claimTheField;

        yield return new TrueFeat(MFeatNames.ReactiveStrike, 6, "You lash out at a foe that leaves an opening.",
            "{b}Trigger{/b} A creature within your reach uses a manipulate action or move action, makes a ranged attack, or leaves a square during a move action it's using.\n\nMake a melee Strike against the triggering creature. If your attack is a critical hit and the trigger was a manipulate action, you disrupt that action. This Strike doesn't count toward your multiple attack penalty, and your multiple attack penalty doesn't apply to this Strike.",
            [MTraits.Commander]).WithActionCost(-2).WithOnCreature(self =>
            {
                QEffect reactiveStrike = QEffect.AttackOfOpportunity();
                reactiveStrike.Name = reactiveStrike.Name?.Replace("Attack of Opportunity", "Reactive Strike");
                self.AddQEffect(reactiveStrike);
            })
            .WithEquivalent(values => values.AllFeats.Any(ft => ft.BaseName is "Attack of Opportunity" or "Reactive Strike" or "Opportunist"));;
        
        TrueFeat defiantBanner = new(MFeatNames.DefiantBanner, 8, "You vigorously wave your banner to remind yourself and your allies that you can and must endure.",
            "You and all allies within the aura of your commander's banner when you use this action gain resistance to bludgeoning, piercing, and slashing damage equal to your Intelligence modifier until the start of your next turn.",
            [MTraits.Commander, MTraits.Brandish, Trait.Flourish, Trait.Manipulate, Trait.Visual]);
        DefiantBannerLogic(defiantBanner);
        yield return defiantBanner;
        
        Feat education = new TrueFeat(MFeatNames.OfficersEducation, 8, "You know that a broad knowledge base is critical for a competent commander.",
            "You become trained in two skills you are not already trained in, become an expert in one skill you are currently trained in, and gain any one general feat that you meet the prerequisites for.",
            [MTraits.Commander]).WithMultipleSelection().WithOnSheet(values => {
            values.AddSkillIncreaseOptionComplex("TrainInOne", "Officer's Education", Proficiency.Trained);
            values.AddSkillIncreaseOptionComplex("TrainInTwo", "Officer's Education", Proficiency.Trained);
            values.AddSkillIncreaseOptionComplex("ExpertInOne", "Officer's Education", Proficiency.Expert);
            values.AddSelectionOption(new SingleFeatSelectionOption("OE_GenFeat", "Officer's Education", values.CurrentLevel, feat => feat.HasTrait(Trait.General)));
        }).WithPrerequisite(sheet => sheet.AllFeatGrants.Count(fg => fg.GrantedFeat.FeatName == MFeatNames.OfficersEducation) < 3, "You can only take this feat twice.");
        string replace = education.RulesText.Replace("multiple times.", "twice.");
        education.RulesText = replace;
        yield return education;

        TrueFeat rallyingBanner = new(MFeatNames.RallyingBanner, 8, "Your banner waves high, reminding your allies that the fight can still be won.",
            "You restore 4d6 Hit Points to each ally within the aura of your commander's banner. This healing increases by an additional 1d6 at 10th level and every 2 levels thereafter. You may only use Rallying Banner once per encounter.",
            [MTraits.Brandish, MTraits.Commander, Trait.Emotion, Trait.Healing, Trait.Mental, Trait.Visual]);
        RallyingBannerLogic(rallyingBanner);
        yield return rallyingBanner;
        
        yield return new TrueFeat(MFeatNames.UnrivaledAnalysis, 8, "Your experience allows you to derive even more information about your opponents from a mere glance.",
            "When you use Rapid Assessment, you can attempt up to four checks to Recall Knowledge about creatures you are observing.", [MTraits.Commander]).WithPrerequisite(MFeatNames.RapidAssessment, "Rapid Assessment");
    }
    internal static void LoadGenericFeats()
    {
        if (AllFeats.GetFeatByFeatName(FeatName.ShieldWarden) is not TrueFeat warden) return;
        warden.WithAllowsForAdditionalClassTrait(MTraits.Commander);
        warden.Prerequisites.RemoveAll(req => req.Description.Contains("must have Shield Ally") || req.Description.Contains("must be a Fighter"));
        warden.WithPrerequisite(
            values => values.HasFeat(FeatName.Fighter) || values.HasFeat(MFeatNames.Commander) ||
                      values.HasFeat(Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion.Champion
                          .ShieldAllyFeatName),
            "You must be a Fighter, a Commander, or you must have Shield Ally as your divine ally.");
    }

    #endregion

    public static IEnumerable<Feat> LoadTactics()
    {
        yield return new ActionFeat(MFeatNames.GatherToMe, "You signal your team to move into position together.",
                "Signal all squadmates; each can immediately Stride as a reaction, though each must end their movement inside your banner’s aura, or as close to your banner's aura as their movement Speed allows.",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.DefensiveRetreat, "You call for a careful retreat.",
                "Signal all squadmates within the aura of your commander's banner; each can immediately Step up to three times as a free action. Each Step must take them farther away from at least one hostile creature they are observing and can only take them closer to a hostile creature if doing so is the only way for them to move toward safety.",
                [MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.NavalTraining,
                "Your instructions make it easier for you and your allies to swim through dangerous waters.",
                "Signal all squadmates; until the end of your next turn, each squadmate gains a swim Speed.",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.PassageOfLines,
                "You command your allies to regroup, allowing endangered units to fall back while rested units press the advantage.",
                "Signal all squadmates within the aura of your commander's banner; each can swap positions with another willing ally adjacent to them.",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.ProtectiveScreen,
                "You've trained your allies in a technique designed to protect war mages.",
                "Signal one squadmate; as a reaction, that squadmate Strides directly toward any other squadmate who is within the aura of your banner. If the first squadmate ends their movement adjacent to that squadmate, that squadmate does not trigger reactions when casting spells or making ranged attacks until the end of their next turn or until they are no longer adjacent to the first squadmate, whichever comes first.",
                [MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.PincerAttack,
                "You signal an aggressive formation designed to exploit enemies' vulnerabilities.",
                "Signal all squadmates affected by your commander's banner; each can Step as a free action. If any of your allies end this movement adjacent to an opponent, that opponent is off-guard to melee attacks from you and all other squadmates who responded to Pincer Attack until the start of your next turn.",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.StrikeHard, "You command an ally to attack.",
                "Choose a squadmate who can see or hear your signal. That ally immediately attempts a Strike as a reaction.",
                [MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.CoordinatingManeuvers,
                "Your team works to slip enemies into a disadvantageous position.",
                "Signal one squadmate within the aura of your banner; that squadmate can immediately Step as a free action. If they end this movement next to an opponent, they can attempt to Reposition that target as a reaction.",
                [MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.DoubleTeam,
                "Your team works together to set an enemy up for a vicious attack.",
                "Signal one squadmate who has an opponent within their reach. That ally can Shove or Reposition an opponent as a free action. If their maneuver is successful and the target ends their movement adjacent to a different squadmate, the second squadmate can attempt a melee Strike against that target as a reaction.",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.EndIt,
                "At your proclamation that victory is already at hand, your allies march forward with an authoritative stomp, scattering your enemies in terror.",
                "If you and your allies outnumber all enemies on the battlefield, and you or a squadmate have reduced an enemy to 0 Hit Points since the start of your last turn, you may signal all squadmates within the aura of your banner; you and each ally can Step as a free action directly toward a hostile creature. Any hostile creatures within 10 feet of a squadmate after this movement must attempt a Will save against your class DC; on a failure they become fleeing for 1 round, and on a critical failure they become fleeing for 1 round and frightened 2. This is an emotion, fear, and mental effect.",
                [MTraits.Tactic, MTraits.Brandish, Trait.Incapacitation, MTraits.BasicTactic]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.Reload,
                "Your drill instruction kicks in, and your allies rapidly reload their weapons to prepare for the next volley.",
                "Signal all squadmates; each can immediately Interact to reload as a reaction.",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.ShieldsUp, "You signal your allies to ready their defenses.",
                "Signal all squadmates within the aura of your commander’s banner; each can immediately Raise a Shield as a reaction. Squadmates who have a parry action (whether from a Parry weapon or a Feat such as Dueling Parry or Twin Parry) may use that instead.\n\n{b}Special{/b} If one of your squadmates knows or has prepared the shield cantrip, they can cast it as a reaction instead of taking the actions normally granted by this tactic.",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(1);
        yield return new ActionFeat(MFeatNames.TacticalTakedown,
                "You direct a coordinated maneuver that sends an enemy tumbling down.",
                "Signal up to two squadmates within the aura of your commander’s banner. Each of those allies can Stride up to half their Speed as a reaction. If they both end this movement adjacent to an enemy, that enemy must succeed at a Reflex save against your class DC or fall prone.\n",
                [MTraits.Tactic, MTraits.BasicTactic]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.DemoralizingCharge,
            "Your team’s coordinated assault strikes fear into your enemies’ hearts.",
            "Signal up to two squadmates within the aura of your commander’s banner; as a free action, those squadmates can immediately Stride toward an enemy they are observing. If they end this movement adjacent to an enemy, they can attempt to Strike that enemy as a reaction. For each of these Strikes that are successful, the target enemy must succeed at a Will save against your class DC or become frightened 1 (frightened 2 on a critical failure); this is an emotion, fear, and mental effect. If both Strikes target the same enemy, that enemy attempts the save only once after the final attack and takes a –1 circumstance penalty to their Will save to resist this effect (this penalty increases to –2 if both Strikes are successful or to –3 if both Strikes are successful and either is a critical hit).",
            [MTraits.Tactic, MTraits.ExpertTactic, MTraits.Brandish]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.BuckleCutBlitz,
            "Your squad dashes past enemies, slicing their boot laces and breaking their belt buckles.",
            "Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Reflex save against your class DC or become clumsy 1 for 1 round (clumsy 2 on a critical failure).",
            [MTraits.Tactic, MTraits.ExpertTactic, MTraits.Brandish]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.StupefyingRaid,
            "Your team dashes about in a series of maneuvers that leave the enemy befuddled.",
            "Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Will save against your class DC or become stupefied 1 for 1 round (stupefied 2 on a critical failure); this is a mental effect.",
            [MTraits.Tactic, MTraits.ExpertTactic, MTraits.Brandish]).WithActionCost(2);
        yield return new ActionFeat(MFeatNames.SlipAndSizzle,
            "Your team executes a brutal technique designed to knock down an opponent and blast them with magical devastation.",
            "Signal up to two squadmates within the aura of your commander’s banner; one of these squadmates must be adjacent to an opponent and the other must be capable of casting a spell that deals damage. The first squadmate can attempt to Trip the adjacent opponent as a reaction. If this Trip is successful, the second squadmate can cast a ranged spell that deals damage and takes 2 or fewer actions to cast. This spell is cast as a reaction and must either target the tripped opponent or include the tripped opponent in the spell’s area.\n\nIf the second squadmate cast a spell using slots or Focus Points as part of this tactic, they are slowed 1 until the end of their next turn and do not gain a reaction when they regain actions at the start of their next turn." +
            "\n{b}Note{/b} Spells with variants, for example: Magic Missile or Scorching Ray, cannot be cast at this time.",
            [MTraits.Tactic, MTraits.ExpertTactic]).WithActionCost(2);
    }

    private static void AddTags(Feat feat)
    {
        feat.WithOnSheet(values => values.Tags.Add(feat.Name, feat.Traits));
    }

    private static IEnumerable<QEffect> TacticsQFs()
    {
        yield return new QEffect
        {
            Tag = MFeatNames.GatherToMe,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(GatherToMe(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.DefensiveRetreat,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(DefensiveRetreat(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.NavalTraining,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(NavalTraining(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.PassageOfLines,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(PassageOfLines(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.ProtectiveScreen,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(ProtectiveScreen(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.PincerAttack,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(PincerAttack(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.StrikeHard,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(StrikeHard(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.CoordinatingManeuvers,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(CoordinatingManeuvers(qf.Owner))
                    : null
        }; 
        yield return new QEffect
        {
            Tag = MFeatNames.DoubleTeam,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(DoubleTeam(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.EndIt,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(EndIt(qf.Owner))
                    : null,
            StartOfYourPrimaryTurn = (effect, _) =>
            {
                effect.AddGrantingOfTechnical(cr => cr.EnemyOf(effect.Owner), qfTech =>
                {
                    qfTech.WhenCreatureDiesAtStateCheckAsync = _ =>
                    {
                        effect.Owner.AddQEffect(new QEffect(ExpirationCondition.CountsDownAtStartOfSourcesTurn)
                        {
                            Source = effect.Owner,
                            Value = 2,
                            Id = MQEffectIds.DeathCounter
                        });
                        return Task.CompletedTask;
                    };
                });
                return Task.CompletedTask;
            }
        };
        yield return new QEffect
        {
            Tag = MFeatNames.Reload,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(Reload(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.PincerAttack,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(ShieldsUp(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.TacticalTakedown,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(TacticalTakedown(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.DemoralizingCharge,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(DemoralizingCharge(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.BuckleCutBlitz,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(BuckleCutBlitz(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.StupefyingRaid,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(StupefyingRaid(qf.Owner))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.SlipAndSizzle,
            ProvideActionIntoPossibilitySection = (qf, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(SlipAndSizzle(qf.Owner))
                    : null
        };
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
                    (cr.FriendOf(self) && cr.PersistentCharacterSheet != null) ||
                    cr.FindQEffect(QEffectId.RangersCompanion)?.Source == self).ToList();
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
                                        { Id = MQEffectIds.DrilledTarget })));
                        }, noConfirmation: true));
                }

                var hey = await self.Battle.SendRequest(new AdvancedRequest(self,
                    "Choose an ally to be the default target for drilled reactions.", options));
                await hey.ChosenOption.Action();
            });
        return choiceAction;
    }

    #region tactics actions
    private static CombatAction GatherToMe(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        CombatAction tactic = new CombatAction(owner, MIllustrations.GatherToMe, "Gather to Me!",
                [MTraits.Tactic, MTraits.Commander, Trait.Basic],
                "Signal all squadmates; each can immediately Stride as a reaction, though each must end their movement inside your banner’s aura or as close to your banner's aura as their movement Speed allows.",
                squadmates.Any(cr => new ReactionRequirement().Satisfied(owner, cr))
                    ? AllSquadmateTarget(owner)
                    : Target.Uncastable("There must be at least one squadmate who can take a reaction."))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (_, caster, targets) =>
            {
                Creature? drilledTarget = targets.ChosenCreatures.Find(cr => cr.HasEffect(MQEffectIds.DrilledTarget)) ??
                                          targets.ChosenCreatures.FirstOrDefault();
                Creature? bannerHolder = caster.Battle.AllCreatures.FirstOrDefault(cr => IsMyBanner(caster, cr));
                Tile? bannerTile = caster.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(caster, tile));
                bool useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                if (bannerHolder != null && targets.ChosenCreatures.Contains(bannerHolder) &&
                    new TacticResponseRequirement().Satisfied(caster, bannerHolder) == Usability.Usable && (
                        (!useDrilledReactions && drilledTarget?.Name == bannerHolder.Name) ||
                        bannerHolder.Actions.CanTakeReaction() || AnimalReactionAvailable(caster, bannerHolder)))
                {
                    if (await bannerHolder.StrideAsync("Move up to your speed.", allowCancel: true))
                    {
                        if (useDrilledReactions && drilledTarget?.Name == bannerHolder.Name)
                        {
                            caster.AddQEffect(DrilledReactionsExpended(caster));
                        }
                        else if (!bannerHolder.HasEffect(MQEffectIds.AnimalReaction))
                        {
                            bannerHolder.Actions.UseUpReaction();
                        }
                        else if (bannerHolder.HasEffect(MQEffectIds.AnimalReaction))
                        {
                            bannerHolder.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        }

                        bannerHolder.AddQEffect(RespondedToTactic(caster));
                    }
                }

                foreach (Creature target in targets.ChosenCreatures.Where(c => c != bannerHolder))
                {
                    useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable || (
                            (useDrilledReactions || drilledTarget?.Name != target.Name) &&
                            !target.Actions.CanTakeReaction() && !AnimalReactionAvailable(caster, target)))
                    {
                        continue;
                    }

                    List<Option> tileOptions =
                    [
                        new CancelOption(true),
                    ];
                    CombatAction? moveAction = (target.Possibilities
                            .Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.Stride)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.RecalculateUsability();
                                return true;
                            })
                            .CreateActions(true)
                            .FirstOrDefault(pw => pw.Action.ActionId == ActionId.Stride) as CombatAction)
                        ?.WithActionCost(0);
                    List<Tile> floodFill = Pathfinding.Floodfill(target, target.Battle, new PathfindingDescription()
                        {
                            Squares = target.Speed
                        })
                        .Where(tile =>
                            tile.LooksFreeTo(target) && ((bannerHolder != null &&
                                                          bannerHolder.DistanceTo(tile) <= GetBannerRadius(caster)) ||
                                                         (bannerTile != null && tile.DistanceTo(bannerTile) <=
                                                             GetBannerRadius(caster))))
                        .ToList();
                    if (floodFill.Count == 0)
                    {
                        floodFill = Pathfinding.Floodfill(target, target.Battle, new PathfindingDescription()
                            {
                                Squares = 100
                            })
                            .Where(tile =>
                                tile.LooksFreeTo(target) && ((bannerHolder != null &&
                                                              bannerHolder.DistanceTo(tile) <=
                                                              GetBannerRadius(caster)) ||
                                                             (bannerTile != null && tile.DistanceTo(bannerTile) <=
                                                                 GetBannerRadius(caster))))
                            .ToList();
                    }

                    floodFill.ForEach(tile =>
                    {
                        if (moveAction == null || !(bool)moveAction.Target.CanBeginToUse(target)) return;
                        tileOptions.Add(moveAction.CreateUseOptionOn(tile).WithIllustration(moveAction.Illustration));
                    });
                    Option move = (await target.Battle.SendRequest(
                        new AdvancedRequest(target,
                            "Choose where to move, you must move closer to the area covered by the commander's banner.",
                            tileOptions)
                        {
                            IsMainTurn = false,
                            IsStandardMovementRequest = true,
                            TopBarIcon = target.Illustration,
                            TopBarText = target.Name +
                                         " choose where to move. You must move closer to the area covered by the commander's banner."
                        })).ChosenOption;
                    switch (move)
                    {
                        case CancelOption:
                            break;
                        case TileOption tileOption:
                            await target.StrideAsync(target.Name + " move as close to the banner area as possible.",
                                strideTowards: tileOption.Tile);
                            if (useDrilledReactions && drilledTarget?.Name == target.Name)
                            {
                                caster.AddQEffect(DrilledReactionsExpended(caster));
                            }
                            else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.Actions.UseUpReaction();
                            }
                            else if (target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                            }

                            target.AddQEffect(RespondedToTactic(caster));
                            break;
                    }
                }
            });
        return tactic;
    }
    private static CombatAction DefensiveRetreat(Creature owner)
    {
        List<Creature> possibles =
            owner.Battle.AllCreatures.Where(cr => cr.EnemyOf(owner) && owner.CanSee(cr)).ToList();
        List<Creature> squadmates =
            owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr) && cr != owner).ToList();
        CombatAction tactic = new CombatAction(owner, MIllustrations.Retreat, "Defensive Retreat",
                [MTraits.Tactic, MTraits.Commander, Trait.Basic],
                "Signal all squadmates within the aura of your banner; each can immediately Step up to three times as a free action. Each Step must take them farther away from at least one hostile creature they are observing and can only take them closer to a hostile creature if doing so is the only way for them to move toward safety.",
                squadmates.Any(cr =>
                    new BrandishRequirement().Satisfied(owner, cr) == Usability.Usable && possibles.Count > 0)
                    ? AllSquadmateInBannerTarget(owner)
                    : Target.Uncastable("You must be holding a banner and be observing at least one hostile creature."))
            .WithActionCost(2)
            .WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (_, caster, targets) =>
            {
                foreach (Creature target in targets.ChosenCreatures)
                {
                    List<Creature> enemies = caster.Battle.AllCreatures
                        .Where(cr => cr.EnemyOf(target) && target.CanSee(cr)).ToList();
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable ||
                        new InBannerAuraRequirement().Satisfied(owner, target) != Usability.Usable || target == caster)
                    {
                        continue;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        List<Option> tileOptions =
                        [
                            new CancelOption(true),
                        ];
                        CombatAction? moveAction = (target.Possibilities
                                .Filter(ap =>
                                {
                                    if (ap.CombatAction.ActionId != ActionId.Step)
                                        return false;
                                    ap.CombatAction.ActionCost = 0;
                                    ap.RecalculateUsability();
                                    return true;
                                })
                                .CreateActions(true)
                                .FirstOrDefault(pw => pw.Action.ActionId == ActionId.Step) as CombatAction)
                            ?.WithActionCost(0);
                        List<Tile> floodFill = Pathfinding.Floodfill(target, target.Battle,
                                new PathfindingDescription()
                                {
                                    Squares = 1
                                })
                            .Where(tile =>
                                tile.LooksFreeTo(target) &&
                                tile.DistanceTo(enemies.MinBy(creature => creature.DistanceTo(target))?.Occupies!) >
                                target.DistanceTo(enemies.MinBy(creature => creature.DistanceTo(target))!))
                            .ToList();
                        if (floodFill.Count == 0)
                        {
                            floodFill = Pathfinding.Floodfill(target, target.Battle,
                                    new PathfindingDescription()
                                    {
                                        Squares = 1
                                    })
                                .Where(tile =>
                                    tile.LooksFreeTo(target))
                                .ToList();
                        }

                        floodFill.ForEach(tile =>
                        {
                            if (moveAction == null || !(bool)moveAction.Target.CanBeginToUse(target)) return;
                            tileOptions.Add(moveAction.CreateUseOptionOn(tile)
                                .WithIllustration(moveAction.Illustration));
                        });
                        Option move = (await target.Battle.SendRequest(
                            new AdvancedRequest(target, "Choose where to step, you must move away from an enemy.",
                                tileOptions)
                            {
                                IsMainTurn = false,
                                IsStandardMovementRequest = true,
                                TopBarIcon = target.Illustration,
                                TopBarText = target.Name +
                                             " choose where to step, you must move away from an enemy."
                            })).ChosenOption;
                        switch (move)
                        {
                            case CancelOption:
                                i = 3;
                                break;
                            case TileOption tileOption:
                                await tileOption.Action();
                                if (!target.HasEffect(MQEffectIds.TacticResponse))
                                    target.AddQEffect(RespondedToTactic(caster));
                                break;
                        }
                    }
                }
            });
        return tactic;
    }
    private static CombatAction NavalTraining(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, IllustrationName.WaterWalk, "Naval Training",
                [MTraits.Tactic, Trait.Basic, MTraits.Commander],
                "Signal all squadmates; until the end of your next turn, all squadmates gain a swim Speed.",
                AllSquadmateTarget(owner))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnEachTarget((_, caster, target, _) =>
            {
                if (target.HasEffect(MQEffectIds.TacticResponse)) return Task.CompletedTask;
                target.AddQEffect(new QEffect("Naval Training", "You have a swim speed.",
                    ExpirationCondition.ExpiresAtEndOfSourcesTurn, owner, IllustrationName.WaterWalk)
                {
                    Id = QEffectId.Swimming,
                    CannotExpireThisTurn = true
                });
                target.AddQEffect(RespondedToTactic(caster));
                return Task.CompletedTask;
            });
        return tactic;
    }
    private static CombatAction PassageOfLines(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.PassageOfLines, "Passage of Lines",
                [Trait.Basic, MTraits.Tactic, MTraits.Commander],
                "Signal all squadmates within the aura of your commander's banner; each can swap positions with another willing ally adjacent to them.",
                AllSquadmateInBannerTarget(owner))
            .WithActionCost(1).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnEachTarget(async (_, caster, target, _) =>
            {
                CombatAction swap = new CombatAction(target, MIllustrations.PassageOfLines, "Swap",
                        [Trait.Basic, Trait.Move], "You switch places with an ally.", Target.AdjacentFriend()
                            .WithAdditionalConditionOnTargetCreature((creature, creature1) =>
                                CommonCombatActions.StepByStepStride(creature1).WithActionCost(0)
                                    .CanBeginToUse(creature1) && CommonCombatActions.StepByStepStride(creature)
                                                                  .WithActionCost(0).CanBeginToUse(creature)
                                                              && (creature1.Occupies.IsSolidGround ||
                                                                  creature.HasEffect(QEffectId.Flying)) &&
                                                              (creature.Occupies.IsSolidGround ||
                                                               creature1.HasEffect(QEffectId.Flying))
                                    ? Usability.Usable
                                    : Usability.NotUsableOnThisCreature("You must both be able to move.")))
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (spell, self, ally, _) =>
                    {
                        Tile selfStart = self.Occupies;
                        Tile allyStart = ally.Occupies;
                        await self.SingleTileMove(allyStart, spell);
                        await ally.SingleTileMove(selfStart, spell);
                    });
                List<Option> options = [new CancelOption(true)];
                GameLoop.AddDirectUsageOnCreatureOptions(swap, options);
                Option swapMove = (await target.Battle.SendRequest(new AdvancedRequest(target,
                    "Choose an adjacent creature to swap with.", options)
                {
                    TopBarIcon = target.Illustration,
                    IsMainTurn = false,
                    IsStandardMovementRequest = false,
                    TopBarText = target.Name + " choose who to swap with."
                })).ChosenOption;
                switch (swapMove)
                {
                    case CancelOption:
                        break;
                    case CreatureOption creatureOption:
                        await creatureOption.Action();
                        target.AddQEffect(RespondedToTactic(caster));
                        break;
                }
            });
        return tactic;
    }
    private static CombatAction ProtectiveScreen(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.ProtectiveScreen, "Protective Screen",
                [Trait.Basic, MTraits.Commander, MTraits.Tactic, MTraits.Brandish],
                "Signal one squadmate; as a reaction, that squadmate Strides directly toward any other squadmate who is within the aura of your banner. If the first squadmate ends their movement adjacent to that squadmate, that squadmate does not trigger reactions when casting spells or making ranged attacks until the end of their next turn or until they are no longer adjacent to the first squadmate, whichever comes first.",
                new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new BrandishRequirement(),
                        new FriendCreatureTargetingRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                        new ReactionRequirement()
                    ],
                    (_, _, _) => -2.14748365E+09f).WithAdditionalConditionOnTargetCreature((_, target) =>
                    CommonCombatActions.StepByStepStride(target).WithActionCost(0).CanBeginToUse(target)
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("This squadmate cannot move.")))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnEachTarget(async (spell, caster, target, _) =>
            {
                CombatAction screenStride = new CombatAction(target, MIllustrations.ProtectiveScreen,
                        "Screen Stride",
                        [Trait.Basic, Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName],
                        "Stride towards an ally, adds a buff to that ally if end adjacent.",
                        Target.RangedFriend(target.Speed).WithAdditionalConditionOnTargetCreature((self, ally) =>
                            new InBannerAuraRequirement().Satisfied(caster, ally) && IsSquadmate(caster, ally) &&
                            self != ally
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature(
                                    "Must be a squadmate in the commander's banner aura.")))
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (_, self, target1, _) =>
                    {
                        await self.StrideAsync("Stride towards a squadmate.", strideTowards: target1.Occupies);
                        if (self.IsAdjacentTo(target1))
                        {
                            target1.AddQEffect(new QEffect("Protective Screen",
                                "Your spells and ranged attacks do not provoke reactions until the end of your turn or until you are no longer adjacent to the original squadmate, whichever comes first.",
                                ExpirationCondition.ExpiresAtEndOfYourTurn, owner,
                                MIllustrations.ProtectiveScreen)
                            {
                                Id = MQEffectIds.ProtectiveScreenQf,
                                StateCheckWithVisibleChanges = effect =>
                                {
                                    if (!target1.IsAdjacentTo(self))
                                        effect.ExpiresAt = ExpirationCondition.Immediately;
                                    return Task.CompletedTask;
                                }
                            });
                        }
                    });
                List<Option> options = [new CancelOption(true)];
                GameLoop.AddDirectUsageOnCreatureOptions(screenStride, options);
                Option protStride = (await target.Battle.SendRequest(new AdvancedRequest(target,
                    "Choose which squadmate to stride to.", options)
                {
                    TopBarIcon = target.Illustration,
                    IsMainTurn = false,
                    IsStandardMovementRequest = false,
                    TopBarText = target.Name + " choose which squadmate to stride to."
                })).ChosenOption;
                switch (protStride)
                {
                    case CancelOption:
                        spell.RevertRequested = true;
                        break;
                    case CreatureOption creatureOption:
                        await creatureOption.Action();
                        bool useDrilledReactions =
                            caster.QEffects.All(qEffect => qEffect.Id != MQEffectIds.ExpendedDrilled);
                        if (useDrilledReactions)
                        {
                            caster.AddQEffect(DrilledReactionsExpended(caster));
                        }
                        target.AddQEffect(RespondedToTactic(caster));
                        if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                        {
                            target.Actions.UseUpReaction();
                        }
                        else if (target.HasEffect(MQEffectIds.AnimalReaction))
                        {
                            target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        }
                        break;
                }
            });
        return tactic;
    }
    private static CombatAction PincerAttack(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        List<Creature> includedAllies = [];
        CombatAction pincerAttack = new CombatAction(owner, MIllustrations.PincerAttack,
                "Pincer Attack", [MTraits.Commander, MTraits.Tactic],
                "Signal all squadmates; each can Step as a reaction. If any of your allies end this movement adjacent to an opponent, that opponent is off-guard to melee attacks from you and all other squadmates who responded to Pincer Attack until the start of your next turn.",
                squadmates.Any(cr => new ReactionRequirement().Satisfied(owner, cr))
                    ? AllSquadmateTarget(owner)
                    : Target.Uncastable("There must be at least one squadmate who can take a reaction."))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (caster, targets) =>
            {
                Creature? drilledTarget = targets.ChosenCreatures.Find(cr =>
                                              cr.HasEffect(MQEffectIds.DrilledTarget) &&
                                              !cr.HasEffect(MQEffectIds.AnimalReaction)) ??
                                          targets.ChosenCreatures.FirstOrDefault(cr =>
                                              !cr.HasEffect(MQEffectIds.AnimalReaction));
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions =
                        caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable || (
                            (useDrilledReactions || drilledTarget != target) && !target.Actions.CanTakeReaction() &&
                            !target.HasEffect(MQEffectIds.AnimalReaction)))
                    {
                        continue;
                    }

                    bool stepped = await target.StepAsync(target.Name + ": Pincer Attack Step", allowCancel: true);
                    if (stepped)
                    {
                        target.AddQEffect(RespondedToTactic(caster));
                        includedAllies.Add(target);
                        if (useDrilledReactions && drilledTarget?.Name == target.Name)
                        {
                            caster.AddQEffect(DrilledReactionsExpended(caster));
                        }
                        else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                        {
                            target.Actions.UseUpReaction();
                        }
                        else if (target.HasEffect(MQEffectIds.AnimalReaction))
                        {
                            target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        }

                        foreach (Creature creature in target.Occupies.Neighbours.Creatures)
                        {
                            if (creature.EnemyOf(target) && creature.QEffects.All(qEffect =>
                                    qEffect.Name != "Pincer Attack Vulnerability"))
                            {
                                creature.AddQEffect(new QEffect("Pincer Attack Vulnerability",
                                    "Off-guard to melee attacks from participating attackers.")
                                {
                                    Illustration = IllustrationName.Flatfooted,
                                    Tag = includedAllies,
                                    IsFlatFootedTo = (qEffect, attacker, combatAction) =>
                                    {
                                        List<Creature> includedAttackers = (List<Creature>)qEffect.Tag!;
                                        if (combatAction != null && combatAction.HasTrait(Trait.Attack) &&
                                            combatAction.HasTrait(Trait.Melee) && includedAttackers.Count > 0 &&
                                            attacker != null &&
                                            includedAttackers.Contains(attacker))
                                        {
                                            return "Pincer Attack";
                                        }

                                        return null;
                                    }
                                }.WithExpirationAtStartOfSourcesTurn(caster, 1));
                            }
                        }
                    }
                }
            });
        return pincerAttack;
    }
    private static CombatAction StrikeHard(Creature owner)
    {
        // We'll automatically use Drilled Reactions if available
        CombatAction strikeHard = new CombatAction(owner, MIllustrations.StrikeHard, "Strike Hard!",
                [MTraits.Brandish, MTraits.Tactic, MTraits.Commander],
                "Signal a squadmate within the aura of your commander's banner. That ally immediately attempts a Strike as a reaction.",
                new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new BrandishRequirement(), new InBannerAuraRequirement(),
                        new FriendCreatureTargetingRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                        new ReactionRequirement(), new CanMakeStrikeWithPrimary()
                    ],
                    (_, _, _) => -2.14748365E+09f))
            .WithActionCost(2)
            .WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnEachTarget(async delegate(CombatAction _, Creature caster, Creature target, CheckResult _)
            {
                bool useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Id != MQEffectIds.ExpendedDrilled);
                if (useDrilledReactions)
                {
                    caster.AddQEffect(DrilledReactionsExpended(caster));
                }

                target.AddQEffect(RespondedToTactic(caster));
                List<CombatAction> possibleStrikes = target.Weapons
                    .Select(item => CreateReactiveAttackFromWeapon(item, target))
                    .Where(atk => atk.CanBeginToUse(target)).ToList();
                if (possibleStrikes.Count == 1)
                {
                    await target.Battle.GameLoop.FullCast(possibleStrikes[0]);
                }
                else if (possibleStrikes.Count > 1)
                {
                    List<Option> options = [];
                    foreach (CombatAction possibleStrike in possibleStrikes)
                        GameLoop.AddDirectUsageOnCreatureOptions(possibleStrike, options);
                    await target.Battle.GameLoop.OfferOptions(target, options, true);
                }

                if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                {
                    target.Actions.UseUpReaction();
                }
                else if (target.HasEffect(MQEffectIds.AnimalReaction))
                {
                    target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                }
            });
        return strikeHard;
    }
    private static CombatAction CoordinatingManeuvers(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.CoordinatingManeuvers, "Coordinating Maneuvers",
                [MTraits.Tactic, MTraits.Brandish, MTraits.Commander, Trait.Basic],
                "Signal one squadmate within the aura of your banner; that squadmate can immediately Step as a free action. If they end this movement next to an opponent, they can attempt to Reposition that target as a reaction. Repositioning requires a free hand.",
                new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new BrandishRequirement(), new InBannerAuraRequirement(),
                        new FriendCreatureTargetingRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                        new CanTargetBeginToMoveRequirement()
                    ],
                    (_, _, _) => -2.14748365E+09f))
            .WithActionCost(1).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnEachTarget(async (spell, caster, target, _) =>
            {
                bool useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Id != MQEffectIds.ExpendedDrilled);
                var step = await target.StepAsync(
                    "Choose where to step, if you end your movement next to an opponent, you may attempt to Reposition the target as a reaction.",
                    true);
                if (step)
                {
                    target.AddQEffect(RespondedToTactic(caster));
                    CombatAction reposition = Reposition(target).WithActionCost(0);
                    reposition.Target = Target.AdjacentCreature()
                        .WithAdditionalConditionOnTargetCreature((self, target2) =>
                            self.HasFreeHand || self.HeldItems.Any(item => item.Name == target2.Name)
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature(
                                    "You must have a hand free or be grappling this creature."))
                        .WithAdditionalConditionOnTargetCreature((self, target2) =>
                            target2.EnemyOf(self)
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature(
                                    "Can only reposition enemies with coordinating maneuvers."));
                    List<Option> options = [new CancelOption(true)];
                    if (reposition.CanBeginToUse(target) && (target.Actions.CanTakeReaction() ||
                                                             target.HasEffect(MQEffectIds.AnimalReaction) ||
                                                             useDrilledReactions))
                    {
                        GameLoop.AddDirectUsageOnCreatureOptions(reposition, options);
                        Option repositionWho = (await target.Battle.SendRequest(new AdvancedRequest(target,
                            "Choose which enemy to reposition.", options)
                        {
                            TopBarIcon = target.Illustration,
                            IsMainTurn = false,
                            IsStandardMovementRequest = false,
                            TopBarText = target.Name + " choose which enemy to reposition."
                        })).ChosenOption;
                        switch (repositionWho)
                        {
                            case CancelOption:
                                break;
                            case CreatureOption creatureOption:
                                await creatureOption.Action();
                                if (useDrilledReactions)
                                {
                                    caster.AddQEffect(DrilledReactionsExpended(caster));
                                }

                                if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                                {
                                    target.Actions.UseUpReaction();
                                }
                                else if (target.HasEffect(MQEffectIds.AnimalReaction))
                                {
                                    target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                                }

                                break;
                        }
                    }
                }
                else
                {
                    spell.RevertRequested = true;
                }
            });
        return tactic;
    }
    private static CombatAction DoubleTeam(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.DoubleTeam, "Double Team",
                [Trait.Basic, MTraits.Commander, MTraits.Tactic],
                "Signal one squadmate who has an opponent within their reach. That ally can Shove or Reposition an opponent as a free action. If their maneuver is successful and the target ends their movement adjacent to a different squadmate, the second squadmate can attempt a melee Strike against that target as a reaction.",
                new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new FriendOrSelfCreatureTargetingRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement()
                    ],
                    (_, _, _) => -2.14748365E+09f).WithAdditionalConditionOnTargetCreature((_, target) =>
                {
                    CombatAction reposition = Reposition(target).WithActionCost(0);
                    reposition.Target = Target.AdjacentCreature()
                        .WithAdditionalConditionOnTargetCreature((self, target2) =>
                            self.HasFreeHand || self.HeldItems.Any(item => item.Name == target2.Name)
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature(
                                    "You must have a hand free or be grappling this creature."))
                        .WithAdditionalConditionOnTargetCreature((self, target2) =>
                            target2.EnemyOf(self)
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature("Can only reposition enemies with double team."));
                    Item shoveItem = target.UnarmedStrike;
                    if (target.HeldItems.Find(item => item.Traits.Contains(Trait.Shove)) is { } shoveWeapon)
                        shoveItem = shoveWeapon;
                    CombatAction shove = CombatManeuverPossibilities.CreateShoveAction(target, shoveItem)
                        .WithActionCost(0);
                    return shove.CanBeginToUse(target) ||
                           reposition.CanBeginToUse(target)
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("Must have a creature within their reach.");
                }))
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnEachTarget(async (spell, caster, target, _) =>
            {
                bool useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Id != MQEffectIds.ExpendedDrilled);
                QEffect reaction = new()
                {
                    AfterYouTakeAction = async (effect, action) =>
                    {
                        if (action.ActionId != ActionId.Shove && action.ActionId != MActionIds.Reposition) return;
                        if (action.CheckResult <= CheckResult.Failure)
                        {
                            effect.ExpiresAt = ExpirationCondition.Immediately;
                            return;
                        }

                        Creature ally = effect.Owner;
                        List<Creature> squadmates = owner.Battle.AllCreatures
                            .Where(cr => IsSquadmate(owner, cr) && cr != ally).ToList();
                        Creature enemy = action.ChosenTargets.ChosenCreature!;
                        foreach (Creature creature in squadmates.Where(mate =>
                                     mate.IsAdjacentTo(enemy) && mate.PrimaryWeapon != null))
                        {
                            if (new TacticResponseRequirement().Satisfied(caster, creature) != Usability.Usable)
                                continue;
                            if (new ReactionRequirement().Satisfied(caster, creature) != Usability.Usable) continue;
                            var confirm = await creature.AskForConfirmation(creature.Illustration,
                                "Do you wish to strike " + enemy.Name +
                                (useDrilledReactions ? "?" : " using a reaction?"), "Yes");
                            if (!confirm) continue;
                            if (useDrilledReactions)
                            {
                                caster.AddQEffect(DrilledReactionsExpended(caster));
                            }

                            creature.AddQEffect(RespondedToTactic(caster));
                            List<CombatAction> possibleStrikes = creature.MeleeWeapons
                                .Select(item => CreateReactiveAttackFromWeapon(item, creature))
                                .Where(atk => atk.CanBeginToUse(target)).ToList();
                            if (possibleStrikes.Count == 1)
                            {
                                await creature.Battle.GameLoop.FullCast(possibleStrikes[0],
                                    ChosenTargets.CreateSingleTarget(enemy));
                            }
                            else if (possibleStrikes.Count > 1)
                            {
                                List<Option> options = [];
                                foreach (CombatAction possibleStrike in possibleStrikes)
                                {
                                    possibleStrike.ChosenTargets = ChosenTargets.CreateSingleTarget(enemy);
                                    GameLoop.AddDirectUsageOnCreatureOptions(possibleStrike, options);
                                }
                                await creature.Battle.GameLoop.OfferOptions(target, options, true);
                            }

                            if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.Actions.UseUpReaction();
                            }
                            else if (target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                            }

                            effect.ExpiresAt = ExpirationCondition.Immediately;
                            return;
                        }

                        effect.ExpiresAt = ExpirationCondition.Immediately;
                    }
                };
                Item shoveItem = target.UnarmedStrike;
                if (target.HeldItems.Find(item => item.Traits.Contains(Trait.Shove)) is { } shoveWeapon)
                    shoveItem = shoveWeapon;
                CombatAction shove = CombatManeuverPossibilities.CreateShoveAction(target, shoveItem).WithActionCost(0);
                CombatAction reposition = Reposition(target).WithActionCost(0);
                reposition.Target = Target.AdjacentCreature().WithAdditionalConditionOnTargetCreature((self, target2) =>
                        self.HasFreeHand || self.HeldItems.Any(item => item.Name == target2.Name)
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature(
                                "You must have a hand free or be grappling this creature."))
                    .WithAdditionalConditionOnTargetCreature((self, target2) =>
                        target2.EnemyOf(self)
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature(
                                "Can only reposition enemies with coordinating maneuvers."));
                List<string> actionNames = [shove.Name, reposition.Name];
                List<CombatAction> actions = [shove, reposition];
                if (!shove.CanBeginToUse(target))
                {
                    actionNames.Remove(shove.Name);
                    actions.Remove(shove);
                }

                if (!reposition.CanBeginToUse(target))
                {
                    actionNames.Remove(reposition.Name);
                    actions.Remove(reposition);
                }

                if (actionNames.Count > 1)
                {
                    actionNames.Add("cancel");
                    ChoiceButtonOption result = await target.AskForChoiceAmongButtons(
                        target.Illustration,
                        "Choose to shove or reposition.", actionNames.ToArray());
                    if (actionNames[result.Index] != "cancel")
                    {
                        target.AddQEffect(reaction);
                        var combat = await target.Battle.GameLoop.FullCast(actions[result.Index]);
                        if (!combat)
                        {
                            spell.RevertRequested = true;
                            reaction.ExpiresAt = ExpirationCondition.Immediately;
                            return;
                        }

                        target.AddQEffect(RespondedToTactic(caster));
                    }
                    else
                    {
                        spell.RevertRequested = true;
                    }
                }
                else if (actionNames.Count == 1)
                {
                    target.AddQEffect(reaction);
                    var combat = await target.Battle.GameLoop.FullCast(actions[0]);
                    if (!combat)
                    {
                        spell.RevertRequested = true;
                        reaction.ExpiresAt = ExpirationCondition.Immediately;
                        return;
                    }

                    target.AddQEffect(RespondedToTactic(caster));
                }
            });
        return tactic;
    }
    private static CombatAction EndIt(Creature owner)
    {
        List<Creature> allies = owner.Battle.AllCreatures.Where(cr => cr.FriendOf(owner) && cr.Alive).ToList();
        List<Creature> enemies = owner.Battle.AllCreatures.Where(cr => cr.EnemyOf(owner) && cr.Alive).ToList();
        bool more = allies.Count > enemies.Count;
        bool died = owner.HasEffect(MQEffectIds.DeathCounter);
        CombatAction tactic = new CombatAction(owner, MIllustrations.EndIt, "End it!",
                [Trait.Basic, Trait.Incapacitation, MTraits.Commander, MTraits.Tactic, MTraits.Brandish],
                "If you and your allies outnumber all enemies on the battlefield, and you or a squadmate have reduced an enemy to 0 Hit Points since the start of your last turn, you may signal all squadmates within the aura of your banner; you and each ally can Step as a free action directly toward a hostile creature. Any hostile creatures within 10 feet of a squadmate after this movement must attempt a Will save against your class DC; on a failure they become fleeing for 1 round, and on a critical failure they become fleeing for 1 round and frightened 2. This is an emotion, fear, and mental effect.",
                more && died
                    ? AllSquadmateInBannerTarget(owner)
                    :
                    !more && died
                        ? Target.Uncastable("You do not outnumber your enemies.")
                        :
                        !died && more
                            ?
                            Target.Uncastable("An enemy hasn't been reduced to 0 hp since the start of your last turn.")
                            : Target.Uncastable(
                                "You do not outnumber your enemies and an enemy hasn't been reduced to 0 hp since the start of your last turn."))
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                var moved = false;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    IEnumerable<Creature> enemies2 =
                        target.Battle.AllCreatures.Where(cr => cr.EnemyOf(target) && cr.Alive);
                    Creature? choice = await target.Battle.AskToChooseACreature(target, enemies2,
                        target.Illustration,
                        "Choose a creature to step towards.", "enemy", "pass");
                    if (choice == null) continue;
                    await target.StrideAsync("End it!", true, true, choice.Occupies);
                    target.AddQEffect(RespondedToTactic(caster));
                    moved = true;
                }

                if (!moved) return;
                foreach (Creature enemy in enemies.Where(cr =>
                             targets.ChosenCreatures.Any(creature => cr.DistanceTo(creature) <= 2)))
                {
                    var dc = caster.ClassDC(MTraits.Commander);
                    if (enemy.IsImmuneTo(Trait.Mental) || enemy.IsImmuneTo(Trait.Fear) ||
                        enemy.IsImmuneTo(Trait.Emotion)) continue;
                    CheckResult savingThrow = CommonSpellEffects.RollSavingThrow(enemy, spell, Defense.Will, dc);
                    QEffect flee = QEffect.Fleeing(caster);
                    flee.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                    switch (savingThrow)
                    {
                        case CheckResult.Failure:
                            enemy.AddQEffect(flee);
                            break;
                        case CheckResult.CriticalFailure:
                            enemy.AddQEffect(flee);
                            enemy.AddQEffect(QEffect.Frightened(2));
                            break;
                        case CheckResult.Success:
                        case CheckResult.CriticalSuccess:
                            break;
                    }
                }
            });
        tactic.SpellLevel = 0;
        return tactic;
    }
    private static CombatAction Reload(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        CombatAction tactic = new CombatAction(owner, MIllustrations.Reload, "Reload!",
                [MTraits.Commander, MTraits.Tactic, Trait.Basic],
                "Signal all squadmates; each can immediately Interact to reload as a reaction.",
                squadmates.Any(cr =>
                    new ReactionRequirement().Satisfied(owner, cr) &&
                    cr.HeldItems.Any(item => item.EphemeralItemProperties.NeedsReload) &&
                    new TacticResponseRequirement().Satisfied(owner, cr) && !cr.HasEffect(QEffectId.RangersCompanion))
                    ? AllSquadmateInBannerTarget(owner)
                    : Target.Uncastable(
                        "There must be at least one squadmate who needs to reload and can take a reaction."))
            .WithActionCost(1).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (_, caster, targets) =>
            {
                Creature drilledTarget = targets.ChosenCreatures.Find(cr =>
                                             cr.HasEffect(MQEffectIds.DrilledTarget) &&
                                             !cr.HasEffect(MQEffectIds.AnimalReaction)) ??
                                         targets.ChosenCreatures.FirstOrDefault(cr =>
                                             !cr.HasEffect(QEffectId.RangersCompanion))!;
                foreach (Creature target in targets.ChosenCreatures.Where(cr =>
                             !cr.HasEffect(QEffectId.RangersCompanion)))
                {
                    bool useDrilledReactions =
                        caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    if (new ReactionRequirement().Satisfied(caster, target) != Usability.Usable) continue;
                    Item? tobeReloaded =
                        target.HeldItems.FirstOrDefault(item => item.EphemeralItemProperties.NeedsReload);
                    if (tobeReloaded == null) continue;
                    CombatAction reload = target.CreateReload(tobeReloaded).WithActionCost(0);
                    var confirm = await target.Battle.AskForConfirmation(target, target.Illustration,
                        "Reload " + tobeReloaded.Name + (useDrilledReactions && drilledTarget.Name == target.Name
                            ? "?"
                            : " using a reaction?"), "yes");
                    if (!confirm) continue;
                    await target.Battle.GameLoop.FullCast(reload);
                    target.AddQEffect(RespondedToTactic(caster));
                    if (useDrilledReactions && drilledTarget.Name == target.Name)
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                    }

                    if ((!useDrilledReactions || drilledTarget.Name != target.Name) &&
                        !target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                    }
                }
            });
        return tactic;
    }
    private static CombatAction ShieldsUp(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        CombatAction tactic = new CombatAction(owner, MIllustrations.ShieldsUp, "Shields up!",
                [MTraits.Commander, MTraits.Tactic, Trait.Basic],
                "Signal all squadmates within the aura of your commander’s banner; each can immediately Raise a Shield as a reaction. Squadmates who have a parry action (whether from a Parry weapon or a feat such as Dueling Parry or Twin Parry) may use that instead.\n\n{b}Special{/b} If one of your squadmates knows or has prepared the shield cantrip, they can cast it as a reaction instead of taking the actions normally granted by this tactic.",
                squadmates.Any(cr =>
                    new ReactionRequirement().Satisfied(owner, cr) &&
                    new TacticResponseRequirement().Satisfied(owner, cr) && !cr.HasEffect(QEffectId.RangersCompanion))
                    ? AllSquadmateInBannerTarget(owner)
                    : Target.Uncastable("There must be at least one squadmate who can take a reaction."))
            .WithActionCost(1).WithSoundEffect(SfxName.RaiseShield)
            .WithEffectOnChosenTargets(async (_, caster, targets) =>
            {
                Creature drilledTarget = targets.ChosenCreatures.Find(cr =>
                                             cr.HasEffect(MQEffectIds.DrilledTarget) &&
                                             !cr.HasEffect(MQEffectIds.AnimalReaction)) ??
                                         targets.ChosenCreatures.FirstOrDefault(cr =>
                                             !cr.HasEffect(QEffectId.RangersCompanion))!;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    if (target.HasEffect(QEffectId.RangersCompanion)) continue;
                    bool useDrilledReactions =
                        caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    if (new ReactionRequirement().Satisfied(caster, target) != Usability.Usable) continue;
                    CombatAction? raiseAShield = Possibilities.Create(target).Filter(ap =>
                    {
                        if (ap.CombatAction.ActionId != ActionId.RaiseShield ||
                            ap.CombatAction.Name == "Raise shield (Devoted Guardian)") return false;
                        ap.CombatAction.ActionCost = 0;
                        ap.RecalculateUsability();
                        return true;
                    }).CreateActions(true).FirstOrDefault() as CombatAction;
                    var parry = Possibilities.Create(target).Filter(ap =>
                    {
                        if (!ap.CombatAction.Name.Contains("Parry")) return false;
                        ap.CombatAction.ActionCost = 0;
                        ap.RecalculateUsability();
                        return true;
                    }).CreateActions(true).OrderBy(action => action.Action.Name.Length).LastOrDefault() as CombatAction;
                    CombatAction? castShield = Possibilities.Create(target).Filter(ap =>
                    {
                        if (ap.CombatAction is not { SpellId: SpellId.Shield }) return false;
                        if (ap.CombatAction is { PsychicAmpInformation.Amped: true }) return false;
                        ap.CombatAction.ActionCost = 0;
                        ap.RecalculateUsability();
                        return true;
                    }).CreateActions(true).FirstOrDefault() as CombatAction;
                    CombatAction? castShieldAmp = Possibilities.Create(target).Filter(ap =>
                    {
                        if (ap.CombatAction is not { SpellId: SpellId.Shield }) return false;
                        if (ap.CombatAction is { PsychicAmpInformation.Amped: false }) return false;
                        if (ap.CombatAction.PsychicAmpInformation == null) return false;
                        ap.CombatAction.ActionCost = 0;
                        ap.RecalculateUsability();
                        return true;
                    }).CreateActions(true).FirstOrDefault() as CombatAction;
                    List<CombatAction> shields = [];
                    List<string> names = [];
                    if (raiseAShield != null)
                    {
                        shields.Add(raiseAShield);
                        names.Add(raiseAShield.Name);
                    }

                    if (parry != null)
                    {
                        shields.Add(parry.Name is "Dueling Parry" or "Twin Parry"
                            ? parry
                            : CreateParryForReaction(target, parry));
                        names.Add(parry.Name);
                    }

                    if (castShield != null)
                    {
                        shields.Add(castShield);
                        names.Add(castShield.Name);
                    }

                    if (castShieldAmp != null)
                    {
                        shields.Add(castShieldAmp);
                        names.Add(castShieldAmp.Name);
                    }

                    if (names.Count == 0) continue;
                    names.Add("Cancel");
                    ChoiceButtonOption choice = await target.AskForChoiceAmongButtons(target.Illustration,
                        "Choose which defensive option to use." +
                        (useDrilledReactions && drilledTarget.Name == target.Name
                            ? ""
                            : " This will use a reaction."), names.ToArray());
                    if (names[choice.Index] == "Cancel") continue;
                    if (!await target.Battle.GameLoop.FullCast(shields[choice.Index])) continue;
                    {
                        // foreach (var action in shieldOptions.CreateActions(true))
                        // {
                        //     names.Add(action.Action.Name);
                        //     caster.Battle.Log(action.Action.Name);
                        // }
                        // string options = "";
                        // for (var index = 0; index < names.Count; index++)
                        // {
                        //     var name = names[index];
                        //     options += name + (index != names.Count - 1 ? ", " : "");
                        // }
                        // List<Option> actions = await target.Battle.GameLoop.CreateActions(target, shieldOptions, null);
                        // if (options == "") continue;
                        // var confirm = await target.Battle.AskForConfirmation(target, target.Illustration, "Do you wish to use "+options+(useDrilledReactions && drilledTarget.Name == target.Name ? "?" : " using a reaction?"), "yes");
                        // if (!confirm) continue;
                        // await target.Battle.GameLoop.OfferOptions(target, actions, true);
                    }
                    target.AddQEffect(RespondedToTactic(caster));
                    if (useDrilledReactions && drilledTarget.Name == target.Name)
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                    }

                    if (!useDrilledReactions || drilledTarget.Name != target.Name)
                    {
                        target.Actions.UseUpReaction();
                    }
                }
            });
        return tactic;
    }
    private static CombatAction TacticalTakedown(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.TacticalTakedown, "Tactical Takedown",
                [MTraits.Commander, MTraits.Tactic, Trait.Basic],
                "Signal up to two squadmates within the aura of your commander’s banner. Each of those allies can Stride up to half their Speed as a reaction. If they both end this movement adjacent to an enemy, that enemy must succeed at a Reflex save against your class DC or fall prone.",
                (Target.MultipleCreatureTargets(2, () =>
                {
                    return new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                        new FriendOrSelfCreatureTargetingRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                        new ReactionRequirement()
                    ], (_, _, _) => int.MinValue);
                }) as MultipleCreatureTargetsTarget)!.WithMinimumTargets(1).WithMustBeDistinct())
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                Creature? drilledTarget = targets.ChosenCreatures.Find(cr =>
                                              cr.HasEffect(MQEffectIds.DrilledTarget) &&
                                              !cr.HasEffect(MQEffectIds.AnimalReaction)) ??
                                          targets.ChosenCreatures.FirstOrDefault(cr =>
                                              !cr.HasEffect(MQEffectIds.AnimalReaction));
                int moved = 0;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions =
                        caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    if (!await target.StrideAsync(
                            target.Name + ": Choose where to stride" +
                            (useDrilledReactions && drilledTarget?.Name == target.Name ? "." : " as a reaction."),
                            maximumHalfSpeed: true, allowCancel: true)) continue;
                    target.AddQEffect(RespondedToTactic(caster));
                    ++moved;
                    if (useDrilledReactions && drilledTarget?.Name == target.Name)
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                    }
                    else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                    }
                }

                switch (moved)
                {
                    case 2:
                    {
                        Creature? enemy = caster.Battle.AllCreatures.FirstOrDefault(cr =>
                            cr.IsAdjacentTo(targets.ChosenCreatures[0]) && cr.IsAdjacentTo(targets.ChosenCreatures[1]));
                        if (enemy != null)
                        {
                            int dc = caster.ClassDC();
                            CheckResult savingThrow =
                                CommonSpellEffects.RollSavingThrow(enemy, spell, Defense.Reflex, dc);
                            if (savingThrow <= CheckResult.Failure)
                                enemy.AddQEffect(QEffect.Prone());
                        }

                        break;
                    }
                    case 0:
                        spell.RevertRequested = true;
                        break;
                }
            });
        return tactic;
    }
    private static CombatAction DemoralizingCharge(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.DemoralizingCharge, "Demoralizing Charge", 
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic], "Signal up to two squadmates within the aura of your commander’s banner; as a free action, those squadmates can immediately Stride toward an enemy they are observing. If they end this movement adjacent to an enemy, they can attempt to Strike that enemy as a reaction. For each of these Strikes that are successful, the target enemy must succeed at a Will save against your class DC or become frightened 1 (frightened 2 on a critical failure); this is an emotion, fear, and mental effect. If both Strikes target the same enemy, that enemy attempts the save only once after the final attack and takes a –1 circumstance penalty to their Will save to resist this effect (this penalty increases to –2 if both Strikes are successful or to –3 if both Strikes are successful and either is a critical hit).",
            (Target.MultipleCreatureTargets(2, () =>
            {
                return new CreatureTarget(RangeKind.Ranged,
                [
                    new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                    new FriendCreatureTargetingRequirement(), new CanTargetBeginToMoveRequirement(),
                    new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(), new BrandishRequirement()
                ], (_, _, _) => int.MinValue);
            }) as MultipleCreatureTargetsTarget)!.WithMinimumTargets(1).WithMustBeDistinct())
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                Creature? drilledTarget = DrilledTarget(targets);
                bool moved = false;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions =
                        caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    if (await target.Battle.AskToChooseACreature(target, target.Battle.AllCreatures.Where(cr => cr.EnemyOf(target) && cr.DistanceTo(target) <= target.Speed+1), target.Illustration, 
                            "Choose an enemy to stride towards, you should choose an enemy you can end adjacent to.", "", "pass") is not {} enemy) continue;
                    List<Option> tileOptions =
                    [
                        new CancelOption(true),
                    ];
                    CombatAction? moveAction = (target.Possibilities
                            .Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.Stride)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.RecalculateUsability();
                                return true;
                            })
                            .CreateActions(true)
                            .FirstOrDefault(pw => pw.Action.ActionId == ActionId.Stride) as CombatAction)
                        ?.WithActionCost(0);
                    List<Tile> floodFill = Pathfinding.Floodfill(target, target.Battle, new PathfindingDescription()
                        {
                            Squares = target.Speed
                        })
                        .Where(tile =>
                            (tile.LooksFreeTo(target) || tile.Equals(target.Occupies)) && tile.IsAdjacentTo(enemy.Space.CenterTile))
                        .ToList();
                    if (floodFill.Count == 0)
                    {
                        floodFill = Pathfinding.Floodfill(target, target.Battle, new PathfindingDescription()
                            {
                                Squares = 100
                            })
                            .Where(tile =>
                                tile.LooksFreeTo(target) && tile.IsAdjacentTo(enemy.Space.CenterTile))
                            .ToList();
                    }
                    floodFill.ForEach(tile =>
                    {
                        if (moveAction == null || !(bool)moveAction.Target.CanBeginToUse(target)) return;
                        tileOptions.Add(moveAction.CreateUseOptionOn(tile).WithIllustration(moveAction.Illustration));
                    });
                    Option move = (await target.Battle.SendRequest(
                        new AdvancedRequest(target,
                            "Choose a square adjacent to the enemy you selected.",
                            tileOptions)
                        {
                            IsMainTurn = false,
                            IsStandardMovementRequest = true,
                            TopBarIcon = target.Illustration,
                            TopBarText = target.Name +
                                         " choose a square adjacent to the enemy you selected."
                        })).ChosenOption;
                    if (move is CancelOption) continue;
                    Tile? tile = (move as TileOption)?.Tile;
                    if (tile == null) continue;
                    await target.StrideAsync("Demoralizing Charge", strideTowards: tile);
                    moved = true;
                    target.AddQEffect(RespondedToTactic(caster));
                    if (!target.IsAdjacentTo(enemy)) continue;
                    if (new ReactionRequirement().Satisfied(caster, target) != Usability.Usable) continue;
                    bool confirm = await target.AskForConfirmation(target.Illustration,
                        "Do you wish to strike " + enemy.Name +
                        (useDrilledReactions && drilledTarget == target ? "?" : " using a reaction?"), "Yes");
                    if (!confirm) continue;
                    CombatAction? bestStrike = DetermineBestMeleeStrike(target);
                    if (bestStrike == null) continue;
                    bestStrike.WithActionCost(0);
                    if (!bestStrike.CanBeginToUse(target)) continue;
                    CheckResult strike = await target.MakeStrike(bestStrike, enemy);
                    QEffect memory = new()
                    {
                        Tag = strike,
                        Source = target,
                        Id = MQEffectIds.DemoCharge
                    };
                    enemy.AddQEffect(memory);
                    if (useDrilledReactions && drilledTarget?.Name == target.Name)
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                    }
                    else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                    }
                }
                if (!moved)
                {
                    spell.RevertRequested = true;
                    return;
                }
                List<Creature> enemies = caster.Battle.AllCreatures.Where(cr => cr.HasEffect(MQEffectIds.DemoCharge)).ToList();
                switch (enemies.Count)
                {
                    case 0:
                        return;
                    case 1:
                        Creature enemy = enemies[0];
                        List<CheckResult> results = enemy.QEffects.Where(qf => qf.Id == MQEffectIds.DemoCharge).Select(qf =>
                            qf.Tag is CheckResult tag ? tag : CheckResult.Failure).ToList();
                        List<CheckResult> goodResults = results.Where(result => result >= CheckResult.Success).ToList();
                        int count = goodResults.Count;
                        var penalty = 0;
                        if (results.Count == 2)
                        {
                            penalty = count == 2 && results.Any(result => result == CheckResult.CriticalSuccess) ? 3 :
                                count == 2 ? 2 : 1;
                        }
                        QEffect penaltyQf = new()
                        {
                            BonusToDefenses = (_, _, defense) => defense == Defense.Will ? new Bonus(-penalty, BonusType.Circumstance, "Demoralizing Charge") : null
                        };
                        if (count < 1) break;
                        if (enemy.IsImmuneTo(Trait.Emotion) || enemy.IsImmuneTo(Trait.Fear) ||
                            enemy.IsImmuneTo(Trait.Mental)) break;
                        enemy.AddQEffect(penaltyQf);
                        CheckResult save = CommonSpellEffects.RollSavingThrow(enemy, spell, Defense.Will,
                            caster.ClassDC(MTraits.Commander));
                        if (save == CheckResult.Failure) enemy.AddQEffect(QEffect.Frightened(1));
                        else if (save == CheckResult.CriticalFailure) enemy.AddQEffect(QEffect.Frightened(2));
                        penaltyQf.ExpiresAt = ExpirationCondition.Immediately;
                        break;
                    case 2:
                        foreach (Creature enemy2 in enemies.Where(enemy2 => !enemy2.IsImmuneTo(Trait.Emotion) && !enemy2.IsImmuneTo(Trait.Fear) &&
                                                                            !enemy2.IsImmuneTo(Trait.Mental)))
                        {
                            List<CheckResult> results2 = enemy2.QEffects.Where(qf => qf.Id == MQEffectIds.DemoCharge).Select(qf =>
                                qf.Tag is CheckResult tag ? tag : CheckResult.Failure).ToList();
                            List<CheckResult> goodResults2 = results2.Where(result => result >= CheckResult.Success).ToList();
                            if (goodResults2.Count == 0) continue;
                            CheckResult save2 = CommonSpellEffects.RollSavingThrow(enemy2, spell, Defense.Will,
                                caster.ClassDC(MTraits.Commander));
                            if (save2 == CheckResult.Failure) enemy2.AddQEffect(QEffect.Frightened(1));
                            else if (save2 == CheckResult.CriticalFailure) enemy2.AddQEffect(QEffect.Frightened(2));
                        }
                        break;
                }
                foreach (Creature enemy in enemies.Where(cr => cr.Alive))
                {
                    enemy.RemoveAllQEffects(qff => qff.Id == MQEffectIds.DemoCharge);
                }
            });
        return tactic;
    }
    private static CombatAction BuckleCutBlitz(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.BuckleCutBlitz, "Buckle-cut Blitz",
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic], "Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Reflex save against your class DC or become clumsy 1 for 1 round (clumsy 2 on a critical failure).",
            (Target.MultipleCreatureTargets(2, () =>
            {
                return new CreatureTarget(RangeKind.Ranged,
                [
                    new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                    new FriendCreatureTargetingRequirement(), new CanTargetBeginToMoveRequirement(), new ReactionRequirement(),
                    new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(), new BrandishRequirement()
                ], (_, _, _) => int.MinValue);
            }) as MultipleCreatureTargetsTarget)!.WithMinimumTargets(1).WithMustBeDistinct())
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                Creature? drilledTarget = DrilledTarget(targets);
                int moved = 0;
                QEffect stateCheck = new()
                {
                    StateCheck = qf =>
                    {
                        Creature self = qf.Owner;
                        if (self.AnimationData.LongMovement == null) return;
                        if (!self.Battle.AllCreatures.Any(cr => cr.EnemyOf(self) && cr.IsAdjacentTo(self) && !cr.HasEffect(MQEffectIds.BuckleBlitz))) return;
                        foreach (Creature enemy in self.Battle.AllCreatures.Where(cr =>
                                     cr.EnemyOf(self) && cr.IsAdjacentTo(self) && !cr.HasEffect(MQEffectIds.BuckleBlitz)))
                        {
                                enemy.AddQEffect(new QEffect()
                                {
                                    Id = MQEffectIds.BuckleBlitz,
                                });
                                CheckResult save = CommonSpellEffects.RollSavingThrow(enemy, spell, Defense.Reflex,
                                    caster.ClassDC(MTraits.Commander));
                                if (save == CheckResult.CriticalFailure)
                                    enemy.AddQEffect(QEffect.Clumsy(2).WithExpirationAtStartOfSourcesTurn(caster, 1));
                                else if (save == CheckResult.Failure)
                                    enemy.AddQEffect(QEffect.Clumsy(1).WithExpirationAtStartOfSourcesTurn(caster, 1));
                        }
                    }
                };
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    target.AddQEffect(stateCheck);
                    CombatAction moveAction = CommonCombatActions.StepByStepStride(target).WithActionCost(0);
                    bool move = await target.Battle.GameLoop.FullCast(moveAction);
                    switch (move)
                    {
                        case false:
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            continue;
                        case true:
                        {
                            ++moved;
                            target.AddQEffect(RespondedToTactic(caster));
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            if (useDrilledReactions && drilledTarget?.Name == target.Name)
                            {
                                caster.AddQEffect(DrilledReactionsExpended(caster));
                            }
                            else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.Actions.UseUpReaction();
                            }
                            else if (target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                            }
                            break;
                        }
                    }
                }
                foreach (Creature bad in caster.Battle.AllCreatures.Where(cr => cr.HasEffect(MQEffectIds.BuckleBlitz)))
                {
                    bad.RemoveAllQEffects(qff => qff.Id == MQEffectIds.BuckleBlitz);
                }
                if (moved == 0)
                    spell.RevertRequested = true;
            });
        return tactic;
    }
    private static CombatAction StupefyingRaid(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.StupefyingRaid, "Stupefying Raid",
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic], "Your team dashes about in a series of maneuvers that leave the enemy befuddled. Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Will save against your class DC or become stupefied 1 for 1 round (stupefied 2 on a critical failure); this is a mental effect.",
            (Target.MultipleCreatureTargets(2, () =>
            {
                return new CreatureTarget(RangeKind.Ranged,
                [
                    new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                    new FriendCreatureTargetingRequirement(), new CanTargetBeginToMoveRequirement(), new ReactionRequirement(),
                    new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(), new BrandishRequirement()
                ], (_, _, _) => int.MinValue);
            }) as MultipleCreatureTargetsTarget)!.WithMinimumTargets(1).WithMustBeDistinct())
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                Creature? drilledTarget = DrilledTarget(targets);
                var moved = 0;
                QEffect stateCheck = new()
                {
                    StateCheck = qf =>
                    {
                        Creature self = qf.Owner;
                        if (self.AnimationData.LongMovement == null) return;
                        if (!self.Battle.AllCreatures.Any(cr => cr.EnemyOf(self) && cr.IsAdjacentTo(self) && !cr.HasEffect(MQEffectIds.StupefyingRaid))) return;
                        foreach (Creature enemy in self.Battle.AllCreatures.Where(cr =>
                                     cr.EnemyOf(self) && cr.IsAdjacentTo(self) && !cr.HasEffect(MQEffectIds.StupefyingRaid) && !cr.IsImmuneTo(Trait.Mental)))
                        {
                                enemy.AddQEffect(new QEffect()
                                {
                                    Id = MQEffectIds.StupefyingRaid
                                });
                                CheckResult save = CommonSpellEffects.RollSavingThrow(enemy, spell, Defense.Will,
                                    caster.ClassDC(MTraits.Commander));
                                if (save == CheckResult.CriticalFailure)
                                    enemy.AddQEffect(QEffect.Stupefied(2).WithExpirationAtStartOfSourcesTurn(caster, 1));
                                else if (save == CheckResult.Failure)
                                    enemy.AddQEffect(QEffect.Stupefied(1).WithExpirationAtStartOfSourcesTurn(caster, 1));
                        }
                    }
                };
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                    target.AddQEffect(stateCheck);
                    CombatAction moveAction = CommonCombatActions.StepByStepStride(target).WithActionCost(0);
                    bool move = await target.Battle.GameLoop.FullCast(moveAction);
                    switch (move)
                    {
                        case false:
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            continue;
                        case true:
                        {
                            ++moved;
                            target.AddQEffect(RespondedToTactic(caster));
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            if (useDrilledReactions && drilledTarget?.Name == target.Name)
                            {
                                caster.AddQEffect(DrilledReactionsExpended(caster));
                            }
                            else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.Actions.UseUpReaction();
                            }
                            else if (target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                            }
                            break;
                        }
                    }
                }
                foreach (Creature bad in caster.Battle.AllCreatures.Where(cr => cr.HasEffect(MQEffectIds.StupefyingRaid)))
                {
                    bad.RemoveAllQEffects(qff => qff.Id == MQEffectIds.StupefyingRaid);
                }
                if (moved == 0)
                    spell.RevertRequested = true;
            });
        return tactic;
    }
    private static CombatAction SlipAndSizzle(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.SlipAndSizzle, "Slip and Sizzle",
            [MTraits.Commander, MTraits.Tactic], "Signal two squadmates within the aura of your commander’s banner; one of these squadmates must be adjacent to an opponent and the other must be capable of casting a spell that deals damage. The first squadmate can attempt to Trip the adjacent opponent as a reaction. If this Trip is successful, the second squadmate can cast a ranged spell that deals damage and takes 2 or fewer actions to cast. This spell is cast as a reaction and must either target the tripped opponent or include the tripped opponent in the spell’s area.\n\nIf the second squadmate cast a spell using slots or Focus Points as part of this tactic, they are slowed 1 until the end of their next turn and do not gain a reaction when they regain actions at the start of their next turn." +
                                                 "\n{b}Note{/b} Spells with variants, for example: Magic Missile or Scorching Ray, cannot be cast at this time.",
            Target.MultipleCreatureTargets(new CreatureTarget(RangeKind.Ranged, [new SquadmateTargetRequirement(), new FriendOrSelfCreatureTargetingRequirement(), new InBannerAuraRequirement(), new ReactionRequirement(), new TacticResponseRequirement(), new UnblockedLineOfEffectCreatureTargetingRequirement(), new CanTargetTripAndIsAdjacentRequirement()], (_, _, _) => int.MinValue), 
                new CreatureTarget(RangeKind.Ranged, [new SquadmateTargetRequirement(), new FriendOrSelfCreatureTargetingRequirement(), new InBannerAuraRequirement(), new ReactionRequirement(), new TacticResponseRequirement(), new UnblockedLineOfEffectCreatureTargetingRequirement(), new CanTargetCastDamageSpell()], (_, _, _) => int.MinValue)).WithMustBeDistinct().WithMinimumTargets(2))
            .WithActionCost(2).WithSoundEffect(SfxName.Trip).WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                Creature? drilledTarget = DrilledTarget(targets);
                bool useDrilledReactions = caster.QEffects.All(qEffect => qEffect.Name != "Drilled Reactions Expended");
                Creature tripper = targets.ChosenCreatures[0];
                Creature mage = targets.ChosenCreatures[1];
                bool usedDrill = false;
                bool lostReaction = false;
                bool animalReact = false;
                CombatAction trip = CombatManeuverPossibilities.CreateTripAction(tripper,
                        tripper.MeleeWeapons.FirstOrDefault(item => item.HasTrait(Trait.Trip)) ?? tripper.UnarmedStrike)
                    .WithActionCost(0);
                trip.Target = Target.AdjacentCreature().WithAdditionalConditionOnTargetCreature((_, cr) =>
                {
                    if (!cr.EnemyOf(tripper))
                        return Usability.NotUsableOnThisCreature("Can only be used on enemies.");
                    return !cr.HasEffect(QEffectId.Prone) &&
                           !cr.HasEffect(QEffect.ImmunityToCondition(QEffectId.Prone))
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("This creature is already prone or immune to prone.");
                });
                if (useDrilledReactions && drilledTarget?.Name == tripper.Name)
                {
                    caster.AddQEffect(DrilledReactionsExpended(caster));
                    usedDrill = true;
                }
                else if (!tripper.HasEffect(MQEffectIds.AnimalReaction))
                {
                    tripper.Actions.UseUpReaction();
                    lostReaction = true;
                }
                else if (tripper.HasEffect(MQEffectIds.AnimalReaction))
                {
                    tripper.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                    animalReact = true;
                }
                CheckResult result = await tripper.Battle.GameLoop.FullCast(trip) ? trip.CheckResult : CheckResult.Failure;
                Creature? which = trip.ChosenTargets.ChosenCreature;
                if (which == null)
                {
                    spell.RevertRequested = true;
                    if (usedDrill)
                        caster.RemoveAllQEffects(qf => qf.Id == MQEffectIds.ExpendedDrilled);
                    if (lostReaction)
                        tripper.Actions.RefundReaction();
                    if (animalReact)
                        tripper.AddQEffect(AnimalReaction(caster));
                    return;
                }
                tripper.AddQEffect(RespondedToTactic(caster));
                if (result <= CheckResult.Failure) return;
                Possibilities spells = CreateSpells(mage);
                bool usedDrilled2 = false;
                bool usedReaction2 = false;
                bool choice = await mage.AskForConfirmation(mage.Illustration, "Do you wish to cast a damaging spell, which must include " +
                                                                               which.Name +
                                                                               (drilledTarget == mage && useDrilledReactions
                                                                                   ? "? If you cast a focus spell or leveled spell, you will be slowed 1 until the end of your next turn and you do not gain a reaction at the start of your next turn."
                                                                                   : " as a reaction? If you cast a focus spell or leveled spell, you will be slowed 1 until the end of your next turn and you do not gain a reaction at the start of your next turn."), "Yes");
                if (!choice) return;
                List<CombatAction> actions = [];
                actions.AddRange(spells.CreateActions(true).Select(action => action.Action));
                CombatAction[] array =  actions.ToArray();
                RequestResult requestResult = await mage.Battle.SendRequest(new ComboBoxInputRequest<CombatAction>(mage, "What spell to cast?", mage.Illustration, "Fulltext search...", array, item => new ComboBoxInformation(item.Illustration, item.Name, item.Description, item.SpellId.ToStringOrTechnical()), item => $"Cast {{i}}{item.Name.ToLower()}{{/i}}", "Cancel"));
                switch (requestResult.ChosenOption)
                {
                    case CancelOption:
                        return;
                    case ComboBoxInputOption<CombatAction> chosenOption2:
                    {
                        mage.AddQEffect(RespondedToTactic(caster));
                        if (useDrilledReactions && drilledTarget?.Name == mage.Name)
                        {
                            caster.AddQEffect(DrilledReactionsExpended(caster));
                            usedDrilled2 = true;
                        }
                        else
                        {
                            mage.Actions.UseUpReaction();
                            usedReaction2 = true;
                        }
                        QEffect forcedChoice = new()
                        {
                            YouBeginAction = async (effect, action) =>
                            {
                                if (!spells.CreateActions(true).Contains(action)) return;
                                if (!action.Targets(which))
                                {
                                    if (!action.Target.IsAreaTarget &&
                                        action.Target is not MultipleCreatureTargetsTarget)
                                    {
                                        action.ChosenTargets = ChosenTargets.CreateSingleTarget(which);
                                    }
                                    else
                                    {
                                        if (await mage.AskForConfirmation(mage.Illustration,
                                                "Your spell did not target " + which.Name +
                                                " would you like to cast the spell again?", "Yes", "No"))
                                        {
                                            action.RevertRequested = true;
                                            await mage.Battle.GameLoop.FullCast(action);
                                            action.Disrupted = true;
                                        }
                                        else
                                        {
                                            action.RevertRequested = true;
                                            if (usedDrilled2)
                                                caster.RemoveAllQEffects(qf => qf.Id == MQEffectIds.ExpendedDrilled);
                                            if (usedReaction2)
                                                tripper.Actions.RefundReaction();
                                            action.Disrupted = true;
                                            effect.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    }
                                }
                            },
                            AfterYouTakeAction = (effect, action) =>
                            {
                                if (!spells.CreateActions(true).Contains(action) || action.RevertRequested)
                                    return Task.CompletedTask;
                                if (!action.HasTrait(Trait.Cantrip) ||
                                    action.SpellInformation?.PsychicAmpInformation?.Amped == true)
                                {
                                    mage.AddQEffect(QEffect.Slowed(1).WithExpirationAtEndOfOwnerTurn()
                                        .WithCannotExpireThisTurn());
                                    mage.AddQEffect(new QEffect()
                                    {
                                        StartOfYourPrimaryTurn = (qEffect, _) =>
                                        {
                                            mage.Actions.UseUpReaction();
                                            qEffect.ExpiresAt = ExpirationCondition.Immediately;
                                            return Task.CompletedTask;
                                        }
                                    });
                                }

                                effect.ExpiresAt = ExpirationCondition.Immediately;
                                return Task.CompletedTask;
                            },
                            ExpiresAt = ExpirationCondition.ExpiresAtEndOfSourcesTurn,
                            Source = caster
                        };
                        mage.AddQEffect(forcedChoice);
                        CombatAction action = chosenOption2.SelectedObject;
                        action.SpentActions = 2;
                        if (!await caster.Battle.GameLoop.FullCast(action))
                        {
                            Sfxs.Play(SfxName.Unallowed);
                            forcedChoice.ExpiresAt = ExpirationCondition.Immediately;
                        }
                        break;
                    }
                }
            });
        return tactic;
    }
    
    #endregion

    private static Creature? DrilledTarget(ChosenTargets targets)
    {
        return targets.ChosenCreatures.Find(cr => cr.HasEffect(MQEffectIds.DrilledTarget) && !cr.HasEffect(MQEffectIds.AnimalReaction)) ??
               targets.ChosenCreatures.FirstOrDefault(cr => !cr.HasEffect(MQEffectIds.AnimalReaction));
    }
     private static Possibilities CreateSpells(Creature target)
     {
         return target.Possibilities.Filter(ap =>
         {
             if (ap.CombatAction.SpellcastingSource == null || ap.CombatAction.ActionCost > 2) return false;
             if ((!ap.CombatAction.Description.ContainsIgnoreCase("deal") &&
                  !ap.CombatAction.Description.ContainsIgnoreCase("attack") &&
                  !ap.CombatAction.Description.ContainsIgnoreCase("take") &&
                  !ap.CombatAction.Description.ContainsIgnoreCase("damage")) ||
                 ap.CombatAction.Description.ContainsIgnoreCase("battleform"))
                 return false;
             if (!ap.CombatAction.WillBecomeHostileAction) return false;
             if (ap.CombatAction.Description.Contains("{b}Range{/b} touch") ||
                 (!ap.CombatAction.Description.Contains("Range") &&
                  !ap.CombatAction.Description.Contains("Area") &&
                  !ap.CombatAction.Description.Contains("line"))) return false;
             if (!ap.CombatAction.Description.Contains("d4") && !ap.CombatAction.Description.Contains("d6") &&
                 !ap.CombatAction.Description.Contains("d8") && !ap.CombatAction.Description.Contains("d10") &&
                 !ap.CombatAction.Description.Contains("d12") &&
                 !ap.CombatAction.Description.Contains("Deal 40")) return false;
             ap.CombatAction.ActionCost = 0;
             ap.RecalculateUsability();
             return true;
         });
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
            // add try parses and feat checks
            var amount = 1;
            if (action.Name == "Parry Fist" &&
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
                        bool observed = target.FindQEffect(MQEffectIds.Observed)?.Source == caster;
                        QEffect crit = new((observed ? "Observational Analysis" : "Combat Assessment") + " (Critical Success)",
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
                    qfThis.Owner.RecalculateLandSpeedAndInitiative();
                    if (qfThis.Owner.BaseArmor is not null || qfThis.Tag is not Item
                        {
                            ArmorProperties: not null
                        } tagItem) return Task.CompletedTask;
                    qfThis.Owner.BaseArmor = tagItem;
                    if (qfThis.Owner.FindQEffect(QEffectId.SpeakAboutMissingArmor) is { } s2E3Armor)
                        s2E3Armor.StartOfYourPrimaryTurn = async (effect, self) =>
                        {
                            if (!self.Actions.CanTakeActions())
                                return;
                            effect.ExpiresAt = ExpirationCondition.Immediately;
                            await self.Battle.Cinematics.ShowQuickBubble(
                                self,
                                "It's a good thing I can sleep in my armor. Now to just pick up my weapons.",
                                null);
                        };
                    return Task.CompletedTask;
                };
                qfFeat.Id = MQEffectIds.ArmorRegiment;
            });
    }
    private static void PlantBannerLogic(TrueFeat feat)
    {
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            CombatAction plantBanner = new CombatAction(qf.Owner, MIllustrations.PlantBanner,
                    "Plant Banner", [MTraits.Commander, Trait.Basic, Trait.Manipulate],
                    "Plant your banner in a corner of your square. Each ally within a 30-foot burst centered on your banner immediately gains 4 temporary Hit Points, plus an additional 4 temporary Hit Points at 4th level and every 4 levels thereafter. " +
                    "These temporary Hit Points last for 1 round; each time an ally starts their turn within the burst, their temporary Hit Points are renewed for another round. " +
                    "If your banner is attached to a weapon, you cannot wield that weapon while your banner is planted. While your banner is planted, the emanation around your banner is a 35 foot emanation.",
                    Target.Burst(1, 6).WithAdditionalRequirementOnCaster(cr =>
                            (cr.HasFreeHand || cr.HeldItems.Any(item => item.HasTrait(MTraits.Banner))) &&
                            cr.HasEffect(MQEffectIds.Banner)
                                ? Usability.Usable
                                : Usability.NotUsable(
                                    "You must be carrying a banner in your hands or have a free hand to use plant banner."))
                        .WithIncludeOnlyIf((_, cr) => cr.FriendOf(qf.Owner)))
                .WithActionCost(1).WithSoundEffect(Sfx.Drums)
                .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                {
                    Item? banner = caster.HeldItems.FirstOrDefault(item => item.Traits.Contains(MTraits.Banner)) ??
                                   caster.CarriedItems.FirstOrDefault(item =>
                                       item.HasTrait(MTraits.Banner));
                    Creature illusory = Creature.CreateIndestructibleObject(IllustrationName.None,
                        "Banner", caster.Level).With(cr =>
                    {
                        cr.Traits.Add(MTraits.Banner);
                        AuraAnimation auraAnimation = cr.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle,
                            GetBannerRadius(caster));
                    });
                    QEffect? radius = caster.FindQEffect(MQEffectIds.BannerRadius);
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
                                            tQ.TileQEffectId == MTileQEffectIds.Banner);
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
                            return bannerTile.IsAdjacentTo(ef.Owner.Occupies) || bannerTile.Equals(ef.Owner.Occupies)
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
                                            cr.HasFreeHand ? null : "You must have a free hand to steal the banner."))
                                    .WithActionCost(1).WithGoodness((_, cr, _) => cr.HasEffect(QEffect.Mindless()) ? int.MinValue : cr.Level * 6)
                                    .WithEffectOnChosenTargets(async (cr, _) =>
                                    {
                                        Item? bannerItem = planted.Tag as Item;
                                        TileQEffect? tileQEffect =
                                            bannerTile.TileQEffects.FirstOrDefault(tQ =>
                                                tQ.TileQEffectId == MTileQEffectIds.Banner);
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
                                                                        CombatAction returnBanner = new CombatAction(
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
                                                                                            GetBannerRadius(creature));
                                                                                animation.Color = Color.Coral;
                                                                                QEffect commandersBannerEffect =
                                                                                    CommandersBannerEffect(animation,
                                                                                        GetBannerRadius(creature),
                                                                                        creature);
                                                                                creature.AddQEffect(
                                                                                    commandersBannerEffect);
                                                                                qfTile.ExpiresAt =
                                                                                    ExpirationCondition.Immediately;
                                                                                return Task.CompletedTask;
                                                                            });
                                                                        return new ActionPossibility(returnBanner);
                                                                    }
                                                                });
                                                        },
                                                        Illustration = MIllustrations.Banner
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
                    caster.RemoveAllQEffects(effect => effect.Id == MQEffectIds.Banner);
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
                                (list.Contains(MTraits.BasicTactic) || list.Contains(MTraits.ExpertTactic)))
                            .ToDictionary();
                        preparedTactics.AddRange(tags.Keys);
                        potentialTactics.AddRange(calculated.AllFeatGrants
                            .Where(grant =>
                                grant.GrantedFeat.HasTrait(MTraits.TacticPre))
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
                        QEffect? tacticQf = TacticsQFs()
                            .FirstOrDefault(qff => (FeatName?)qff.Tag is { } here && here == tactic);
                        if (tacticQf != null) self.AddQEffect(tacticQf);
                    }
                };
            });
    }
    private static void DefensiveSwapLogic(TrueFeat feat)
    {
        feat.WithActionCost(-2).WithPermanentQEffect("When you or an adjacent ally are the target of an attack, you may use a reaction to immediately swap positions with each other, and whichever of you was not the target of the triggering attack becomes the target instead.", qf =>
        {
            Creature self = qf.Owner;
            qf.AddGrantingOfTechnical(cr => cr.EnemyOf(self), qfTech =>
            {
                qfTech.YouBeginAction = async (_, action) =>
                {
                    if (!action.HasTrait(Trait.Attack) || action.ChosenTargets.ChosenCreatures.Count != 1) return;
                    if (action.ChosenTargets.ChosenCreature is { } ally && ally.FriendOfAndNotSelf(self) &&
                        ally.IsAdjacentTo(self)
                        && CommonCombatActions.StepByStepStride(ally).WithActionCost(0).CanBeginToUse(ally) &&
                        CommonCombatActions.StepByStepStride(self).WithActionCost(0).CanBeginToUse(self))
                    {
                        bool confirm = await self.AskToUseReaction(
                            "Do you wish to use a reaction to swap positions with {Green}" + ally.Name +
                            "{/Green} and become the target of {b}" + action.Name + "{/b} from {Red}" + action.Owner+"{/Red}.");
                        if (confirm)
                        {
                            Tile selfStart = self.Occupies;
                            Tile allyStart = ally.Occupies;
                            await self.SingleTileMove(allyStart, null);
                            await ally.SingleTileMove(selfStart, null);
                            action.ChosenTargets = ChosenTargets.CreateSingleTarget(self);
                            self.Overhead("Defensive Swap", Color.Black, self + " uses {b}Defensive Swap{/b}",
                                "Defensive Swap {icon:Reaction}", qf.Description,
                                new Traits([MTraits.Commander]));
                        }
                    }
                    if (action.ChosenTargets.ChosenCreature == self &&
                        CommonCombatActions.StepByStepStride(self).WithActionCost(0).CanBeginToUse(self)
                        && self.Battle.AllCreatures.Any(friend => friend.FriendOfAndNotSelf(self) &&
                            friend.IsAdjacentTo(self) && CommonCombatActions.StepByStepStride(friend).WithActionCost(0)
                                .CanBeginToUse(friend)))
                    {
                        bool confirm = await self.AskToUseReaction(
                            "Do you wish to use a reaction to swap positions with an adjacent ally and cause them to become the target of {b}" +
                            action.Name + "{/b} from {Red}" + action.Owner+"{/Red}.");
                        if (confirm)
                        {
                            Creature? friend = null;
                            IEnumerable<Creature?> allies = self.Battle.AllCreatures.Where(creature => creature.FriendOfAndNotSelf(self) &&
                                creature.IsAdjacentTo(self) && CommonCombatActions.StepByStepStride(creature)
                                    .WithActionCost(0)
                                    .CanBeginToUse(creature));
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
                                    "Defensive Swap {icon:Reaction}", qf.Description, new Traits([MTraits.Commander]));
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
                guidingShot.Traits.Add(MTraits.Commander);
                guidingShot.WithEffectOnEachTarget((shot, caster, target, result) =>
                {
                    int amount = result == CheckResult.CriticalSuccess ? 2 : 1;
                    if (result < CheckResult.Success) return Task.CompletedTask;
                    QEffect guide = new("Guiding Shot", "The next attack made against this creature by anyone other than "+self.Name+" will have a +"+amount+" circumstance bonus to hit.", ExpirationCondition.ExpiresAtStartOfSourcesTurn, self, IllustrationName.TrueStrike)
                    {
                        AfterYouAreTargeted = (effect, action) =>
                        {
                            if (!action.HasTrait(Trait.Attack) || action == shot || action.Owner == caster) return Task.CompletedTask;
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
                guidingShot.Description = StrikeRules.CreateBasicStrikeDescription4(guidingShot.StrikeModifiers, additionalSuccessText: "The next creature other than you to attack the same target before the start of your next turn gains a +1 circumstance bonus to their roll, or a +2 circumstance bonus if your Strike was a critical hit.");
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
                setupStrike.Illustration = new SideBySideIllustration(item.Illustration, IllustrationName.BigFlatfooted);
                setupStrike.Traits.Add(Trait.Flourish);
                setupStrike.Traits.Add(MTraits.Commander);
                setupStrike.WithEffectOnEachTarget((strike, caster, target, result) =>
                {
                    if (result < CheckResult.Success) return Task.CompletedTask;
                    QEffect setup = new("Set-up Strike", "This creature will be off guard against the next attack made by allies of "+caster.Name+".", ExpirationCondition.ExpiresAtStartOfSourcesTurn, self, IllustrationName.Flatfooted)
                    {
                        AfterYouAreTargeted = (effect, action) =>
                        {
                            if (!action.HasTrait(Trait.Attack) || action == strike || action.Owner == caster) return Task.CompletedTask;
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
                setupStrike.Description = StrikeRules.CreateBasicStrikeDescription4(setupStrike.StrikeModifiers, additionalSuccessText: "The target is off guard against the next attack that one of your allies attempts against it before the start of your next turn.");
                return setupStrike;
            };
        });
    }
    private static void RapidAssessmentLogic(TrueFeat feat)
    {
        feat.WithActionCost(0).WithPermanentQEffect($"Attempt a check to {UseCreatedTooltip("Recall Weakness")} against one creature you are observing.", effect =>
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
                    if (self.HasFeat(MFeatNames.UnrivaledAnalysis))
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
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            qf.ProvideMainAction = effect =>
            {
                CombatAction twirl = new CombatAction(effect.Owner, MIllustrations.BannerTwirl,
                    "Banner Twirl",
                    [MTraits.Brandish, MTraits.Commander, Trait.Manipulate],
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
                                if (qEffect.Owner.QEffects.FirstOrDefault(qff => qff.Name == concealment.Name) is {} conceal)
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
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            qf.ProvideMainAction = effect =>
            {
                CombatAction inspiration = new CombatAction(effect.Owner, MIllustrations.InspiringBanner, "Banner's Inspiration",
                    [MTraits.Brandish, MTraits.Commander, Trait.Emotion, Trait.Flourish, Trait.Mental, Trait.Visual],
                    "Each ally in your banner's aura reduces their frightened and stupefied conditions by 1, and can make a Will save against a standard level-based DC for your level, and on a success or better remove the Confused or Paralyzed condition. Regardless of the result, any ally that attempts this save is temporarily immune to Banner's Inspiration for 10 minutes.",
                    new EmanationTarget(100, false).WithAdditionalRequirementOnCaster(cr => new BrandishRequirement().Satisfied(cr, cr)).WithIncludeOnlyIf((_, creature) => new InBannerAuraRequirement().Satisfied(effect.Owner, creature)))
                    .WithActionCost(1).WithSoundEffect(SfxName.Drum).WithActionId(MActionIds.BannersInspiration).WithEffectOnEachTarget((spell, caster, target, _) =>
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
                        if (!target.HasEffect(QEffectId.Confused) && !target.HasEffect(QEffectId.Paralyzed)) return Task.CompletedTask;
                        CheckResult save = CommonSpellEffects.RollSavingThrow(target, spell, Defense.Will,
                            Checks.LevelBasedDC(caster.Level));
                        if (save >= CheckResult.Success)
                        {
                            QEffectId? toRemove = target.QEffects.FirstOrDefault(qff => qff.Id is QEffectId.Confused or QEffectId.Paralyzed)?.Id;
                            target.RemoveAllQEffects(qff => qff.Id == toRemove);
                            
                        }
                        target.AddQEffect(QEffect.ImmunityToTargeting(MActionIds.BannersInspiration));
                        return Task.CompletedTask;

                    });
                return new ActionPossibility(inspiration).WithPossibilityGroup("Abilities");
            };

        });
    }
    private static void ObservationalAnalysisLogic(TrueFeat feat)
    {
        feat.WithPrerequisite(MFeatNames.CombatAssessment, "Combat Assessment").WithPermanentQEffect(
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
                                        Id = MQEffectIds.Observed,
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
                unsteady.Traits.Add(MTraits.Commander);
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
                                  action.ActionId == MActionIds.Reposition) && defense == Defense.Fortitude) ||
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
        feat.WithPrerequisite(MFeatNames.OfficerMedic, "Officer's Medical Training").WithPermanentQEffect(
            "You can use the same hand you are using to wield a shield to Treat Wounds or use Battle Medicine, and you are considered to have a hand free for other uses of Medicine as long as the only thing you are holding or wielding in that hand is a shield. When you use Battle Medicine on an ally while wielding a shield, they gain a +1 circumstance bonus to AC and Reflex saves that lasts until the start of your next turn or until they are no longer adjacent to you, whichever comes first.",
            qf =>
            {
                Creature self = qf.Owner;
                qf.AfterYouTakeActionAgainstTarget = (_, action, ally, _) =>
                {
                    if (!ally.FriendOfAndNotSelf(self) || !action.Name.Contains("Battle Medicine")) return Task.CompletedTask;
                    ally.AddQEffect(new QEffect("Shielded Recovery",
                        "You gain a +1 circumstance bonus to AC and Reflex saves as long as you are adjacent to " +
                        self.Name + ".",
                        ExpirationCondition.ExpiresAtStartOfSourcesTurn, self, IllustrationName.HealersTools)
                    {
                        BonusToDefenses = (_, _, defense) => defense is not (Defense.AC or Defense.Reflex) ? null : new Bonus(1, BonusType.Circumstance,  "Shielded Recovery"),
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
        feat.WithActionCost(1).WithPrerequisite(MFeatNames.PlantBanner, "Plant Banner").WithPermanentQEffect(null, qf =>
        {
            Creature owner = qf.Owner;
            qf.ProvideStrikeModifier = item =>
            {
                int? distance = ModManager.TryParse("Thrown30Feet", out Trait thrown30) && item.HasTrait(thrown30) ? 6 : item.HasTrait(Trait.Thrown20Feet) ? 4 : item.HasTrait(Trait.Thrown10Feet) ? 2 : null;
                if (distance == null) return null;
                CombatAction claimTheField = new CombatAction(qf.Owner, new SideBySideIllustration(item.Illustration, MIllustrations.PlantBanner),
                        "Claim the Field", [MTraits.Commander, Trait.Basic, Trait.Manipulate],
                        "You can plant your banner at any corner within your weapon's first range increment. Each ally within a 30-foot burst centered on your banner immediately gains 4 temporary Hit Points, plus an additional 4 temporary Hit Points at 4th level and every 4 levels thereafter. " +
                        "These temporary Hit Points last for 1 round; each time an ally starts their turn within the burst, their temporary Hit Points are renewed for another round. " +
                        "You cannot wield your banner while it is planted. While your banner is planted, the emanation around your banner is a 35 foot emanation." +
                        "\n\nAny enemy who attempts to remove your banner while it is planted in this way must succeed at a Will save against your class DC or the attempt fails. On a critical failure, the enemy is fleeing for 1 round. This is an incapacitation and mental effect.",
                        Target.Burst(distance.Value, 6)
                            .WithIncludeOnlyIf((_, cr) => cr.FriendOf(qf.Owner)))
                    .WithActionCost(1).WithSoundEffect(Sfx.Drums)
                    .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                    {
                        Creature illusory = Creature.CreateIndestructibleObject(
                            IllustrationName.None,
                            "Banner", caster.Level);
                        illusory.Traits.Add(MTraits.Banner);
                        QEffect? radius = caster.FindQEffect(MQEffectIds.BannerRadius);
                        Tile? thrownTo = caster.Battle.Map.GetTile(targets.ChosenPointOfOrigin.X, targets.ChosenPointOfOrigin.Y);
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
                                CombatAction removeBanner = new CombatAction(caster, MIllustrations.SimpleBanner,
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
                                                tQ.TileQEffectId == MTileQEffectIds.Banner);
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
                                    ? new ActionPossibility(new CombatAction(qfTech.Owner, MIllustrations.SimpleBanner,
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
                                                    tQ.TileQEffectId == MTileQEffectIds.Banner);
                                            CheckResult save = CommonSpellEffects.RollSavingThrow(
                                                combatAction.Owner, spell, Defense.Will,
                                                caster.ClassDC(MTraits.Commander));
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
                                                                                        MIllustrations.Banner,
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
                                                            Illustration = MIllustrations.Banner
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
                        caster.RemoveAllQEffects(effect => effect.Id == MQEffectIds.Banner);
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
                return item.HasTrait(MTraits.Banner) && (item.HasTrait(Trait.Thrown20Feet) || item.HasTrait(Trait.Thrown10Feet) || item.HasTrait(thrown30)) ? claimTheField : null;
            };
        });
    }
    private static void BattleTestedCompanionLogic(TrueFeat feat)
    {
        feat.WithPrerequisite(MFeatNames.CommandersCompanion, "Commander's Companion").WithPermanentQEffect("Your animal companion is stronger.",qf =>
        {
            qf.Id = QEffectId.MatureAnimalCompanion;
            qf.StartOfCombat = _ =>
            {
                if (qf.Owner.HasFeat(MFeatNames.BattleHardenedCompanion) || qf.Owner.HasFeat(FeatName.MatureAnimalCompanionRanger) || qf.Owner.HasFeat(FeatName.MatureAnimalCompanionDruid) || qf.Owner.HasFeat(FeatName.LoyalCompanion)) return Task.CompletedTask;
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
                        SubmenuPossibility submenuPossibility = new(animalCompanion.Illustration, "Command your animal companion");
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
                            CombatAction combatAction = new(owner, numberOfActions > 0 ? new SideBySideIllustration(animalCompanion.Illustration, numberOfActions == 2 ? IllustrationName.TwoActions : IllustrationName.Action) : animalCompanion.Illustration, "Command your animal companion",
                                [Trait.Auditory], $"Take {actionCost + 1} actions as your animal companion.\n\nYou can only command your animal companion once per turn.", Target.Self().WithAdditionalRestriction(_ => GetAnimalCompanionCommandRestriction(controller, animalCompanion)))
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
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            Creature owner = qf.Owner;
            qf.ProvideMainAction = _ =>
            {
                CombatAction defiant = new CombatAction(owner, MIllustrations.DefiantBanner, "Defiant Banner",
                        [MTraits.Brandish, MTraits.Commander, Trait.Flourish, Trait.Manipulate, Trait.Visual], $"You and all allies within the aura of your commander's banner when you use this action gain resistance {owner.Abilities.Intelligence} to bludgeoning, piercing, and slashing damage until the start of your next turn.",
                        Target.Emanation(GetBannerRadius(owner)).WithAdditionalRequirementOnCaster(cr => new BrandishRequirement().Satisfied(cr, cr))
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
        feat.WithActionCost(1).WithPermanentQEffect(null, qf =>
        {
            Creature owner = qf.Owner;
            qf.ProvideMainAction = _ =>
            {
                CombatAction rally = new CombatAction(owner, MIllustrations.RallyingBanner, "Rallying Banner",
                    [MTraits.Brandish, MTraits.Commander, Trait.Emotion, Trait.Healing, Trait.Mental, Trait.Visual],
                    $"You restore {4 + (owner.Level - 8) / 2}d6 Hit Points to each ally within the aura of your commander's banner. You may only use Rallying Banner once per encounter.",
                    Target.Emanation(GetBannerRadius(owner))
                        .WithAdditionalRequirementOnCaster(cr => new BrandishRequirement().Satisfied(cr, cr))
                        .WithIncludeOnlyIf((_, cr) =>
                            new InBannerAuraRequirement().Satisfied(owner, cr) && cr.FriendOf(owner)))
                    .WithActionCost(1).WithSoundEffect(SfxName.Healing).WithActionId(MActionIds.RallyBanner)
                    .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                    {
                        foreach (Creature target in targets.ChosenCreatures)
                        {
                            await target.HealAsync($"{4 + (owner.Level - 8) / 2}d6", spell);
                        }
                        caster.AddQEffect(new QEffect
                        {
                            PreventTakingAction = action =>
                                action.ActionId == MActionIds.RallyBanner
                                    ? "You can only use Rally Banner once per encounter."
                                    : null
                        });
                    });
                return new ActionPossibility(rally).WithPossibilityGroup("Abilities");

            };

        });
    }
    
    #endregion

    #region QEffects

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

    private static QEffect CommandersBannerEffect(AuraAnimation auraAnimation, int radius, Creature source)
    {
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
                    friend.AddQEffect(new QEffect("Commander's Banner",
                        "You gain a +1 status bonus to Will saves and DCs against fear effects.",
                        ExpirationCondition.Ephemeral, qfBanner.Owner)
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
            Source = source
        };
    }

    private static TileQEffect CommandersBannerTileEffect(int radius, Creature source, Creature illusion)
    {
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
                    friend.AddQEffect(new QEffect("Commander's Banner",
                        "You gain a +1 status bonus to Will saves and DCs against fear effects.",
                        ExpirationCondition.Ephemeral, source)
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
            VisibleDescription = $"{source.Name} and all their allies in a {5*radius}-foot emanation gain a +1 status bonus to Will saves and DCs against fear effects."
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
                .WithAdditionalConditionOnTargetCreature((self, other) =>
                    other.PersistentCharacterSheet != null && other != self
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("Must be a party member."))) as
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
        return squadmate.FindQEffect(MQEffectIds.Squadmate)?.Source == commander;
    }

    internal static bool IsMyAnimalCompanion(Creature commander, Creature companion)
    {
        return companion.FindQEffect(QEffectId.RangersCompanion)?.Source == commander;
    }

    internal static bool AnimalReactionAvailable(Creature source, Creature target)
    {
        return target.FindQEffect(MQEffectIds.AnimalReaction)?.Source == source;
    }

    internal static bool IsMyBanner(Creature commander, Creature target)
    {
        return target.FindQEffect(MQEffectIds.Banner)?.Source == commander;
    }

    internal static bool IsMyBanner(Creature commander, Tile tile)
    {
        return tile.TileQEffects.FirstOrDefault(t1 => t1.TileQEffectId == MTileQEffectIds.Banner)?.Name ==
               commander.Name + "'s Banner";
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
                 (inventory.Backpack.Any(item =>
                     item?.StoredItems.Any(item1 => item1.BaseItemName == banner.BaseItemName) ?? false)))
        {
        }
        else if ((inventory.LeftHand == null || inventory.LeftHand.BaseItemName != banner.BaseItemName) &&
                 (inventory.RightHand == null || inventory.RightHand.BaseItemName != banner.BaseItemName) &&
                 !inventory.Backpack.Any(item => item != null && item.BaseItemName == banner.BaseItemName))
        {
            if (inventory.RightHand == null && banner.ItemName != BannerItem.Banner &&
                (inventory.LeftHand == null || !inventory.LeftHand.HasTrait(Trait.TwoHanded)))
            {
                inventory.RightHand = banner;
            }
            else if (inventory.LeftHand == null && banner.ItemName != BannerItem.Banner)
            {
                inventory.LeftHand = banner;
            }
            else
            {
                if (inventory.CanBackpackFit(banner, 0))
                {
                    inventory.AddAtEndOfBackpack(banner);
                }
            }

            return true;
        }

        return false;
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
            return Usability.Usable;
        }
    }

    public class TacticResponseRequirement : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (target.QEffects.Any(qEffect => qEffect.Id == MQEffectIds.TacticResponse))
            {
                return Usability.NotUsableOnThisCreature(target.Name +
                                                         " has already responded to a tactic this round.");
            }
            return Usability.Usable;
        }
    }

    public class CanMakeStrikeWithPrimary : CreatureTargetingRequirement
    {
        public override Usability Satisfied(Creature source, Creature target)
        {
            if (target.PrimaryWeaponIncludingRanged == null || !target.CreateStrike(target.PrimaryWeaponIncludingRanged)
                    .WithActionCost(0).CanBeginToUse(target))
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
            bool canUse = CreateSpells(target).CreateActions(true).Count != 0;
            return !canUse ? Usability.NotUsableOnThisCreature(target.Name + " cannot cast a damaging spell.") : Usability.Usable;
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