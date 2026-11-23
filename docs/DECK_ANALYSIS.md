# BrawlTCG deck analysis

32 decks played a full round robin: 26 new decks plus the 6 premade decks from v1.2.0. Every deck played every other deck 200 times, **99,200 games** in total. All 32 decks now ship with the game.

## How this was done

1. **26 new decks** were built around one plan each (rush, lockdown, removal, scythe executions, stage control, ramp, swarm, card advantage and more). Every element and every v1.2.0 card got at least one try, including Alpine Kaya, the new gauntlets, Tombstone, A-Oooooo, Sozin's Comet and Full Eclipse.
2. **Each new deck was tuned by simulation.** Up to 50 single-card changes were tried per deck, each tested against the other 31 decks. A change was kept only if it won on two separate sets of games, so lucky streaks didn't ratchet a deck.
3. **Final round robin:** all 32 decks, 200 games per matchup. Each deck goes first in half its games. Both players get the starting-hand redraw. A game still undecided after 160 turns counts as half a win for each side.
4. **The player is the bot at Normal difficulty** (the in-game Normal: best move plus a one-move lookahead). It plays the same rules as the game, with 50 starting health. The bot never sees the real deck order or the opponent's hand.

**Tiers by overall win rate:** **S** 70%+, **A** 60–70%, **B** 45–60%, **C** under 45%.

**Read the numbers with care.** These are bot-vs-bot results. A human who plays a combo deck well (Last Hour's Death's Hour, Hammer Time's Hammer Gimp, Scythe Executioners) can do better than the bot does with it. Each matchup has 200 games, so a single matchup number is accurate to about ±7 points. Overall win rates average 6,200 games, so they're accurate to about ±1.3 points.

## Key findings

- **Silent Galaxy still rules.** The top three decks are Cosmic decks built around it (Cosmic Lockdown 77.1%, Galaxy Lock 72.7%, Cosmic Lancers 67.9%). Decks that only win with non-Cosmic damage collapse against it: Rock Wall wins 4% against Galaxy Lock, Iron Wall 2%. The previous tournament showed the same, so it's a balance issue, not a quirk of these lists.
  - The only real counter in the pool is replacing the stage. Mastermind's Matrix does it, which is why Matrix Masters is #4 and the best non-Cosmic deck against Cosmic Lockdown.
  - **Possible fixes:** let non-Cosmic legends deal reduced damage instead of none; raise Silent Galaxy's cost (it is 5); or limit it to a number of turns.
- **Card draw beats the lock.** Treasure Hunt is the only deck that goes even (50%) with Cosmic Lockdown. Its 13 chest cards and three Witch Parties outlast the lock.
- **Shadow is the deepest element.** It has five decks in the top 12, with removal (Tombstone, Bouncy Bomb) and big bodies (Dark Mage, Kitsune Hattori, Raymesis).
- **Arctic is the weakest element.** Frozen Tide (#14) is the only Arctic deck above 30%.
  - The new Arctic decks finish last: Frozen Fortress 27.7%, Eclipse Tide 14.1%. Arctic legends hit softly (Alpine Kaya 2 ATK, Atlantean Ada 2 ATK, Snowman Kor 0 ATK), and Full Eclipse costs 7, which is too late to save a weak board.
  - **Possible fixes:** a cheaper Full Eclipse (5–6), or an Arctic legend with real attack.
- **Seven-cost stages come too late.** Tuning cut Sozin's Comet down to one copy in Sozin's Inferno, and Full Eclipse decks rank last. The bot rarely reaches 7 Essence while the board still matters.
- **Glass cannons don't work.** Astro Commander (14 ATK, 3 HP) decks are in the C tier, because anything kills them before they swing twice.
- **The v1.2 premade decks hold up well.** All six are in the top 14 and four are in the top 9, so the new decks join them rather than replace them.

## Tier list (all 32 decks)

| # | Tier | Deck | Element | Win rate | Origin |
|---|---|---|---|---|---|
| 1 | **S** | Cosmic Lockdown | Cosmic | 77.1% | v1.2 premade |
| 2 | **S** | Galaxy Lock | Cosmic | 72.7% | new |
| 3 | **A** | Cosmic Lancers | Cosmic | 67.9% | new |
| 4 | **A** | Matrix Masters | Shadow | 67.8% | new |
| 5 | **A** | Tombstone Control | Shadow | 65.2% | new |
| 6 | **A** | Wild Axe | Wild | 64.8% | v1.2 premade |
| 7 | **A** | Scythe Executioners | Cosmic | 64.3% | new |
| 8 | **A** | Treasure Hunt | Magic | 62.3% | v1.2 premade |
| 9 | **A** | Hideout Rush | Shadow | 61.7% | v1.2 premade |
| 10 | **B** | Briar Garden | Nature | 58.6% | new |
| 11 | **B** | Cursed Kunai | Shadow | 58.1% | new |
| 12 | **B** | Kitsune Blades | Shadow | 57.8% | new |
| 13 | **B** | Scorched Earth | Fire | 57.4% | v1.2 premade |
| 14 | **B** | Frozen Tide | Arctic | 54.4% | v1.2 premade |
| 15 | **B** | Sozin's Inferno | Fire | 54.2% | new |
| 16 | **B** | Rock Wall | Wild | 53.8% | new |
| 17 | **B** | Ninja Swarm | Shadow | 52.5% | new |
| 18 | **B** | Workshop Forge | Fire | 50.7% | new |
| 19 | **B** | Wild Stampede | Wild | 45.2% | new |
| 20 | **C** | Mustafar Burn | Fire | 44.4% | new |
| 21 | **C** | Deathcap Storm | Nature | 43.1% | new |
| 22 | **C** | Last Hour | Shadow | 41.5% | new |
| 23 | **C** | Fire Nation | Fire | 39.2% | new |
| 24 | **C** | Treasure Engine | Magic | 39.1% | new |
| 25 | **C** | Enchanted Court | Magic | 37.0% | new |
| 26 | **C** | Astro Blasters | Cosmic | 36.3% | new |
| 27 | **C** | Iron Wall | Nature | 35.7% | new |
| 28 | **C** | Hammer Time | Nature | 34.0% | new |
| 29 | **C** | Blaster Barrage | Shadow | 32.4% | new |
| 30 | **C** | Adrenaline Rush | Nature | 28.9% | new |
| 31 | **C** | Frozen Fortress | Arctic | 27.7% | new |
| 32 | **C** | Eclipse Tide | Arctic | 14.1% | new |

## Top 20 in detail

### 1. Cosmic Lockdown (S tier, 77.1%)

**Cosmic** · 13 Essence · 11 legends · average cost 2.93

Silent Galaxy lock. Every legend is Cosmic, so once Silent Galaxy is down only this deck can deal damage. Aurora Brynn (10/16) and Artemis carry the board while four Chests and a Cosmic Chest keep the hand full. Its only even matchup is Treasure Hunt, whose card flood outlasts the lock.

- **Main cards:** Silent Galaxy, Aurora Brynn, Artemis, Orion, Shattering Star, Chest
- **Best matchups:** Eclipse Tide 97%, Iron Wall 94%, Frozen Fortress 94%
- **Worst matchups:** Treasure Hunt 50%, Cosmic Lancers 54%, Matrix Masters 58%
- **Legends:** 4× Aurora Brynn, 3× Artemis, 2× Orion, 1× Wu Shang, 1× Breaker Shang
- **Weapons:** 3× Shattering Star, 1× Shooting Star, 1× Sweet Magi
- **Battles:** 4× Chest, 1× Cody Travis, 1× Bouncy Bomb, 1× Cosmic Chest, 1× Scythe Forever
- **Stages:** 3× Silent Galaxy
- **Essence:** 13

### 2. Galaxy Lock (S tier, 72.7%)

**Cosmic** · 13 Essence · 13 legends · average cost 3.00

The all-in version of the lock: four Silent Galaxies, so the stage is almost always up. Aurora Brynn hits hardest, Wu Shang's Gauntlet Down Sig (two Infinite & Absolute) hits every enemy legend, and Breaker Shang crafts more Cosmic cards. It crushes decks that can't remove the stage (Iron Wall 98%, Rock Wall 96%) but loses the Cosmic mirror to Cosmic Lockdown, which has more card draw.

- **Main cards:** Silent Galaxy x4, Aurora Brynn, Wu Shang, Artemis, Breaker Shang, Infinite & Absolute
- **Best matchups:** Iron Wall 98%, Rock Wall 96%, Eclipse Tide 96%
- **Worst matchups:** Cosmic Lockdown 38%, Cosmic Lancers 46%, Hideout Rush 53%
- **Legends:** 4× Aurora Brynn, 3× Wu Shang, 3× Artemis, 3× Breaker Shang
- **Weapons:** 3× Infinite & Absolute, 3× Shattering Star, 2× Starry Scythe
- **Battles:** 2× Cosmic Chest
- **Stages:** 4× Silent Galaxy
- **Essence:** 13

### 3. Cosmic Lancers (A tier, 67.9%)

**Cosmic** · 11 Essence · 12 legends · average cost 2.45

Cheap Cosmic midrange. Artemis and Orion (12 HP for 3) come down early, and two Galaxy Lances unlock Flamethrower (+4). Four Cosmic Chests refuel, and two Silent Galaxies close out games. It has the lowest curve of the Cosmic decks, so it is fastest out of the gate.

- **Main cards:** Artemis, Orion, Galaxy Lance, Cosmic Chest, Aurora Brynn, Silent Galaxy
- **Best matchups:** Eclipse Tide 94%, Frozen Fortress 91%, Iron Wall 90%
- **Worst matchups:** Hideout Rush 44%, Cosmic Lockdown 46%, Scythe Executioners 49%
- **Legends:** 4× Artemis, 4× Orion, 2× Aurora Brynn, 2× Breaker Shang
- **Weapons:** 4× Galaxy Lance, 3× Shattering Star, 2× Shooting Star, 2× Sweet Magi
- **Battles:** 4× Cosmic Chest
- **Stages:** 2× Silent Galaxy
- **Essence:** 11

### 4. Matrix Masters (A tier, 67.8%)

**Shadow** · 13 Essence · 12 legends · average cost 3.44

Big Shadow bodies plus stage denial. Mastermind sets Matrix when played, which throws out the opponent's stage (Silent Galaxy, Atlantis, Mustafar). That makes it the best non-Cosmic deck against Cosmic Lockdown (42%), though Galaxy Lock, with four Silent Galaxies, still beats it 60–40. Dark Mage (11/12) and Kitsune Hattori (15/13) with Necro Aura and Shadow Edge do the killing; Bouncy Bomb and Tombstone clear blockers.

- **Main cards:** Mastermind, Dark Mage, Kitsune Hattori, Necro Aura, Bouncy Bomb, Forgeborne Chest
- **Best matchups:** Frozen Fortress 98%, Eclipse Tide 94%, Mustafar Burn 88%
- **Worst matchups:** Galaxy Lock 40%, Cosmic Lockdown 42%, Cosmic Lancers 46%
- **Legends:** 4× Dark Mage, 4× Mastermind, 3× Kitsune Hattori, 1× Raymesis
- **Weapons:** 4× Necro Aura, 3× Shadow Edge
- **Battles:** 3× Bouncy Bomb, 3× Forgeborne Chest, 1× Tombstone, 1× Cursed Kunai
- **Essence:** 13

### 5. Tombstone Control (A tier, 65.2%)

**Shadow** · 13 Essence · 9 legends · average cost 3.26

Removal-heavy Shadow control. Three Tombstones and two Bouncy Bombs remove the opponent's best legends, while Dark Mage and Raymesis (9/16) hold the board. Forgeborne Chests and Dark Duo keep the Shadow legends coming. It is weak to Galaxy Lock (20%) because it can't remove a stage.

- **Main cards:** Tombstone, Dark Mage, Raymesis, Necro Aura, Forgeborne Chest, Dark Duo
- **Best matchups:** Frozen Fortress 95%, Eclipse Tide 94%, Hammer Time 86%
- **Worst matchups:** Galaxy Lock 20%, Cosmic Lockdown 23%, Cosmic Lancers 41%
- **Legends:** 3× Dark Mage, 3× Raymesis, 1× Mastermind, 1× Artemis, 1× Caspian
- **Weapons:** 4× Necro Aura, 2× Boiling Point, 2× Sleight of Hand
- **Battles:** 3× Tombstone, 3× Forgeborne Chest, 2× Dark Duo, 2× Bouncy Bomb
- **Essence:** 13

### 6. Wild Axe (A tier, 64.8%)

**Wild** · 14 Essence · 8 legends · average cost 2.38

v1.2 premade. Danders on turn one, The Minotaur and Raymesis later, with eight chests (Wild Chest and Chest) for reach.

- **Main cards:** Dander, Wild Chest, Chest, The Minotaur, Nightmare Claws
- **Best matchups:** Eclipse Tide 96%, Frozen Fortress 93%, Treasure Engine 82%
- **Worst matchups:** Galaxy Lock 32%, Cosmic Lockdown 34%, Cosmic Lancers 36%
- **Legends:** 3× Dander, 2× The Minotaur, 1× Raymesis, 1× Heatblast, 1× Jhala
- **Weapons:** 3× Nightmare Claws, 1× Buzz Axe, 1× Boiling Point
- **Battles:** 4× Wild Chest, 4× Chest, 2× Bouncy Bomb, 1× Cody Travis, 1× Did Shaw Win Yet?, 1× Adrenaline
- **Essence:** 14

### 7. Scythe Executioners (A tier, 64.3%)

**Cosmic** · 14 Essence · 12 legends · average cost 2.88

Scythe Gimp: Artemis and Iron Lady one-hit-KO any legend once they hold three scythes. Shooting Star, Starry Scythe and Scythe Forever (crafts three scythes) feed them, and Dark Mage is the big Scythe Slash body. It beats Treasure Hunt hardest of anyone (64%).

- **Main cards:** Artemis, Dark Mage, Shooting Star, Starry Scythe, Scythe Forever, Cosmic Chest
- **Best matchups:** Frozen Fortress 92%, Eclipse Tide 89%, Mustafar Burn 86%
- **Worst matchups:** Cosmic Lockdown 40%, Galaxy Lock 44%, Matrix Masters 45%
- **Legends:** 4× Dark Mage, 4× Artemis, 2× Iron Lady, 1× The Rock, 1× Mastermind
- **Weapons:** 4× Shooting Star, 4× Starry Scythe, 1× Galaxy Lance
- **Battles:** 3× Cosmic Chest, 2× Scythe Forever
- **Essence:** 14

### 8. Treasure Hunt (A tier, 62.3%)

**Magic** · 16 Essence · 6 legends · average cost 2.88

v1.2 premade. Pure card advantage: 13 chest cards and three Witch Parties, few legends of its own. It is the only deck that goes even with Cosmic Lockdown.

- **Main cards:** Witch Party, Wild Chest, Forgeborne Chest, Chest, Breaker Shang
- **Best matchups:** Frozen Fortress 95%, Eclipse Tide 94%, Blaster Barrage 78%
- **Worst matchups:** Scythe Executioners 36%, Briar Garden 40%, Cosmic Lancers 44%
- **Legends:** 3× Breaker Shang, 2× Fait, 1× Ulgrim
- **Weapons:** 2× Sweet Magi, 1× Amethyst Scythe
- **Battles:** 3× Witch Party, 3× Wild Chest, 3× Forgeborne Chest, 3× Chest, 1× Cody Travis, 1× Dragon's Chest, 1× Adrenaline
- **Essence:** 16

### 9. Hideout Rush (A tier, 61.7%)

**Shadow** · 12 Essence · 11 legends · average cost 2.32

v1.2 premade. Shadow/Wild aggro under Evil Hideout (+3 ATK): Ninja Spirit needs no weapon, Caspian draws, Forgeborne Chests refill.

- **Main cards:** Ninja Spirit, Evil Hideout, Forgeborne Chest, Nightmare Claws, Caspian
- **Best matchups:** Eclipse Tide 96%, Blaster Barrage 86%, Adrenaline Rush 86%
- **Worst matchups:** Cosmic Lockdown 34%, Rock Wall 42%, Wild Axe 43%
- **Legends:** 4× Ninja Spirit, 3× Caspian, 2× Dander, 1× Kitsune Hattori, 1× The Minotaur
- **Weapons:** 4× Nightmare Claws, 1× Boiling Point, 1× Rippers, 1× Shadow Edge
- **Battles:** 4× Forgeborne Chest, 2× Bouncy Bomb, 1× Dark Duo
- **Stages:** 3× Evil Hideout
- **Essence:** 12

### 10. Briar Garden (B tier, 58.6%)

**Nature** · 10 Essence · 15 legends · average cost 2.13

Nature go-wide. Faerie Queen gives every Nature legend +1/+1 whenever a Nature card is played, and almost every card is Nature. Briar Rose stuns with Blow a Kiss, Rayman and Forest Guardian hit, and Fangwild grows them every turn. Lowest curve of the B tier (2.13).

- **Main cards:** Faerie Queen, Briar Rose, Rayman, Forest Guardian, Law of the Land, Piercing Regret, Fangwild
- **Best matchups:** Frozen Fortress 86%, Hammer Time 85%, Eclipse Tide 82%
- **Worst matchups:** Galaxy Lock 31%, Cosmic Lockdown 32%, Kitsune Blades 44%
- **Legends:** 4× Briar Rose, 4× Rayman, 4× Faerie Queen, 3× Forest Guardian
- **Weapons:** 4× Law of the Land, 4× Piercing Regret, 2× Axe of Regrowth, 2× Spine-Chilling Fists
- **Battles:** 1× Adrenaline
- **Stages:** 2× Fangwild
- **Essence:** 10

### 11. Cursed Kunai (B tier, 58.1%)

**Shadow** · 11 Essence · 11 legends · average cost 2.62

Katar swarm under four Evil Hideouts. Cursed Kunai crafts a Katars legend and a Katars weapon, Dander attacks on turn one, and The Rock walls up while the +3 ATK stage pushes damage.

- **Main cards:** Cursed Kunai, Evil Hideout, The Rock, Dander, Nightmare Claws, Winter Daggers
- **Best matchups:** Eclipse Tide 97%, Enchanted Court 80%, Adrenaline Rush 80%
- **Worst matchups:** Matrix Masters 32%, Cosmic Lockdown 34%, Cosmic Lancers 40%
- **Legends:** 4× The Rock, 4× Dander, 2× Ninja Spirit, 1× Caspian
- **Weapons:** 3× Nightmare Claws, 3× Winter Daggers
- **Battles:** 4× Cursed Kunai, 2× Forgeborne Chest, 1× Death's Hour, 1× Bouncy Bomb
- **Stages:** 4× Evil Hideout
- **Essence:** 11

### 12. Kitsune Blades (B tier, 57.8%)

**Shadow** · 12 Essence · 11 legends · average cost 2.82

Sword and blaster Shadow midrange. Hellshot Hattori double shots, Dullahan Jiro summons Ninja Spirits, and Kitsune Hattori finishes. Tombstones handle walls, but it can't touch Silent Galaxy (12% vs Galaxy Lock).

- **Main cards:** Hellshot Hattori, Dullahan Jiro, Kitsune Hattori, Revolvers, Shadow Edge, Tombstone
- **Best matchups:** Eclipse Tide 94%, Iron Wall 80%, Adrenaline Rush 80%
- **Worst matchups:** Galaxy Lock 12%, Cosmic Lockdown 16%, Cosmic Lancers 25%
- **Legends:** 3× Hellshot Hattori, 3× Dullahan Jiro, 2× Kitsune Hattori, 2× Dark Mage, 1× Ninja Spirit
- **Weapons:** 4× Revolvers, 4× Shadow Edge, 2× Necro Aura
- **Battles:** 3× Bouncy Bomb, 2× Tombstone, 2× Forgeborne Chest
- **Essence:** 12

### 13. Scorched Earth (B tier, 57.4%)

**Fire** · 12 Essence · 14 legends · average cost 2.54

v1.2 premade. All Fire under Mustafar: every non-Fire legend loses HP each turn while Fire legends are immune.

- **Main cards:** Hothead Jiro, Molten Kor, Mustafar, Dragon's Chest, Iron Lady
- **Best matchups:** Eclipse Tide 98%, Adrenaline Rush 91%, Blaster Barrage 90%
- **Worst matchups:** Matrix Masters 22%, Tombstone Control 27%, Scythe Executioners 28%
- **Legends:** 4× Hothead Jiro, 3× Molten Kor, 2× Iron Lady, 2× Fury Shang, 1× Heatblast, 1× Jhala, 1× Seven
- **Weapons:** 2× Candlelit Scythe, 1× Searing Blade, 1× Magma Spear, 1× Ancestor's Flame
- **Battles:** 3× Dragon's Chest, 2× Chest, 1× Super Saiyan, 1× Bouncy Bomb
- **Stages:** 2× Mustafar
- **Essence:** 12

### 14. Frozen Tide (B tier, 54.4%)

**Arctic** · 13 Essence · 9 legends · average cost 2.67

v1.2 premade. Arctic control under Atlantis, with The Rock and Snowman Kor as walls and Sunken Chests refilling.

- **Main cards:** Atlantis, The Rock, Atlantean Ada, Sunken Chest, Ice Crusher
- **Best matchups:** Eclipse Tide 89%, Blaster Barrage 85%, Adrenaline Rush 82%
- **Worst matchups:** Matrix Masters 23%, Treasure Hunt 23%, Wild Axe 24%
- **Legends:** 3× The Rock, 3× Atlantean Ada, 2× Snowman Kor, 1× Hellshot Hattori
- **Weapons:** 3× Ice Crusher, 1× Glacier's Grace
- **Battles:** 4× Sunken Chest, 4× Chest, 2× Bouncy Bomb, 1× Cody Travis
- **Stages:** 3× Atlantis
- **Essence:** 13

### 15. Sozin's Inferno (B tier, 54.2%)

**Fire** · 14 Essence · 14 legends · average cost 3.04

Fire ramp. Molten Kor sets Mustafar, Iron Lady (7/15) and Heatblast carry the mid-game, and Sozin's Comet gives every Fire legend +5/+5 late. The tuning cut it to one Comet: at 7 Essence it arrives too late to be the plan.

- **Main cards:** Iron Lady, Heatblast, Molten Kor, Blazing Fire, Searing Blade, Dragon's Chest
- **Best matchups:** Eclipse Tide 90%, Iron Wall 82%, Frozen Fortress 82%
- **Worst matchups:** Cosmic Lockdown 28%, Matrix Masters 28%, Tombstone Control 29%
- **Legends:** 4× Iron Lady, 3× Heatblast, 3× Molten Kor, 2× Fury Shang, 1× Ulgrim, 1× Jhala
- **Weapons:** 3× Blazing Fire, 3× Searing Blade, 2× Boiling Point
- **Battles:** 3× Dragon's Chest
- **Stages:** 1× Sozin's Comet
- **Essence:** 14

### 16. Rock Wall (B tier, 53.8%)

**Wild** · 14 Essence · 7 legends · average cost 3.00

The Rock (4/20) times many: Did Shaw Win Yet? crafts three more. Chikara and Ice Crusher arm them, Super Saiyan makes one a threat, and Cody Travis zeroes the opponent's best attacker. It loses almost every game to Silent Galaxy (4% vs Galaxy Lock).

- **Main cards:** The Rock, Did Shaw Win Yet?, Chikara, Ice Crusher, Super Saiyan, Cody Travis
- **Best matchups:** Eclipse Tide 90%, Blaster Barrage 76%, Frozen Fortress 76%
- **Worst matchups:** Galaxy Lock 4%, Cosmic Lockdown 8%, Cosmic Lancers 12%
- **Legends:** 4× The Rock, 2× Fox Spirit, 1× The Minotaur
- **Weapons:** 4× Chikara, 3× Ice Crusher, 1× Nightmare Claws
- **Battles:** 3× Did Shaw Win Yet?, 2× Super Saiyan, 2× Cody Travis, 2× Bouncy Bomb, 2× Krabby Patty
- **Essence:** 14

### 17. Ninja Swarm (B tier, 52.5%)

**Shadow** · 11 Essence · 15 legends · average cost 2.38

Ninja Spirits and Danders flood the board under Evil Hideout. Dullahan Jiro summons more Ninja Spirits with a Shadow Edge, and Kitsune Hattori is the top end.

- **Main cards:** Ninja Spirit, Dander, Dullahan Jiro, Shadow Edge, Evil Hideout, Bouncy Bomb
- **Best matchups:** Eclipse Tide 93%, Adrenaline Rush 90%, Blaster Barrage 84%
- **Worst matchups:** Frozen Tide 20%, Scorched Earth 22%, Cosmic Lockdown 24%
- **Legends:** 4× Ninja Spirit, 4× Dander, 3× Dullahan Jiro, 2× Kitsune Hattori, 1× Faerie Queen, 1× Caspian
- **Weapons:** 4× Shadow Edge, 3× Nightmare Claws
- **Battles:** 3× Bouncy Bomb, 1× Tombstone, 1× Forgeborne Chest
- **Stages:** 2× Evil Hideout
- **Essence:** 11

### 18. Workshop Forge (B tier, 50.7%)

**Fire** · 14 Essence · 13 legends · average cost 2.46

Ulgrim sets The Workshop (a random card every turn instead of a draw), and Seven crafts four weapons at a time. Boiling Point, Magma Spear and Molten Cannon give Fire legends the elemental bonus.

- **Main cards:** Ulgrim, Seven, Boiling Point, Magma Spear, Molten Cannon
- **Best matchups:** Eclipse Tide 87%, Frozen Fortress 84%, Mustafar Burn 70%
- **Worst matchups:** Tombstone Control 25%, Cosmic Lockdown 28%, Cosmic Lancers 30%
- **Legends:** 4× Ulgrim, 4× Seven, 2× Heatblast, 2× Molten Kor, 1× Iron Lady
- **Weapons:** 4× Boiling Point, 4× Magma Spear, 3× Molten Cannon
- **Battles:** 2× Dragon's Chest
- **Essence:** 14

### 19. Wild Stampede (B tier, 45.2%)

**Wild** · 13 Essence · 10 legends · average cost 2.67

The Minotaur (10/15) behind early Danders. A-Oooooo gives every Wild legend +4 ATK, Adrenaline lets one attack twice, and Wild Chests refill.

- **Main cards:** The Minotaur, Dander, Nightmare Claws, Buzz Axe, A-Oooooo, Wild Chest
- **Best matchups:** Eclipse Tide 78%, Frozen Fortress 74%, Iron Wall 70%
- **Worst matchups:** Galaxy Lock 14%, Cosmic Lockdown 16%, Ninja Swarm 23%
- **Legends:** 4× The Minotaur, 3× Dander, 2× The Rock, 1× Fox Spirit
- **Weapons:** 4× Nightmare Claws, 3× Buzz Axe, 1× Rippers, 1× Chikara
- **Battles:** 3× Wild Chest, 2× A-Oooooo, 2× Adrenaline
- **Stages:** 1× Evil Hideout
- **Essence:** 13

### 20. Mustafar Burn (C tier, 44.4%)

**Fire** · 11 Essence · 14 legends · average cost 2.31

Molten Kor sets Mustafar so every non-Fire legend melts. Fury Shang buffs each Fire legend that follows, and Heatblast burns. Ancestor's Flame swords give the elemental bonus.

- **Main cards:** Molten Kor, Mustafar, Heatblast, Fury Shang, Ancestor's Flame, Dragon's Chest
- **Best matchups:** Eclipse Tide 93%, Adrenaline Rush 85%, Blaster Barrage 78%
- **Worst matchups:** Cosmic Lockdown 12%, Matrix Masters 12%, Scythe Executioners 14%
- **Legends:** 3× Heatblast, 3× Molten Kor, 3× Fury Shang, 3× Hothead Jiro, 2× Jhala
- **Weapons:** 4× Ancestor's Flame, 3× Blazing Fire, 2× Hot Lava
- **Battles:** 4× Dragon's Chest
- **Stages:** 2× Mustafar
- **Essence:** 11

## The other decks (ranks 21+)

Kept with their full lists. They lose too often against the decks above, but they still play.

### 21. Deathcap Storm (C tier, 43.1%)

**Nature** · 13 Essence · 15 legends · average cost 2.56

Orb weapons for Deathcap's Storm, which hits every enemy legend. Fait draws three, and Faerie Queen and Fangwild grow the Nature board.

- **Main cards:** Deathcap, Fait, Sacred Relic, Briar Rose, Faerie Queen
- **Best matchups:** Eclipse Tide 80%, Frozen Fortress 72%, Treasure Engine 67%
- **Worst matchups:** Cosmic Lockdown 19%, Galaxy Lock 22%, Cursed Kunai 22%
- **Legends:** 4× Deathcap, 4× Fait, 3× Briar Rose, 3× Faerie Queen, 1× Forest Spirit
- **Weapons:** 4× Sacred Relic, 4× Piercing Regret, 2× Remnant of Fate
- **Stages:** 2× Fangwild
- **Essence:** 13

### 22. Last Hour (C tier, 41.5%)

**Shadow** · 10 Essence · 11 legends · average cost 2.80

Death's Hour sets every legend to 1 HP, then Ninja Spirits, Danders and Forest Spirits sweep the board. Tombstones and Bouncy Bombs fill the gaps.

- **Main cards:** Death's Hour, Tombstone, Ninja Spirit, Dander, Forest Spirit, Bouncy Bomb
- **Best matchups:** Adrenaline Rush 93%, Eclipse Tide 90%, Blaster Barrage 87%
- **Worst matchups:** Scorched Earth 13%, Cosmic Lockdown 15%, Matrix Masters 21%
- **Legends:** 4× Ninja Spirit, 4× Dander, 3× Forest Spirit
- **Weapons:** 2× Nightmare Claws
- **Battles:** 3× Death's Hour, 3× Tombstone, 3× Bouncy Bomb, 2× Forgeborne Chest, 2× Orb, 1× Dark Duo, 1× Cursed Kunai
- **Stages:** 2× Evil Hideout
- **Essence:** 10

### 23. Fire Nation (C tier, 39.2%)

**Fire** · 10 Essence · 16 legends · average cost 2.03

Low-curve Fire swarm. Fury Shang and Fire Nation buff every Fire legend; Hothead Jiro and Jhala hit early.

- **Main cards:** Fury Shang, Hothead Jiro, Jhala, Fire Nation, Ancestor's Flame
- **Best matchups:** Eclipse Tide 73%, Adrenaline Rush 65%, Mustafar Burn 59%
- **Worst matchups:** Cosmic Lockdown 10%, Galaxy Lock 16%, Cosmic Lancers 19%
- **Legends:** 4× Fury Shang, 4× Hothead Jiro, 3× Jhala, 2× Heatblast, 2× Ninja Spirit, 1× Seven
- **Weapons:** 4× Ancestor's Flame, 3× Magma Spear, 2× Candlelit Scythe, 1× Hot Lava
- **Battles:** 3× Fire Nation
- **Stages:** 1× Mustafar
- **Essence:** 10

### 24. Treasure Engine (C tier, 39.1%)

**Magic** · 12 Essence · 13 legends · average cost 2.79

Chests and Witch Party for card advantage, with Fait and Amethyst Scythes to use them. It's a weaker take on Treasure Hunt.

- **Main cards:** Chest, Witch Party, Fait, Amethyst Scythe, Breaker Shang
- **Best matchups:** Eclipse Tide 89%, Frozen Fortress 71%, Adrenaline Rush 67%
- **Worst matchups:** Cosmic Lockdown 14%, Galaxy Lock 15%, Scythe Executioners 17%
- **Legends:** 4× Fait, 3× Breaker Shang, 2× Iron Lady, 2× Plague Knight, 2× Caspian
- **Weapons:** 4× Amethyst Scythe, 2× Sleight of Hand
- **Battles:** 4× Chest, 3× Witch Party, 2× Promotion I
- **Essence:** 12

### 25. Enchanted Court (C tier, 37.0%)

**Magic** · 11 Essence · 10 legends · average cost 2.14

Arcadia's Pink Roses (spear + greatsword), Enchantress buffs and curses, Promotion I for reach.

- **Main cards:** Arcadia, Fait, Enchantress, Remnant of Fate, Holy Covenant, Chest
- **Best matchups:** Eclipse Tide 80%, Frozen Fortress 67%, Adrenaline Rush 62%
- **Worst matchups:** Galaxy Lock 10%, Cosmic Lockdown 14%, Tombstone Control 16%
- **Legends:** 4× Arcadia, 4× Fait, 2× Enchantress
- **Weapons:** 4× Remnant of Fate, 3× Holy Covenant, 2× Law of the Land, 2× Amethyst Scythe
- **Battles:** 4× Chest, 2× <3, 1× Witch Party, 1× Promotion I
- **Essence:** 11

### 26. Astro Blasters (C tier, 36.3%)

**Cosmic** · 12 Essence · 13 legends · average cost 2.43

Astro Commander (14 ATK, 3 HP) and Hellshot Hattori with blasters. They hit hard but die to anything.

- **Main cards:** Astro Commander, Hellshot Hattori, Breaker Shang, Revolvers, Bouncy Bomb
- **Best matchups:** Eclipse Tide 77%, Blaster Barrage 68%, Adrenaline Rush 64%
- **Worst matchups:** Scorched Earth 14%, Wild Axe 18%, Cosmic Lockdown 21%
- **Legends:** 4× Astro Commander, 4× Hellshot Hattori, 4× Breaker Shang, 1× Aurora Brynn
- **Weapons:** 4× Revolvers, 2× Splish Splash, 2× Sweet Magi, 2× Shadow Edge
- **Battles:** 3× Bouncy Bomb, 1× Cody Travis, 1× Sunken Chest
- **Essence:** 12

### 27. Iron Wall (C tier, 35.7%)

**Nature** · 12 Essence · 14 legends · average cost 2.32

All walls (Kor, Forest Guardian, The Rock) behind Faerie Queen buffs. It can't close games.

- **Main cards:** The Rock, Forest Guardian, Kor, Faerie Queen, Law of the Land
- **Best matchups:** Eclipse Tide 76%, Adrenaline Rush 64%, Last Hour 63%
- **Worst matchups:** Galaxy Lock 2%, Cosmic Lockdown 6%, Cosmic Lancers 10%
- **Legends:** 4× The Rock, 4× Forest Guardian, 3× Faerie Queen, 3× Kor
- **Weapons:** 4× Law of the Land, 3× Chikara, 2× Piercing Regret
- **Battles:** 3× Sunken Chest, 1× Cursed Kunai, 1× Vial of Crows
- **Essence:** 12

### 28. Hammer Time (C tier, 34.0%)

**Nature** · 13 Essence · 13 legends · average cost 2.41

Yumiko's Hammer Gimp needs three hammers to one-hit KO. It is too slow to assemble in practice.

- **Main cards:** Yumiko, Faerie Queen, Primrose Mallet, Ice Crusher, Fangwild
- **Best matchups:** Eclipse Tide 64%, Last Hour 55%, Fire Nation 54%
- **Worst matchups:** Galaxy Lock 12%, Cosmic Lockdown 14%, Tombstone Control 14%
- **Legends:** 4× Yumiko, 4× Faerie Queen, 3× Fox Spirit, 2× The Rock
- **Weapons:** 4× Primrose Mallet, 4× Ice Crusher, 2× Chikara
- **Battles:** 1× Krabby Patty, 1× Chest
- **Stages:** 2× Fangwild
- **Essence:** 13

### 29. Blaster Barrage (C tier, 32.4%)

**Shadow** · 13 Essence · 11 legends · average cost 2.41

Every blaster legend with Revolvers and Blazing Fire. It has the same fragility problem as Astro Blasters.

- **Main cards:** Astro Commander, Hellshot Hattori, Heatblast, Revolvers, Blazing Fire
- **Best matchups:** Eclipse Tide 75%, Adrenaline Rush 70%, Deathcap Storm 53%
- **Worst matchups:** Scorched Earth 10%, Last Hour 13%, Hideout Rush 14%
- **Legends:** 4× Astro Commander, 4× Hellshot Hattori, 2× Heatblast, 1× Kitsune Hattori
- **Weapons:** 4× Blazing Fire, 4× Revolvers, 2× Aqua Blasters, 2× Shadow Edge
- **Battles:** 3× Bouncy Bomb, 1× Adrenaline
- **Essence:** 13

### 30. Adrenaline Rush (C tier, 28.9%)

**Nature** · 11 Essence · 10 legends · average cost 1.93

Rayman Power Punches and Dander slashes twice with Adrenaline. It runs out of cards quickly.

- **Main cards:** Rayman, Dander, Spine-Chilling Fists, Adrenaline, Super Saiyan
- **Best matchups:** Eclipse Tide 64%, Hammer Time 63%, Deathcap Storm 47%
- **Worst matchups:** Last Hour 7%, Cosmic Lockdown 9%, Scorched Earth 9%
- **Legends:** 4× Rayman, 4× Dander, 2× Breaker Shang
- **Weapons:** 4× Spine-Chilling Fists, 4× Nightmare Claws, 3× Axe of Regrowth, 1× Piercing Regret
- **Battles:** 3× Adrenaline, 2× Super Saiyan, 2× Xull's Fury
- **Essence:** 11

### 31. Frozen Fortress (C tier, 27.7%)

**Arctic** · 11 Essence · 9 legends · average cost 2.48

Alpine Kaya crafts Snowballs and Snowman Kor under Atlantis. Arctic legends hit too softly to close.

- **Main cards:** Alpine Kaya, Atlantis, Atlantean Ada, Glacier's Grace, Snowman Kor
- **Best matchups:** Adrenaline Rush 68%, Blaster Barrage 60%, Fire Nation 58%
- **Worst matchups:** Matrix Masters 2%, Tombstone Control 5%, Treasure Hunt 5%
- **Legends:** 4× Alpine Kaya, 3× Atlantean Ada, 2× Snowman Kor
- **Weapons:** 4× Glacier's Grace, 3× Aqua Blasters, 2× Ice Crusher
- **Battles:** 2× Sunken Chest, 2× Snowball, 2× Water Tribe
- **Stages:** 4× Atlantis, 1× Full Eclipse
- **Essence:** 11

### 32. Eclipse Tide (C tier, 14.1%)

**Arctic** · 12 Essence · 11 legends · average cost 2.50

Full Eclipse (+5/+5 to Arctic) on an Atlantean Ada board. At 7 Essence the stage comes too late, and the cheap Arctic legends die first. Weakest deck in the pool.

- **Main cards:** Full Eclipse, Atlantean Ada, Hellshot Hattori, Alpine Kaya, Water Tribe
- **Best matchups:** Frozen Fortress 46%, Hammer Time 36%, Adrenaline Rush 36%
- **Worst matchups:** Scorched Earth 2%, Cosmic Lockdown 3%, Cursed Kunai 3%
- **Legends:** 4× Hellshot Hattori, 4× Atlantean Ada, 3× Alpine Kaya
- **Weapons:** 3× Aqua Blasters, 3× Glacier's Grace, 2× Splish Splash, 2× Frozen Edge
- **Battles:** 3× Water Tribe, 1× Bubble
- **Stages:** 3× Full Eclipse
- **Essence:** 12

## Full matchup matrix

Win rate of the row deck against the column deck (200 games each, both seats). Columns use the rank numbers from the tier list.

| Deck | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20 | 21 | 22 | 23 | 24 | 25 | 26 | 27 | 28 | 29 | 30 | 31 | 32 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1. Cosmic Lockdown | - | 62 | 54 | 58 | 77 | 66 | 60 | 50 | 66 | 68 | 66 | 84 | 71 | 73 | 72 | 92 | 76 | 72 | 84 | 88 | 81 | 85 | 90 | 86 | 86 | 79 | 94 | 86 | 82 | 91 | 94 | 97 |
| 2. Galaxy Lock | 38 | - | 46 | 60 | 80 | 68 | 56 | 54 | 53 | 69 | 58 | 88 | 53 | 65 | 57 | 96 | 74 | 66 | 86 | 74 | 78 | 77 | 84 | 85 | 90 | 73 | 98 | 88 | 78 | 77 | 88 | 96 |
| 3. Cosmic Lancers | 46 | 55 | - | 55 | 59 | 64 | 49 | 56 | 44 | 54 | 60 | 75 | 52 | 67 | 55 | 88 | 60 | 70 | 75 | 73 | 71 | 75 | 81 | 80 | 82 | 65 | 90 | 84 | 68 | 68 | 91 | 94 |
| 4. Matrix Masters | 42 | 40 | 46 | - | 51 | 49 | 55 | 53 | 48 | 47 | 68 | 62 | 78 | 77 | 72 | 59 | 56 | 69 | 63 | 88 | 70 | 79 | 75 | 82 | 76 | 78 | 84 | 82 | 82 | 80 | 98 | 94 |
| 5. Tombstone Control | 23 | 20 | 41 | 49 | - | 48 | 54 | 51 | 56 | 52 | 59 | 53 | 73 | 74 | 71 | 65 | 57 | 75 | 68 | 80 | 67 | 73 | 72 | 80 | 84 | 68 | 83 | 86 | 78 | 70 | 95 | 94 |
| 6. Wild Axe | 34 | 32 | 36 | 51 | 52 | - | 51 | 54 | 57 | 47 | 58 | 49 | 70 | 76 | 66 | 60 | 58 | 68 | 54 | 78 | 68 | 71 | 74 | 82 | 79 | 82 | 79 | 81 | 74 | 80 | 93 | 96 |
| 7. Scythe Executioners | 40 | 44 | 51 | 45 | 46 | 49 | - | 64 | 50 | 46 | 54 | 46 | 72 | 73 | 70 | 64 | 48 | 70 | 58 | 86 | 68 | 65 | 77 | 83 | 76 | 69 | 81 | 82 | 72 | 62 | 92 | 89 |
| 8. Treasure Hunt | 50 | 46 | 44 | 47 | 49 | 46 | 36 | - | 46 | 40 | 51 | 46 | 71 | 77 | 68 | 60 | 63 | 62 | 61 | 74 | 58 | 74 | 67 | 72 | 72 | 72 | 70 | 74 | 78 | 69 | 95 | 94 |
| 9. Hideout Rush | 34 | 47 | 56 | 52 | 44 | 43 | 50 | 54 | - | 52 | 55 | 55 | 44 | 48 | 61 | 42 | 55 | 62 | 69 | 65 | 73 | 69 | 69 | 76 | 84 | 79 | 58 | 63 | 86 | 86 | 86 | 96 |
| 10. Briar Garden | 32 | 31 | 46 | 53 | 48 | 53 | 54 | 60 | 48 | - | 51 | 44 | 51 | 62 | 61 | 66 | 54 | 63 | 60 | 60 | 62 | 59 | 57 | 77 | 64 | 58 | 81 | 85 | 48 | 62 | 86 | 82 |
| 11. Cursed Kunai | 34 | 42 | 40 | 32 | 41 | 42 | 46 | 49 | 45 | 49 | - | 40 | 45 | 41 | 56 | 41 | 49 | 56 | 72 | 63 | 78 | 67 | 72 | 77 | 80 | 76 | 61 | 76 | 79 | 80 | 76 | 97 |
| 12. Kitsune Blades | 16 | 12 | 25 | 38 | 47 | 51 | 54 | 55 | 46 | 56 | 60 | - | 42 | 56 | 54 | 47 | 52 | 58 | 66 | 58 | 71 | 58 | 75 | 66 | 75 | 70 | 80 | 76 | 74 | 80 | 79 | 94 |
| 13. Scorched Earth | 29 | 47 | 48 | 22 | 27 | 30 | 28 | 29 | 56 | 49 | 55 | 58 | - | 56 | 40 | 39 | 78 | 42 | 51 | 64 | 64 | 87 | 52 | 60 | 70 | 86 | 76 | 70 | 90 | 91 | 89 | 98 |
| 14. Frozen Tide | 27 | 35 | 33 | 23 | 26 | 24 | 27 | 23 | 52 | 38 | 59 | 44 | 44 | - | 44 | 54 | 80 | 46 | 52 | 70 | 53 | 79 | 69 | 59 | 66 | 78 | 74 | 72 | 85 | 82 | 77 | 89 |
| 15. Sozin's Inferno | 28 | 42 | 45 | 28 | 29 | 34 | 30 | 32 | 39 | 39 | 44 | 46 | 60 | 56 | - | 40 | 66 | 44 | 50 | 68 | 52 | 74 | 52 | 66 | 73 | 77 | 82 | 62 | 76 | 79 | 82 | 90 |
| 16. Rock Wall | 8 | 4 | 12 | 41 | 35 | 40 | 36 | 40 | 57 | 34 | 59 | 53 | 61 | 46 | 60 | - | 55 | 55 | 56 | 75 | 52 | 68 | 75 | 72 | 72 | 73 | 62 | 60 | 76 | 65 | 76 | 90 |
| 17. Ninja Swarm | 24 | 26 | 40 | 44 | 43 | 42 | 52 | 37 | 45 | 46 | 51 | 48 | 22 | 20 | 34 | 45 | - | 42 | 77 | 28 | 76 | 69 | 68 | 73 | 65 | 79 | 51 | 62 | 84 | 90 | 49 | 93 |
| 18. Workshop Forge | 28 | 34 | 30 | 31 | 25 | 32 | 30 | 38 | 38 | 37 | 44 | 42 | 58 | 54 | 56 | 45 | 58 | - | 51 | 70 | 48 | 63 | 51 | 52 | 60 | 67 | 64 | 57 | 66 | 70 | 84 | 87 |
| 19. Wild Stampede | 16 | 14 | 25 | 37 | 32 | 46 | 42 | 39 | 31 | 40 | 28 | 34 | 49 | 48 | 50 | 44 | 23 | 49 | - | 55 | 48 | 35 | 50 | 69 | 62 | 46 | 70 | 62 | 48 | 57 | 74 | 78 |
| 20. Mustafar Burn | 12 | 26 | 27 | 12 | 20 | 22 | 14 | 26 | 35 | 40 | 37 | 42 | 36 | 30 | 32 | 25 | 72 | 30 | 46 | - | 62 | 75 | 41 | 38 | 58 | 74 | 57 | 53 | 78 | 85 | 76 | 93 |
| 21. Deathcap Storm | 19 | 22 | 29 | 30 | 33 | 32 | 32 | 42 | 27 | 38 | 22 | 29 | 36 | 47 | 48 | 48 | 24 | 52 | 52 | 38 | - | 34 | 49 | 67 | 55 | 48 | 65 | 67 | 47 | 53 | 72 | 80 |
| 22. Last Hour | 15 | 23 | 25 | 21 | 27 | 29 | 35 | 26 | 31 | 41 | 33 | 42 | 13 | 21 | 26 | 32 | 31 | 37 | 65 | 25 | 66 | - | 60 | 34 | 52 | 77 | 37 | 45 | 87 | 93 | 50 | 90 |
| 23. Fire Nation | 10 | 16 | 19 | 25 | 28 | 26 | 23 | 33 | 31 | 43 | 28 | 25 | 48 | 31 | 48 | 25 | 32 | 49 | 50 | 59 | 51 | 40 | - | 53 | 39 | 51 | 52 | 46 | 57 | 65 | 42 | 73 |
| 24. Treasure Engine | 14 | 15 | 20 | 18 | 20 | 18 | 17 | 28 | 24 | 23 | 23 | 34 | 40 | 41 | 34 | 28 | 27 | 48 | 31 | 62 | 33 | 66 | 47 | - | 55 | 56 | 48 | 52 | 62 | 67 | 71 | 89 |
| 25. Enchanted Court | 14 | 10 | 18 | 24 | 16 | 21 | 24 | 28 | 16 | 36 | 20 | 25 | 30 | 34 | 27 | 28 | 35 | 40 | 38 | 42 | 45 | 48 | 61 | 45 | - | 55 | 41 | 60 | 56 | 62 | 67 | 80 |
| 26. Astro Blasters | 21 | 27 | 35 | 22 | 32 | 18 | 31 | 28 | 21 | 42 | 24 | 30 | 14 | 22 | 23 | 27 | 21 | 33 | 54 | 26 | 52 | 23 | 49 | 44 | 45 | - | 50 | 59 | 68 | 64 | 42 | 77 |
| 27. Iron Wall | 6 | 2 | 10 | 16 | 17 | 21 | 19 | 30 | 42 | 19 | 39 | 20 | 24 | 26 | 18 | 38 | 49 | 36 | 30 | 43 | 35 | 63 | 48 | 52 | 59 | 50 | - | 55 | 52 | 64 | 47 | 76 |
| 28. Hammer Time | 14 | 12 | 16 | 18 | 14 | 19 | 18 | 26 | 37 | 15 | 24 | 24 | 30 | 28 | 38 | 40 | 38 | 43 | 38 | 47 | 33 | 55 | 54 | 48 | 40 | 41 | 45 | - | 48 | 37 | 53 | 64 |
| 29. Blaster Barrage | 18 | 22 | 32 | 18 | 22 | 26 | 28 | 22 | 14 | 52 | 21 | 26 | 10 | 15 | 24 | 24 | 16 | 34 | 52 | 22 | 53 | 13 | 43 | 38 | 44 | 32 | 48 | 52 | - | 70 | 40 | 75 |
| 30. Adrenaline Rush | 9 | 23 | 32 | 20 | 30 | 20 | 38 | 31 | 14 | 38 | 20 | 20 | 9 | 18 | 21 | 35 | 10 | 30 | 43 | 15 | 47 | 7 | 35 | 33 | 38 | 36 | 36 | 63 | 30 | - | 32 | 64 |
| 31. Frozen Fortress | 6 | 12 | 9 | 2 | 5 | 7 | 8 | 5 | 14 | 14 | 24 | 21 | 11 | 23 | 18 | 24 | 51 | 16 | 26 | 24 | 28 | 50 | 58 | 29 | 33 | 58 | 53 | 47 | 60 | 68 | - | 54 |
| 32. Eclipse Tide | 3 | 4 | 6 | 6 | 6 | 4 | 11 | 6 | 4 | 18 | 3 | 6 | 2 | 11 | 10 | 10 | 7 | 13 | 22 | 7 | 20 | 10 | 27 | 11 | 20 | 23 | 24 | 36 | 25 | 36 | 46 | - |
