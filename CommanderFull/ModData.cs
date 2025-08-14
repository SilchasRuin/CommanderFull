using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework.Audio;

namespace CommanderFull;

public abstract class ModData
{
    public abstract class MTraits
    {
        public static readonly Trait Commander = ModManager.RegisterTrait("Commander", new TraitProperties("Commander", true) { IsClassTrait = true });
        public static readonly Trait Tactic = ModManager.RegisterTrait("Tactic", new TraitProperties("Tactic", true, "Tactics are special abilities that involve you signaling your allies to perform predetermined maneuvers. To use a tactic ability, you must have one or more willing allies you have instructed beforehand during your daily preparations, called squadmates. Your squadmates must also be able to perceive your signal, either when you speak or shout it (in which case the tactic action gains the auditory trait), or by physically signaling them, typically by waving your banner (in which case it gains the visual trait). While you can use multiple tactic actions in a round, a character cannot respond to more than one tactic per round, regardless of source. You can't Ready a tactic."));
        public static readonly Trait TacticPre = ModManager.RegisterTrait("Tactic2", new TraitProperties("Tactic", false));
        public static readonly Trait BasicTactic = ModManager.RegisterTrait("BasicTactic", new TraitProperties("Tactic", false));
        public static readonly Trait OffensiveTactic = ModManager.RegisterTrait("OffensiveTactic", new TraitProperties("Offensive", true));
        public static readonly Trait MobilityTactic = ModManager.RegisterTrait("MobilityTactic", new TraitProperties("Mobility", true));
        public static readonly Trait ExpertTactic = ModManager.RegisterTrait("ExpertTactic2", new TraitProperties("Expert", true));
        public static readonly Trait MasterTactic = ModManager.RegisterTrait("MasterTactic2", new TraitProperties("Master", true));
        public static readonly Trait LegendaryTactic = ModManager.RegisterTrait("LegendaryTactic2", new TraitProperties("Legendary", true));
        public static readonly Trait BasicTacticPre = ModManager.RegisterTrait("BasicTactic2", new TraitProperties("Tactic", true));
        public static readonly Trait ExpertTacticPre = ModManager.RegisterTrait("ExpertTactic", new TraitProperties("Expert", false));
        public static readonly Trait MasterTacticPre = ModManager.RegisterTrait("MasterTactic", new TraitProperties("Master", false));
        public static readonly Trait LegendaryTacticPre = ModManager.RegisterTrait("LegendaryTactic", new TraitProperties("Legendary", false));
        public static readonly Trait Banner = ModManager.RegisterTrait("Banner");
        public static readonly Trait Brandish = ModManager.RegisterTrait("Brandish", new TraitProperties("Brandish", true, "To use an ability that has the brandish trait, you must be holding your banner in one hand or wielding a weapon it is attached to. You cannot use free actions or reactions granted as part of a brandish action unless noted otherwise.", true));
        public static readonly Trait MagicalBanner = ModManager.RegisterTrait("Magical Banner", new TraitProperties("Magical Banner", true, "Magical banners can be affixed to a weapon or shield and apply their benefit while being wielded or while in the inventory. A weapon or shield can have only one magical banner affixed to it at a time. A creature can benefit from the effects of only one magical banner at a time. If a creature is in the aura of two or more friendly magical banners, they gain the benefit of the higher-level one, or in the case of a tie, the banner the creature feels most loyal to.\n\nMagical banners can only grant their benefits and be used when they're in somebody's possession. They often provide a benefit to creatures within the banner's aura, which is a 30-foot emanation centered on the creature in possession of the magical banner.\n\nIf the banner is applied to a commander's banner, then any ability that modifies the commander's banner aura also modifies the aura of magical banners in the same way."));
        public static readonly Trait BlazingBanner = ModManager.RegisterTrait("BlazingBanner", new TraitProperties("BlazingBanner", false));
        public static readonly Trait VandalBanner = ModManager.RegisterTrait("VandalBanner", new TraitProperties("VandalBanner", false));
        public static readonly Trait KnavesStandard = ModManager.RegisterTrait("KnavesStandard", new TraitProperties("KnavesStandard", false));
    }
    public abstract class MFeatNames
    {
        public static readonly FeatName Commander = ModManager.RegisterFeatName("FC_CommanderClass", "Commander");
        #region Feats
        public static readonly FeatName OfficerMedic = ModManager.RegisterFeatName("FC_OfficerMedic", "Officer's Medical Training");
        public static readonly FeatName CommandersCompanion = ModManager.RegisterFeatName("FC_CommandersCompanion", "Commander's Companion");
        public static readonly FeatName DeceptiveTactics = ModManager.RegisterFeatName("FC_DeceptiveTactics", "Deceptive Tactics");
        public static readonly FeatName EfficientPreparation = ModManager.RegisterFeatName("FC_EfficientPreparation", "Efficient Preparation");
        public static readonly FeatName CombatAssessment = ModManager.RegisterFeatName("FC_CombatAssessment", "Combat Assessment");
        public static readonly FeatName ArmorRegiment = ModManager.RegisterFeatName("FC_ArmorRegiment", "Armor Regiment Training");
        public static readonly FeatName PlantBanner = ModManager.RegisterFeatName("FC_PlantBanner", "Plant Banner");
        public static readonly FeatName AdaptiveStratagem = ModManager.RegisterFeatName("FC_AdaptiveStratagem", "Adaptive Stratagem");
        public static readonly FeatName DefensiveSwap = ModManager.RegisterFeatName("FC_DefensiveSwap", "Defensive Swap");
        public static readonly FeatName GuidingShot = ModManager.RegisterFeatName("FC_GuidingShot", "Guiding Shot");
        public static readonly FeatName SetupStrike = ModManager.RegisterFeatName("FC_SetupStrike", "Set-up Strike");
        public static readonly FeatName RapidAssessment = ModManager.RegisterFeatName("FC_RapidAssessment", "Rapid Assessment");
        public static readonly FeatName TacticalExpansion = ModManager.RegisterFeatName("FC_TacticalExpansion", "Tactical Expansion");
        public static readonly FeatName BannerTwirl = ModManager.RegisterFeatName("FC_BannerTwirl", "Banner Twirl");
        public static readonly FeatName BannersInspiration = ModManager.RegisterFeatName("FC_BannersInspiration", "Banner's Inspiration");
        public static readonly FeatName ObservationalAnalysis = ModManager.RegisterFeatName("FC_ObservationalAnalysis", "Observational Analysis");
        public static readonly FeatName UnsteadyingStrike = ModManager.RegisterFeatName("FC_UnsteadyingStrike", "Unsteadying Strike");
        public static readonly FeatName ShieldedRecovery = ModManager.RegisterFeatName("FC_ShieldedRecovery", "Shielded Recovery");
        public static readonly FeatName ClaimTheField = ModManager.RegisterFeatName("FC_ClaimTheField", "Claim the Field");
        public static readonly FeatName BattleTestedCompanion = ModManager.RegisterFeatName("FC_BattleTestedCompanion", "Battle-Tested Companion");
        public static readonly FeatName BattleHardenedCompanion = ModManager.RegisterFeatName("FC_BattleHardenedCompanion", "Battle-Hardened Companion");
        public static readonly FeatName DefiantBanner = ModManager.RegisterFeatName("FC_DefiantBanner", "Defiant Banner");
        public static readonly FeatName OfficersEducation =  ModManager.RegisterFeatName("FC_OfficersEducation", "Officer's Education");
        public static readonly FeatName RallyingBanner = ModManager.RegisterFeatName("FC_RallyingBanner", "Rallying Banner");
        public static readonly FeatName UnrivaledAnalysis = ModManager.RegisterFeatName("FC_UnrivaledAnalysis", "Unrivaled Analysis");
        public static readonly FeatName ReactiveStrike = ModManager.RegisterFeatName("FC_ReactiveStrike", "Reactive Strike");
        public static readonly FeatName TacticalExcellence4 = ModManager.RegisterFeatName("TacticalExcellence4", "Tactical Excellence - 4");
        public static readonly FeatName TacticalExcellence8 = ModManager.RegisterFeatName("TacticalExcellence8", "Tactical Excellence - 8");
        #endregion
        #region tactics
        public static readonly FeatName GatherToMe = ModManager.RegisterFeatName("FC_GatherToMe", "Gather to Me!");
        public static readonly FeatName PincerAttack = ModManager.RegisterFeatName("FC_PincerAttack", "Pincer Attack");
        public static readonly FeatName StrikeHard = ModManager.RegisterFeatName("FC_StrikeHard", "Strike Hard!");
        public static readonly FeatName DefensiveRetreat = ModManager.RegisterFeatName("FC_DefensiveRetreat", "Defensive Retreat");
        public static readonly FeatName NavalTraining = ModManager.RegisterFeatName("FC_NavalTraining", "Naval Training");
        public static readonly FeatName PassageOfLines = ModManager.RegisterFeatName("FC_PassageOfLines", "Passage of Lines");
        public static readonly FeatName ProtectiveScreen = ModManager.RegisterFeatName("FC_ProtectiveScreen", "Protective Screen");
        public static readonly FeatName CoordinatingManeuvers = ModManager.RegisterFeatName("FC_CoordinatingManeuvers", "Coordinating Maneuvers");
        public static readonly FeatName DoubleTeam = ModManager.RegisterFeatName("FC_DoubleTeam", "Double Team");
        public static readonly FeatName EndIt = ModManager.RegisterFeatName("FC_EndIt", "End it!");
        public static readonly FeatName Reload = ModManager.RegisterFeatName("FC_Reload", "Reload!");
        public static readonly FeatName ShieldsUp = ModManager.RegisterFeatName("FC_ShieldsUp", "Shields up!");
        public static readonly FeatName TacticalTakedown = ModManager.RegisterFeatName("FC_TacticalTakedown", "Tactical Takedown");
        public static readonly FeatName DemoralizingCharge = ModManager.RegisterFeatName("FC_DemoralizingCharge", "Demoralizing Charge");
        public static readonly FeatName BuckleCutBlitz = ModManager.RegisterFeatName("FC_BuckleCutBlitz", "Buckle-cut Blitz");
        public static readonly FeatName StupefyingRaid = ModManager.RegisterFeatName("FC_StupefyingRaid", "Stupefying Raid");
        public static readonly FeatName SlipAndSizzle = ModManager.RegisterFeatName("FC_SlipAndSizzle", "Slip and Sizzle");
        public static readonly FeatName AlleyOop = ModManager.RegisterFeatName("FC_AlleyOop", "Alley-oop");
        public static readonly FeatName TakeTheHighGround = ModManager.RegisterFeatName("FC_TakeTheHighGround", "Take the High Ground");
        #endregion
    }

    public abstract class MFeatGroups
    {
        public static readonly FeatGroup OffensiveTactics = new("Offensive Tactics", 5);
        public static readonly FeatGroup MobilityTactics = new("Mobility Tactics", 4);
        public static readonly FeatGroup ExpertTactics = new("Expert Tactics", 3);
        public static readonly FeatGroup MasterTactics = new("Master Tactics", 2);
        public static readonly FeatGroup LegendaryTactics = new("Legendary Tactics", 1);
    }
    public abstract class MQEffectIds
    {
        public static QEffectId DrilledTarget { get; } = ModManager.RegisterEnumMember<QEffectId>("DrilledTarget");
        public static QEffectId Banner { get; } = ModManager.RegisterEnumMember<QEffectId>("Banner");
        public static QEffectId AnimalReaction { get; } = ModManager.RegisterEnumMember<QEffectId>("AnimalReaction");
        public static QEffectId Squadmate { get; } = ModManager.RegisterEnumMember<QEffectId>("Squadmate");
        public static QEffectId ExpendedDrilled { get; } = ModManager.RegisterEnumMember<QEffectId>("ExpendedDrilled");
        public static QEffectId TacticResponse { get; } = ModManager.RegisterEnumMember<QEffectId>("TacticResponse");
        public static QEffectId ProtectiveScreenQf { get; } = ModManager.RegisterEnumMember<QEffectId>("ProtectiveScreenQf");
        public static QEffectId DeathCounter { get; } = ModManager.RegisterEnumMember<QEffectId>("DeathCounter");
        public static QEffectId ArmorRegiment { get; } = ModManager.RegisterEnumMember<QEffectId>("ArmorRegiment");
        public static QEffectId BannerRadius { get; } = ModManager.RegisterEnumMember<QEffectId>("BannerRadius");
        public static QEffectId BannerTempGen { get; } = ModManager.RegisterEnumMember<QEffectId>("BannerTempGen");
        public static QEffectId Observed { get; } = ModManager.RegisterEnumMember<QEffectId>("Observed");
        public static QEffectId DemoCharge  { get; } = ModManager.RegisterEnumMember<QEffectId>("DemoCharge");
        public static QEffectId BuckleBlitz { get; } = ModManager.RegisterEnumMember<QEffectId>("BuckleBlitz");
        public static QEffectId StupefyingRaid { get; } = ModManager.RegisterEnumMember<QEffectId>("StupefyingRaid");
        public static QEffectId AudibleTactics { get; } = ModManager.RegisterEnumMember<QEffectId>("AudibleTactics");
        public static QEffectId VisualTactics { get; } = ModManager.RegisterEnumMember<QEffectId>("VisualTactics");
        public static QEffectId MagicalBanner { get; } =  ModManager.RegisterEnumMember<QEffectId>("MagicalBanner");
        
    }
    public abstract class MTileQEffectIds
    {
        public static TileQEffectId Banner { get; } = ModManager.RegisterEnumMember<TileQEffectId>("Banner");
    }
    public abstract class MPossibilitySectionIds
    {
        public static readonly PossibilitySectionId Toggle = ModManager.RegisterEnumMember<PossibilitySectionId>("CommanderToggle");
        public static readonly PossibilitySectionId MobilityTactics = ModManager.RegisterEnumMember<PossibilitySectionId>("MobilityTactics");
        public static readonly PossibilitySectionId OffensiveTactics = ModManager.RegisterEnumMember<PossibilitySectionId>("OffensiveTactics");
        public static readonly PossibilitySectionId ExpertTactics = ModManager.RegisterEnumMember<PossibilitySectionId>("ExpertTactics");
        public static readonly PossibilitySectionId AuditoryTactics = ModManager.RegisterEnumMember<PossibilitySectionId>("AuditoryTactics");
        public static readonly PossibilitySectionId VisualTactics = ModManager.RegisterEnumMember<PossibilitySectionId>("VisualTactics");
    }
    public abstract class MSubmenuIds
    {
        public static readonly SubmenuId Commander = ModManager.RegisterEnumMember<SubmenuId>("Commander");
        public static readonly SubmenuId SignalToggle = ModManager.RegisterEnumMember<SubmenuId>("SignalToggle");
    }
    public abstract class MRuneKinds
    {
        public static readonly RuneKind Banner = ModManager.RegisterEnumMember<RuneKind>("Banner");
        public static readonly RuneKind MagicalBanner = ModManager.RegisterEnumMember<RuneKind>("MagicalBanner");
    }
    public abstract class MActionIds
    {
        public static readonly ActionId Reposition = ModManager.TryParse("Reposition", out ActionId reposition) ? reposition : ModManager.RegisterEnumMember<ActionId>("FC_Reposition");
        public static readonly ActionId BannersInspiration = ModManager.RegisterEnumMember<ActionId>("BannersInspiration");
        public static readonly ActionId RallyBanner = ModManager.RegisterEnumMember<ActionId>("RallyBanner");
    }
    public abstract class Sfx
    {
        public static readonly SfxName Drums = ModManager.RegisterNewSoundEffect("drums.mp3");
    }

    public abstract class MIllustrations
    {
        public static readonly Illustration Toggle = new ModdedIllustration("FCAssets/Toggle.png");
        public static readonly Illustration Banner = new ModdedIllustration("FCAssets/Banner.png");
        public static readonly Illustration SimpleBanner = new ModdedIllustration("FCAssets/SimpleBanner.png");
        public static readonly Illustration InspiringBanner = new ModdedIllustration("FCAssets/InspiringBanner.png");
        public static readonly Illustration DefiantBanner = new ModdedIllustration("FCAssets/DefiantBanner.png");
        public static readonly Illustration RallyingBanner = new ModdedIllustration("FCAssets/RallyingBanner.png");
        public static readonly Illustration PlantBanner = new ModdedIllustration("FCAssets/PlantBanner.png");
        public static readonly Illustration BannerTwirl = new ModdedIllustration("FCAssets/BannerTwirl.png");
        public static readonly Illustration GatherToMe = new ModdedIllustration("FCAssets/GatherToMe.png");
        public static readonly Illustration PassageOfLines = new ModdedIllustration("FCAssets/PassageOfLines.png");
        public static readonly Illustration StrikeHard = new ModdedIllustration("FCAssets/StrikeHard.png");
        public static readonly Illustration Retreat = new ModdedIllustration("FCAssets/Retreat.png");
        public static readonly Illustration ShieldsUp = new ModdedIllustration("FCAssets/ShieldsUp.png");
        public static readonly Illustration StupefyingRaid = new ModdedIllustration("FCAssets/StupefyingRaid.png");
        public static readonly Illustration BuckleCutBlitz = new ModdedIllustration("FCAssets/BuckleCutBlitz.png");
        public static readonly Illustration ProtectiveScreen = new ModdedIllustration("FCAssets/ProtectiveScreen.png");
        public static readonly Illustration PincerAttack = new ModdedIllustration("FCAssets/PincerAttack.png");
        public static readonly Illustration CoordinatingManeuvers = new ModdedIllustration("FCAssets/CoordinatingManeuvers.png");
        public static readonly Illustration SlipAndSizzle = new ModdedIllustration("FCAssets/SlipAndSizzle.png");
        public static readonly Illustration DoubleTeam = new ModdedIllustration("FCAssets/DoubleTeam.png");
        public static readonly Illustration EndIt = new ModdedIllustration("FCAssets/EndIt.png");
        public static readonly Illustration Reload = new ModdedIllustration("FCAssets/Reload.png");
        public static readonly Illustration TacticalTakedown = new ModdedIllustration("FCAssets/TacticalTakedown.png");
        public static readonly Illustration DemoralizingCharge = new ModdedIllustration("FCAssets/DemoralizingCharge.png");
        public static readonly Illustration Reposition = new ModdedIllustration("FCAssets/Reposition.png");
        public static readonly Illustration Auditory = new ModdedIllustration("FCAssets/Auditory.png");
        public static readonly Illustration Visual = new ModdedIllustration("FCAssets/Visual.png");
        public static readonly Illustration AlleyOop = new ModdedIllustration("FCAssets/AlleyOop.png");
        public static readonly Illustration TakeTheHighGround = IllustrationName.JumpSpell;
        public static readonly Illustration BlazingBanner = new ModdedIllustration("FCAssets/BlazingBanner.png");
        public static readonly Illustration KnavesStandard = new ModdedIllustration("FCAssets/KnavesStandard.png");
        public static readonly Illustration VandalsBanner = new ModdedIllustration("FCAssets/VandalsBanner.png");
    }

    public abstract class MItemGroups
    {
        public static readonly ItemGreaterGroup MagicalBanner = ModManager.RegisterEnumMember<ItemGreaterGroup>("MagicalBanner");
    }
}