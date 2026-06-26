using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Text;
using static CommanderFull.ModData;

namespace CommanderFull;

public abstract class MasterTactics : Commander
{
    public static IEnumerable<Feat> LoadMasterTactics()
    {
        yield return new ActionFeat(MFeatNames.RoaringCharge, "You and your squad surge forward with a mighty roar.", 
            "Once per encounter: signal all squadmates within the aura of your commander’s banner. As a reaction, these squadmates can Stride up to twice their Speed directly toward any enemy they are observing. Any creature within 10 feet of a squadmate once all movement from this tactic has been completed must attempt a Will save against your class DC with the following results. This is an emotion, fear, incapacitation, and mental effect." +
            S.FourDegreesOfSuccess("No effect", "The target is frightened 1.", "The target is frightened 2.", "The target is frightened 3 and fleeing for 1 round."),
            [MTraits.Commander, MTraits.Tactic, MTraits.MasterTactic])
            .WithActionCost(2)
            .WithIllustration(MIllustrations.CreateIllustration("RoaringCharge"));
        yield return new ActionFeat(MFeatNames.PiranhaAssault, "You know that a thousand small bites can fell a large foe just as surely a single well-placed hit.",
            "Once per encounter: designate a creature within the aura of your commander's banner and signal all squadmates; for 1 minute, each time they attack that creature and deal damage to it of a type the creature resists, they ignore an amount of that creature’s resistance equal to your level.",
            [MTraits.Commander, MTraits.Tactic, MTraits.MasterTactic])
            .WithActionCost(1)
            .WithIllustration(MIllustrations.CreateIllustration("Piranha"));
        yield return new ActionFeat(MFeatNames.PopDropLock, "You command your squadmates to perform a devastating coordinated takedown.",
                "Once per encounter: choose an enemy and signal up to three squadmates within the aura of your commander's banner who all have that enemy within their reach; the squadmates can attempt to Strike, Trip, or Grapple the enemy as a reaction. Each squadmate can only attempt one specific action granted by this tactic, the actions can be attempted in any order and a specific action can only be attempted once as part of this tactic.",
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, MTraits.MasterTactic])
            .WithActionCost(2)
            .WithIllustration(MIllustrations.CreateIllustration("PopDropLock"));
        yield return new ActionFeat(MFeatNames.ReadyAimFire, "You signal a volley of ranged attacks from your allies.",
                "Once per encounter: choose an enemy and signal up to three squadmates within the aura of your commander's banner; your squadmates can Interact to reload as a free action and attempt a ranged Strike against the enemy as a reaction." +
                "\n\n{b}Special{/b} If one of your squadmates knows or has prepared a cantrip with a range of 30 feet or more that deals damage and requires 2 or fewer actions to cast, they can cast it targeting the enemy instead of taking the other actions normally granted by this tactic.",
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, MTraits.MasterTactic])
            .WithActionCost(2)
            .WithIllustration(MIllustrations.CreateIllustration("ReadyAimFire"));
        yield return new ActionFeat(MFeatNames.TheBiggerTheyAre, "Regardless of your individual strengths, collectively your squad has the power to move mountains and topple giants.",
            "Signal a squadmate within the aura of your commander's banner. That squadmate can attempt to Reposition, Shove, or Trip a target within their reach as a free action. Each other squadmate who is adjacent to the original squadmate or the target can attempt to assist with the maneuver as a reaction. For each squadmate who assists in this way, the original squadmate increases the maximum size of creature they can target (for example, if a total of two squadmates participated in this maneuver, the initial squadmate could target a creature up to two sizes larger than them.) The original squadmate gains a circumstance bonus on their check to Reposition, Shove, or Trip equal to the number of additional squadmates who assisted in the maneuver (maximum +4).",
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, MTraits.MasterTactic])
            .WithActionCost(1)
            .WithIllustration(MIllustrations.Reposition);
        yield return new ActionFeat(MFeatNames.MirroredWall, "Your squadmates have polished their shields to a reflective sheen and now position them to reflect a blinding light into your enemy’s eyes",
            MirroredWall(Creature.DefaultCreature).Description, [MTraits.Commander, MTraits.Tactic, MTraits.MasterTactic, Trait.Visual])
            .WithActionCost(2)
            .WithIllustration(MirroredWall(Creature.DefaultCreature).Illustration);
    }

    internal static IEnumerable<QEffect> MasterTacticsEffects(Creature cr)
    {
        yield return new QEffect
        {
            Tag = MFeatNames.RoaringCharge,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MasterTactics
                    ? new ActionPossibility(RoaringCharge(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.PiranhaAssault,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MasterTactics
                    ? new ActionPossibility(PiranhaAssault(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.PopDropLock,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MasterTactics
                    ? new ActionPossibility(PopDropAndLock(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.ReadyAimFire,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MasterTactics
                    ? new ActionPossibility(ReadyAimFire(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.TheBiggerTheyAre,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MasterTactics
                    ? new ActionPossibility(TheBiggerTheyAre(cr))
                    : null
        };
        yield return new QEffect
        {
            Tag = MFeatNames.MirroredWall,
            ProvideActionIntoPossibilitySection = (_, section) =>
                section.PossibilitySectionId == MPossibilitySectionIds.MasterTactics
                    ? new ActionPossibility(MirroredWall(cr))
                    : null
        };
    }

    internal static CombatAction RoaringCharge(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        CombatAction roaringCharge = new CombatAction(owner, MIllustrations.CreateIllustration("RoaringCharge"), "Roaring Charge", [MTraits.Commander, MTraits.Tactic, Trait.Basic],
            "Once per encounter: signal all squadmates within the aura of your commander's banner. As a reaction, these squadmates can Stride up to twice their Speed directly toward any enemy they are observing. Any creature within 10 feet of a squadmate once all movement from this tactic has been completed must attempt a Will save against your class DC with the following results. This is an emotion, fear, incapacitation, and mental effect."+
            S.FourDegreesOfSuccess("No effect", "The target is frightened 1.", "The target is frightened 2.", "The target is frightened 3 and fleeing for 1 round."),
            owner.HasEffect(MQEffectIds.RoaringCharged) ? Target.Uncastable("You have already used Roaring Charge this encounter.") :
            squadmates.Any(cr => new ReactionRequirement().Satisfied(owner, cr) && new TacticResponseRequirement().Satisfied(owner, cr))
                ? AllSquadmateTarget(owner)
                : Target.Uncastable("There must be at least one squadmate who can take a reaction to a tactic.")
            )
            .WithActionCost(2)
            .WithSoundEffect(owner.PersistentCharacterSheet?.Calculated.VoiceGender == VoiceGender.Masculine ? SfxName.RageMale1 : SfxName.RageFemale1)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                List<Creature> drilledTargets = DrilledTargets(targets, caster);
                var moved = false;
                CombatAction roar = new CombatAction(caster, spell.Illustration, "Roaring Charge",
                    [Trait.Emotion, Trait.Mental, Trait.Incapacitation, Trait.Fear, Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName], "Any creature within 10 feet of a squadmate once all movement from this tactic has been completed must attempt a Will save against your class DC with the following results."
                        +S.FourDegreesOfSuccess("No effect", "The target is frightened 1.", "The target is frightened 2.", "The target is frightened 3 and fleeing for 1 round."), Target.Distance(2))
                    .WithSavingThrow(new SavingThrow(Defense.Will, caster.ClassDC(MTraits.Commander)))
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (_, creature, target, result) =>
                    {
                        switch (result)
                        {
                            case CheckResult.CriticalSuccess:
                                return;
                            case CheckResult.Success:
                                target.AddQEffect(QEffect.Frightened(1));
                                break;
                            case CheckResult.Failure:
                                target.AddQEffect(QEffect.Frightened(2));
                                break;
                            case CheckResult.CriticalFailure:
                                target.AddQEffect(QEffect.Frightened(3));
                                target.AddQEffect(QEffect.Fleeing(creature));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(result), result, null);
                        }
                    });
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (!CanTakeReaction(useDrilledReactions, target, caster) || !new TacticResponseRequirement().Satisfied(caster, target) || !new CanTargetBeginToMoveRequirement().Satisfied(caster, target))
                        continue;
                    if (await AlternateTaskImplements.AskToChooseACreature(target.Battle, target,
                            target.Battle.AllCreatures.Where(cr =>
                                cr.EnemyOf(target) && target.CanSee(cr)), target.Illustration,
                            "Choose an enemy to Stride towards, you should choose an enemy you can end the stride within ten feet of.",
                            cr => CombatActionExecution.BreakdownSavingThrowForTooltip(roar, cr, roar.SavingThrow!).TooltipDescription, "pass") is not { } enemy)
                        continue;
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
                    int baseSpeed = target.Speed;
                    QEffect speed = new(ExpirationCondition.ExpiresAtEndOfAnyTurn)
                    {
                        BonusToAllSpeeds = _ => new Bonus(baseSpeed, BonusType.Untyped, "RoaringCharge")
                    };
                    target.AddQEffect(speed);
                    if (!await target.StrideOrStepAsync("Roaring Charge!", strideTowards: enemy.Occupies, allowCancel: true))
                    {
                        if (usedDrill)
                            RemoveDrilledExpended(caster);
                        if (lostReaction)
                            target.Actions.RefundReaction();
                        if (animalReact)
                            target.AddQEffect(AnimalReaction(caster));
                        speed.ExpiresAt = ExpirationCondition.Immediately;
                        continue;
                    }
                    speed.ExpiresAt = ExpirationCondition.Immediately;
                    target.AddQEffect(RespondedToTactic(caster));
                    moved = true;
                }
                if (!moved)
                {
                    spell.RevertRequested = true;
                    return;
                }
                foreach (Creature enemy in caster.Battle.AllCreatures.Where(cr =>
                             squadmates.Any(sq => sq.DistanceTo(cr) <= 2) && cr.EnemyOf(caster)))
                {
                    await caster.Battle.GameLoop.FullCast(roar, ChosenTargets.CreateSingleTarget(enemy));
                }
            });
        return roaringCharge;
    }

    internal static CombatAction PiranhaAssault(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        CombatAction assault = new CombatAction(owner, MIllustrations.CreateIllustration("Piranha"), "Piranha Assault",
            [MTraits.Commander, MTraits.Tactic],
            "Once per encounter: designate a creature within the aura of your commander’s banner and signal all squadmates; for the rest of the encounter, each time they attack that creature and deal damage to it of a type the creature resists, they ignore an amount of that creature's resistance equal to your level.",
            new CreatureTarget(RangeKind.Ranged, [new InBannerAuraRequirement(), new EnemyCreatureTargetingRequirement()], (_, _, _) => float.MinValue)
                .WithAdditionalConditionOnTargetCreature((self, _) => squadmates.Any(cr => new TacticResponseRequirement().Satisfied(self, cr)) ? Usability.Usable : Usability.NotUsable("At least one squadmate must be able to respond to this tactic."))
                .WithAdditionalConditionOnTargetCreature((self, _) => self.HasEffect(MQEffectIds.PiranhaAssaultUsed) ? Usability.NotUsable("You may only use Piranha Assault once per encounter.") : Usability.Usable))
            .WithSoundEffect(SfxName.GluttonBite)
            .WithActionCost(1)
            .WithEffectOnEachTarget(async (spell, caster, target, _) =>
            {
                QEffect ignoreResist = new()
                {
                    IgnoreAmountOfResistanceAgainstYourActions = (_, _, _, enemy, _) => enemy != target ? 0 : caster.Level,
                    DoNotShowUpOverhead = true
                };
                List<string> piranhaGang = [];
                foreach (Creature squadmate in squadmates.Where(mate => new TacticResponseRequirement().Satisfied(caster, mate)))
                {
                    if (!await squadmate.AskForConfirmation(squadmate.Illustration, "Respond to Piranha Assault?",
                            "Yes", "No"))
                        continue;
                    squadmate.AddQEffect(RespondedToTactic(caster));
                    squadmate.AddQEffect(ignoreResist);
                    piranhaGang.Add(squadmate.Name);
                }
                string lastMate = piranhaGang.Last();
                piranhaGang.RemoveAt(piranhaGang.Count - 1);
                string gangMates = string.Join(", ", piranhaGang) + ", and " + lastMate;
                target.AddQEffect(new QEffect("Piranha Assault", $"Attacks made against this creature by {gangMates} ignore {caster.Level} resistance to any damage type.", ExpirationCondition.Never, caster, spell.Illustration));
                caster.AddQEffect(new QEffect {Id = MQEffectIds.PiranhaAssaultUsed});
            });

        return assault;
    }

    internal static CombatAction PopDropAndLock(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        CombatAction popDrop = new CombatAction(owner, MIllustrations.CreateIllustration("PopDropLock"),
            "Pop, Drop, and Lock",
            [MTraits.Brandish, MTraits.Tactic, MTraits.Commander, Trait.Basic],
            "Once per encounter: target one enemy and signal up to three squadmates within the aura of your commander's banner who all have that enemy within their reach; the squadmates can attempt to Strike, Trip, or Grapple the opponent as a reaction. Each squadmate can only attempt one specific action granted by this tactic, the actions can be attempted in any order and a specific action can only be attempted once as part of this tactic.",
            Target.Distance(100).WithAdditionalConditionOnTargetCreature((self, enemy) => squadmates.Where(cr => cr != self && new InBannerAuraRequirement().Satisfied(self, cr)).Any(cr =>
                MeleeReachCreatureTargetingRequirement.WithPrimaryWeapon().Satisfied(cr, enemy) || MeleeReachCreatureTargetingRequirement.WithSecondaryWeapon().Satisfied(cr, enemy)) ? Usability.Usable : Usability.NotUsableOnThisCreature("Must be in reach of at least 1 squadmate."))
                .WithAdditionalConditionOnTargetCreature((self, _) => squadmates.Any(cr => new BrandishRequirement().Satisfied(self, cr)) ? Usability.Usable : Usability.NotUsable("You must be wielding a banner and at least one squadmate must be able to see you."))
                .WithAdditionalConditionOnTargetCreature((self, _) => squadmates.Any(cr => new TacticResponseRequirement().Satisfied(self, cr)) ? Usability.Usable : Usability.NotUsable("At least one squadmate must be able to respond to this tactic."))
                .WithAdditionalConditionOnTargetCreature((self, _) => squadmates.Any(cr => new ReactionRequirement().Satisfied(self, cr)) ? Usability.Usable : Usability.NotUsable("At least one squadmate must be able to take a reaction."))
                .WithAdditionalConditionOnTargetCreature((self, _) => self.HasEffect(MQEffectIds.PoppedDroppedLocked) ? Usability.NotUsable("Pop, Drop, and Lock can only be used once per encounter.") : Usability.Usable)
            )
            .WithSoundEffect(SfxName.Trip)
            .WithActionCost(2)
            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
            {
                List<Creature> mates = squadmates.Where(cr => new BrandishRequirement().Satisfied(caster, cr) && new TacticResponseRequirement().Satisfied(caster, cr) && new InBannerAuraRequirement().Satisfied(caster, cr) && new ReactionRequirement().Satisfied(caster, cr)
                && (MeleeReachCreatureTargetingRequirement.WithPrimaryWeapon().Satisfied(cr, target) || MeleeReachCreatureTargetingRequirement.WithSecondaryWeapon().Satisfied(cr, target))).ToList();
                bool striked = false;
                bool tripped = false;
                bool grappled = false;
                List<Creature> chosen = [];
                List<Creature> copy = mates.ToList();
                foreach (Creature _ in mates)
                {
                    Creature? choice = await caster.Battle.AskToChooseACreature(caster, copy, spell.Illustration,
                        "Choose up to 3 squadmates to signal. Squadmates will act in the order they are chosen.", "", "Pass");
                    if (choice == null)
                        break;
                    chosen.Add(choice);
                    copy.Remove(choice);
                    if (chosen.Count == 3)
                        break;
                }
                if (chosen.Count == 0)
                {
                    spell.RevertRequested = true;
                    return;
                }
                List<Creature> drilledTargets = DrilledTargets(chosen, caster);
                foreach (Creature mate in chosen)
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (!CanTakeReaction(useDrilledReactions, target, caster))
                        continue;
                    if (striked &&
                        !MeleeReachCreatureTargetingRequirement.WithWeaponOfTrait(Trait.Trip).Satisfied(mate, target) &&
                        !MeleeReachCreatureTargetingRequirement.WithWeaponOfTrait(Trait.Grapple)
                            .Satisfied(mate, target))
                        continue;
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
                    mate.RegeneratePossibilities();
                    bool tripped1 = tripped;
                    bool grappled1 = grappled;
                    bool striked1 = striked;
                    List<ICombatAction> combatActions = mate.Possibilities.Filter(ap =>
                    {
                        if (ap.CombatAction.ActionId is not ActionId.Trip and not ActionId.Grapple &&
                            (!ap.CombatAction.Name.StartsWith("Strike") || ap.CombatAction.ActionCost != 1))
                            return false;
                        if ((ap.CombatAction.ActionId == ActionId.Trip && tripped1) ||
                            (ap.CombatAction.ActionId == ActionId.Grapple && grappled1))
                            return false;
                        if (ap.CombatAction.Name.StartsWith("Strike") && striked1)
                            return false;
                        ap.CombatAction.ActionCost = 0;
                        ap.RecalculateUsability();
                        return true;
                    }).CreateActions(true);
                    List<Option> options = [];
                    foreach (ICombatAction action in combatActions)
                    {
                        AlternateTaskImplements.AddDirectUsageOnACreatureOptions(target, action.Action, options);
                    }
                    options.Add(new CancelOption(true));
                    string strike = striked ? "strike, " : "";
                    string trip =  tripped ? "trip, " : "";
                    string grapple = grappled ? "grapple, " : "";
                    QEffect check = new()
                    {
                        AfterYouTakeAction = async (_, action) =>
                        {
                            switch (action.ActionId)
                            {
                                case ActionId.Trip:
                                    tripped = true;
                                    break;
                                case ActionId.Grapple:
                                    grappled = true;
                                    break;
                                default:
                                {
                                    if (action.Name.StartsWith("Strike"))
                                        striked = true;
                                    break;
                                }
                            }
                        }
                    };
                    Option which = (await mate.Battle.SendRequest(new AdvancedRequest(mate, "Choose to "+strike+trip+grapple+(usedDrill ? "or cancel." : "(as a reaction) or cancel."), options)
                    {
                        IsMainTurn = false,
                    })).ChosenOption;
                    switch (which)
                    {
                        case CreatureOption creatureOption:
                        {
                            mate.AddQEffect(check);
                            await creatureOption.Action.Invoke();
                            mate.AddQEffect(RespondedToTactic(caster));
                            check.ExpiresAt = ExpirationCondition.Immediately;
                            break;
                        }
                        case CancelOption:
                        {
                            if (usedDrill)
                                RemoveDrilledExpended(caster);
                            if (lostReaction)
                                mate.Actions.RefundReaction();
                            if (animalReact)
                                mate.AddQEffect(AnimalReaction(caster));
                            continue;
                        }
                    }
                }
                if (!striked && !tripped && !grappled)
                {
                    spell.RevertRequested = true;
                    return;
                }
                caster.AddQEffect(new QEffect { Id = MQEffectIds.PoppedDroppedLocked });
            });
        return popDrop;
    }

    internal static CombatAction ReadyAimFire(Creature owner)
    {
        CombatAction readyAim = new CombatAction(owner, MIllustrations.CreateIllustration("ReadyAimFire"),
            "Ready, Aim, Fire!",
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, Trait.Basic],
            "Once per encounter: choose an enemy and signal up to three squadmates within the aura of your commander's banner; your squadmates can Interact to reload as a free action and attempt a ranged Strike against the enemy as a reaction." +
            "\n\n{b}Special{/b} If one of your squadmates knows or has prepared a cantrip with a range of 30 feet or more that deals damage and requires 2 or fewer actions to cast, they can cast it targeting the enemy instead of taking the other actions normally granted by this tactic.",
            Target.MultipleCreatureTargets(3,
                () => new CreatureTarget(RangeKind.Ranged,
                    [new SquadmateTargetRequirement(), new BrandishRequirement(), new InBannerAuraRequirement(), new TacticResponseRequirement(), new ReactionRequirement(), new CanTargetMakeRangedAttackOrCastCantrip(), new FriendCreatureTargetingRequirement(), new OncePerEncounterRequirement(MQEffectIds.ReadyAimFire)],
                    (_, _, _) => int.MinValue)))
            .WithActionCost(2)
            .WithSoundEffect(SfxName.ReloadCrossbow)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                List<Creature> drilledTargets = DrilledTargets(targets, caster);
                Creature? enemy = await AlternateTaskImplements.AskToChooseACreature(caster.Battle, caster, caster.Battle.AllCreatures.Where(cr => cr.EnemyOf(caster)), spell.Illustration,
                    "Choose an enemy to target, it should be in range of all signaled squadmates.", cr => "AC: " + cr.Defenses.GetBaseValue(Defense.AC), "Cancel");
                if (enemy == null)
                {
                    spell.RevertRequested = true;
                    return;
                }
                bool acted = false;
                foreach (Creature target in targets.ChosenCreatures)
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    bool reload = false;
                    if (target.Weapons.Any(wp => wp.EphemeralItemProperties.NeedsReload))
                    {
                        if (await target.AskForConfirmation(target.Illustration, "Would you like to reload a weapon as a free action?", "Yes", "No"))
                        {
                            Creature? original = target.Battle.ActiveCreature;
                            target.RegeneratePossibilities();
                            target.Possibilities = target.Possibilities.Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.Reload)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.RecalculateUsability();
                                return true;
                            });
                            target.Battle.ActiveCreature = target;
                            List<Option> options = await target.Battle.GameLoop.CreateActions(target, target.Possibilities, null);
                            if (await AlternateTaskImplements.OfferOptions(target, options, true))
                            {
                                target.AddQEffect(RespondedToTactic(caster));
                                reload = true;
                                acted = true;
                            }
                            target.Battle.ActiveCreature = original;
                        }
                    }
                    if (!CanTakeReaction(useDrilledReactions, target, caster))
                        continue;
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
                    target.RegeneratePossibilities();
                    List<ICombatAction> actions = target.Possibilities.Filter(ap =>
                    {
                        if (ap.CombatAction.HasTrait(Trait.Ranged) && (ap.CombatAction.Name.StartsWith("Strike") ||
                                                                       ap.CombatAction.Name.StartsWith("Throw")) && ap.CombatAction.ActionCost == 1)
                        {
                            ap.CombatAction.ActionCost = 0;
                            ap.RecalculateUsability();
                            return true;
                        }
                        if (SpellDealsDamage(ap.CombatAction) && ap.CombatAction.HasTrait(Trait.Spell) && ap.CombatAction.HasTrait(Trait.Cantrip) && !reload)
                        {
                            ap.CombatAction.ActionCost = 0;
                            ap.RecalculateUsability();
                            return true;
                        }
                        return false;
                    }).CreateActions(true);
                    List<Option> options2 = [];
                    foreach (ICombatAction action in actions)
                    {
                        AlternateTaskImplements.AddDirectUsageOnACreatureOptions(enemy, action.Action, options2);
                    }
                    options2.Add(new CancelOption(true));
                    Option which = (await target.Battle.SendRequest(new AdvancedRequest(target, "Make a ranged attack" +
                        (reload || !CanCastDamageCantrip(target) ? $"{(usedDrill ? "" : " as a reaction")}?" : $" or cast a cantrip{(usedDrill ? "" : " as a reaction")}?"), options2)
                    {
                        IsMainTurn = false,
                    })).ChosenOption;
                    switch (which)
                    {
                        case CreatureOption creatureOption:
                        {
                            await creatureOption.Action.Invoke();
                            if (!reload)
                                target.AddQEffect(RespondedToTactic(caster));
                            acted = true;
                            break;
                        }
                        case CancelOption:
                        {
                            if (usedDrill)
                                RemoveDrilledExpended(caster);
                            if (lostReaction)
                                target.Actions.RefundReaction();
                            if (animalReact)
                                target.AddQEffect(AnimalReaction(caster));
                            break;
                        }
                    }
                    if (enemy.DeathScheduledForNextStateCheck || !enemy.Alive)
                        break;
                }
                if (!acted)
                {
                    spell.RevertRequested = true;
                    return;
                }
                caster.AddQEffect(new QEffect {Id = MQEffectIds.ReadyAimFire});
            });
        return readyAim;
    }

    internal static CombatAction TheBiggerTheyAre(Creature owner)
    {
        CombatAction theBiggerTheyAre = new CombatAction(owner, MIllustrations.Reposition,
            "The Bigger They Are",
            [MTraits.Brandish, MTraits.Commander, MTraits.Tactic, Trait.Basic],
            "Signal a squadmate within the aura of your commander's banner. That squadmate can attempt to Reposition, Shove, or Trip a target within their reach as a free action. Each other squadmate who is adjacent to the original squadmate or the target can attempt to assist with the maneuver as a reaction. For each squadmate who assists in this way, the original squadmate increases the maximum size of creature they can target (for example, if a total of two squadmates participated in this maneuver, the initial squadmate could target a creature up to two sizes larger than them.) The original squadmate gains a circumstance bonus on their check to Reposition, Shove, or Trip equal to the number of additional squadmates who assisted in the maneuver (maximum +4).",
            new CreatureTarget(RangeKind.Ranged,
                [
                    new BrandishRequirement(), new FriendCreatureTargetingRequirement(), new InBannerAuraRequirement(),
                    new SquadmateTargetRequirement(), new TacticResponseRequirement()
                ],
                (_, _, _) => int.MinValue)
                .WithAdditionalConditionOnTargetCreature((_, target) => target.Battle.AllCreatures.Any(cr => cr.EnemyOf(target) &&
                    ((MeleeReachCreatureTargetingRequirement.WithWeaponOfTrait(Trait.Trip).Satisfied(target, cr) &&
                      (target.WieldsItem(Trait.Trip) || target.HasFreeHand))
                     || (MeleeReachCreatureTargetingRequirement.WithWeaponOfTrait(Trait.Shove).Satisfied(target, cr) && (target.WieldsItem(Trait.Shove) || target.HasFreeHand))
                     )) ? Usability.Usable : Usability.CommonReasons.TargetOutOfReach))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.DropProne)
            .WithEffectOnEachTarget(async (spell, caster, target, _) =>
            {
                Trait bestManeuver = target.Weapons.FirstOrDefault(wp => wp.HasTrait(Trait.Reach) && (wp.HasTrait(Trait.Grapple) || wp.HasTrait(Trait.Trip) || wp.HasTrait(Trait.Shove)))?.Traits.FirstOrDefault(t => t == Trait.Reach) ?? (target.Weapons.Any(wp => wp.HasTrait(Trait.Trip)) ? Trait.Trip : target.Weapons.Any(wp => wp.HasTrait(Trait.Shove)) ? Trait.Shove : Trait.Grapple);
                CombatAction targetingAction = CombatAction.CreateSimple(target, spell.Name, Trait.DoNotShowInCombatLog,
                    Trait.DoNotShowOverheadOfActionName);
                targetingAction.Target = Target.ReachWithWeaponOfTrait(bestManeuver);
                targetingAction.Illustration = spell.Illustration;
                if (!targetingAction.CanBeginToUse(target) || !await target.Battle.GameLoop.FullCast(targetingAction) || targetingAction.ChosenTargets.ChosenCreature is not {} enemy)
                {
                    spell.RevertRequested = true;
                    return;
                }
                List<Creature> squadmates = target.Battle.AllCreatures.Where(c => c != caster && c != target && IsSquadmate(caster, c) &&
                    (c.IsAdjacentTo(target) || c.IsAdjacentTo(enemy)) && new BrandishRequirement().Satisfied(caster, c) && new TacticResponseRequirement().Satisfied(caster, c) && new ReactionRequirement().Satisfied(caster, c)).ToList();
                List<Creature> drilledTargets = DrilledTargets(squadmates, caster);
                List<Creature> reacters = [];
                foreach (Creature mate in squadmates)
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (!CanTakeReaction(useDrilledReactions, mate, caster))
                        continue;
                    if (useDrilledReactions && IsDrilledTarget(drilledTargets, mate))
                    {
                        caster.AddQEffect(DrilledReactionsExpended(caster));
                        usedDrill = true;
                    }
                    else if (!mate.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        mate.Actions.UseUpReaction();
                        lostReaction = true;
                    }
                    else if (mate.HasEffect(MQEffectIds.AnimalReaction))
                    {
                        mate.RemoveAllQEffects(qf => qf.Id == MQEffectIds.AnimalReaction);
                        animalReact = true;
                    }
                    if (!await mate.AskForConfirmation(mate.Illustration,
                            "Assist " + target.Name + " with a combat maneuver" +
                            (usedDrill ? "?" : " as a reaction?"), "Yes"))
                    {
                        if (usedDrill)
                            RemoveDrilledExpended(caster);
                        if (lostReaction)
                            mate.Actions.RefundReaction();
                        if (animalReact)
                            mate.AddQEffect(AnimalReaction(caster));
                        continue;
                    }
                    reacters.Add(mate);
                    mate.AddQEffect(RespondedToTactic(caster));
                    mate.Battle.Log($"{{Navy}}{{b}}{mate.Name}{{/b}}{{/}} assists {{Navy}}{{b}}{target.Name}{{/b}}{{/}}.");
                }
                QEffect biggerTheyAre = new()
                {
                    Id = MQEffectIds.TheBiggerTheyAre,
                    Value = reacters.Count,
                    BonusToSkillChecks = (_, action, targ) =>
                        targ == enemy && (action.ActionId == ActionId.Trip || action.ActionId == ActionId.Shove ||
                                          action.ActionId == MActionIds.Reposition)
                            ? new Bonus(Math.Min(reacters.Count, 4), BonusType.Circumstance, "The Bigger They Are")
                            : null
                };
                if (reacters.Count > 0)
                    target.AddQEffect(biggerTheyAre);
                target.RegeneratePossibilities();
                List<ICombatAction> combatActions = target.Possibilities.Filter(ap =>
                {
                    if (ap.CombatAction.ActionId != ActionId.Trip && ap.CombatAction.ActionId != ActionId.Shove &&
                        ap.CombatAction.ActionId != MActionIds.Reposition)
                        return false;
                    ap.CombatAction.ActionCost = 0;
                    ap.RecalculateUsability();
                    return true;
                }).CreateActions(true);
                List<Option> options = [];
                foreach (ICombatAction combatAction in combatActions)
                {
                    AlternateTaskImplements.AddDirectUsageOnACreatureOptions(enemy, combatAction.Action, options);
                }
                Option which = (await target.Battle.SendRequest(new AdvancedRequest(target, "Choose to trip, shove, or reposition.", options)
                {
                    IsMainTurn = false
                })).ChosenOption;
                await which.Action.Invoke();
                target.AddQEffect(RespondedToTactic(caster));
                target.RemoveAllQEffects(qf => qf.Id == MQEffectIds.TheBiggerTheyAre);
            });
        return theBiggerTheyAre;
    }

    internal static CombatAction MirroredWall(Creature owner)
    {
        List<Creature> squadmates = owner.Battle.AllCreatures.Where(cr => IsSquadmate(owner, cr)).ToList();
        bool canRaiseOrRaised = squadmates.Any(mate =>
            new CanRaiseOrCastShieldRequirement().Satisfied(mate, mate) || mate.HasEffect(QEffectId.ShieldSpell) ||
            mate.HasEffect(QEffectId.RaisingAShield));
        CombatAction mirroredWall = CombatAction.CreateAction(owner, MIllustrations.CreateIllustration("MirroredWall"),
                "Mirrored Wall",
                [MTraits.Commander, MTraits.Tactic, Trait.Visual, Trait.Basic],
                "Once per encounter: all of your squadmates can Raise a Shield or cast shield as a reaction. Then, signal a squadmate within the aura of your commander's banner who currently has a shield raised (including spellcasting allies with an active casting of the shield cantrip), and choose an enemy within 60 feet. The formation bounces light off the raised shield and into the enemy’s eyes; the target must succeed at a Fortitude saving throw against your class DC or become blinded for 1 round (on a critical failure, the creature remains dazzled for 3 rounds after the blindness ends)." +
                "\n\nYou can signal additional allies with raised shields to participate in this tactic; the target takes a circumstance penalty on this save equal to the number of additional participating squadmates (to a maximum –4 circumstance penalty to the target’s save).",
                 owner.HasEffect(MQEffectIds.MirroredWall) ? Target.Uncastable("You can only use Mirrored Wall once per encounter.") : !canRaiseOrRaised ? Target.Uncastable("This tactic could not do anything.") : AllSquadmateWithReactionTarget(owner),
                2,
                SfxName.PowerfulLight, null)
            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
            {
                QEffect blocker = new() { Id = MQEffectIds.MirroredWall };
                List<Creature> drilledTargets = DrilledTargets(targets, caster);
                List<Creature> responded = [];
                foreach (Creature target in targets.ChosenCreatures.Where(cr => new CanRaiseOrCastShieldRequirement().Satisfied(cr, cr)))
                {
                    bool useDrilledReactions = UseDrilledReactions(caster);
                    bool usedDrill = false;
                    bool lostReaction = false;
                    bool animalReact = false;
                    if (!CanTakeReaction(useDrilledReactions, target, caster))
                        continue;
                    target.RegeneratePossibilities();
                    Possibilities shields = target.Possibilities.Filter(ap =>
                    {
                        if (ap.CombatAction.ActionId != ActionId.RaiseShield && ap.CombatAction.SpellId != SpellId.Shield)
                            return false;
                        if (ap.CombatAction.Name == "Raise shield (Devoted Guardian)")
                            return false;
                        ap.CombatAction.ActionCost = 0;
                        ap.RecalculateUsability();
                        return true;
                    });
                    if (shields.CreateActions(true).Count == 0)
                        continue;
                    if (!await target.AskForConfirmation(target.Illustration, "Raise a shield or cast the shield spell?", "Yes"))
                        continue;
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
                    Creature? original = target.Battle.ActiveCreature;
                    target.Possibilities = shields;
                    target.Battle.ActiveCreature = target;
                    List<Option> options = await target.Battle.GameLoop.CreateActions(target, target.Possibilities, null);
                    options.Add(new CancelOption(true));
                    if (await AlternateTaskImplements.OfferOptions(target, options, true))
                    {
                        responded.Add(target);
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
                    target.Battle.ActiveCreature = original;
                }
                Creature? mirror = await caster.Battle.AskToChooseACreature(caster, targets.ChosenCreatures.Where(cr =>
                        (cr.HasEffect(QEffectId.RaisingAShield) || cr.HasEffect(QEffectId.ShieldSpell)) && new InBannerAuraRequirement().Satisfied(caster, cr)).ToList(), spell.Illustration, 
                    "Choose a squadmate who is raising a shield or has an active casting of shield.", "", "End tactic");
                if (mirror == null)
                { 
                    if (responded.Count == 0) spell.RevertRequested = true;
                    else
                    {
                        foreach (Creature responder in responded)
                        {
                            responder.AddQEffect(RespondedToTactic(caster));
                        }

                        caster.AddQEffect(blocker);
                    }
                    return;
                }
                CombatAction reflect = new CombatAction(mirror, spell.Illustration, "Mirrored Wall", [Trait.Visual],
                        "The formation bounces light off the raised shield and into the enemy’s eyes; the target must succeed at a Fortitude saving throw against your class DC or become blinded for 1 round (on a critical failure, the creature remains dazzled for 3 rounds after the blindness ends).", Target.Ranged(12))
                    .WithSavingThrow(new SavingThrow(Defense.Fortitude, caster.ClassDC(MTraits.Commander)))
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (_, _, target, result) =>
                    {
                        if (result >= CheckResult.Success)
                            return;
                        QEffect blinded = QEffect.Blinded().WithExpirationAtStartOfSourcesTurn(caster, 1);
                        if (result == CheckResult.CriticalFailure)
                        {
                            blinded.WhenExpires = effect =>
                            {
                                effect.Owner.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(caster, 3));
                            };
                        }
                        target.AddQEffect(blinded);
                    });
                if (!reflect.CanBeginToUse(mirror))
                {
                    if (responded.Count == 0) spell.RevertRequested = true;
                    else
                    {
                        foreach (Creature responder in responded)
                        {
                            responder.AddQEffect(RespondedToTactic(caster));
                        }

                        caster.AddQEffect(blocker);
                    }
                    return;
                }
                if (!responded.Contains(mirror))
                {
                    responded.Add(mirror);
                }
                List<Creature> helpers = [];
                foreach (Creature mate in targets.ChosenCreatures.Where(cr => cr != mirror &&
                                                                              (cr.HasEffect(QEffectId.RaisingAShield) ||
                                                                               cr.HasEffect(QEffectId.ShieldSpell))))
                {
                    if (!responded.Contains(mate))
                    {
                        if (await mate.AskForConfirmation(mate.Illustration, "Respond to Mirrored Wall?", "Yes"))
                        {
                            responded.Add(mate);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    helpers.Add(mate);
                }
                QEffect debuff = new()
                {
                    BonusToDefenses = (_, action, _) => action == reflect ? new Bonus(Math.Max(-helpers.Count, -4), BonusType.Circumstance, "Mirrored Wall", false) : null
                };
                if (helpers.Count > 0)
                {
                    reflect.WithPrologueEffectOnChosenTargetsBeforeRolls(async (_, _, chosen) =>
                    {
                        if (chosen.ChosenCreature == null)
                            return;
                        chosen.ChosenCreature.AddQEffect(debuff);
                    });
                }
                await mirror.Battle.GameLoop.FullCast(reflect);
                debuff.ExpiresAt = ExpirationCondition.Immediately;
                foreach (Creature responder in responded)
                {
                    responder.AddQEffect(RespondedToTactic(caster));
                }

                caster.AddQEffect(blocker);
            });
        return mirroredWall;
    }
}