using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;
using static CommanderFull.ModData;

namespace CommanderFull;

public abstract class BannerItem
{
    public static readonly ItemName Banner = ModManager.RegisterNewItemIntoTheShop("Banner", itemName =>
    {
        return new Item(itemName, MIllustrations.Banner, "commander's banner", 1, 0, Trait.DoNotAddToShop, MTraits.Commander, Trait.Runestone)
            .WithRuneProperties(new RuneProperties("heraldic", MRuneKinds.Banner, "A commander needs a battle standard to help guide their allies on the field. Your banner can take many forms; it could be a flag or pennant, a decorated fan, a personalized totem, or some other highly visible but light item.",
                "Affix your banner to your weapon, shield or an animal companion's barding or harness. You can only use Brandish actions if your banner is affixed to a weapon or shield. You only benefit from a banner affixed to barding if you have the Commander's Companion feat. If you start without a banner equipped, a basic banner will be provided.", item =>
            {
                item.Traits.Add(MTraits.Banner);
            }).WithCanBeAppliedTo((banner, appliedTo) =>
            {
                if (!appliedTo.HasTrait(Trait.Weapon) &&
                    !appliedTo.HasTrait(Trait.Barding) && !appliedTo.HasTrait(Trait.Shield))
                    return "Your banner can only be applied to a weapon, a shield, or an animal companion's barding.";
                return null;
            }));

    });
    public static readonly ItemName Harness = ModManager.RegisterNewItemIntoTheShop("Harness", itemName => new Item(itemName, IllustrationName.GenericCombatManeuver, "harness", 1, 1, Trait.Barding)
        .WithDescription("A harness is worn by an animal companion. If you are a commander, you can attach a banner to a harness.").WithWornAt(Trait.Barding).WithArmorProperties(new ArmorProperties(0, 7, 0, 0, 0)));
}