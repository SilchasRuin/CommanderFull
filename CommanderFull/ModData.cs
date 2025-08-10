using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
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
        public static readonly Trait Tactic = ModManager.RegisterTrait("Tactic");
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
        #endregion
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
        
    }
    public abstract class MSubmenuIds
    {
        public static readonly SubmenuId Commander = ModManager.RegisterEnumMember<SubmenuId>("Commander");
    }
    public abstract class MRuneKinds
    {
        public static readonly RuneKind Banner = ModManager.RegisterEnumMember<RuneKind>("Banner");
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
    }
}