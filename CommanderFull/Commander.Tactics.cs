using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
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
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Modding;
using static CommanderFull.ModData;

namespace CommanderFull;

public abstract partial class Commander
{
    public static IEnumerable<Feat> LoadTactics()
    {
        yield return new ActionFeat(MFeatNames.GatherToMe,
            "You signal your team to move into position together.",
            "Signal all squadmates; each can immediately Stride as a reaction, though each must end their movement inside your banner’s aura, or as close to your banner's aura as their movement Speed allows.",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.MobilityTactic]).WithActionCost(1).WithIllustration(MIllustrations.GatherToMe);
        yield return new ActionFeat(MFeatNames.DefensiveRetreat, "You call for a careful retreat.",
            "Signal all squadmates within the aura of your commander's banner; each can immediately Step up to three times as a free action. Each Step must take them farther away from at least one hostile creature they are observing and can only take them closer to a hostile creature if doing so is the only way for them to move toward safety.",
            [
                MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic,
                MTraits.MobilityTactic
            ]).WithActionCost(2).WithIllustration(MIllustrations.Retreat);
        yield return new ActionFeat(MFeatNames.NavalTraining,
            "Your instructions make it easier for you and your allies to swim through dangerous waters.",
            "Signal all squadmates; until the end of your next turn, each squadmate gains a swim Speed.",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.MobilityTactic]).WithActionCost(1).WithIllustration(IllustrationName.WaterWalk);
        yield return new ActionFeat(MFeatNames.PassageOfLines,
            "You command your allies to regroup, allowing endangered units to fall back while rested units press the advantage.",
            "Signal all squadmates within the aura of your commander's banner; each can swap positions with another willing ally adjacent to them.",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.MobilityTactic]).WithActionCost(1).WithIllustration(MIllustrations.PassageOfLines);
        yield return new ActionFeat(MFeatNames.ProtectiveScreen,
            "You've trained your allies in a technique designed to protect war mages.",
            "Signal one squadmate; as a reaction, that squadmate Strides directly toward any other squadmate who is within the aura of your banner. If the first squadmate ends their movement adjacent to that squadmate, that squadmate does not trigger reactions when casting spells or making ranged attacks until the end of their next turn or until they are no longer adjacent to the first squadmate, whichever comes first.",
            [
                MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic,
                MTraits.MobilityTactic
            ]).WithActionCost(1).WithIllustration(MIllustrations.ProtectiveScreen);
        yield return new ActionFeat(MFeatNames.PincerAttack,
            "You signal an aggressive formation designed to exploit enemies' vulnerabilities.",
            "Signal all squadmates affected by your commander's banner; each can Step as a reaction. If any of your allies end this movement adjacent to an opponent, that opponent is off-guard to melee attacks from you and all other squadmates who responded to Pincer Attack until the start of your next turn.",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.OffensiveTactic]).WithActionCost(1).WithIllustration(MIllustrations.PincerAttack);
        yield return new ActionFeat(MFeatNames.StrikeHard, "You command an ally to attack.",
            "Choose a squadmate who can see or hear your signal. That ally immediately attempts a Strike as a reaction.",
            [
                MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic,
                MTraits.OffensiveTactic
            ]).WithActionCost(2).WithIllustration(MIllustrations.StrikeHard);
        yield return new ActionFeat(MFeatNames.CoordinatingManeuvers,
            "Your team works to slip enemies into a disadvantageous position.",
            $"Signal one squadmate within the aura of your banner; that squadmate can immediately Step as a free action. If they end this movement next to an opponent, they can attempt to {UseCreatedTooltip("Reposition")} that target as a reaction.",
            [
                MTraits.Tactic, MTraits.Brandish, MTraits.BasicTactic,
                MTraits.OffensiveTactic
            ]).WithActionCost(1).WithIllustration(MIllustrations.CoordinatingManeuvers);
        yield return new ActionFeat(MFeatNames.DoubleTeam,
            "Your team works together to set an enemy up for a vicious attack.",
            $"Signal one squadmate who has an opponent within their reach. That ally can Shove or {UseCreatedTooltip("Reposition")} an opponent as a free action. If their maneuver is successful and the target ends their movement adjacent to a different squadmate, the second squadmate can attempt a melee Strike against that target as a reaction.",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.OffensiveTactic]).WithActionCost(2).WithIllustration(MIllustrations.DoubleTeam);
        yield return new ActionFeat(MFeatNames.EndIt,
            "At your proclamation that victory is already at hand, your allies march forward with an authoritative stomp, scattering your enemies in terror.",
            "If you and your allies outnumber all enemies on the battlefield, and you or a squadmate have reduced an enemy to 0 Hit Points since the start of your last turn, you may signal all squadmates within the aura of your banner; you and each ally can Step as a free action directly toward a hostile creature. Any hostile creatures within 10 feet of a squadmate after this movement must attempt a Will save against your class DC; on a failure they become fleeing for 1 round, and on a critical failure they become fleeing for 1 round and frightened 2. This is an emotion, fear, and mental effect.",
            [
                MTraits.Tactic, MTraits.Brandish, Trait.Incapacitation, MTraits.BasicTactic,
                MTraits.OffensiveTactic
            ]).WithActionCost(2).WithIllustration(MIllustrations.EndIt);
        yield return new ActionFeat(MFeatNames.Reload,
            "Your drill instruction kicks in, and your allies rapidly reload their weapons to prepare for the next volley.",
            "Signal all squadmates; each can immediately Interact to reload as a reaction.",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.OffensiveTactic]).WithActionCost(1).WithIllustration(MIllustrations.Reload);
        yield return new ActionFeat(MFeatNames.ShieldsUp, "You signal your allies to ready their defenses.",
            "Signal all squadmates within the aura of your commander’s banner; each can immediately Raise a Shield as a reaction. Squadmates who have a parry action (whether from a Parry weapon or a Feat such as Dueling Parry or Twin Parry) may use that instead.\n\n{b}Special{/b} If one of your squadmates knows or has prepared the shield cantrip, they can cast it as a reaction instead of taking the actions normally granted by this tactic.",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.OffensiveTactic]).WithActionCost(1).WithIllustration(MIllustrations.ShieldsUp);
        yield return new ActionFeat(MFeatNames.TacticalTakedown,
            "You direct a coordinated maneuver that sends an enemy tumbling down.",
            "Signal up to two squadmates within the aura of your commander’s banner. Each of those allies can Stride up to half their Speed as a reaction. If they both end this movement adjacent to an enemy, that enemy must succeed at a Reflex save against your class DC or fall prone.\n",
            [MTraits.Tactic, MTraits.BasicTactic, MTraits.OffensiveTactic]).WithActionCost(2).WithIllustration(MIllustrations.TacticalTakedown);
        yield return new ActionFeat(MFeatNames.DemoralizingCharge,
            "Your team’s coordinated assault strikes fear into your enemies’ hearts.",
            "Signal up to two squadmates within the aura of your commander’s banner; as a free action, those squadmates can immediately Stride toward an enemy they are observing. If they end this movement adjacent to an enemy, they can attempt to Strike that enemy as a reaction. For each of these Strikes that are successful, the target enemy must succeed at a Will save against your class DC or become frightened 1 (frightened 2 on a critical failure); this is an emotion, fear, and mental effect. If both Strikes target the same enemy, that enemy attempts the save only once after the final attack and takes a –1 circumstance penalty to their Will save to resist this effect (this penalty increases to –2 if both Strikes are successful or to –3 if both Strikes are successful and either is a critical hit).",
            [MTraits.Tactic, MTraits.ExpertTactic, MTraits.Brandish]).WithActionCost(2).WithIllustration(MIllustrations.DemoralizingCharge);
        yield return new ActionFeat(MFeatNames.BuckleCutBlitz,
            "Your squad dashes past enemies, slicing their boot laces and breaking their belt buckles.",
            "Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Reflex save against your class DC or become clumsy 1 for 1 round (clumsy 2 on a critical failure).",
            [MTraits.Tactic, MTraits.ExpertTactic, MTraits.Brandish]).WithActionCost(2).WithIllustration(MIllustrations.BuckleCutBlitz);
        yield return new ActionFeat(MFeatNames.StupefyingRaid,
            "Your team dashes about in a series of maneuvers that leave the enemy befuddled.",
            "Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Will save against your class DC or become stupefied 1 for 1 round (stupefied 2 on a critical failure); this is a mental effect.",
            [MTraits.Tactic, MTraits.ExpertTactic, MTraits.Brandish]).WithActionCost(2).WithIllustration(MIllustrations.StupefyingRaid);
        yield return new ActionFeat(MFeatNames.SlipAndSizzle,
            "Your team executes a brutal technique designed to knock down an opponent and blast them with magical devastation.",
            "Signal up to two squadmates within the aura of your commander’s banner; one of these squadmates must be adjacent to an opponent and the other must be capable of casting a spell that deals damage. The first squadmate can attempt to Trip the adjacent opponent as a reaction. If this Trip is successful, the second squadmate can cast a ranged spell that deals damage and takes 2 or fewer actions to cast. This spell is cast as a reaction and must either target the tripped opponent or include the tripped opponent in the spell’s area.\n\nIf the second squadmate cast a spell using slots or Focus Points as part of this tactic, they are slowed 1 until the end of their next turn and do not gain a reaction when they regain actions at the start of their next turn." +
            "\n{b}Note{/b} Spells with variants, for example: Magic Missile or Scorching Ray, cannot be cast at this time.",
            [MTraits.Tactic, MTraits.ExpertTactic]).WithActionCost(2).WithIllustration(MIllustrations.SlipAndSizzle);
        yield return new ActionFeat(MFeatNames.AlleyOop,
            "Your team excels at sharing resources and delivering them exactly where they need to be.",
            "Signal a squadmate within the aura of your banner who is holding or wearing a consumable that can be activated as a single action (if the target is wearing a consumable, they need a free hand to toss it). That squadmate can toss their consumable to any other squadmate within the aura of your banner as a free action, and the receiving squadmate can catch and activate the consumable as a reaction. If the receiving squadmate chooses not to catch the consumable or if they don’t have a free hand to catch it with, it lands on the ground in their space.",
            [MTraits.Tactic, MTraits.ExpertTactic]).WithActionCost(1).WithIllustration(MIllustrations.AlleyOop);
        yield return new ActionFeat(MFeatNames.TakeTheHighGround,
                "Your ally leaps to secure the high ground with a little help from the squad.",
                "Signal a squadmate within the aura of your commander’s banner; as a free action, that squadmate can Stride directly toward any other squadmate you are both observing. If the first squadmate ends this movement adjacent to another squadmate, the first squadmate can immediately Leap up to 25 feet as a reaction, boosted by the other squadmate. This distance increases to 40 feet if you have legendary proficiency in Warfare Lore.",
                [MTraits.Tactic, MTraits.ExpertTactic]).WithActionCost(1).WithIllustration(MIllustrations.TakeTheHighGround);
    }

    private static IEnumerable<QEffect> TacticsQFs(Creature cr)
    {
        yield return new QEffect
        {
            Tag = MFeatNames.GatherToMe,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(GatherToMe(cr))
                    : null,
        };
        yield return new QEffect
        {
            Tag = MFeatNames.DefensiveRetreat,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(DefensiveRetreat(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.NavalTraining,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(NavalTraining(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.PassageOfLines,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(PassageOfLines(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.ProtectiveScreen,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MobilityTactics
                    ? new ActionPossibility(ProtectiveScreen(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.PincerAttack,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(PincerAttack(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.StrikeHard,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(StrikeHard(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.CoordinatingManeuvers,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(CoordinatingManeuvers(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.DoubleTeam,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(DoubleTeam(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.EndIt,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(EndIt(cr))
                    : null,
            StartOfYourPrimaryTurn = (effect, _) =>
            {
                effect.AddGrantingOfTechnical(creature => creature.EnemyOf(cr), qfTech =>
                {
                    qfTech.WhenCreatureDiesAtStateCheckAsync = _ =>
                    {
                        cr.AddQEffect(new QEffect(ExpirationCondition.CountsDownAtStartOfSourcesTurn)
                        {
                            Source = cr,
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
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(Reload(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.ShieldsUp,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(ShieldsUp(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.TacticalTakedown,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.OffensiveTactics
                    ? new ActionPossibility(TacticalTakedown(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.DemoralizingCharge,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(DemoralizingCharge(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.BuckleCutBlitz,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(BuckleCutBlitz(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.StupefyingRaid,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(StupefyingRaid(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.SlipAndSizzle,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(SlipAndSizzle(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.AlleyOop,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(AlleyOop(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.TakeTheHighGround,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.ExpertTactics
                    ? new ActionPossibility(TakeTheHighGround(cr))
                    : null
        };
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
                var drilledTargets = DrilledTargets(targets, caster);
                Creature? bannerHolder = caster.Battle.AllCreatures.FirstOrDefault(cr => IsMyBanner(caster, cr));
                Tile? bannerTile = caster.Battle.Map.AllTiles.FirstOrDefault(tile => IsMyBanner(caster, tile));
                bool useDrilledReactions = UseDrilledReactions(caster);
                bool usedDrill = false;
                bool lostReaction = false;
                bool animalReact = false;
                if (bannerHolder != null && targets.ChosenCreatures.Contains(bannerHolder) &&
                    new TacticResponseRequirement().Satisfied(caster, bannerHolder) == Usability.Usable && CanTakeReaction(useDrilledReactions, bannerHolder, drilledTargets, caster))
                {
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, bannerHolder))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill = true;
                    }
                    else if (!bannerHolder.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        bannerHolder.Actions.UseUpReaction();
                        lostReaction = true;
                    }
                    else if (bannerHolder.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        bannerHolder.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact = true;
                    }

                    if (await bannerHolder.StrideAsync("Move up to your speed.", allowCancel: true))
                    {
                        bannerHolder.AddQEffect(RespondedToTactic(caster));
                    }
                    else
                    {
                        if (usedDrill)
                            caster.RemoveAllQEffects(qf => qf.Id == MQEffectIds.ExpendedDrilled);
                        if (lostReaction)
                            bannerHolder.Actions.RefundReaction();
                        if (animalReact)
                            bannerHolder.AddQEffect(AnimalReaction(caster));
                    }
                }

                foreach (Creature target in targets.ChosenCreatures.Where(c => c != bannerHolder))
                {
                    useDrilledReactions = UseDrilledReactions(caster);
                    bool usedDrill2 = false;
                    bool lostReaction2 = false;
                    bool animalReact2 = false;
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable ||
                        !CanTakeReaction(useDrilledReactions, target, drilledTargets, caster))
                    {
                        continue;
                    }
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill2 = true;
                    }
                    else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                        lostReaction2 = true;
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact2 = true;
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
                            Squares = target.Speed,
                            Style =
                            {
                                PermitsStep = false
                            }
                        })
                        .Where(tile =>
                            tile.LooksFreeTo(target) && ((bannerHolder != null &&
                                                          bannerHolder.DistanceTo(tile) <=
                                                          GetBannerRadius(caster)) ||
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
                        tileOptions.Add(
                            moveAction.CreateUseOptionOn(tile).WithIllustration(moveAction.Illustration));
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
                            if (usedDrill2)
                                RemoveDrilledExpended(caster);
                            if (lostReaction2)
                                target.Actions.RefundReaction();
                            if (animalReact2)
                                target.AddQEffect(AnimalReaction(caster));
                            break;
                        case TileOption tileOption:
                            await target.StrideAsync(target.Name + " move as close to the banner area as possible.",
                                strideTowards: tileOption.Tile);
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
                bool useDrilledReactions = UseDrilledReactions(caster);
                bool usedDrill = false;
                bool lostReaction = false;
                bool animalReact = false;
                if (useDrilledReactions)
                {
                    caster.AddQEffect(DrilledReactionsExpended(caster));
                    usedDrill = true;
                }
                if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                {
                    target.Actions.UseUpReaction();
                    lostReaction = true;
                }
                else if (target.HasEffect(MQEffectIds.AnimalReaction))
                {
                    target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                    animalReact = true;
                }
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
                        if (usedDrill)
                            RemoveDrilledExpended(caster);
                        if (lostReaction)
                            target.Actions.RefundReaction();
                        if (animalReact)
                            target.AddQEffect(AnimalReaction(caster));
                        break;
                    case CreatureOption creatureOption:
                        target.AddQEffect(RespondedToTactic(caster));
                        await creatureOption.Action();
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
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                var drilledTargets = DrilledTargets(targets, caster);
                bool cancel = true;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable || !CanTakeReaction(useDrilledReactions, target, drilledTargets, caster))
                    {
                        continue;
                    }
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill = true;
                    }
                    else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                        lostReaction = true;
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact = true;
                    }

                    bool stepped = await target.StepAsync(target.Name + ": Pincer Attack Step", allowCancel: true);
                    if (stepped)
                    {
                        cancel = false;
                        target.AddQEffect(RespondedToTactic(caster));
                        includedAllies.Add(target);
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
                    else
                    {
                        if (usedDrill)
                            RemoveDrilledExpended(caster);
                        if (lostReaction)
                            target.Actions.RefundReaction();
                        if (animalReact)
                            target.AddQEffect(AnimalReaction(caster));
                    }
                }

                if (cancel)
                    spell.RevertRequested = true;
            });
        return pincerAttack;
    }

    private static CombatAction StrikeHard(Creature owner)
    {
        CombatAction strikeHard = new CombatAction(owner, MIllustrations.StrikeHard, "Strike Hard!",
                [MTraits.Brandish, MTraits.Tactic, MTraits.Commander, Trait.Basic],
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
            .WithEffectOnEachTarget(async delegate(CombatAction spell, Creature caster, Creature target, CheckResult _)
            {
                bool useDrilledReactions = UseDrilledReactions(caster);
                bool usedDrill = false;
                bool lostReaction = false;
                bool animalReact = false;
                bool cancel = true;
                if (useDrilledReactions)
                {
                    caster.AddQEffect(DrilledReactionsExpended(caster));
                    usedDrill = true;
                }
                if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                {
                    target.Actions.UseUpReaction();
                    lostReaction = true;
                }
                else if (target.HasEffect(MQEffectIds.AnimalReaction))
                {
                    target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                    animalReact = true;
                }

                List<CombatAction> possibleStrikes = target.Weapons
                    .Select(item => CreateReactiveAttackFromWeapon(item, target))
                    .Where(atk => atk.CanBeginToUse(target)).ToList();
                switch (possibleStrikes.Count)
                {
                    case 1:
                    {
                        if (await target.Battle.GameLoop.FullCast(possibleStrikes[0]))
                            cancel = false;
                        break;
                    }
                    case > 1:
                    {
                        List<Option> options = [];
                        foreach (CombatAction possibleStrike in possibleStrikes)
                            GameLoop.AddDirectUsageOnCreatureOptions(possibleStrike, options);
                        if (await AlternateTaskImplements.OfferOptions(target, options, true))
                            cancel = false;
                        break;
                    }
                }
                switch (cancel)
                {
                    case false:
                        target.AddQEffect(RespondedToTactic(caster));
                        break;
                    case true:
                    {
                        spell.RevertRequested = true;
                        if (usedDrill)
                            RemoveDrilledExpended(caster);
                        if (lostReaction)
                            target.Actions.RefundReaction();
                        if (animalReact)
                            target.AddQEffect(AnimalReaction(caster));
                        break;
                    }
                }
            });
        return strikeHard;
    }

    private static CombatAction CoordinatingManeuvers(Creature owner)
    {
        CombatAction tactic = new CombatAction(owner, MIllustrations.CoordinatingManeuvers,
                "Coordinating Maneuvers",
                [MTraits.Tactic, MTraits.Brandish, MTraits.Commander, Trait.Basic],
                $"Signal one squadmate within the aura of your banner; that squadmate can immediately Step as a free action. If they end this movement next to an opponent, they can attempt to {UseCreatedTooltip("Reposition")} that target as a reaction. Repositioning requires a free hand.",
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
                bool useDrilledReactions = UseDrilledReactions(caster);
                var step = await target.StepAsync(
                    "Choose where to step, if you end your movement next to an opponent, you may attempt to Reposition the target as a reaction.",
                    true);
                if (step)
                {
                    target.AddQEffect(RespondedToTactic(caster));
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (useDrilledReactions)
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill = true;
                    }

                    if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                        lostReaction = true;
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact = true;
                    }

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
                    if (reposition.CanBeginToUse(target) && CanTakeReaction(useDrilledReactions, target, caster))
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
                                if (usedDrill)
                                    RemoveDrilledExpended(caster);
                                if (lostReaction)
                                    target.Actions.RefundReaction();
                                if (animalReact)
                                    target.AddQEffect(AnimalReaction(caster));
                                break;
                            case CreatureOption creatureOption:
                                await creatureOption.Action();
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
                $"Signal one squadmate who has an opponent within their reach. That ally can Shove or {UseCreatedTooltip("Reposition")} an opponent as a free action. If their maneuver is successful and the target ends their movement adjacent to a different squadmate, the second squadmate can attempt a melee Strike against that target as a reaction.",
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
                bool useDrilledReactions = UseDrilledReactions(caster);
                QEffect reaction = new()
                {
                    AfterYouTakeAction = async (effect, action) =>
                    {
                        if (action.ActionId != ActionId.Shove &&
                            action.ActionId != MActionIds.Reposition) return;
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
                            if (!useDrilledReactions && !target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.Actions.UseUpReaction();
                            }
                            else if (target.HasEffect(MQEffectIds.AnimalReaction))
                            {
                                target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                            }
                            creature.AddQEffect(RespondedToTactic(caster));
                            List<CombatAction> possibleStrikes = creature.MeleeWeapons
                                .Select(item => CreateReactiveAttackFromWeapon(item, creature))
                                .Where(atk => atk.CanBeginToUse(target)).ToList();
                            switch (possibleStrikes.Count)
                            {
                                case 1:
                                    await creature.Battle.GameLoop.FullCast(possibleStrikes[0],
                                        ChosenTargets.CreateSingleTarget(enemy));
                                    break;
                                case > 1:
                                {
                                    List<Option> options = [];
                                    foreach (CombatAction possibleStrike in possibleStrikes)
                                    {
                                        possibleStrike.ChosenTargets = ChosenTargets.CreateSingleTarget(enemy);
                                        GameLoop.AddDirectUsageOnCreatureOptions(possibleStrike, options);
                                    }
                                    await creature.Battle.GameLoop.OfferOptions(target, options, true);
                                    break;
                                }
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
                [
                    Trait.Basic, Trait.Incapacitation, MTraits.Commander, MTraits.Tactic,
                    MTraits.Brandish
                ],
                "If you and your allies outnumber all enemies on the battlefield, and you or a squadmate have reduced an enemy to 0 Hit Points since the start of your last turn, you may signal all squadmates within the aura of your banner; you and each ally can Step as a free action directly toward a hostile creature. Any hostile creatures within 10 feet of a squadmate after this movement must attempt a Will save against your class DC; on a failure they become fleeing for 1 round, and on a critical failure they become fleeing for 1 round and frightened 2. This is an emotion, fear, and mental effect.",
                more && died
                    ? AllSquadmateInBannerTarget(owner)
                    : !more && died
                        ? Target.Uncastable("You do not outnumber your enemies.")
                        : !died && more
                            ? Target.Uncastable(
                                "An enemy hasn't been reduced to 0 hp since the start of your last turn.")
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
                if (!moved)
                {
                    spell.RevertRequested = true;
                    return;
                }
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
                    cr.HeldItems.Any(item => item.EphemeralItemProperties.NeedsReload || item.EphemeralItemProperties.AmmunitionLeftInMagazine <= 0) &&
                    new TacticResponseRequirement().Satisfied(owner, cr) && !cr.HasEffect(QEffectId.RangersCompanion))
                    ? AllSquadmateInBannerTarget(owner)
                    : Target.Uncastable(
                        "There must be at least one squadmate who needs to reload and can take a reaction."))
            .WithActionCost(1).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (_, caster, targets) =>
            {
                var drilledTargets = DrilledTargets(targets, caster);
                foreach (Creature target in targets.ChosenCreatures.Where(cr =>
                             !cr.HasEffect(QEffectId.RangersCompanion)))
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    if (!CanTakeReaction(useDrilledReactions, target, drilledTargets, caster)) continue;
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable) continue;
                    Item? tobeReloaded =
                        target.HeldItems.FirstOrDefault(item => item.EphemeralItemProperties.NeedsReload || item.EphemeralItemProperties.AmmunitionLeftInMagazine <= 0);
                    if (tobeReloaded == null) continue;
                    CombatAction reload = target.CreateReload(tobeReloaded).WithActionCost(0);
                    var confirm = await target.Battle.AskForConfirmation(target, target.Illustration,
                        "Reload " + tobeReloaded.Name + (useDrilledReactions && IsDrilledTarget(drilledTargets, target)
                            ? "?"
                            : " using a reaction?"), "yes");
                    if (!confirm) continue;
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                    }
                    else
                    {
                        target.Actions.UseUpReaction();
                    }
                    await target.Battle.GameLoop.FullCast(reload);
                    target.AddQEffect(RespondedToTactic(caster));
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
                var drilledTargets = DrilledTargets(targets, caster);
                foreach (Creature target in targets.ChosenCreatures)
                {
                    if (target.HasEffect(QEffectId.RangersCompanion)) continue;
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    if (!CanTakeReaction(useDrilledReactions, target, drilledTargets, caster)) continue;
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable) continue;
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
                        (useDrilledReactions && IsDrilledTarget(drilledTargets, target)
                            ? ""
                            : " This will use a reaction."), names.ToArray());
                    if (names[choice.Index] == "Cancel") continue;
                    if (!await target.Battle.GameLoop.FullCast(shields[choice.Index])) continue;
                    target.AddQEffect(RespondedToTactic(caster));
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                    }
                    else
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
                var drilledTargets = DrilledTargets(targets, caster);
                int moved = 0;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    if (!CanTakeReaction(useDrilledReactions, target, drilledTargets, caster)) continue;
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill = true;
                    }
                    else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                        lostReaction = true;
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact = true;
                    }

                    if (!await target.StrideAsync(
                            target.Name + ": Choose where to stride" +
                            (useDrilledReactions && IsDrilledTarget(drilledTargets, target) ? "." : " as a reaction."),
                            maximumHalfSpeed: true, allowCancel: true))
                    {
                        if (usedDrill)
                            RemoveDrilledExpended(caster);
                        if (lostReaction)
                            target.Actions.RefundReaction();
                        if (animalReact)
                            target.AddQEffect(AnimalReaction(caster));
                        continue;
                    }

                    target.AddQEffect(RespondedToTactic(caster));
                    ++moved;
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
                [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, Trait.Basic],
                "Signal up to two squadmates within the aura of your commander’s banner; as a free action, those squadmates can immediately Stride toward an enemy they are observing. If they end this movement adjacent to an enemy, they can attempt to Strike that enemy as a reaction. For each of these Strikes that are successful, the target enemy must succeed at a Will save against your class DC or become frightened 1 (frightened 2 on a critical failure); this is an emotion, fear, and mental effect. If both Strikes target the same enemy, that enemy attempts the save only once after the final attack and takes a –1 circumstance penalty to their Will save to resist this effect (this penalty increases to –2 if both Strikes are successful or to –3 if both Strikes are successful and either is a critical hit).",
                (Target.MultipleCreatureTargets(2, () =>
                {
                    return new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                        new FriendCreatureTargetingRequirement(), new CanTargetBeginToMoveRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                        new BrandishRequirement()
                    ], (_, _, _) => int.MinValue);
                }) as MultipleCreatureTargetsTarget)!.WithMinimumTargets(1).WithMustBeDistinct())
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                var drilledTargets = DrilledTargets(targets, caster);
                bool moved = false;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    if (await target.Battle.AskToChooseACreature(target,
                            target.Battle.AllCreatures.Where(cr =>
                                cr.EnemyOf(target) && cr.DistanceTo(target) <= target.Speed + 1), target.Illustration,
                            "Choose an enemy to stride towards, you should choose an enemy you can end adjacent to.",
                            "", "pass") is not { } enemy)
                        continue;
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
                            Squares = target.Speed,
                            Style =
                            {
                                PermitsStep = false
                            }
                        })
                        .Where(tile =>
                            (tile.LooksFreeTo(target) || tile.Equals(target.Occupies)) &&
                            tile.IsAdjacentTo(enemy.Space.CenterTile))
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
                    if (move is CancelOption)
                    {
                        continue;
                    }
                    Tile? tile = (move as TileOption)?.Tile;
                    if (tile == null) continue;
                    await target.StrideAsync("Demoralizing Charge", strideTowards: tile);
                    moved = true;
                    target.AddQEffect(RespondedToTactic(caster));
                    if (!target.IsAdjacentTo(enemy)) continue;
                    if (!CanTakeReaction(useDrilledReactions, target, drilledTargets, caster)) continue;
                    bool confirm = await target.AskForConfirmation(target.Illustration,
                        "Do you wish to strike " + enemy.Name +
                        (useDrilledReactions && IsDrilledTarget(drilledTargets, target) ? "?" : " using a reaction?"), "Yes");
                    if (!confirm) continue;
                    CombatAction? bestStrike = DetermineBestMeleeStrike(target);
                    if (bestStrike == null) continue;
                    bestStrike.WithActionCost(0);
                    if (!bestStrike.CanBeginToUse(target)) continue;
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
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

                    CheckResult strike = await target.MakeStrike(bestStrike, enemy);
                    QEffect memory = new()
                    {
                        Tag = strike,
                        Source = target,
                        Id = MQEffectIds.DemoCharge
                    };
                    enemy.AddQEffect(memory);
                }

                if (!moved)
                {
                    spell.RevertRequested = true;
                    return;
                }

                List<Creature> enemies = caster.Battle.AllCreatures
                    .Where(cr => cr.HasEffect(MQEffectIds.DemoCharge)).ToList();
                switch (enemies.Count)
                {
                    case 0:
                        return;
                    case 1:
                        Creature enemy = enemies[0];
                        List<CheckResult> results = enemy.QEffects.Where(qf => qf.Id == MQEffectIds.DemoCharge)
                            .Select(qf =>
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
                            BonusToDefenses = (_, _, defense) =>
                                defense == Defense.Will
                                    ? new Bonus(-penalty, BonusType.Circumstance, "Demoralizing Charge")
                                    : null
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
                        foreach (Creature enemy2 in enemies.Where(enemy2 =>
                                     !enemy2.IsImmuneTo(Trait.Emotion) && !enemy2.IsImmuneTo(Trait.Fear) &&
                                     !enemy2.IsImmuneTo(Trait.Mental)))
                        {
                            List<CheckResult> results2 = enemy2.QEffects
                                .Where(qf => qf.Id == MQEffectIds.DemoCharge).Select(qf =>
                                    qf.Tag is CheckResult tag ? tag : CheckResult.Failure).ToList();
                            List<CheckResult> goodResults2 =
                                results2.Where(result => result >= CheckResult.Success).ToList();
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
                [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, Trait.Basic],
                "Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Reflex save against your class DC or become clumsy 1 for 1 round (clumsy 2 on a critical failure).",
                (Target.MultipleCreatureTargets(2, () =>
                {
                    return new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                        new FriendCreatureTargetingRequirement(), new CanTargetBeginToMoveRequirement(),
                        new ReactionRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                        new BrandishRequirement()
                    ], (_, _, _) => int.MinValue);
                }) as MultipleCreatureTargetsTarget)!.WithMinimumTargets(1).WithMustBeDistinct())
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                var drilledTargets = DrilledTargets(targets, caster);
                int moved = 0;
                QEffect stateCheck = new()
                {
                    StateCheck = qf =>
                    {
                        Creature self = qf.Owner;
                        if (self.AnimationData.LongMovement == null) return;
                        if (!self.Battle.AllCreatures.Any(cr =>
                                cr.EnemyOf(self) && cr.IsAdjacentTo(self) &&
                                !cr.HasEffect(MQEffectIds.BuckleBlitz))) return;
                        foreach (Creature enemy in self.Battle.AllCreatures.Where(cr =>
                                     cr.EnemyOf(self) && cr.IsAdjacentTo(self) &&
                                     !cr.HasEffect(MQEffectIds.BuckleBlitz)))
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
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    if (!CanTakeReaction(useDrilledReactions, target, drilledTargets, caster)) continue;
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill = true;
                    }
                    else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                        lostReaction = true;
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact = true;
                    }

                    target.AddQEffect(stateCheck);
                    CombatAction moveAction = CommonCombatActions.StepByStepStride(target).WithActionCost(0);
                    bool move = await target.Battle.GameLoop.FullCast(moveAction);
                    switch (move)
                    {
                        case false:
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            if (usedDrill)
                                RemoveDrilledExpended(caster);
                            if (lostReaction)
                                target.Actions.RefundReaction();
                            if (animalReact)
                                target.AddQEffect(AnimalReaction(caster));
                            continue;
                        case true:
                        {
                            ++moved;
                            target.AddQEffect(RespondedToTactic(caster));
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            break;
                        }
                    }
                }

                foreach (Creature bad in caster.Battle.AllCreatures.Where(cr =>
                             cr.HasEffect(MQEffectIds.BuckleBlitz)))
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
                [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, Trait.Basic],
                "Your team dashes about in a series of maneuvers that leave the enemy befuddled. Signal up to two squadmates within the aura of your commander’s banner; these squadmates can Stride up to their Speed as a reaction. Each enemy they are adjacent to at any point during this movement must attempt a Will save against your class DC or become stupefied 1 for 1 round (stupefied 2 on a critical failure); this is a mental effect.",
                (Target.MultipleCreatureTargets(2, () =>
                {
                    return new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                        new FriendCreatureTargetingRequirement(), new CanTargetBeginToMoveRequirement(),
                        new ReactionRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                        new BrandishRequirement()
                    ], (_, _, _) => int.MinValue);
                }) as MultipleCreatureTargetsTarget)!.WithMinimumTargets(1).WithMustBeDistinct())
            .WithActionCost(2).WithSoundEffect(SfxName.BeastRoar)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                var drilledTargets = DrilledTargets(targets, caster);
                var moved = 0;
                QEffect stateCheck = new()
                {
                    StateCheck = qf =>
                    {
                        Creature self = qf.Owner;
                        if (self.AnimationData.LongMovement == null) return;
                        if (!self.Battle.AllCreatures.Any(cr =>
                                cr.EnemyOf(self) && cr.IsAdjacentTo(self) &&
                                !cr.HasEffect(MQEffectIds.StupefyingRaid))) return;
                        foreach (Creature enemy in self.Battle.AllCreatures.Where(cr =>
                                     cr.EnemyOf(self) && cr.IsAdjacentTo(self) &&
                                     !cr.HasEffect(MQEffectIds.StupefyingRaid) && !cr.IsImmuneTo(Trait.Mental)))
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
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    if (!CanTakeReaction(useDrilledReactions, target, drilledTargets, caster)) continue;
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, target))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill = true;
                    }
                    else if (!target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.Actions.UseUpReaction();
                        lostReaction = true;
                    }
                    else if (target.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact = true;
                    }

                    target.AddQEffect(stateCheck);
                    CombatAction moveAction = CommonCombatActions.StepByStepStride(target).WithActionCost(0);
                    bool move = await target.Battle.GameLoop.FullCast(moveAction);
                    switch (move)
                    {
                        case false:
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            if (usedDrill)
                                RemoveDrilledExpended(caster);
                            if (lostReaction)
                                target.Actions.RefundReaction();
                            if (animalReact)
                                target.AddQEffect(AnimalReaction(caster));
                            continue;
                        case true:
                        {
                            ++moved;
                            target.AddQEffect(RespondedToTactic(caster));
                            stateCheck.ExpiresAt = ExpirationCondition.Immediately;
                            break;
                        }
                    }
                }

                foreach (Creature bad in caster.Battle.AllCreatures.Where(cr =>
                             cr.HasEffect(MQEffectIds.StupefyingRaid)))
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
                [MTraits.Commander, MTraits.Tactic, Trait.Basic],
                "Signal two squadmates within the aura of your commander’s banner; one of these squadmates must be adjacent to an opponent and the other must be capable of casting a spell that deals damage. The first squadmate can attempt to Trip the adjacent opponent as a reaction. If this Trip is successful, the second squadmate can cast a ranged spell that deals damage and takes 2 or fewer actions to cast. This spell is cast as a reaction and must either target the tripped opponent or include the tripped opponent in the spell’s area.\n\nIf the second squadmate cast a spell using slots or Focus Points as part of this tactic, they are slowed 1 until the end of their next turn and do not gain a reaction when they regain actions at the start of their next turn." +
                "\n{b}Note{/b} Spells with action cost variants (for example: Magic Missile or Scorching Ray) cannot be cast at this time.",
                Target.MultipleCreatureTargets(
                    new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new FriendOrSelfCreatureTargetingRequirement(),
                        new InBannerAuraRequirement(), new ReactionRequirement(), new TacticResponseRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(),
                        new CanTargetTripAndIsAdjacentRequirement()
                    ], (_, _, _) => int.MinValue),
                    new CreatureTarget(RangeKind.Ranged,
                    [
                        new SquadmateTargetRequirement(), new FriendOrSelfCreatureTargetingRequirement(),
                        new InBannerAuraRequirement(), new ReactionRequirement(), new TacticResponseRequirement(),
                        new UnblockedLineOfEffectCreatureTargetingRequirement(), new CanTargetCastDamageSpell()
                    ], (_, _, _) => int.MinValue)).WithMustBeDistinct().WithMinimumTargets(2))
            .WithActionCost(2).WithSoundEffect(SfxName.Trip).WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                var drilledTargets = DrilledTargets(targets, caster);
                bool useDrilledReactions = UseDrilledReactions(caster);
                Creature tripper = targets.ChosenCreatures[0];
                if (!CanTakeReaction(useDrilledReactions, tripper, drilledTargets, caster))
                {
                    spell.RevertRequested = true;
                    return;
                }
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
                if (useDrilledReactions && IsDrilledTarget(drilledTargets, tripper))
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
                CheckResult result = await tripper.Battle.GameLoop.FullCast(trip)
                    ? trip.CheckResult
                    : CheckResult.Failure;
                Creature? which = trip.ChosenTargets.ChosenCreature;
                if (which == null)
                {
                    spell.RevertRequested = true;
                    if (usedDrill)
                        RemoveDrilledExpended(caster);
                    if (lostReaction)
                        tripper.Actions.RefundReaction();
                    if (animalReact)
                        tripper.AddQEffect(AnimalReaction(caster));
                    return;
                }
                tripper.AddQEffect(RespondedToTactic(caster));
                if (result <= CheckResult.Failure || !CanTakeReaction(useDrilledReactions, mage, drilledTargets, caster)) return;
                Possibilities spells = CreateSpells(mage);
                bool usedDrilled2 = false;
                bool usedReaction2 = false;
                bool choice = await mage.AskForConfirmation(mage.Illustration,
                    "Do you wish to cast a damaging spell, which must include " +
                    which.Name +
                    (IsDrilledTarget(drilledTargets, mage) && useDrilledReactions
                        ? "? If you cast a focus spell or leveled spell, you will be slowed 1 until the end of your next turn and you do not gain a reaction at the start of your next turn."
                        : " as a reaction? If you cast a focus spell or leveled spell, you will be slowed 1 until the end of your next turn and you do not gain a reaction at the start of your next turn."),
                    "Yes");
                if (!choice) return;
                List<CombatAction> actions = [];
                actions.AddRange(spells.CreateActions(true).Select(action => action.Action));
                CombatAction[] array = actions.ToArray();
                RequestResult requestResult = await mage.Battle.SendRequest(new ComboBoxInputRequest<CombatAction>(mage,
                    "What spell to cast?", mage.Illustration, "Fulltext search...", array,
                    item => new ComboBoxInformation(item.Illustration, item.Name, item.Description,
                        item.SpellId.ToStringOrTechnical()), item => $"Cast {{i}}{item.Name.ToLower()}{{/i}}",
                    "Cancel"));
                switch (requestResult.ChosenOption)
                {
                    case CancelOption:
                        return;
                    case ComboBoxInputOption<CombatAction> chosenOption2:
                    {
                        mage.AddQEffect(RespondedToTactic(caster));
                        if (useDrilledReactions && IsDrilledTarget(drilledTargets, mage))
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
                                                RemoveDrilledExpended(caster);
                                            if (usedReaction2)
                                                mage.Actions.RefundReaction();
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
    
    private static CombatAction AlleyOop(Creature owner)
    {
        return new CombatAction(owner, MIllustrations.AlleyOop, "Alley-oop", [MTraits.Commander, MTraits.Tactic, Trait.Basic],
            "Signal a squadmate within the aura of your banner who is holding or wearing a consumable that can be activated as a single action (if the target is wearing a consumable, they need a free hand to toss it). That squadmate can toss their consumable to any other squadmate within the aura of your banner as a free action, and the receiving squadmate can catch and activate the consumable as a reaction. If the receiving squadmate chooses not to catch the consumable or if they don’t have a free hand to catch it with, it lands on the ground in their space. ",
            new CreatureTarget(RangeKind.Ranged,
                [
                    new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                    new FriendOrSelfCreatureTargetingRequirement(), new CanTargetThrowConsumable(),
                    new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(),
                    new AdditionalSquadmateInBannerAuraRequirement()
                ],
                (_, _, _) => -2.14748365E+09f))
            .WithActionCost(1).WithSoundEffect(SfxName.ItemGet).WithEffectOnEachTarget(async (spell, caster, target, _) =>
            {
                
                List<Item> consumables = [];
                consumables.AddRange(target.HeldItems.Where(item => IsConsumable(item, target)));
                consumables.AddRange(target.CarriedItems.Where(item => IsConsumable(item, target)));
                Item[] array = consumables.ToArray();
                Item item = consumables[0];
                switch (consumables.Count)
                {
                    case > 1:
                    {
                        RequestResult choice = await target.Battle.SendRequest(new ComboBoxInputRequest<Item>(target,
                            "Choose a consumable to toss", target.Illustration, "Fulltext search...",
                            array,
                            item1 => new ComboBoxInformation(item1.Illustration, item1.Name, item1.Description ?? "",
                                item1.ToString()), item1 => "{i}Toss "+item1.Name.ToLower()+"{/i}", "Cancel"));
                        switch (choice.ChosenOption)
                        {
                            case CancelOption:
                                spell.RevertRequested = true;
                                return;
                            case ComboBoxInputOption<Item> chosenOption:
                                item = chosenOption.SelectedObject;
                                break;
                        }
                        break;
                    }
                    case 1:
                    {
                        bool confirm = await target.AskForConfirmation(target.Illustration,
                            "Would you like to toss " + item + " to an ally?", "Yes");
                        if (!confirm)
                        {
                            spell.RevertRequested = true;
                            return;
                        }
                        break;
                    }
                }
                Creature? ally = await target.Battle.AskToChooseACreature(target,
                    target.Battle.AllCreatures.Where(cr =>
                        IsSquadmate(caster, cr) && new InBannerAuraRequirement().Satisfied(caster, cr)),
                    target.Illustration, "Choose a squadmate to toss " + item + " to.", "", "Cancel");
                if (ally == null)
                {
                    spell.RevertRequested = true;
                    return;
                }
                target.AddQEffect(RespondedToTactic(caster));
                if (target.CarriedItems.Contains(item))
                    target.CarriedItems.Remove(item);
                else if (target.HeldItems.Contains(item))
                    target.HeldItems.Remove(item);
                bool useDrilledReactions = UseDrilledReactions(caster);
                if (ally.HasFreeHand && new TacticResponseRequirement().Satisfied(caster, ally) && new ReactionRequirement().Satisfied(caster, ally))
                {
                    bool verify = await ally.AskForConfirmation(ally.Illustration,
                        "Would you like to catch and use " +
                        item +
                        (useDrilledReactions ? " ?" : " as a reaction?"), "Yes");
                    if (!verify)
                    {
                        ally.Space.CenterTile.DropItem(item);
                        return;
                    }
                    ally.HeldItems.Add(item);
                    await ally.Battle.GameLoop.StateCheck();
                    if (item.ProvidesItemAction?.Invoke(ally, item) is ActionPossibility actionPossibility)
                    {
                        CombatAction action = actionPossibility.CombatAction.WithActionCost(0);
                        await ally.Battle.GameLoop.FullCast(action);
                    }
                    else if (ally.Possibilities.Filter(ap =>
                             {
                                 if (ap.CombatAction.Item == null || ap.CombatAction.Item != item)
                                     return false;
                                 ap.CombatAction.ActionCost = 0;
                                 ap.RecalculateUsability();
                                 return true;
                             }).CreateActions(true).FirstOrDefault() is CombatAction action)
                    {
                        await ally.Battle.GameLoop.FullCast(action);
                    }
                    else if (ally.Possibilities.Filter(ap =>
                             {
                                 if (ap.PossibilityGroup != "Item" && item.ScrollProperties?.Spell.CombatActionSpell != ap.CombatAction)
                                     return false;
                                 ap.CombatAction.ActionCost = 0;
                                 ap.RecalculateUsability();
                                 return true;
                             }).CreateActions(true).FirstOrDefault() is CombatAction action2)
                    {
                        await ally.Battle.GameLoop.FullCast(action2);
                    }
                    else
                    {
                        ally.Space.CenterTile.DropItem(item);
                        return;
                    }
                    if (!useDrilledReactions)
                        ally.Actions.UseUpReaction();
                    else
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                    ally.AddQEffect(RespondedToTactic(caster));
                }
                else
                    ally.Space.CenterTile.DropItem(item);
            });
    }

    private static CombatAction TakeTheHighGround(Creature owner)
    {
        return new CombatAction(owner, MIllustrations.TakeTheHighGround, "Take the Heights", [MTraits.Tactic, MTraits.Commander, Trait.Basic],
            "Signal a squadmate within the aura of your commander’s banner; as a free action, that squadmate can Stride directly toward any other squadmate you are both observing. If the first squadmate ends this movement adjacent to another squadmate, the first squadmate can immediately Leap up to 25 feet as a reaction, boosted by the other squadmate. This distance increases to 40 feet if you have legendary proficiency in Warfare Lore.",
            new CreatureTarget(RangeKind.Ranged,
                [
                    new SquadmateTargetRequirement(), new InBannerAuraRequirement(),
                    new FriendOrSelfCreatureTargetingRequirement(),
                    new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement()
                ],
                (_, _, _) => -2.14748365E+09f))
            .WithActionCost(1).WithSoundEffect(SfxName.Footsteps)
            .WithEffectOnEachTarget(async (spell, caster, target, _) =>
            {
                Creature? ally = await target.Battle.AskToChooseACreature(target,
                    target.Battle.AllCreatures.Where(cr =>
                        IsSquadmate(caster, cr) && caster.CanSee(cr) && target.CanSee(cr) && cr != target), target.Illustration,
                    "Choose a squadmate to stride towards.", "stride towards", "Cancel");
                if (ally == null || !await target.StrideAsync("Stride towards a squadmate.", strideTowards: ally.Space.CenterTile,
                        allowCancel: true))
                {
                    spell.RevertRequested = true;
                    return;
                }
                target.AddQEffect(RespondedToTactic(caster));
                if (!target.IsAdjacentTo(ally) || new ReactionRequirement().Satisfied(caster, target) != Usability.Usable) return;
                bool useDrilledReactions = UseDrilledReactions(caster);
                int distance = caster.Proficiencies.Get(WarfareLoreTrait) == Proficiency.Legendary ? 8 : 5; 
                CombatAction combatAction = CommonCombatActions.Leap(target, distance).WithActionCost(0);
                // combatAction.Traits.Add(Trait.DoNotShowInCombatLog);
                // combatAction.Traits.Add(Trait.DoNotShowOverheadOfActionName);
                bool usedDrill = false;
                bool lostReaction = false;
                bool animalReact = false;
                if (useDrilledReactions)
                {
                    caster.AddQEffect(DrilledReactionsExpended(caster));
                    usedDrill = true;
                }
                else if (target.HasEffect(MQEffectIds.AnimalReaction))
                {
                    target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                    animalReact = true;
                }
                else
                {
                    target.Actions.UseUpReaction();
                    lostReaction = true;
                }
                if (!await target.Battle.GameLoop.FullCast(combatAction))
                {
                    if (usedDrill)
                        RemoveDrilledExpended(caster);
                    if (lostReaction)
                        target.Actions.RefundReaction();
                    if (animalReact)
                        target.AddQEffect(AnimalReaction(caster));
                }
            });
    }
    

    #endregion
}