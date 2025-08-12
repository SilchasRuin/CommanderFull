*"A leader leads by example, not by force"*

**The Art of War**, Sun Tzu

A battlefield leader and supporter. Make use of clever tactics and potent buffs to lead your allies to victory.

⚠️ Required Mod ⚠️
This mod requires my [Exploration Activities](https://steamcommunity.com/sharedfiles/filedetails/?id=3527574947) mod, as it adds the Warfare Lore skill. Additionally, the **Battle Planner** skill feat from this mod has an alternate effect for level 3+ Commanders.

⚠️ Optional Mods ⚠️
Commander has several features based on **Recall Knowledge**, these require [Dawnni Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3163146733) and apply to the **Recall Weakness** action provided by that mod.
If you take the Cadet background from my Exploration Activities mod and choose the Commander class, if you have Pixie1001's [Bundle of Backgrounds](https://steamcommunity.com/sharedfiles/filedetails/?id=3348552008&searchtext=backgrounds) mod, you will gain the **Fount of Knowledge** skill feat instead of **Additional Lore - Warfare Lore**.

# [Class Features](https://2e.aonprd.com/Classes.aspx?ID=66)
**Key Attribute** Intelligence
**Perception** Expert
**Offenses** trained in up to Martial Weapons
**Defenses** 8 hp, trained in up to Heavy Armor, trained Fortitude, expert Reflex and Will
**Skills** 2 + Society + Warfare Lore + Intelligence; standard skill increases and feats

**Level 1:** Commander's Banner, Tactics, Drilled Reactions, Shield Block, Commander Feat

**Level 2:** Commander Feat

**Level 3:** Warfare Expertise

**Level 4:** Commander Feat

**Level 5:** Military Expertise

**Level 6:** Commander Feat

**Level 7:** Expert Tactician

**Level 8:** Commander Feat

**Level 9:** Armor Expertise, Commanding Will

## [Class Feats](https://2e.aonprd.com/Feats.aspx?Traits=855&values-to=level%3A8&sort=level-asc+name-asc&display=table&columns=pfs+source+rarity+trait+level+prerequisite+summary+spoilers)
* **(1st)** Armor Regiment Training, Combat Assessment, Commander's Companion, Deceptive Tactics, Officer's Medical Training, Plant Banner
* **(2nd)** Adaptive Stratagem, Defensive Swap, Guiding Shot, Rapid Assessment, Set-up Strike, Tactical Expansion
* **(4th)** Banner Twirl, Banner's Inspiration, Observational Analysis, Shielded Recovery, Unsteadying Strike
* **(6th)** Battle-Tested Companion, Claim the Field, Efficient Preparation, Reactive Strike, Shield Warden
* **(8th)** Defiant Banner, Officer's Education, Rallying Banner, Unrivaled Analysis

## [Tactics](https://2e.aonprd.com/Tactics.aspx)
* **Mobility:** Defensive Retreat, Gather To Me!, Naval Training, Passage of Lines, Protective Screen
* **Offensive:** Coordinating Maneuvers, Double Team, End It!, Pincer Attack, Reload!, Shields Up!, Strike Hard!, Tactical Takedown
* **Expert:** Buckle-cut Blitz, Demoralizing Charge, Slip and Sizzle, Stupefying Raid
Mountaineering Training is not implementable without a completely homebrew effect. 
Alley-oop and Take the High Ground will be coming soon™.

## Notes on Implementations
* The Banner is implemented as an attachable item. Attach it to your weapon, shield, or animal companion's barding. As long as you have it in your inventory, you will benefit from the banner. If you do not equip it to anything, a basic banner will be added into your carried items at the start of combat (this mimics attaching your banner to a pole on your back).
* Tactics must be added to your Folio as a level up selection, and then prepared as a pre-combat preparation, this is to implement the ability to swap your tactics after 10 minutes of training time. Currently, all pre-combat preparations are treated as optional, so you can load into a battle with none prepared. I have a warning if that happens, and I'm considering adding something that will automatically load tactics if you didn't prepare any.
* The character that uses Drilled Reactions is either determined by a toggle (set in combat or as a pre-combat preparation) or automatically determined (if the tactic targets only 1 creature, or you do not have a default target set).
* Tactics default to Auditory, but can be change to visual and back through a toggle.

## Differences from Tabletop
I endeavored to keep things the same as tabletop, but in some places that was not possible or would be too limiting. The differences are listed below.
* [Armor Regiment Training](https://2e.aonprd.com/Feats.aspx?ID=7792) Instead of it's tabletop effect, it allows you to ignore your armor's speed penalty and still allows you to rest in armor (thanks to @AnaseSkyrider).
* [Plant Banner](https://2e.aonprd.com/Feats.aspx?ID=7796) A planted banner cannot be destroyed, though it can be stolen. Instead of changing the banner's aura to a 40-foot burst, it becomes a 35-foot emanation.
* [Shielded Recovery](https://2e.aonprd.com/Feats.aspx?ID=7795) Dawnsbury does not implement a free hand requirement for medicine actions other than Battle Medicine, so references to changing those requirements has been removed.
* [Banner's Inspiration](https://2e.aonprd.com/Feats.aspx?ID=7804) Dawnsbury doesn't keep information regarding saving throws, or whether the ability is a mental effect on most effects. Therefore, the implementation is slightly different: in addition to reducing fear, stupefied is also reduced. Instead of rerolling the original saving throw, a will save is made against a standard DC by level check to remove Confusion or Paralysis.
* [Claim the Field](https://2e.aonprd.com/Feats.aspx?ID=7809) Where the banner is placed is a bit wonky because of how the Tile it should be placed on is determined.
* [Naval Training](https://2e.aonprd.com/Tactics.aspx?ID=4) Grants a swim speed equal to your land speed instead of 20 feet. The Passive effect is not implemented as Dawnsbury does not have Athletics checks to swim at this time.
* [Shield's Up](https://2e.aonprd.com/Tactics.aspx?ID=12) Applies to Parry actions granted by feats as well as those granted by weapons.
* [Slip and Sizzle](https://2e.aonprd.com/Tactics.aspx?ID=18) At this time, the spellcaster cannot cast spells with variable action costs due to limitations in creating those actions.
* [Commander Dedication](https://2e.aonprd.com/Feats.aspx?ID=7886) If you are already trained in Warfare Lore when you take this dedication, you can become trained in any skill instead of  any lore.

### Attributions and Thanks
* Thanks to Ubik2, who made the [BattleCry Playtest](https://steamcommunity.com/sharedfiles/filedetails/?id=3246164409&searchtext=battlecry) mod, for their blessing to make this mod and for some of the code I used as a basis for some functionality.
* Thanks to AnaseSkyrider for help with coding some functionality (specifically, adding a tactics section into the character sheet and resting in armor).
* Thanks to SudoTrainer for the code to add the Banner to the inventory (it's based off Thaumaturge's functionality).
* Thanks to Petr for answering my questions and giving advice on implementation and for Dawnsbury as a whole.
* Thanks to everyone else in the Dawnsbury modding community who have helped me out.
* [Red Cloth Banners by upklyak from FreePik](https://www.freepik.com/free-vector/medieval-red-cloth-banners-set-stone-background_152129960.htm#fromView=search&page=1&position=6&uuid=b0b67d47-8dea-417c-b3e0-c02a94d1c2ef&query=Fantasy+Flag)
* [Medieval Complements from FreePik](https://www.freepik.com/free-vector/flat-variety-medieval-complements_1356011.htm#fromView=search&page=1&position=8&uuid=c12e9817-72dc-43ce-860d-1085dbcaff7f&query=flag+fantasy)
* [2D Skills Icon Set by Sahil Gandhi](https://assetstore.unity.com/packages/2d/gui/icons/2d-skills-icon-set-handpainted-210622)
* [Aeromancer Skill Icons from CraftPix](https://craftpix.net/freebies/free-50-rpg-aeromancer-skill-icons/)
* [Swordsman Skill Icons from CraftPix](https://craftpix.net/freebies/free-swordsman-skills-icon-pack/)
* [Night Elf Skill Icons from CraftPix](https://craftpix.net/freebies/free-rpg-night-elf-skill-icons/ )
* [Knight Skill Icons from CraftPix](https://craftpix.net/freebies/free-rpg-knight-skill-icons/)
* Photo by [Birmingham Museums Trust](https://unsplash.com/@birminghammuseumstrust?utm_content=creditCopyText&utm_medium=referral&utm_source=unsplash) on [Unsplash](https://unsplash.com/photos/a-painting-of-a-group-of-men-on-horses-5EUh-tq31eA?utm_content=creditCopyText&utm_medium=referral&utm_source=unsplash)
