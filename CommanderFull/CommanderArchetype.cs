using System.Reflection;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using static CommanderFull.Commander;
using static CommanderFull.ModData;

namespace CommanderFull;

public class CommanderArchetype
{
    public static IEnumerable<Feat> LoadArchetypeFeats()
    {
        yield return ArchetypeFeats.CreateMulticlassDedication(MTraits.Commander,
                "You have dabbled in the art of war, learning some of the tricks of the trade needed for command.",
                "You gain the tactics class feature like a commander and gain your own folio; this folio contains two common mobility or offensive tactics of your choosing. You can prepare one of these tactics as a precombat preparation. You gain a commander's banner that grants you a 30-foot aura for the purposes of using your tactics, but the banner does not grant the commander's banner bonus to Will saves and DCs against fear effects. You become trained in commander class DC and Warfare Lore; if you were already trained in Warfare Lore, you become trained in another skill of your choice.")
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
                values.Tags.Add("PreparedTactics", 1);
                values.AddSelectionOption(new MultipleFeatSelectionOption("CommanderFolio", "Commander Folio", values.CurrentLevel,
                    feat => feat.HasTrait(MTraits.BasicTacticPre), 2));
                values.AddSelectionOption(new MultipleFeatSelectionOption("CommanderTactics", "Prepared Tactics",
                        SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, feat => feat.HasTrait(MTraits.Tactic),
                        1));
                values.TrainInThisOrSubstitute(Commander.WarfareLore);
                values.SetProficiency(MTraits.Commander, Proficiency.Trained);
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
            })
            .WithPermanentQEffect(null, qf =>
            {
                qf.Id = MQEffectIds.ExpendedDrilled;
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
            })
            .WithPrerequisite(values => // Can't use the built-in WithDemandsAbility, to avoid non-ORC text.
                    values.FinalAbilityScores.TotalScore(Ability.Intelligence) >= 14,
                "You must have Intelligence +2 or more.");
        foreach (Feat feat in ArchetypeFeats.CreateBasicAndAdvancedMulticlassFeatGrantingArchetypeFeats(MTraits.Commander, "Field Training"))
            yield return feat;
        yield return new TrueFeat(MFeatNames.TacticalExcellence4, 4, "Your knowledge of battle grows.",
            "You add two new mobility or offensive tactics to your folio and increase your maximum number of tactics prepared by 1.", [])
            .WithAvailableAsArchetypeFeat(MTraits.Commander)
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new MultipleFeatSelectionOption("TacticalExcellence4", "Tactical Excellence - 4", values.CurrentLevel,
                    feat => feat.HasTrait(MTraits.BasicTacticPre), 2));
                values.Tags.Remove("PreparedTactics");
                values.Tags.Add("PreparedTactics", 2);
                var myOption = values.SelectionOptions
                    .FirstOrDefault(option => option.Name == "Prepared Tactics") as MultipleFeatSelectionOption;
                if (myOption == null) return;
                FieldInfo? maxOptions = typeof(MultipleFeatSelectionOption)
                    .GetField("<MaximumNumberOfOptions>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (maxOptions == null) return;
                maxOptions.SetValue(myOption, 2);
                
            });
        yield return new TrueFeat(MFeatNames.TacticalExcellence8, 8, "Your knowledge of battle grows further.",
                "You add two new mobility, offensive, or expert tactics to your folio and increase your maximum number of tactics prepared by 1.", [])
            .WithAvailableAsArchetypeFeat(MTraits.Commander)
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new MultipleFeatSelectionOption("TacticalExcellence8", "Tactical Excellence - 8", values.CurrentLevel,
                    feat => feat.HasTrait(MTraits.BasicTacticPre) || feat.HasTrait(MTraits.ExpertTacticPre), 2));
                values.Tags.Remove("PreparedTactics");
                values.Tags.Add("PreparedTactics", 3);
                var myOption = values.SelectionOptions
                    .FirstOrDefault(option => option.Name == "Prepared Tactics") as MultipleFeatSelectionOption;
                if (myOption == null) return;
                FieldInfo? maxOptions = typeof(MultipleFeatSelectionOption)
                    .GetField("<MaximumNumberOfOptions>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (maxOptions == null) return;
                maxOptions.SetValue(myOption, 3);
            })
            .WithPrerequisite(MFeatNames.TacticalExcellence4, "Tactical Excellence - 4");
    }
}