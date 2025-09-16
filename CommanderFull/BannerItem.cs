using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using static CommanderFull.ModData;

namespace CommanderFull;

public abstract class BannerItem
{
    public static void LoadItems()
    {
        ItemName dummy2 = Harness;
    }
    public static IEnumerable<ItemName> LoadBanners()
    {
        yield return Banner;
        yield return BlazingBanner;
        yield return VandalsBanner;
        yield return KnavesStandard;
    }
    
    public static readonly ItemName Banner = ModManager.RegisterNewItemIntoTheShop("Banner", itemName =>
    {
        return new Item(itemName, MIllustrations.Banner, "commander's banner", 1, 0, Trait.DoNotAddToShop, MTraits.Commander, Trait.Runestone)
            .WithRuneProperties(new RuneProperties("heraldic", MRuneKinds.Banner, "A commander needs a battle standard to help guide their allies on the field. Your banner can take many forms; it could be a flag or pennant, a decorated fan, a personalized totem, or some other highly visible but light item.",
                "Affix your banner to your weapon, shield or an animal companion's barding or harness. You can only use Brandish actions if your banner is affixed to a weapon or shield. You only benefit from a banner affixed to barding if you have the Commander's Companion feat. If you start without a banner equipped, a basic banner will be provided.", item =>
            {
                item.Traits.Add(MTraits.Banner);
            }).WithCanBeAppliedTo((_, appliedTo) =>
            {
                if (!appliedTo.HasTrait(Trait.Weapon) &&
                    !appliedTo.HasTrait(Trait.Barding) && !appliedTo.HasTrait(Trait.Shield))
                    return "Your banner can only be applied to a weapon, a shield, or an animal companion's barding.";
                return null;
            }));

    });
    public static readonly ItemName Harness = ModManager.RegisterNewItemIntoTheShop("Harness", itemName => new Item(itemName, new ModdedIllustration("FCAssets/Harness.png"), "harness", 1, 1, Trait.Barding)
        .WithDescription("A harness is worn by an animal companion. If you are a commander, you can attach a banner to a harness.").WithWornAt(Trait.Barding).WithArmorProperties(new ArmorProperties(0, 7, 0, 0, 0)));

    public static readonly ItemName BlazingBanner = ModManager.RegisterNewItemIntoTheShop("BlazingBanner", itemName =>
    {
        return new Item(itemName, MIllustrations.BlazingBanner, "blazing banner", 4, 100, Trait.Runestone,
                MTraits.MagicalBanner, Trait.Aura, Trait.Fire, Trait.Magical)
            .WithItemGreaterGroup(MItemGroups.MagicalBanner)
            .WithRuneProperties(new RuneProperties("blazing", MRuneKinds.MagicalBanner, "This magical banner shimmers in a fiery array of reds, oranges, and yellows. The rampant threads catch light in the wind and give the appearance of a blazing flame.", 
                "Whenever you or an ally within the banner's aura critically succeeds with a Strike, the Strike deals an additional 1d4 persistent fire damage." +
                "\n{b}Special{/b} A commander with the Commander's Companion feat can apply this magical banner to their animal companion's barding.",
                item =>
                {
                    item.Traits.Add(MTraits.BlazingBanner);
                }).WithCanBeAppliedTo((_, toApply) =>
            {
                if (!toApply.HasTrait(Trait.Shield) && !toApply.HasTrait(Trait.Weapon) &&
                    (!toApply.HasTrait(Trait.Barding) || !toApply.HasTrait(MTraits.Banner)))
                    return "You can only apply a magical banner to a weapon, a shield, or an animal companion's barding.";
                return null;
            }));
    });
    public static readonly ItemName VandalsBanner = ModManager.RegisterNewItemIntoTheShop("VandalsBanner", itemName =>
    {
        return new Item(itemName, MIllustrations.VandalsBanner, "vandal's banner", 4, 100, Trait.Runestone,
                MTraits.MagicalBanner, Trait.Aura, Trait.Magical)
            .WithItemGreaterGroup(MItemGroups.MagicalBanner)
            .WithRuneProperties(new RuneProperties("wrecking", MRuneKinds.MagicalBanner, "This magical banner is imbued with the foolhardy courage of hooligans and troublemakers.", 
                "Strikes you or an ally make while within the banner’s aura ignore the first 2 points of Hardness of an object." +
                "\n{b}Special{/b} A commander with the Commander's Companion feat can apply this magical banner to their animal companion's barding.",
                item =>
                {
                    item.Traits.Add(MTraits.VandalBanner);
                }).WithCanBeAppliedTo((_, toApply) =>
            {
                if (!toApply.HasTrait(Trait.Shield) && !toApply.HasTrait(Trait.Weapon) &&
                    (!toApply.HasTrait(Trait.Barding) || !toApply.HasTrait(MTraits.Banner)))
                    return "You can only apply a magical banner to a weapon, a shield, or an animal companion's barding.";
                return null;
            }));
    });
    
    public static readonly ItemName KnavesStandard = ModManager.RegisterNewItemIntoTheShop("KnavesStandard", itemName =>
    {
        return new Item(itemName, MIllustrations.KnavesStandard, "knave's standard", 4, 100, Trait.Runestone,
                MTraits.MagicalBanner, Trait.Aura, Trait.Magical)
            .WithItemGreaterGroup(MItemGroups.MagicalBanner)
            .WithRuneProperties(new RuneProperties("devious", MRuneKinds.MagicalBanner, "This magical banner is dip-dyed in an ombre from black to red, mottled and uneven.", 
                "Whenever you or an ally within the banner's aura critically succeeds with a Strike against an off-guard target, the Strike deals an additional 1d4 precision damage." +
                "\n{b}Special{/b} A commander with the Commander's Companion feat can apply this magical banner to their animal companion's barding.",
                item =>
                {
                    item.Traits.Add(MTraits.KnavesStandard);
                }).WithCanBeAppliedTo((_, toApply) =>
            {
                if (!toApply.HasTrait(Trait.Shield) && !toApply.HasTrait(Trait.Weapon) &&
                    (!toApply.HasTrait(Trait.Barding) || !toApply.HasTrait(MTraits.Banner)))
                    return "You can only apply a magical banner to a weapon, a shield, or an animal companion's barding.";
                return null;
            }));
    });

    public static void LoadBannerEffects()
    {
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            if (!cr.HeldItems.Any(item => item.HasTrait(MTraits.BlazingBanner)) &&
                !cr.CarriedItems.Any(item => item.HasTrait(MTraits.BlazingBanner))) return;
            Item blazingBannerItem = cr.HeldItems.FirstOrDefault(item => item.HasTrait(MTraits.BlazingBanner)) ?? cr.CarriedItems.FirstOrDefault(item => item.HasTrait(MTraits.BlazingBanner))!;
            int itemLevel = blazingBannerItem.Level;
            if (blazingBannerItem.HasTrait(Trait.Barding))
            {
                if (!cr.HasFeat(MFeatNames.CommandersCompanion) || cr.PersistentUsedUpResources.AnimalCompanionIsDead) return;
                cr.AddQEffect(new QEffect
                {
                    StateCheck = qf =>
                    {
                        Creature? companion = cr.Battle.AllCreatures.FirstOrDefault(creature => Commander.IsMyAnimalCompanion(cr, creature));
                        if (companion == null) return;
                        companion.AddQEffect(BlazingBannerEffect(companion, itemLevel, blazingBannerItem, cr));
                        qf.ExpiresAt = ExpirationCondition.Immediately;
                    }
                });
            }
            else
                cr.AddQEffect(BlazingBannerEffect(cr,itemLevel, blazingBannerItem));
        });
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            if (!cr.HeldItems.Any(item => item.HasTrait(MTraits.VandalBanner)) &&
                !cr.CarriedItems.Any(item => item.HasTrait(MTraits.VandalBanner))) return;
            Item vandalBannerItem = cr.HeldItems.FirstOrDefault(item => item.HasTrait(MTraits.VandalBanner)) ?? cr.CarriedItems.FirstOrDefault(item => item.HasTrait(MTraits.VandalBanner))!;
            int itemLevel = vandalBannerItem.Level;
            if (vandalBannerItem.HasTrait(Trait.Barding))
            {
                if (!cr.HasFeat(MFeatNames.CommandersCompanion) || cr.PersistentUsedUpResources.AnimalCompanionIsDead) return;
                cr.AddQEffect(new QEffect
                {
                    StateCheck = qf =>
                    {
                        Creature? companion = cr.Battle.AllCreatures.FirstOrDefault(creature => Commander.IsMyAnimalCompanion(cr, creature));
                        if (companion == null) return;
                        companion.AddQEffect(VandalBannerEffect(companion, itemLevel, vandalBannerItem, cr));
                        qf.ExpiresAt = ExpirationCondition.Immediately;
                    }
                });
            }
            else
                cr.AddQEffect(VandalBannerEffect(cr, itemLevel, vandalBannerItem));
        });
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            if (!cr.HeldItems.Any(item => item.HasTrait(MTraits.KnavesStandard)) &&
                !cr.CarriedItems.Any(item => item.HasTrait(MTraits.KnavesStandard))) return;
            Item knaveStandard = cr.HeldItems.FirstOrDefault(item => item.HasTrait(MTraits.KnavesStandard)) ?? cr.CarriedItems.FirstOrDefault(item => item.HasTrait(MTraits.KnavesStandard))!;
            int itemLevel = knaveStandard.Level;
            if (knaveStandard.HasTrait(Trait.Barding))
            {
                if (!cr.HasFeat(MFeatNames.CommandersCompanion) || cr.PersistentUsedUpResources.AnimalCompanionIsDead) return;
                cr.AddQEffect(new QEffect
                {
                    StateCheck = qf =>
                    {
                        Creature? companion = cr.Battle.AllCreatures.FirstOrDefault(creature => Commander.IsMyAnimalCompanion(cr, creature));
                        if (companion == null) return;
                        companion.AddQEffect(KnavesStandardEffect(companion, itemLevel, knaveStandard, cr));
                        qf.ExpiresAt = ExpirationCondition.Immediately;
                    }
                });
            }
            else
                cr.AddQEffect(KnavesStandardEffect(cr, itemLevel, knaveStandard));
        });
    }

    public static QEffect BlazingBannerEffect(Creature cr, int level, Item bannerItem, Creature? commander = null)
    {
        Creature cmndr = commander ?? cr;
        QEffect blazingBanner = new("Blazing Banner",
            "Whenever you or an ally within the banner's aura critically succeeds with a Strike, the Strike deals an additional 1d4 persistent fire damage.");
        int? bannerRadius = bannerItem.HasTrait(MTraits.Banner) ? cr.FindQEffect(MQEffectIds.BannerRadius)?.Value : 6;
        if (cr.HasEffect(QEffectId.RangersCompanion))
            bannerRadius = commander?.FindQEffect(MQEffectIds.BannerRadius)?.Value ?? 6;
        bannerRadius ??= 6;
        blazingBanner.AddGrantingOfTechnical(creature => creature.FriendOf(cr) && creature.DistanceTo(cr) <= bannerRadius,
            qfTech =>
            {
                bool onPerson = cmndr.HeldItems.Any(item => item.HasTrait(MTraits.BlazingBanner)) ||
                                cmndr.CarriedItems.Any(item => item.HasTrait(MTraits.BlazingBanner));
                bool higherQf = BannerSuppression(qfTech, level);
                qfTech.Source = commander ?? cr;
                qfTech.Id = MQEffectIds.MagicalBanner;
                qfTech.Value = level;
                qfTech.Tag = 4;
                if (!onPerson || higherQf) return;
                qfTech.Owner.AddQEffect(new QEffect("Blazing Banner",
                    "Whenever you critically succeed with a Strike, the strike deals an additional 1d4 persistent fire damage.",
                    ExpirationCondition.Ephemeral, cr, MIllustrations.BlazingBanner)
                {
                    AfterYouTakeActionAgainstTarget = (_, action, target, result) =>
                    {
                        if (!action.HasTrait(Trait.Strike) || result != CheckResult.CriticalSuccess) return Task.CompletedTask;
                        target.AddQEffect(QEffect.PersistentDamage("1d4", DamageKind.Fire));
                        return Task.CompletedTask;
                    }
                });
            });
        return blazingBanner;
    }
    public static QEffect VandalBannerEffect(Creature cr, int level, Item bannerItem, Creature? commander = null)
    {
        Creature cmndr = commander ?? cr;
        QEffect vandalBanner = new("Vandal's Banner",
            "Strikes you or an ally make while within the banner’s aura ignore the first 2 points of Hardness of an object.");
        int? bannerRadius = bannerItem.HasTrait(MTraits.Banner) ? cr.FindQEffect(MQEffectIds.BannerRadius)?.Value : 6;
        if (cr.HasEffect(QEffectId.RangersCompanion))
            bannerRadius = commander?.FindQEffect(MQEffectIds.BannerRadius)?.Value ?? 6;
        bannerRadius ??= 6;
        vandalBanner.AddGrantingOfTechnical(creature => creature.FriendOf(cr) && creature.DistanceTo(cr) <= bannerRadius,
            qfTech =>
            {
                bool onPerson = cmndr.HeldItems.Any(item => item.HasTrait(MTraits.VandalBanner)) ||
                                cmndr.CarriedItems.Any(item => item.HasTrait(MTraits.VandalBanner));
                int? original = null;
                int? shieldVal = null;
                bool higherQf = BannerSuppression(qfTech, level);
                qfTech.Source = commander ?? cr;
                qfTech.Id = MQEffectIds.MagicalBanner;
                qfTech.Value = level;
                qfTech.Tag = 3;
                if (!onPerson || higherQf) return;
                qfTech.Owner.AddQEffect(new QEffect("Vandal Banner",
                    "Strikes you make ignore the first 2 points of Hardness of an object.",
                    ExpirationCondition.Ephemeral, cr, MIllustrations.VandalsBanner)
                {
                    BeforeYourActiveRoll = (_, action, _) =>
                    {
                        if (!action.HasTrait(Trait.Strike) || action.ChosenTargets.ChosenCreature is not
                            {} creature || (creature.WeaknessAndResistance.Hardness < 1 && !creature.HasEffect(QEffectId.RaisingAShield)) ) return Task.CompletedTask;
                        original = creature.WeaknessAndResistance.Hardness;
                        switch (creature.WeaknessAndResistance.Hardness)
                        {
                            case >= 2:
                                creature.WeaknessAndResistance.Hardness -= 2;
                                break;
                            case 1:
                                creature.WeaknessAndResistance.Hardness -= 1;
                                break;
                        }
                        if (creature.HeldItems.FirstOrDefault(item => item.HasTrait(Trait.Shield)) is not {} shield)
                            return Task.CompletedTask;
                        shieldVal = shield.Hardness;
                        switch (shield.Hardness)
                        {
                            case >= 2:
                                shield.Hardness -= 2;
                                break;
                            case 1:
                                shield.Hardness -= 1;
                                break;
                        }
                        return Task.CompletedTask;
                    },
                    AfterYouTakeHostileAction = (_, action) =>
                    {
                        if (!action.HasTrait(Trait.Strike) || action.ChosenTargets.ChosenCreature is not {} creature ||
                            (original == null && shieldVal == null)) return;
                        if (original != null)
                            creature.WeaknessAndResistance.Hardness = original.Value;
                        if (shieldVal != null && creature.HeldItems.FirstOrDefault(item => item.HasTrait(Trait.Shield)) is {} shield)
                            shield.Hardness = shieldVal.Value;
                    }
                });
            });
        return vandalBanner;
    }
    
    public static QEffect KnavesStandardEffect(Creature cr, int level, Item bannerItem, Creature? commander = null)
    {
        Creature cmndr = commander ?? cr;
        QEffect vandalBanner = new("Knave's Standard",
            "Whenever you or an ally within the banner's aura critically succeeds with a Strike against an off-guard target, the Strike deals an additional 1d4 precision damage.");
        int? bannerRadius = bannerItem.HasTrait(MTraits.Banner) ? cr.FindQEffect(MQEffectIds.BannerRadius)?.Value : 6;
        if (cr.HasEffect(QEffectId.RangersCompanion))
            bannerRadius = commander?.FindQEffect(MQEffectIds.BannerRadius)?.Value ?? 6;
        bannerRadius ??= 6;
        vandalBanner.AddGrantingOfTechnical(creature => creature.FriendOf(cr) && creature.DistanceTo(cr) <= bannerRadius,
            qfTech =>
            {
                bool onPerson = cmndr.HeldItems.Any(item => item.HasTrait(MTraits.KnavesStandard)) ||
                                cmndr.CarriedItems.Any(item => item.HasTrait(MTraits.KnavesStandard));
                bool higherQf = BannerSuppression(qfTech, level);
                qfTech.Source = commander ?? cr;
                qfTech.Id = MQEffectIds.MagicalBanner;
                qfTech.Value = level;
                qfTech.Tag = 5;
                if (!onPerson || higherQf) return;
                qfTech.Owner.AddQEffect(new QEffect("Knave's Standard",
                    "Whenever you critically succeed with a Strike against an off-guard target, the Strike deals an additional 1d4 precision damage.",
                    ExpirationCondition.Ephemeral, cr, MIllustrations.KnavesStandard)
                {
                   YouDealDamageEvent = (_, damageEvent) =>
                   {
                       CombatAction? action = damageEvent.CombatAction;
                       if (action == null) return Task.CompletedTask;
                       Creature defender = damageEvent.TargetCreature;
                       if (!action.HasTrait(Trait.Strike) || defender.IsImmuneTo(Trait.PrecisionDamage)) return Task.CompletedTask;
                       damageEvent.KindedDamages[0].AddPostCriticalDamage(DiceFormula.FromText("1d4", "Knave's Standard"));
                       return Task.CompletedTask;
                   }
                });
            });
        return vandalBanner;
    }

    public static bool BannerSuppression(QEffect qfTech, int level)
    {
        bool higherQf = qfTech.Owner.QEffects.Any(qf =>
            qf.Id == MQEffectIds.MagicalBanner && qf.Value >= level && qf != qfTech);
        if (!qfTech.Owner.QEffects.Any(qf =>
                qf.Id == MQEffectIds.MagicalBanner && qf.Value == level && qf != qfTech)) return higherQf;
        if (qfTech.Owner.QEffects.Any(qf =>
                qf.Id == MQEffectIds.MagicalBanner && qf.Value == level && qf != qfTech &&
                qf.Source != null && !qf.Source.HasFeat(MFeatNames.Commander)) && qfTech.Source != null && qfTech.Source.HasFeat(MFeatNames.Commander) || qfTech.Owner.QEffects.Any(qf =>
                qf.Id == MQEffectIds.MagicalBanner && qf.Value == level && qf != qfTech && (int)(qfTech.Tag ?? 1) is var b && (int)(qf.Tag ?? 0) is var i && b > i))
            higherQf = false;
        return higherQf;
    }
}