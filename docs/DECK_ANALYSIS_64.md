# BrawlTCG deck analysis: 64 decks

All 64 decks played a full round robin: **201,600 games**, 100 per matchup.

- **32 earlier decks:** the 32 decks that ship with the game now (see `DECK_ANALYSIS.md`).
- **32 new stage decks:** each built around **4 copies of one stage card** and **at most 2 weapon types**. Every stage got at least two decks.

## How this was done

1. **32 stage decks** were built, 2 to 5 per stage, each around its own plan. The stage is the core of the deck, and the legends, weapons and battle cards were picked to work under it.
2. **25 of the 32 stage decks were tuned by simulation.** For each deck, up to 50 single-card changes were tried against the other 63 decks. A change was kept only if it also won on a second set of games. Tuning could never drop below 4 copies of the stage or add a third weapon type.
3. **7 stage decks were not tuned** because time ran out: Space Time Chests, Space Time Giants, Space Time Bombs, Comet Blades, Comet Blaze, Eclipse Spears and Eclipse Blasters. They play as first built, so their numbers below are likely too low (see the caveats).
4. **Round robin:** 100 games per matchup, each deck going first in half of them. The player on both sides is the in-game **Normal** bot, with 50 starting health and the starting-hand redraw. A game still undecided after 160 turns counts as half a win for each side.

**Tiers by overall win rate:** **S** 70%+, **A** 60–70%, **B** 45–60%, **C** under 45%.

**Accuracy:** a single matchup (100 games) is accurate to about ±10 points. An overall win rate averages 6,300 games and is accurate to about ±1.3 points.

## Key findings

- **13 stage decks made the top 32**, and **4 made the top 10:**
  - #3 Galaxy Fists (73.0%)
  - #5 Hideout Ninjas (70.7%)
  - #7 Hideout Brutes (69.8%)
  - #8 Galaxy Lancers (69.5%)

  The others in the top 32:
  - Galaxy Reapers #13
  - Fangwild Garden #15
  - Galaxy Commanders #17
  - Space Time Giants #20 (untuned)
  - Mustafar Walls #27
  - Matrix Witches #28
  - Hideout Claws #29
  - Space Time Rush #31
  - Matrix Reapers #32

- **Which stage you build around matters more than how many copies you run.**
  - **4× Silent Galaxy** decks average 69.1% and **4× Evil Hideout** decks 58.5%. Both are clearly better than the field.
  - **4× Atlantis** (30.5%), **4× Sozin's Comet** (33.5%) and **4× Full Eclipse** (9.2%) drag the average down.
  - Across all 64 decks, the 35 decks with 4 copies of their main stage average 46.0%, against 55.1% for the 14 decks with no stage at all. That average is mostly about the weak stages, not about running 4 copies.

- **Silent Galaxy is still the strongest card in the game.** The top 3 decks run it, and all five 4× Galaxy decks are in the top 17. A deck built on Galaxy only needs Cosmic legends that can hit; Galaxy Fists does that with Wu Shang's Down Sig and Breaker Shang.

- **Evil Hideout is the best non-Cosmic stage.**
  - Hideout Ninjas makes 1-HP Ninja Spirits hit for 6.
  - Hideout Brutes gives Raymesis and The Minotaur 12–13 power.
  - Both beat almost everything outside the Galaxy decks.

- **Seven-cost stages arrive too late.** Both Sozin's Comet decks are in the C tier, and the Full Eclipse decks are near the bottom. By the time the bot has 7 Essence, the board is usually decided. The previous tournament found the same.
  - **Possible fixes:** a cost of 5–6, or a first effect on the turn the stage is played.

- **Arctic is still the weakest element.**
  - Of the 8 Arctic decks, only Frozen Tide (58.0%) does well.
  - Atlantis's −2 power helps only if your own legends can hit. Arctic legends hit for 0–2, so the stage takes power away from the opponent but adds nothing to your own damage.
  - Atlantis Snowfall (45.4%) works best because it wins with Snowball and Bouncy Bomb damage to the player instead of with attacks.

- **Space Time helps both players.** Both players draw an extra card, so the stage only pays off for the deck that uses the cards better. Space Time Giants digs to Kitsune Hattori and Aurora Brynn and placed #20 even untuned. The cheap Space Time decks gain nothing over the opponent.

- **Matrix does nothing on its own.** It is only an answer to the opponent's stage. Four Matrix cards are more answer than a deck needs, and the 4× Matrix decks average 49.1%.
  - Matrix Masters (#4) wins with Dark Mage and Kitsune Hattori more than with Matrix: it wins only 38% and 40% against the two Galaxy decks.
  - **The best decks against Galaxy:**
    - Cosmic Lancers: 59% vs Galaxy Lock, 49% vs Cosmic Lockdown.
    - Space Time Giants, which also runs Cosmic legends: 47% and 59%.
    - Hideout Ninjas: 56% and 44%. Its Ninja Spirits are cheap enough to rebuild the board after Galaxy comes down.
    - Treasure Hunt: 46% and 50%.

- **One or two weapon types made no difference here.**
  - Decks with 1 weapon type average 49.1%, and decks with 2 types 49.0%.
  - Among the earlier decks, the ones with exactly 2 types did best (59.9%, against 53.2% for 3 types and 51.2% for 4).
  - Your tip holds well enough: 2 weapon types is the sweet spot, 1 is just as good, and more than 2 is slightly worse.

## Caveats

- **The bot plays stages simply.** It plays a stage as soon as that looks better than its other moves. It never holds a stage back, never saves Matrix to answer a Galaxy, and never plans a turn around the stage it will play. A human gets more out of stage decks than the bot does, especially out of Matrix, Space Time and Sozin's Comet. Treat these numbers as a floor for stage decks.
- **The 7 untuned decks are underrated.** Their tuning finished after the round robin had already started, so they played as first built. Against an earlier version of the field, tuning raised them a lot:

  | Deck | First built | Tuned |
  |---|---|---|
  | Space Time Giants | 63.5% | 71.6% |
  | Comet Blades | 36% | 58% |
  | Space Time Bombs | 13% | 35% |
  | Comet Blaze | 37% | 42% |
  | Eclipse Spears | 15% | 26% |
  | Eclipse Blasters | 9% | 24% |

  The tuned lists are saved and can join a rerun.
- **The earlier decks were tuned with no limit on stages or weapons**, so tuning could freely cut stage copies.

## Stage decks vs the earlier decks

| | Stage decks (new) | Earlier decks |
|---|---|---|
| Average win rate | 45.1% | 54.9% |
| In the top 10 | 4 | 6 |
| In the top 20 | 8 | 12 |
| In the top 32 | 13 | 19 |
| S tier | 2 | 4 |
| A tier | 6 | 9 |
| B tier | 8 | 10 |
| C tier | 16 | 9 |

### Win rate by number of copies of the main stage (all 64 decks)

| Copies of the main stage | Decks | Average win rate |
|---|---|---|
| 0 | 14 | 55.1% |
| 1 | 3 | 52.2% |
| 2 | 8 | 54.7% |
| 3 | 4 | 55.9% |
| 4 | 35 | 46.0% |

### By stage (decks running 4 copies)

| Stage | Cost | Decks | Average | Best deck |
|---|---|---|---|---|
| Silent Galaxy | 5 | 5 | 69.1% | Galaxy Lock (73.2%, #2) |
| Evil Hideout | 3 | 5 | 58.5% | Hideout Ninjas (70.7%, #5) |
| Matrix | 1 | 3 | 49.1% | Matrix Witches (53.9%, #28) |
| Fangwild | 4 | 4 | 45.3% | Fangwild Garden (64.9%, #15) |
| The Workshop | 2 | 3 | 45.2% | Workshop Smiths (48.0%, #35) |
| Mustafar | 2 | 3 | 44.6% | Mustafar Walls (55.4%, #27) |
| Space Time | 1 | 4 | 41.9% | Space Time Giants (62.1%, #20) |
| Sozin's Comet | 7 | 2 | 33.5% | Comet Blades (35.3%, #55) |
| Atlantis | 3 | 4 | 30.5% | Atlantis Snowfall (45.4%, #39) |
| Full Eclipse | 7 | 2 | 9.2% | Eclipse Spears (11.3%, #62) |

## Tier list (all 64 decks)

| # | Tier | Deck | Win rate | Stages | Weapons | Batch |
|---|---|---|---|---|---|---|
| 1 | **S** | Cosmic Lockdown | 77.5% | 3× Silent Galaxy | Spear 4 + Scythe 1 | v1.2 premade |
| 2 | **S** | Galaxy Lock | 73.2% | 4× Silent Galaxy | Gauntlets 3 + Spear 3 + Scythe 2 | first batch |
| 3 | **S** | Galaxy Fists | 73.0% | 4× Silent Galaxy | Gauntlets 5 + Spear 4 | **stage deck** |
| 4 | **S** | Matrix Masters | 70.7% | none | Scythe 4 + Sword 3 | first batch |
| 5 | **S** | Hideout Ninjas | 70.7% | 4× Evil Hideout | Sword 4 | **stage deck** |
| 6 | **S** | Cosmic Lancers | 70.6% | 2× Silent Galaxy | Spear 5 + RocketLance 4 + Scythe 2 | first batch |
| 7 | **A** | Hideout Brutes | 69.8% | 4× Evil Hideout | Axe 8 | **stage deck** |
| 8 | **A** | Galaxy Lancers | 69.5% | 4× Silent Galaxy | Spear 6 + RocketLance 4 | **stage deck** |
| 9 | **A** | Scythe Executioners | 69.2% | none | Scythe 8 + RocketLance 1 | first batch |
| 10 | **A** | Tombstone Control | 69.1% | none | Scythe 4 + Axe 2 + Gauntlets 2 | first batch |
| 11 | **A** | Wild Axe | 67.4% | none | Katars 3 + Axe 2 | v1.2 premade |
| 12 | **A** | Hideout Rush | 67.0% | 3× Evil Hideout | Katars 4 + Axe 1 + Gauntlets 1 + Sword 1 | v1.2 premade |
| 13 | **A** | Galaxy Reapers | 66.9% | 4× Silent Galaxy | Scythe 6 + Spear 4 | **stage deck** |
| 14 | **A** | Treasure Hunt | 66.2% | none | Spear 2 + Scythe 1 | v1.2 premade |
| 15 | **A** | Fangwild Garden | 64.9% | 4× Fangwild | Spear 7 + Greatsword 4 | **stage deck** |
| 16 | **A** | Cursed Kunai | 63.9% | 4× Evil Hideout | Katars 6 | first batch |
| 17 | **A** | Galaxy Commanders | 62.7% | 4× Silent Galaxy | Blasters 4 + Gauntlets 4 | **stage deck** |
| 18 | **A** | Briar Garden | 62.4% | 2× Fangwild | Greatsword 4 + Spear 4 + Axe 2 + Gauntlets 2 | first batch |
| 19 | **A** | Kitsune Blades | 62.4% | none | Blasters 4 + Sword 4 + Scythe 2 | first batch |
| 20 | **A** | Space Time Giants | 62.1% | 4× Space Time | Spear 4 + Sword 4 | **stage deck** |
| 21 | **A** | Scorched Earth | 61.1% | 2× Mustafar | Scythe 3 + Spear 1 + Sword 1 | v1.2 premade |
| 22 | **B** | Sozin's Inferno | 59.1% | 1× Sozin's Comet | Blasters 3 + Scythe 3 + Axe 2 | first batch |
| 23 | **B** | Rock Wall | 58.2% | none | Hammer 7 + Katars 1 | first batch |
| 24 | **B** | Frozen Tide | 58.0% | 3× Atlantis | Hammer 3 + Spear 1 | v1.2 premade |
| 25 | **B** | Ninja Swarm | 57.2% | 2× Evil Hideout | Sword 4 + Katars 3 | first batch |
| 26 | **B** | Workshop Forge | 56.1% | none | Axe 4 + Spear 4 + Cannon 3 | first batch |
| 27 | **B** | Mustafar Walls | 55.4% | 4× Mustafar | Scythe 7 + RocketLance 2 | **stage deck** |
| 28 | **B** | Matrix Witches | 53.9% | 4× Matrix | Scythe 6 + RocketLance 3 | **stage deck** |
| 29 | **B** | Hideout Claws | 51.7% | 4× Evil Hideout | Katars 6 + Hammer 3 | **stage deck** |
| 30 | **B** | Wild Stampede | 51.5% | 1× Evil Hideout | Katars 4 + Axe 3 + Gauntlets 1 + Hammer 1 | first batch |
| 31 | **B** | Space Time Rush | 51.3% | 4× Space Time | Katars 4 + Sword 4 | **stage deck** |
| 32 | **B** | Matrix Reapers | 51.1% | 4× Matrix | Scythe 6 + Sword 3 | **stage deck** |
| 33 | **B** | Mustafar Burn | 49.0% | 2× Mustafar | Sword 4 + Blasters 3 + Gauntlets 2 | first batch |
| 34 | **B** | Deathcap Storm | 48.5% | 2× Fangwild | Orb 6 + Spear 4 | first batch |
| 35 | **B** | Workshop Smiths | 48.0% | 4× The Workshop | Spear 6 + Gauntlets 4 | **stage deck** |
| 36 | **B** | Last Hour | 48.0% | 2× Evil Hideout | Katars 2 | first batch |
| 37 | **B** | Ulgrim's Forge | 46.4% | 4× The Workshop | Axe 4 + Spear 4 | **stage deck** |
| 38 | **B** | Fire Nation | 46.1% | 1× Mustafar | Sword 4 + Spear 3 + Scythe 2 + Gauntlets 1 | first batch |
| 39 | **B** | Atlantis Snowfall | 45.4% | 4× Atlantis | Spear 4 | **stage deck** |
| 40 | **C** | Treasure Engine | 44.8% | none | Scythe 4 + Gauntlets 2 | first batch |
| 41 | **C** | Astro Blasters | 44.6% | none | Blasters 6 + Spear 2 + Sword 2 | first batch |
| 42 | **C** | Enchanted Court | 43.5% | none | Orb 4 + Spear 3 + Greatsword 2 + Scythe 2 | first batch |
| 43 | **C** | Space Time Chests | 43.5% | 4× Space Time | RocketLance 2 + Spear 2 | **stage deck** |
| 44 | **C** | Fangwild Hammers | 43.1% | 4× Fangwild | Hammer 6 + Greatsword 4 | **stage deck** |
| 45 | **C** | Matrix Hammers | 42.4% | 4× Matrix | Hammer 9 | **stage deck** |
| 46 | **C** | Workshop Thieves | 41.3% | 4× The Workshop | Scythe 6 + Gauntlets 4 | **stage deck** |
| 47 | **C** | Blaster Barrage | 41.3% | none | Blasters 10 + Sword 2 | first batch |
| 48 | **C** | Iron Wall | 41.1% | none | Greatsword 4 + Hammer 3 + Spear 2 | first batch |
| 49 | **C** | Hammer Time | 41.0% | 2× Fangwild | Hammer 10 | first batch |
| 50 | **C** | Mustafar Spears | 40.3% | 4× Mustafar | Gauntlets 4 + Spear 3 | **stage deck** |
| 51 | **C** | Fangwild Brawlers | 40.2% | 4× Fangwild | Axe 4 + Gauntlets 4 | **stage deck** |
| 52 | **C** | Lava Blasters | 38.2% | 4× Mustafar | Sword 4 + Blasters 3 | **stage deck** |
| 53 | **C** | Adrenaline Rush | 36.8% | none | Gauntlets 4 + Katars 4 + Axe 3 + Spear 1 | first batch |
| 54 | **C** | Hideout Bombers | 36.3% | 4× Evil Hideout | Gauntlets 8 + Katars 4 | **stage deck** |
| 55 | **C** | Comet Blades | 35.3% | 4× Sozin's Comet | Scythe 4 + Sword 4 | **stage deck** |
| 56 | **C** | Fangwild Witches | 33.0% | 4× Fangwild | Orb 7 + Scythe 4 | **stage deck** |
| 57 | **C** | Comet Blaze | 31.6% | 4× Sozin's Comet | Spear 4 + Blasters 4 | **stage deck** |
| 58 | **C** | Frozen Fortress | 31.1% | 4× Atlantis, 1× Full Eclipse | Spear 4 + Blasters 3 + Hammer 2 | first batch |
| 59 | **C** | Atlantis Freeze | 29.4% | 4× Atlantis | Hammer 4 + Spear 3 | **stage deck** |
| 60 | **C** | Eclipse Tide | 20.9% | 3× Full Eclipse | Blasters 5 + Spear 3 + Sword 2 | first batch |
| 61 | **C** | Atlantis Tide | 16.1% | 4× Atlantis | Blasters 6 + Spear 4 | **stage deck** |
| 62 | **C** | Eclipse Spears | 11.3% | 4× Full Eclipse | Spear 7 | **stage deck** |
| 63 | **C** | Space Time Bombs | 10.6% | 4× Space Time | Gauntlets 4 + Katars 4 | **stage deck** |
| 64 | **C** | Eclipse Blasters | 7.2% | 4× Full Eclipse | Blasters 7 + Hammer 3 | **stage deck** |

## Every deck in brief

**1. Cosmic Lockdown** (S, 77.5%, Cosmic). All-Cosmic Silent Galaxy, Aurora Brynn and Artemis.
Best: Atlantis Tide 100%, Space Time Bombs 98%. Worst: Space Time Giants 41%, Treasure Hunt 50%.
<br>*3× Silent Galaxy, 4× Aurora Brynn, 3× Artemis, 2× Orion, 1× Wu Shang, 1× Breaker Shang, 3× Shattering Star, 1× Shooting Star, 1× Sweet Magi, 4× Chest, 1× Cody Travis, 1× Bouncy Bomb, 1× Cosmic Chest, 1× Scythe Forever, 13× Essence*

**2. Galaxy Lock** (S, 73.2%, Cosmic). Four Silent Galaxies: only Cosmic legends deal damage; Aurora Brynn and Wu Shang's Down Sig win.
Best: Eclipse Blasters 100%, Eclipse Spears 99%. Worst: Cosmic Lockdown 37%, Galaxy Fists 39%.
<br>*4× Silent Galaxy, 4× Aurora Brynn, 3× Wu Shang, 3× Artemis, 3× Breaker Shang, 3× Infinite & Absolute, 3× Shattering Star, 2× Starry Scythe, 2× Cosmic Chest, 13× Essence*

**3. Galaxy Fists** (S, 73.0%, Cosmic, stage deck). Silent Galaxy plus Wu Shang's multi-hit Down Sig and Breaker Shang's crafted Cosmic cards.
Best: Eclipse Blasters 100%, Eclipse Tide 99%. Worst: Cosmic Lockdown 35%, Cosmic Lancers 46%.
<br>*4× Silent Galaxy, 4× Aurora Brynn, 4× Wu Shang, 3× Breaker Shang, 3× Infinite & Absolute, 3× Shattering Star, 2× Spine-Chilling Fists, 1× Hand Saw, 2× Cosmic Chest, 1× Cody Travis, 13× Essence*

**4. Matrix Masters** (S, 70.7%, Shadow). Mastermind and Witch Scarlet set Matrix; Dark Mage and Kitsune with scythes and swords.
Best: Atlantis Tide 100%, Eclipse Spears 100%. Worst: Galaxy Fists 32%, Galaxy Commanders 34%.
<br>*4× Dark Mage, 4× Mastermind, 3× Kitsune Hattori, 1× Raymesis, 4× Necro Aura, 3× Shadow Edge, 3× Bouncy Bomb, 3× Forgeborne Chest, 1× Tombstone, 1× Cursed Kunai, 13× Essence*

**5. Hideout Ninjas** (S, 70.7%, Shadow, stage deck). Evil Hideout turns 1-HP Ninja Spirits into 6-power swords; Dullahan Jiro summons more.
Best: Eclipse Spears 100%, Eclipse Blasters 100%. Worst: Scorched Earth 42%, Rock Wall 43%.
<br>*4× Evil Hideout, 4× Dullahan Jiro, 4× Ninja Spirit, 3× Hellshot Hattori, 2× Kitsune Hattori, 4× Shadow Edge, 2× Tombstone, 2× Bouncy Bomb, 2× Forgeborne Chest, 1× Cursed Kunai, 12× Essence*

**6. Cosmic Lancers** (S, 70.6%, Cosmic). Artemis and Orion with Galaxy Lances for Flamethrower; Aurora Brynn and Silent Galaxy close.
Best: Eclipse Blasters 99%, Eclipse Spears 98%. Worst: Hideout Ninjas 34%, Hideout Rush 42%.
<br>*2× Silent Galaxy, 4× Artemis, 4× Orion, 2× Aurora Brynn, 2× Breaker Shang, 4× Galaxy Lance, 3× Shattering Star, 2× Shooting Star, 2× Sweet Magi, 4× Cosmic Chest, 11× Essence*

**7. Hideout Brutes** (A, 69.8%, Shadow, stage deck). Raymesis and The Minotaur with axes swing for 13+ under Evil Hideout.
Best: Atlantis Tide 99%, Eclipse Spears 99%. Worst: Hideout Rush 37%, Ninja Swarm 41%.
<br>*4× Evil Hideout, 4× Raymesis, 4× The Minotaur, 2× Rayman, 4× Buzz Axe, 3× Axe of Regrowth, 1× Boiling Point, 3× Bouncy Bomb, 2× Forgeborne Chest, 13× Essence*

**8. Galaxy Lancers** (A, 69.5%, Cosmic, stage deck). Four Silent Galaxies; Artemis and Orion Flamethrower with Galaxy Lances, Aurora Brynn spears.
Best: Atlantis Tide 100%, Eclipse Blasters 100%. Worst: Galaxy Lock 32%, Cosmic Lockdown 33%.
<br>*4× Silent Galaxy, 3× Aurora Brynn, 3× Orion, 3× Breaker Shang, 2× Artemis, 4× Galaxy Lance, 4× Shattering Star, 2× Sweet Magi, 3× Cosmic Chest, 12× Essence*

**9. Scythe Executioners** (A, 69.2%, Cosmic). Stack three scythes for Scythe Gimp one-hit KOs; Scythe Forever crafts them.
Best: Atlantis Tide 100%, Eclipse Blasters 100%. Worst: Cosmic Lockdown 35%, Hideout Brutes 41%.
<br>*4× Dark Mage, 4× Artemis, 2× Iron Lady, 1× The Rock, 1× Mastermind, 4× Shooting Star, 4× Starry Scythe, 1× Galaxy Lance, 3× Cosmic Chest, 2× Scythe Forever, 14× Essence*

**10. Tombstone Control** (A, 69.1%, Shadow). Big Shadow bodies (Raymesis, Dark Mage) behind removal: Tombstone, Bouncy Bomb, Death's Hour.
Best: Eclipse Blasters 100%, Atlantis Tide 98%. Worst: Galaxy Lock 20%, Galaxy Fists 22%.
<br>*3× Dark Mage, 3× Raymesis, 1× Mastermind, 1× Artemis, 1× Caspian, 4× Necro Aura, 2× Boiling Point, 2× Sleight of Hand, 3× Tombstone, 3× Forgeborne Chest, 2× Dark Duo, 2× Bouncy Bomb, 13× Essence*

**11. Wild Axe** (A, 67.4%, Wild). Dander early, The Minotaur and Raymesis late.
Best: Eclipse Tide 96%, Atlantis Tide 96%. Worst: Galaxy Fists 26%, Galaxy Lock 28%.
<br>*3× Dander, 2× The Minotaur, 1× Raymesis, 1× Heatblast, 1× Jhala, 3× Nightmare Claws, 1× Buzz Axe, 1× Boiling Point, 4× Wild Chest, 4× Chest, 2× Bouncy Bomb, 1× Cody Travis, 1× Did Shaw Win Yet?, 1× Adrenaline, 14× Essence*

**12. Hideout Rush** (A, 67.0%, Shadow). Shadow/Wild aggro under Evil Hideout.
Best: Eclipse Spears 100%, Eclipse Blasters 99%. Worst: Cosmic Lockdown 29%, Galaxy Fists 41%.
<br>*3× Evil Hideout, 4× Ninja Spirit, 3× Caspian, 2× Dander, 1× Kitsune Hattori, 1× The Minotaur, 4× Nightmare Claws, 1× Boiling Point, 1× Rippers, 1× Shadow Edge, 4× Forgeborne Chest, 2× Bouncy Bomb, 1× Dark Duo, 12× Essence*

**13. Galaxy Reapers** (A, 66.9%, Cosmic, stage deck). Silent Galaxy stops the enemy while Artemis Scythe Gimps anything; Scythe Forever refills.
Best: Eclipse Blasters 97%, Space Time Bombs 95%. Worst: Hideout Ninjas 31%, Cosmic Lockdown 35%.
<br>*4× Silent Galaxy, 4× Aurora Brynn, 4× Artemis, 3× Orion, 4× Starry Scythe, 4× Sweet Magi, 2× Shooting Star, 2× Scythe Forever, 13× Essence*

**14. Treasure Hunt** (A, 66.2%, Magic). Chests, Witch Party and crafting legends.
Best: Eclipse Blasters 98%, Lava Blasters 96%. Worst: Space Time Giants 16%, Scythe Executioners 34%.
<br>*3× Breaker Shang, 2× Fait, 1× Ulgrim, 2× Sweet Magi, 1× Amethyst Scythe, 3× Witch Party, 3× Wild Chest, 3× Forgeborne Chest, 3× Chest, 1× Cody Travis, 1× Dragon's Chest, 1× Adrenaline, 16× Essence*

**15. Fangwild Garden** (A, 64.9%, Nature, stage deck). Fangwild grows Nature and Magic legends every turn; Arcadia's Pink Roses and Briar Rose stuns.
Best: Space Time Bombs 99%, Eclipse Blasters 97%. Worst: Hideout Brutes 37%, Cosmic Lockdown 38%.
<br>*4× Fangwild, 4× Arcadia, 3× Briar Rose, 3× Faerie Queen, 3× Forest Guardian, 4× Law of the Land, 4× Piercing Regret, 3× Holy Covenant, 12× Essence*

**16. Cursed Kunai** (A, 63.9%, Shadow). Katar legends and Cursed Kunai crafting under Evil Hideout.
Best: Eclipse Tide 99%, Eclipse Blasters 99%. Worst: Matrix Masters 27%, Hideout Ninjas 32%.
<br>*4× Evil Hideout, 4× The Rock, 4× Dander, 2× Ninja Spirit, 1× Caspian, 3× Nightmare Claws, 3× Winter Daggers, 4× Cursed Kunai, 2× Forgeborne Chest, 1× Death's Hour, 1× Bouncy Bomb, 11× Essence*

**17. Galaxy Commanders** (A, 62.7%, Cosmic, stage deck). Silent Galaxy makes Astro Commander's 14 ATK glass cannon safe to keep on board.
Best: Eclipse Blasters 100%, Atlantis Tide 97%. Worst: Cosmic Lockdown 28%, Galaxy Fists 32%.
<br>*4× Silent Galaxy, 4× Astro Commander, 3× Wu Shang, 3× Breaker Shang, 4× Revolvers, 4× Infinite & Absolute, 2× Cosmic Chest, 1× Cody Travis, 1× Dark Duo, 1× Bouncy Bomb, 1× Forgeborne Chest, 12× Essence*

**18. Briar Garden** (A, 62.4%, Nature). Faerie Queen grows every Nature legend as Nature cards hit the board; Briar Rose stuns.
Best: Eclipse Spears 98%, Space Time Bombs 97%. Worst: Galaxy Lancers 26%, Galaxy Lock 30%.
<br>*2× Fangwild, 4× Briar Rose, 4× Rayman, 4× Faerie Queen, 3× Forest Guardian, 4× Law of the Land, 4× Piercing Regret, 2× Axe of Regrowth, 2× Spine-Chilling Fists, 1× Adrenaline, 10× Essence*

**19. Kitsune Blades** (A, 62.4%, Shadow). Sword and blaster midrange: Hellshot Hattori double shots, Kitsune Hattori finishes, Tombstone removes walls.
Best: Space Time Bombs 100%, Atlantis Tide 98%. Worst: Galaxy Lock 11%, Cosmic Lockdown 16%.
<br>*3× Hellshot Hattori, 3× Dullahan Jiro, 2× Kitsune Hattori, 2× Dark Mage, 1× Ninja Spirit, 4× Revolvers, 4× Shadow Edge, 2× Necro Aura, 3× Bouncy Bomb, 2× Tombstone, 2× Forgeborne Chest, 12× Essence*

**20. Space Time Giants** (A, 62.1%, Shadow, stage deck). Space Time digs to Kitsune Hattori and Aurora Brynn faster; swords and spears.
Best: Atlantis Tide 100%, Eclipse Spears 100%. Worst: Hideout Ninjas 14%, Space Time Rush 14%.
<br>*4× Space Time, 4× Kitsune Hattori, 4× Aurora Brynn, 3× Hellshot Hattori, 1× Wu Shang, 4× Shattering Star, 4× Shadow Edge, 2× Tombstone, 14× Essence*

**21. Scorched Earth** (A, 61.1%, Fire). All Fire under Mustafar.
Best: Eclipse Blasters 100%, Eclipse Spears 99%. Worst: Matrix Masters 19%, Hideout Brutes 21%.
<br>*2× Mustafar, 4× Hothead Jiro, 3× Molten Kor, 2× Iron Lady, 2× Fury Shang, 1× Heatblast, 1× Jhala, 1× Seven, 2× Candlelit Scythe, 1× Searing Blade, 1× Magma Spear, 1× Ancestor's Flame, 3× Dragon's Chest, 2× Chest, 1× Super Saiyan, 1× Bouncy Bomb, 12× Essence*

**22. Sozin's Inferno** (B, 59.1%, Fire). Ramp into Sozin's Comet (+5/+5 to Fire) with Iron Lady, Ulgrim and Heatblast.
Best: Space Time Bombs 97%, Eclipse Spears 96%. Worst: Cosmic Lockdown 26%, Matrix Masters 26%.
<br>*1× Sozin's Comet, 4× Iron Lady, 3× Heatblast, 3× Molten Kor, 2× Fury Shang, 1× Ulgrim, 1× Jhala, 3× Blazing Fire, 3× Searing Blade, 2× Boiling Point, 3× Dragon's Chest, 14× Essence*

**23. Rock Wall** (B, 58.2%, Wild). The Rock (20 HP) times seven: Did Shaw Win Yet? crafts more; Super Saiyan and Cody Travis.
Best: Space Time Bombs 100%, Eclipse Blasters 95%. Worst: Galaxy Lock 4%, Galaxy Lancers 4%.
<br>*4× The Rock, 2× Fox Spirit, 1× The Minotaur, 4× Chikara, 3× Ice Crusher, 1× Nightmare Claws, 3× Did Shaw Win Yet?, 2× Super Saiyan, 2× Cody Travis, 2× Bouncy Bomb, 2× Krabby Patty, 14× Essence*

**24. Frozen Tide** (B, 58.0%, Arctic). Arctic control under Atlantis.
Best: Space Time Bombs 98%, Comet Blades 97%. Worst: Space Time Giants 18%, Fangwild Garden 23%.
<br>*3× Atlantis, 3× The Rock, 3× Atlantean Ada, 2× Snowman Kor, 1× Hellshot Hattori, 3× Ice Crusher, 1× Glacier's Grace, 4× Sunken Chest, 4× Chest, 2× Bouncy Bomb, 1× Cody Travis, 13× Essence*

**25. Ninja Swarm** (B, 57.2%, Shadow). Cheap no-weapon Ninja Spirits and Danders flood the board under Evil Hideout; Caspian refills.
Best: Space Time Bombs 100%, Eclipse Blasters 97%. Worst: Galaxy Lock 20%, Galaxy Fists 20%.
<br>*2× Evil Hideout, 4× Ninja Spirit, 4× Dander, 3× Dullahan Jiro, 2× Kitsune Hattori, 1× Faerie Queen, 1× Caspian, 4× Shadow Edge, 3× Nightmare Claws, 3× Bouncy Bomb, 1× Tombstone, 1× Forgeborne Chest, 11× Essence*

**26. Workshop Forge** (B, 56.1%, Fire). Ulgrim's Workshop crafts every turn; Seven's cannons and crafted weapons arm the board.
Best: Space Time Bombs 96%, Eclipse Blasters 95%. Worst: Galaxy Lock 27%, Space Time Giants 27%.
<br>*4× Ulgrim, 4× Seven, 2× Heatblast, 2× Molten Kor, 1× Iron Lady, 4× Boiling Point, 4× Magma Spear, 3× Molten Cannon, 2× Dragon's Chest, 14× Essence*

**27. Mustafar Walls** (B, 55.4%, Fire, stage deck). Iron Lady and Molten Kor tank under Mustafar; scythes and lances finish.
Best: Atlantis Tide 98%, Space Time Bombs 98%. Worst: Matrix Masters 12%, Space Time Giants 13%.
<br>*4× Mustafar, 4× Iron Lady, 4× Molten Kor, 2× Hothead Jiro, 4× Searing Blade, 3× Candlelit Scythe, 2× Galaxy Lance, 2× Dragon's Chest, 1× Krabby Patty, 14× Essence*

**28. Matrix Witches** (B, 53.9%, Cosmic, stage deck). Matrix plus Witch Party: Artemis and Iron Lady Meltdown with scythe and lance.
Best: Atlantis Tide 96%, Eclipse Blasters 93%. Worst: Tombstone Control 14%, Hideout Brutes 17%.
<br>*4× Matrix, 4× Artemis, 2× Iron Lady, 2× Fait, 1× Witch Scarlet, 4× Amethyst Scythe, 3× Galaxy Lance, 2× Starry Scythe, 3× Witch Party, 3× Chest, 12× Essence*

**29. Hideout Claws** (B, 51.7%, Wild, stage deck). Evil Hideout makes Dander a 9-power 1-drop; The Rock and Minotaur hold the line with hammers.
Best: Atlantis Tide 96%, Eclipse Spears 96%. Worst: Hideout Ninjas 15%, Ninja Swarm 17%.
<br>*4× Evil Hideout, 4× Dander, 3× The Minotaur, 3× The Rock, 4× Nightmare Claws, 3× Chikara, 2× Winter Daggers, 2× A-Oooooo, 2× Wild Chest, 13× Essence*

**30. Wild Stampede** (B, 51.5%, Wild). Dander rush into The Minotaur; A-Oooooo and Evil Hideout pump the pack.
Best: Atlantis Tide 95%, Space Time Bombs 91%. Worst: Galaxy Fists 12%, Galaxy Lock 13%.
<br>*1× Evil Hideout, 4× The Minotaur, 3× Dander, 2× The Rock, 1× Fox Spirit, 4× Nightmare Claws, 3× Buzz Axe, 1× Rippers, 1× Chikara, 3× Wild Chest, 2× A-Oooooo, 2× Adrenaline, 13× Essence*

**31. Space Time Rush** (B, 51.3%, Shadow, stage deck). Space Time draws an extra card a turn for a cheap Dander, Ninja and Jiro flood.
Best: Eclipse Spears 98%, Space Time Bombs 98%. Worst: Scorched Earth 15%, Mustafar Walls 15%.
<br>*4× Space Time, 4× Ninja Spirit, 4× Dander, 3× Hellshot Hattori, 3× Hothead Jiro, 4× Nightmare Claws, 4× Shadow Edge, 1× Forgeborne Chest, 1× Cursed Kunai, 1× Scythe Forever, 11× Essence*

**32. Matrix Reapers** (B, 51.1%, Shadow, stage deck). Cheap Matrix wipes the enemy stage; Dark Mage and Mastermind slash with scythes.
Best: Eclipse Blasters 96%, Eclipse Spears 94%. Worst: Rock Wall 20%, Cosmic Lockdown 21%.
<br>*4× Matrix, 4× Dark Mage, 4× Dullahan Jiro, 3× Mastermind, 3× Ninja Spirit, 3× Candlelit Scythe, 3× Necro Aura, 3× Shadow Edge, 1× Bouncy Bomb, 12× Essence*

**33. Mustafar Burn** (B, 49.0%, Fire). Molten Kor sets Mustafar; Heatblast burns; Fire legends are immune while the rest melt.
Best: Eclipse Blasters 99%, Eclipse Spears 95%. Worst: Matrix Masters 11%, Cosmic Lockdown 12%.
<br>*2× Mustafar, 3× Heatblast, 3× Molten Kor, 3× Fury Shang, 3× Hothead Jiro, 2× Jhala, 4× Ancestor's Flame, 3× Blazing Fire, 2× Hot Lava, 4× Dragon's Chest, 11× Essence*

**34. Deathcap Storm** (B, 48.5%, Nature). Orb weapons for Deathcap's board-wide Storm; Fait draws, Forest Spirits trade.
Best: Eclipse Blasters 95%, Eclipse Spears 88%. Worst: Hideout Ninjas 18%, Galaxy Lock 19%.
<br>*2× Fangwild, 4× Deathcap, 4× Fait, 3× Briar Rose, 3× Faerie Queen, 1× Forest Spirit, 4× Sacred Relic, 4× Piercing Regret, 2× Remnant of Fate, 13× Essence*

**35. Workshop Smiths** (B, 48.0%, Cosmic, stage deck). The Workshop plus crafting legends: Breaker Shang, Seven and Wu Shang with spears and gauntlets.
Best: Eclipse Blasters 86%, Eclipse Spears 80%. Worst: Cosmic Lockdown 19%, Matrix Masters 22%.
<br>*4× The Workshop, 4× Fury Shang, 4× Breaker Shang, 3× Wu Shang, 3× Seven, 4× Infinite & Absolute, 3× Magma Spear, 3× Shattering Star, 12× Essence*

**36. Last Hour** (B, 48.0%, Shadow). Death's Hour sets every legend to 1 HP, then 0-cost Orbs and Ninja Spirits sweep up.
Best: Eclipse Blasters 98%, Space Time Bombs 96%. Worst: Cosmic Lockdown 12%, Scorched Earth 16%.
<br>*2× Evil Hideout, 4× Ninja Spirit, 4× Dander, 3× Forest Spirit, 2× Nightmare Claws, 3× Death's Hour, 3× Tombstone, 3× Bouncy Bomb, 2× Forgeborne Chest, 2× Orb, 1× Dark Duo, 1× Cursed Kunai, 10× Essence*

**37. Ulgrim's Forge** (B, 46.4%, Fire, stage deck). The Workshop crafts a card every turn; Ulgrim, Seven and Heatblast with spears and axes.
Best: Eclipse Blasters 85%, Atlantis Freeze 80%. Worst: Space Time Giants 19%, Tombstone Control 22%.
<br>*4× The Workshop, 4× Ulgrim, 4× Seven, 2× Heatblast, 2× Fury Shang, 1× Molten Kor, 4× Boiling Point, 4× Magma Spear, 1× Dragon's Chest, 14× Essence*

**38. Fire Nation** (B, 46.1%, Fire). Fire swarm: Fury Shang and Fire Nation grow every Fire legend; Ancestor's Flame swords.
Best: Space Time Bombs 100%, Eclipse Blasters 96%. Worst: Cosmic Lockdown 11%, Galaxy Fists 12%.
<br>*1× Mustafar, 4× Fury Shang, 4× Hothead Jiro, 3× Jhala, 2× Heatblast, 2× Ninja Spirit, 1× Seven, 4× Ancestor's Flame, 3× Magma Spear, 2× Candlelit Scythe, 1× Hot Lava, 3× Fire Nation, 10× Essence*

**39. Atlantis Snowfall** (B, 45.4%, Arctic, stage deck). Atlantis plus burn: Snowballs, Sunken Chests and Kaya's crafted Snowballs hit the player.
Best: Space Time Bombs 97%, Eclipse Blasters 96%. Worst: Matrix Masters 3%, Hideout Brutes 6%.
<br>*4× Atlantis, 4× Atlantean Ada, 3× Kitsune Hattori, 3× Alpine Kaya, 4× Glacier's Grace, 4× Sunken Chest, 3× Bouncy Bomb, 3× Snowball, 1× Water Tribe, 11× Essence*

**40. Treasure Engine** (C, 44.8%, Magic). Chests, Witch Party, and crafting/drawing legends bury the opponent in cards.
Best: Eclipse Blasters 98%, Atlantis Tide 94%. Worst: Hideout Brutes 14%, Cosmic Lockdown 16%.
<br>*4× Fait, 3× Breaker Shang, 2× Iron Lady, 2× Plague Knight, 2× Caspian, 4× Amethyst Scythe, 2× Sleight of Hand, 4× Chest, 3× Witch Party, 2× Promotion I, 12× Essence*

**41. Astro Blasters** (C, 44.6%, Cosmic). Glass cannons: Astro Commander (14 ATK) and Hellshot Hattori with blasters, Adrenaline for double shots.
Best: Space Time Bombs 99%, Eclipse Blasters 96%. Worst: Hideout Ninjas 6%, Scorched Earth 15%.
<br>*4× Astro Commander, 4× Hellshot Hattori, 4× Breaker Shang, 1× Aurora Brynn, 4× Revolvers, 2× Splish Splash, 2× Sweet Magi, 2× Shadow Edge, 3× Bouncy Bomb, 1× Cody Travis, 1× Sunken Chest, 12× Essence*

**42. Enchanted Court** (C, 43.5%, Magic). Arcadia's Pink Roses (spear + greatsword), Enchantress buffs and curses, Promotion for reach.
Best: Eclipse Blasters 92%, Atlantis Tide 88%. Worst: Galaxy Lock 9%, Cosmic Lockdown 14%.
<br>*4× Arcadia, 4× Fait, 2× Enchantress, 4× Remnant of Fate, 3× Holy Covenant, 2× Law of the Land, 2× Amethyst Scythe, 4× Chest, 2× <3, 1× Witch Party, 1× Promotion I, 11× Essence*

**43. Space Time Chests** (C, 43.5%, Cosmic, stage deck). Space Time and Cosmic Chests flood the hand; Breaker Shang crafts more Cosmic cards.
Best: Eclipse Blasters 100%, Space Time Bombs 98%. Worst: Scythe Executioners 10%, Cosmic Lancers 12%.
<br>*4× Space Time, 4× Breaker Shang, 3× Orion, 3× Artemis, 2× Aurora Brynn, 2× Galaxy Lance, 2× Shattering Star, 4× Cosmic Chest, 3× Chest, 13× Essence*

**44. Fangwild Hammers** (C, 43.1%, Nature, stage deck). Yumiko and Forest Guardian grow huge under Fangwild; Hammer Gimp removes threats.
Best: Space Time Bombs 97%, Eclipse Blasters 94%. Worst: Briar Garden 11%, Fangwild Garden 13%.
<br>*4× Fangwild, 4× Yumiko, 4× Forest Guardian, 2× Kor, 4× Law of the Land, 4× Primrose Mallet, 2× Chikara, 2× Wild Chest, 2× Krabby Patty, 1× Super Saiyan, 11× Essence*

**45. Matrix Hammers** (C, 42.4%, Wild, stage deck). Matrix shuts off enemy stages; an all-hammer deck for Yumiko's Hammer Gimp.
Best: Space Time Bombs 98%, Eclipse Blasters 95%. Worst: Tombstone Control 8%, Space Time Giants 12%.
<br>*4× Matrix, 4× Yumiko, 3× The Minotaur, 3× The Rock, 2× Witch Scarlet, 2× Fox Spirit, 4× Primrose Mallet, 4× Chikara, 1× Ice Crusher, 1× Sunken Chest, 12× Essence*

**46. Workshop Thieves** (C, 41.3%, Magic, stage deck). The Workshop crafts while Caspian and Fait draw three; Dark Mage and Raymesis finish.
Best: Atlantis Tide 85%, Eclipse Blasters 78%. Worst: Galaxy Fists 15%, Space Time Giants 16%.
<br>*4× The Workshop, 4× Fait, 3× Dark Mage, 3× Caspian, 2× Plague Knight, 1× Raymesis, 4× Sleight of Hand, 4× Necro Aura, 1× Candlelit Scythe, 1× Amethyst Scythe, 1× Bouncy Bomb, 12× Essence*

**47. Blaster Barrage** (C, 41.3%, Shadow). Every blaster legend (Hellshot, Heatblast, Astro) with Revolvers and Blazing Fire.
Best: Space Time Bombs 96%, Eclipse Blasters 87%. Worst: Hideout Ninjas 12%, Frozen Tide 12%.
<br>*4× Astro Commander, 4× Hellshot Hattori, 2× Heatblast, 1× Kitsune Hattori, 4× Blazing Fire, 4× Revolvers, 2× Aqua Blasters, 2× Shadow Edge, 3× Bouncy Bomb, 1× Adrenaline, 13× Essence*

**48. Iron Wall** (C, 41.1%, Nature). All walls: Kor, Forest Guardian, Snowman Kor, The Rock; greatsword strings and heals.
Best: Space Time Bombs 97%, Eclipse Spears 91%. Worst: Galaxy Lancers 3%, Galaxy Lock 4%.
<br>*4× The Rock, 4× Forest Guardian, 3× Faerie Queen, 3× Kor, 4× Law of the Land, 3× Chikara, 2× Piercing Regret, 3× Sunken Chest, 1× Cursed Kunai, 1× Vial of Crows, 12× Essence*

**49. Hammer Time** (C, 41.0%, Nature). Yumiko's Hammer Gimp kills anything with three hammers; Fangwild and Krabby Patty keep her up.
Best: Space Time Bombs 96%, Eclipse Blasters 94%. Worst: Galaxy Lock 8%, Galaxy Fists 10%.
<br>*2× Fangwild, 4× Yumiko, 4× Faerie Queen, 3× Fox Spirit, 2× The Rock, 4× Primrose Mallet, 4× Ice Crusher, 2× Chikara, 1× Krabby Patty, 1× Chest, 13× Essence*

**50. Mustafar Spears** (C, 40.3%, Fire, stage deck). Fury Shang, Heatblast and Seven with spears and gauntlets under Mustafar; Fire Nation pumps.
Best: Eclipse Blasters 99%, Eclipse Spears 94%. Worst: Scythe Executioners 9%, Galaxy Lock 10%.
<br>*4× Mustafar, 4× Fury Shang, 3× Seven, 2× Heatblast, 2× Molten Kor, 4× Hot Lava, 3× Magma Spear, 4× Fire Nation, 1× Xull's Fury, 1× Forgeborne Chest, 12× Essence*

**51. Fangwild Brawlers** (C, 40.2%, Nature, stage deck). Rayman Power Punches twice with Adrenaline; Fangwild keeps the punchers alive.
Best: Eclipse Blasters 96%, Eclipse Spears 92%. Worst: Last Hour 13%, Cosmic Lockdown 14%.
<br>*4× Fangwild, 4× Rayman, 3× Faerie Queen, 2× Plague Knight, 2× Kor, 2× Forest Spirit, 4× Axe of Regrowth, 4× Spine-Chilling Fists, 2× Adrenaline, 13× Essence*

**52. Lava Blasters** (C, 38.2%, Fire, stage deck). Mustafar (plus Molten Kor) melts non-Fire boards while Heatblast burns and Jhala slashes.
Best: Eclipse Blasters 100%, Eclipse Tide 94%. Worst: Treasure Hunt 4%, Matrix Masters 7%.
<br>*4× Mustafar, 4× Molten Kor, 4× Jhala, 4× Hothead Jiro, 3× Heatblast, 1× Fury Shang, 4× Ancestor's Flame, 3× Blazing Fire, 2× Sunken Chest, 1× Did Shaw Win Yet?, 10× Essence*

**53. Adrenaline Rush** (C, 36.8%, Nature). Rayman Power Punches and Dander slashes twice with Adrenaline; Super Saiyan to finish.
Best: Eclipse Blasters 83%, Eclipse Spears 82%. Worst: Scorched Earth 7%, Mustafar Walls 8%.
<br>*4× Rayman, 4× Dander, 2× Breaker Shang, 4× Spine-Chilling Fists, 4× Nightmare Claws, 3× Axe of Regrowth, 1× Piercing Regret, 3× Adrenaline, 2× Super Saiyan, 2× Xull's Fury, 11× Essence*

**54. Hideout Bombers** (C, 36.3%, Shadow, stage deck). Caspian draws and crafts Bouncy Bombs while Dander and Raymesis punch under Evil Hideout.
Best: Eclipse Blasters 91%, Atlantis Tide 90%. Worst: Hideout Rush 6%, Space Time Rush 8%.
<br>*4× Evil Hideout, 3× The Rock, 3× Caspian, 3× Dander, 1× Raymesis, 4× Sleight of Hand, 4× Rippers, 4× Nightmare Claws, 1× Tombstone, 1× Bouncy Bomb, 1× Dragon's Chest, 1× Forgeborne Chest, 10× Essence*

**55. Comet Blades** (C, 35.3%, Fire, stage deck). Sozin's Comet turns Jhala into 16/6 and Iron Lady into 12/20; swords and scythes.
Best: Eclipse Blasters 93%, Space Time Bombs 92%. Worst: Frozen Tide 3%, Rock Wall 6%.
<br>*4× Sozin's Comet, 4× Iron Lady, 4× Jhala, 4× Hothead Jiro, 4× Searing Blade, 4× Ancestor's Flame, 1× Fire Nation, 1× Krabby Patty, 14× Essence*

**56. Fangwild Witches** (C, 33.0%, Magic, stage deck). Fait draws, Enchantress curses and Deathcap storms under Fangwild with scythes and orbs.
Best: Eclipse Blasters 95%, Eclipse Spears 89%. Worst: Hideout Brutes 6%, Matrix Masters 7%.
<br>*4× Fangwild, 4× Fait, 2× Deathcap, 2× Enchantress, 4× Remnant of Fate, 4× Amethyst Scythe, 3× Sacred Relic, 4× Chest, 13× Essence*

**57. Comet Blaze** (C, 31.6%, Fire, stage deck). Heatblast and Fury Shang with blasters and spears ramp into Sozin's Comet.
Best: Eclipse Blasters 79%, Eclipse Spears 78%. Worst: Hideout Ninjas 1%, Galaxy Lock 4%.
<br>*4× Sozin's Comet, 4× Heatblast, 4× Fury Shang, 3× Seven, 4× Magma Spear, 4× Blazing Fire, 2× Dragon's Chest, 1× Fire Nation, 14× Essence*

**58. Frozen Fortress** (C, 31.1%, Arctic). Alpine Kaya crafts Snowballs and Snowman Kor; Atlantis drains the rest.
Best: Space Time Bombs 88%, Eclipse Blasters 84%. Worst: Matrix Masters 2%, Space Time Giants 3%.
<br>*4× Atlantis, 1× Full Eclipse, 4× Alpine Kaya, 3× Atlantean Ada, 2× Snowman Kor, 4× Glacier's Grace, 3× Aqua Blasters, 2× Ice Crusher, 2× Sunken Chest, 2× Snowball, 2× Water Tribe, 11× Essence*

**59. Atlantis Freeze** (C, 29.4%, Arctic, stage deck). Stun lock: Snowman Kor freezes, Bubble and Ymir's Breath stun, Atlantis drains.
Best: Eclipse Blasters 83%, Space Time Bombs 78%. Worst: Matrix Masters 2%, Hideout Brutes 2%.
<br>*4× Atlantis, 4× Snowman Kor, 4× Alpine Kaya, 3× Atlantean Ada, 4× Ice Crusher, 3× Glacier's Grace, 3× Sunken Chest, 2× Ymir's Breath, 1× Snowball, 1× Bubble, 11× Essence*

**60. Eclipse Tide** (C, 20.9%, Arctic). Full Eclipse (+5/+5 Arctic) on a wide Atlantean Ada board with Water Tribe.
Best: Eclipse Blasters 85%, Eclipse Spears 80%. Worst: Galaxy Fists 1%, Cursed Kunai 1%.
<br>*3× Full Eclipse, 4× Hellshot Hattori, 4× Atlantean Ada, 3× Alpine Kaya, 3× Aqua Blasters, 3× Glacier's Grace, 2× Splish Splash, 2× Frozen Edge, 3× Water Tribe, 1× Bubble, 12× Essence*

**61. Atlantis Tide** (C, 16.1%, Arctic, stage deck). Atlantis weakens every non-Arctic legend; Ada and Kaya shoot and stab, Ymir's Breath stuns.
Best: Eclipse Blasters 73%, Atlantis Freeze 50%. Worst: Cosmic Lockdown 0%, Matrix Masters 0%.
<br>*4× Atlantis, 4× Alpine Kaya, 4× Atlantean Ada, 2× Snowman Kor, 4× Glacier's Grace, 3× Aqua Blasters, 3× Splish Splash, 2× Ymir's Breath, 2× Water Tribe, 12× Essence*

**62. Eclipse Spears** (C, 11.3%, Arctic, stage deck). Full Eclipse makes Kaya 7/21 and Ada 7/10; all spears, Water Tribe pumps.
Best: Atlantis Tide 62%, Eclipse Blasters 59%. Worst: Matrix Masters 0%, Hideout Ninjas 0%.
<br>*4× Full Eclipse, 4× Alpine Kaya, 4× Atlantean Ada, 3× Snowman Kor, 4× Glacier's Grace, 3× Shattering Star, 2× Sunken Chest, 2× Water Tribe, 14× Essence*

**63. Space Time Bombs** (C, 10.6%, Shadow, stage deck). Space Time and Caspian draw into Bouncy Bombs and Snowballs aimed at the player.
Best: Atlantis Tide 57%, Eclipse Spears 48%. Worst: Kitsune Blades 0%, Rock Wall 0%.
<br>*4× Space Time, 4× Caspian, 4× Dander, 2× Plague Knight, 4× Sleight of Hand, 4× Nightmare Claws, 4× Bouncy Bomb, 2× Snowball, 12× Essence*

**64. Eclipse Blasters** (C, 7.2%, Arctic, stage deck). Ada's blasters and Snowman Kor's hammers under Full Eclipse; Ymir's Breath stuns.
Best: Space Time Bombs 62%, Eclipse Spears 41%. Worst: Galaxy Lock 0%, Galaxy Fists 0%.
<br>*4× Full Eclipse, 4× Atlantean Ada, 3× Alpine Kaya, 3× Snowman Kor, 4× Aqua Blasters, 3× Splish Splash, 3× Ice Crusher, 2× Ymir's Breath, 14× Essence*

## Full matchup matrix

<details><summary>Win rate of the row deck against the column deck (columns are rank numbers)</summary>

| Deck | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20 | 21 | 22 | 23 | 24 | 25 | 26 | 27 | 28 | 29 | 30 | 31 | 32 | 33 | 34 | 35 | 36 | 37 | 38 | 39 | 40 | 41 | 42 | 43 | 44 | 45 | 46 | 47 | 48 | 49 | 50 | 51 | 52 | 53 | 54 | 55 | 56 | 57 | 58 | 59 | 60 | 61 | 62 | 63 | 64 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1. Cosmic Lockdown | - | 63 | 65 | 60 | 56 | 51 | 56 | 67 | 65 | 74 | 69 | 71 | 65 | 50 | 62 | 64 | 72 | 68 | 84 | 41 | 69 | 74 | 93 | 76 | 79 | 68 | 76 | 67 | 78 | 84 | 72 | 79 | 88 | 77 | 81 | 88 | 71 | 89 | 84 | 84 | 74 | 86 | 82 | 80 | 86 | 74 | 78 | 93 | 83 | 84 | 86 | 92 | 88 | 86 | 89 | 88 | 88 | 93 | 92 | 96 | 100 | 96 | 98 | 97 |
| 2. Galaxy Lock | 37 | - | 39 | 62 | 44 | 41 | 55 | 68 | 56 | 80 | 72 | 54 | 46 | 54 | 56 | 59 | 55 | 70 | 89 | 53 | 49 | 54 | 96 | 66 | 80 | 73 | 51 | 56 | 79 | 87 | 74 | 65 | 76 | 81 | 66 | 74 | 70 | 87 | 76 | 84 | 77 | 91 | 77 | 80 | 76 | 76 | 77 | 96 | 92 | 90 | 74 | 78 | 73 | 79 | 93 | 86 | 96 | 90 | 88 | 95 | 96 | 99 | 99 | 100 |
| 3. Galaxy Fists | 35 | 61 | - | 68 | 50 | 46 | 56 | 48 | 57 | 78 | 74 | 59 | 48 | 52 | 60 | 50 | 68 | 68 | 83 | 56 | 60 | 56 | 93 | 64 | 80 | 62 | 60 | 52 | 62 | 88 | 62 | 74 | 74 | 76 | 62 | 74 | 73 | 88 | 82 | 81 | 68 | 81 | 82 | 84 | 82 | 85 | 78 | 95 | 90 | 86 | 73 | 72 | 80 | 78 | 86 | 86 | 88 | 89 | 90 | 99 | 96 | 97 | 94 | 100 |
| 4. Matrix Masters | 40 | 38 | 32 | - | 49 | 44 | 53 | 37 | 58 | 45 | 52 | 50 | 41 | 52 | 47 | 73 | 34 | 45 | 60 | 46 | 81 | 74 | 57 | 74 | 52 | 69 | 88 | 81 | 74 | 63 | 59 | 65 | 89 | 71 | 78 | 74 | 68 | 74 | 97 | 82 | 77 | 73 | 85 | 84 | 83 | 74 | 81 | 86 | 84 | 90 | 79 | 93 | 78 | 88 | 74 | 93 | 79 | 98 | 98 | 94 | 100 | 100 | 98 | 99 |
| 5. Hideout Ninjas | 44 | 56 | 50 | 51 | - | 66 | 57 | 62 | 53 | 61 | 52 | 58 | 69 | 53 | 55 | 68 | 63 | 60 | 55 | 86 | 42 | 52 | 43 | 45 | 55 | 67 | 50 | 67 | 85 | 74 | 64 | 68 | 56 | 82 | 72 | 76 | 71 | 82 | 70 | 80 | 94 | 81 | 85 | 75 | 72 | 82 | 88 | 68 | 65 | 77 | 86 | 60 | 90 | 87 | 92 | 89 | 99 | 78 | 78 | 94 | 96 | 100 | 98 | 100 |
| 6. Cosmic Lancers | 49 | 59 | 54 | 56 | 34 | - | 52 | 67 | 50 | 63 | 60 | 42 | 57 | 56 | 50 | 60 | 65 | 59 | 76 | 67 | 60 | 56 | 92 | 71 | 61 | 64 | 62 | 63 | 69 | 76 | 70 | 60 | 72 | 74 | 60 | 76 | 64 | 85 | 78 | 83 | 65 | 85 | 88 | 82 | 82 | 64 | 71 | 94 | 84 | 73 | 74 | 76 | 67 | 78 | 74 | 82 | 82 | 90 | 84 | 92 | 94 | 98 | 98 | 99 |
| 7. Hideout Brutes | 44 | 45 | 44 | 47 | 43 | 48 | - | 50 | 59 | 46 | 50 | 37 | 56 | 52 | 63 | 51 | 58 | 67 | 52 | 44 | 79 | 70 | 58 | 74 | 41 | 66 | 73 | 83 | 62 | 56 | 65 | 62 | 87 | 72 | 66 | 62 | 66 | 71 | 94 | 86 | 71 | 83 | 85 | 82 | 73 | 69 | 70 | 73 | 79 | 90 | 79 | 92 | 79 | 83 | 79 | 94 | 85 | 96 | 98 | 93 | 99 | 99 | 95 | 99 |
| 8. Galaxy Lancers | 33 | 32 | 52 | 63 | 38 | 33 | 50 | - | 34 | 70 | 70 | 51 | 47 | 57 | 56 | 49 | 60 | 74 | 81 | 55 | 46 | 57 | 96 | 68 | 64 | 65 | 59 | 55 | 68 | 84 | 60 | 60 | 68 | 76 | 61 | 72 | 70 | 82 | 84 | 78 | 61 | 75 | 86 | 82 | 84 | 80 | 67 | 97 | 85 | 76 | 70 | 73 | 68 | 80 | 84 | 80 | 84 | 90 | 95 | 90 | 100 | 98 | 97 | 100 |
| 9. Scythe Executioners | 35 | 44 | 43 | 42 | 47 | 50 | 41 | 66 | - | 44 | 52 | 48 | 46 | 66 | 49 | 61 | 48 | 46 | 49 | 59 | 77 | 67 | 64 | 73 | 48 | 65 | 72 | 78 | 67 | 56 | 58 | 66 | 88 | 64 | 66 | 68 | 73 | 79 | 86 | 80 | 71 | 74 | 90 | 82 | 82 | 76 | 73 | 81 | 84 | 91 | 68 | 88 | 64 | 86 | 81 | 89 | 82 | 93 | 81 | 93 | 100 | 98 | 97 | 100 |
| 10. Tombstone Control | 26 | 20 | 22 | 55 | 39 | 37 | 54 | 30 | 56 | - | 47 | 56 | 44 | 46 | 60 | 60 | 27 | 47 | 58 | 50 | 70 | 71 | 64 | 75 | 55 | 68 | 85 | 86 | 67 | 73 | 48 | 78 | 82 | 64 | 75 | 69 | 78 | 75 | 88 | 84 | 75 | 80 | 72 | 84 | 92 | 74 | 80 | 84 | 86 | 87 | 82 | 84 | 65 | 88 | 88 | 86 | 79 | 95 | 94 | 93 | 98 | 98 | 96 | 100 |
| 11. Wild Axe | 31 | 28 | 26 | 48 | 48 | 40 | 50 | 30 | 48 | 53 | - | 53 | 39 | 54 | 46 | 60 | 44 | 41 | 50 | 45 | 64 | 65 | 56 | 76 | 51 | 72 | 82 | 78 | 67 | 52 | 54 | 69 | 78 | 69 | 66 | 74 | 69 | 72 | 82 | 83 | 84 | 80 | 70 | 72 | 80 | 80 | 77 | 78 | 78 | 90 | 82 | 88 | 84 | 88 | 77 | 84 | 85 | 94 | 86 | 96 | 96 | 95 | 94 | 95 |
| 12. Hideout Rush | 29 | 46 | 41 | 50 | 42 | 58 | 63 | 49 | 52 | 44 | 47 | - | 50 | 57 | 50 | 62 | 61 | 52 | 57 | 70 | 48 | 68 | 43 | 47 | 49 | 68 | 52 | 74 | 82 | 76 | 50 | 63 | 58 | 73 | 70 | 72 | 78 | 73 | 78 | 76 | 80 | 85 | 62 | 55 | 74 | 80 | 85 | 56 | 63 | 73 | 83 | 50 | 87 | 94 | 91 | 85 | 89 | 88 | 78 | 97 | 90 | 100 | 97 | 99 |
| 13. Galaxy Reapers | 35 | 54 | 52 | 59 | 31 | 43 | 44 | 53 | 54 | 56 | 61 | 50 | - | 35 | 62 | 42 | 62 | 68 | 76 | 44 | 49 | 62 | 92 | 52 | 62 | 61 | 57 | 43 | 69 | 83 | 70 | 59 | 59 | 68 | 59 | 66 | 60 | 88 | 67 | 78 | 76 | 78 | 80 | 78 | 81 | 63 | 69 | 92 | 88 | 82 | 67 | 61 | 81 | 68 | 84 | 75 | 70 | 84 | 86 | 92 | 91 | 94 | 95 | 97 |
| 14. Treasure Hunt | 50 | 46 | 48 | 48 | 47 | 44 | 48 | 43 | 34 | 54 | 46 | 42 | 65 | - | 37 | 54 | 62 | 37 | 42 | 16 | 79 | 69 | 58 | 76 | 61 | 63 | 76 | 64 | 49 | 64 | 40 | 65 | 76 | 62 | 68 | 71 | 69 | 70 | 90 | 74 | 72 | 68 | 61 | 75 | 82 | 81 | 74 | 69 | 76 | 86 | 67 | 96 | 67 | 83 | 76 | 82 | 86 | 94 | 96 | 94 | 96 | 96 | 92 | 98 |
| 15. Fangwild Garden | 38 | 44 | 40 | 53 | 45 | 50 | 37 | 44 | 51 | 40 | 54 | 50 | 38 | 63 | - | 41 | 60 | 49 | 54 | 65 | 58 | 62 | 70 | 77 | 63 | 58 | 62 | 65 | 52 | 63 | 69 | 73 | 63 | 62 | 70 | 52 | 61 | 66 | 74 | 77 | 61 | 70 | 88 | 87 | 77 | 72 | 64 | 78 | 76 | 61 | 54 | 68 | 60 | 75 | 73 | 89 | 82 | 90 | 86 | 86 | 88 | 94 | 99 | 97 |
| 16. Cursed Kunai | 36 | 41 | 50 | 27 | 32 | 40 | 49 | 51 | 39 | 40 | 40 | 38 | 58 | 46 | 59 | - | 56 | 52 | 40 | 72 | 36 | 58 | 39 | 38 | 55 | 60 | 56 | 74 | 76 | 71 | 52 | 49 | 64 | 78 | 74 | 64 | 64 | 67 | 55 | 80 | 82 | 82 | 60 | 59 | 74 | 78 | 80 | 64 | 76 | 73 | 84 | 70 | 82 | 90 | 92 | 85 | 88 | 74 | 72 | 99 | 96 | 96 | 98 | 99 |
| 17. Galaxy Commanders | 28 | 45 | 32 | 66 | 37 | 35 | 42 | 40 | 52 | 73 | 56 | 39 | 38 | 38 | 40 | 44 | - | 62 | 81 | 48 | 41 | 54 | 90 | 42 | 70 | 44 | 46 | 43 | 66 | 72 | 40 | 60 | 56 | 70 | 55 | 60 | 62 | 77 | 60 | 74 | 66 | 70 | 76 | 68 | 79 | 72 | 66 | 92 | 83 | 80 | 77 | 56 | 80 | 67 | 75 | 63 | 82 | 83 | 77 | 84 | 97 | 96 | 86 | 100 |
| 18. Briar Garden | 32 | 30 | 32 | 55 | 40 | 41 | 33 | 26 | 54 | 53 | 59 | 48 | 32 | 63 | 51 | 48 | 38 | - | 46 | 70 | 50 | 62 | 72 | 60 | 53 | 64 | 65 | 64 | 54 | 59 | 72 | 66 | 54 | 63 | 64 | 59 | 49 | 48 | 66 | 82 | 63 | 65 | 87 | 89 | 80 | 63 | 54 | 84 | 83 | 64 | 63 | 64 | 64 | 75 | 73 | 78 | 68 | 94 | 86 | 84 | 84 | 98 | 97 | 94 |
| 19. Kitsune Blades | 16 | 11 | 17 | 40 | 45 | 24 | 48 | 19 | 51 | 42 | 50 | 43 | 24 | 57 | 46 | 60 | 19 | 54 | - | 82 | 46 | 50 | 48 | 57 | 49 | 56 | 62 | 73 | 79 | 65 | 57 | 69 | 61 | 70 | 63 | 58 | 57 | 77 | 70 | 67 | 71 | 74 | 73 | 80 | 77 | 72 | 73 | 79 | 75 | 84 | 82 | 66 | 84 | 76 | 80 | 84 | 83 | 84 | 68 | 91 | 98 | 97 | 100 | 98 |
| 20. Space Time Giants | 59 | 47 | 44 | 54 | 14 | 33 | 56 | 45 | 41 | 50 | 55 | 30 | 56 | 84 | 35 | 28 | 52 | 30 | 18 | - | 71 | 67 | 63 | 82 | 19 | 73 | 87 | 61 | 43 | 58 | 14 | 65 | 69 | 52 | 62 | 19 | 81 | 38 | 92 | 79 | 18 | 78 | 85 | 87 | 88 | 84 | 24 | 66 | 64 | 86 | 84 | 78 | 33 | 83 | 76 | 90 | 88 | 97 | 94 | 96 | 100 | 100 | 92 | 99 |
| 21. Scorched Earth | 31 | 51 | 40 | 19 | 58 | 40 | 21 | 54 | 23 | 30 | 36 | 52 | 51 | 21 | 42 | 64 | 59 | 50 | 55 | 29 | - | 40 | 41 | 59 | 79 | 40 | 61 | 47 | 55 | 50 | 85 | 60 | 66 | 66 | 61 | 84 | 46 | 52 | 78 | 60 | 85 | 69 | 70 | 55 | 60 | 63 | 87 | 74 | 71 | 62 | 68 | 91 | 93 | 69 | 53 | 78 | 68 | 92 | 92 | 98 | 97 | 99 | 98 | 100 |
| 22. Sozin's Inferno | 26 | 46 | 44 | 26 | 48 | 44 | 30 | 43 | 33 | 29 | 35 | 32 | 38 | 31 | 38 | 42 | 46 | 38 | 50 | 33 | 60 | - | 44 | 55 | 65 | 45 | 62 | 58 | 53 | 53 | 78 | 56 | 70 | 53 | 54 | 72 | 55 | 50 | 75 | 67 | 80 | 77 | 72 | 62 | 56 | 62 | 77 | 81 | 62 | 79 | 73 | 88 | 77 | 55 | 55 | 74 | 72 | 82 | 86 | 92 | 92 | 96 | 97 | 96 |
| 23. Rock Wall | 7 | 4 | 7 | 42 | 57 | 8 | 42 | 4 | 36 | 36 | 44 | 57 | 8 | 42 | 30 | 61 | 10 | 28 | 52 | 37 | 59 | 56 | - | 46 | 54 | 61 | 66 | 71 | 74 | 54 | 61 | 80 | 74 | 52 | 64 | 70 | 60 | 73 | 81 | 74 | 72 | 67 | 52 | 53 | 74 | 66 | 76 | 68 | 63 | 82 | 65 | 90 | 70 | 78 | 94 | 80 | 73 | 76 | 62 | 87 | 92 | 90 | 100 | 95 |
| 24. Frozen Tide | 24 | 34 | 36 | 26 | 55 | 29 | 26 | 32 | 27 | 25 | 24 | 53 | 48 | 24 | 23 | 62 | 58 | 40 | 42 | 18 | 41 | 45 | 54 | - | 76 | 40 | 46 | 51 | 54 | 50 | 70 | 70 | 68 | 52 | 64 | 80 | 44 | 66 | 65 | 57 | 72 | 67 | 50 | 60 | 71 | 66 | 88 | 69 | 66 | 74 | 57 | 86 | 80 | 78 | 97 | 73 | 81 | 74 | 79 | 88 | 92 | 92 | 98 | 96 |
| 25. Ninja Swarm | 21 | 20 | 20 | 48 | 45 | 39 | 59 | 36 | 52 | 45 | 49 | 51 | 38 | 39 | 37 | 45 | 30 | 47 | 51 | 81 | 21 | 35 | 46 | 24 | - | 38 | 27 | 66 | 83 | 76 | 72 | 62 | 25 | 75 | 66 | 78 | 63 | 67 | 26 | 72 | 78 | 64 | 80 | 44 | 57 | 76 | 85 | 48 | 64 | 39 | 79 | 38 | 89 | 86 | 89 | 80 | 89 | 48 | 48 | 94 | 71 | 89 | 100 | 97 |
| 26. Workshop Forge | 32 | 27 | 38 | 31 | 33 | 36 | 34 | 35 | 35 | 32 | 28 | 32 | 39 | 37 | 42 | 40 | 56 | 36 | 44 | 27 | 60 | 55 | 39 | 60 | 62 | - | 67 | 56 | 49 | 49 | 69 | 57 | 69 | 51 | 48 | 62 | 63 | 56 | 70 | 56 | 69 | 60 | 53 | 61 | 63 | 61 | 71 | 66 | 54 | 74 | 58 | 85 | 71 | 69 | 51 | 67 | 68 | 82 | 78 | 87 | 91 | 92 | 96 | 95 |
| 27. Mustafar Walls | 24 | 49 | 40 | 12 | 50 | 38 | 27 | 41 | 28 | 15 | 18 | 48 | 43 | 24 | 38 | 44 | 54 | 35 | 38 | 13 | 39 | 38 | 34 | 54 | 73 | 33 | - | 40 | 47 | 45 | 85 | 63 | 48 | 54 | 64 | 78 | 35 | 42 | 68 | 56 | 83 | 68 | 60 | 60 | 64 | 56 | 80 | 78 | 68 | 48 | 62 | 78 | 92 | 68 | 47 | 77 | 54 | 91 | 96 | 95 | 98 | 97 | 98 | 97 |
| 28. Matrix Witches | 33 | 44 | 48 | 19 | 33 | 37 | 17 | 45 | 22 | 14 | 22 | 26 | 57 | 36 | 35 | 26 | 57 | 36 | 27 | 39 | 53 | 42 | 29 | 49 | 34 | 44 | 60 | - | 46 | 49 | 59 | 49 | 76 | 52 | 42 | 56 | 64 | 45 | 72 | 56 | 52 | 66 | 68 | 64 | 73 | 56 | 53 | 56 | 57 | 80 | 59 | 83 | 60 | 74 | 68 | 76 | 79 | 83 | 92 | 75 | 96 | 91 | 92 | 93 |
| 29. Hideout Claws | 22 | 21 | 38 | 26 | 15 | 31 | 38 | 32 | 33 | 33 | 33 | 18 | 31 | 51 | 48 | 24 | 34 | 46 | 21 | 57 | 45 | 47 | 26 | 46 | 17 | 51 | 53 | 54 | - | 41 | 40 | 43 | 56 | 49 | 52 | 32 | 54 | 50 | 64 | 66 | 56 | 61 | 69 | 70 | 61 | 69 | 39 | 64 | 63 | 71 | 69 | 59 | 61 | 67 | 65 | 80 | 83 | 78 | 74 | 83 | 96 | 96 | 92 | 92 |
| 30. Wild Stampede | 16 | 13 | 12 | 37 | 26 | 24 | 44 | 16 | 44 | 27 | 48 | 24 | 17 | 36 | 37 | 29 | 28 | 41 | 35 | 42 | 50 | 47 | 46 | 50 | 24 | 51 | 55 | 51 | 59 | - | 37 | 47 | 54 | 57 | 51 | 36 | 64 | 54 | 76 | 65 | 45 | 63 | 66 | 65 | 58 | 66 | 50 | 68 | 63 | 62 | 69 | 72 | 57 | 69 | 67 | 76 | 75 | 74 | 61 | 82 | 95 | 90 | 91 | 90 |
| 31. Space Time Rush | 28 | 26 | 38 | 41 | 36 | 30 | 35 | 40 | 42 | 52 | 46 | 50 | 30 | 60 | 31 | 48 | 60 | 28 | 43 | 86 | 15 | 22 | 39 | 30 | 28 | 31 | 15 | 41 | 60 | 63 | - | 33 | 38 | 68 | 52 | 45 | 53 | 41 | 27 | 53 | 55 | 50 | 53 | 38 | 34 | 74 | 80 | 26 | 46 | 50 | 80 | 46 | 83 | 92 | 91 | 80 | 95 | 51 | 52 | 83 | 72 | 98 | 98 | 98 |
| 32. Matrix Reapers | 21 | 35 | 26 | 35 | 32 | 40 | 38 | 40 | 34 | 22 | 31 | 37 | 41 | 35 | 27 | 51 | 40 | 34 | 31 | 35 | 40 | 44 | 20 | 30 | 38 | 43 | 37 | 51 | 57 | 53 | 67 | - | 47 | 49 | 42 | 64 | 50 | 57 | 54 | 55 | 57 | 48 | 75 | 50 | 64 | 61 | 64 | 50 | 47 | 57 | 67 | 56 | 59 | 76 | 67 | 70 | 73 | 71 | 74 | 77 | 90 | 94 | 94 | 96 |
| 33. Mustafar Burn | 12 | 24 | 26 | 11 | 44 | 28 | 13 | 32 | 12 | 18 | 22 | 42 | 41 | 24 | 37 | 36 | 44 | 46 | 39 | 31 | 34 | 30 | 26 | 32 | 75 | 31 | 52 | 24 | 44 | 46 | 62 | 53 | - | 66 | 46 | 72 | 45 | 42 | 46 | 40 | 76 | 60 | 49 | 42 | 37 | 57 | 77 | 56 | 48 | 67 | 66 | 76 | 86 | 62 | 45 | 66 | 51 | 76 | 74 | 94 | 90 | 95 | 90 | 99 |
| 34. Deathcap Storm | 23 | 19 | 24 | 29 | 18 | 26 | 28 | 24 | 36 | 36 | 31 | 27 | 32 | 38 | 38 | 22 | 30 | 37 | 30 | 48 | 34 | 47 | 48 | 48 | 25 | 49 | 46 | 48 | 51 | 43 | 32 | 51 | 34 | - | 52 | 36 | 56 | 48 | 61 | 70 | 41 | 58 | 72 | 72 | 67 | 50 | 46 | 66 | 66 | 54 | 58 | 44 | 49 | 53 | 67 | 71 | 69 | 70 | 75 | 81 | 83 | 88 | 86 | 95 |
| 35. Workshop Smiths | 19 | 34 | 38 | 22 | 28 | 40 | 34 | 39 | 34 | 25 | 34 | 30 | 41 | 32 | 30 | 26 | 45 | 36 | 37 | 38 | 39 | 46 | 36 | 36 | 34 | 52 | 36 | 58 | 48 | 49 | 48 | 58 | 55 | 48 | - | 37 | 72 | 57 | 42 | 52 | 47 | 43 | 62 | 60 | 56 | 59 | 56 | 51 | 52 | 64 | 53 | 62 | 50 | 53 | 57 | 62 | 75 | 52 | 60 | 79 | 62 | 80 | 80 | 86 |
| 36. Last Hour | 12 | 26 | 26 | 26 | 24 | 24 | 38 | 28 | 32 | 31 | 26 | 28 | 34 | 29 | 48 | 36 | 40 | 41 | 42 | 81 | 16 | 28 | 30 | 20 | 22 | 38 | 22 | 44 | 68 | 64 | 55 | 36 | 28 | 64 | 63 | - | 41 | 60 | 30 | 30 | 74 | 52 | 54 | 38 | 42 | 56 | 86 | 38 | 43 | 36 | 87 | 22 | 92 | 82 | 88 | 78 | 90 | 47 | 47 | 90 | 68 | 88 | 96 | 98 |
| 37. Ulgrim's Forge | 29 | 30 | 27 | 32 | 29 | 36 | 34 | 30 | 27 | 22 | 31 | 22 | 40 | 31 | 39 | 36 | 38 | 51 | 43 | 19 | 54 | 45 | 40 | 56 | 37 | 37 | 65 | 36 | 46 | 36 | 47 | 50 | 55 | 44 | 28 | 59 | - | 50 | 67 | 49 | 53 | 48 | 40 | 60 | 52 | 52 | 51 | 55 | 50 | 58 | 54 | 74 | 41 | 42 | 44 | 44 | 54 | 68 | 80 | 60 | 70 | 70 | 69 | 85 |
| 38. Fire Nation | 11 | 13 | 12 | 26 | 18 | 15 | 29 | 18 | 21 | 25 | 28 | 27 | 12 | 30 | 34 | 33 | 23 | 52 | 23 | 62 | 48 | 50 | 27 | 34 | 33 | 44 | 58 | 55 | 50 | 46 | 59 | 43 | 58 | 52 | 43 | 40 | 50 | - | 25 | 49 | 55 | 34 | 69 | 54 | 52 | 55 | 54 | 56 | 50 | 71 | 72 | 83 | 63 | 69 | 49 | 57 | 68 | 38 | 52 | 79 | 64 | 88 | 100 | 96 |
| 39. Atlantis Snowfall | 16 | 24 | 18 | 3 | 30 | 22 | 6 | 16 | 14 | 12 | 18 | 22 | 33 | 10 | 26 | 45 | 40 | 34 | 30 | 8 | 22 | 25 | 19 | 35 | 74 | 30 | 32 | 28 | 36 | 24 | 73 | 46 | 54 | 39 | 58 | 70 | 33 | 75 | - | 35 | 68 | 64 | 34 | 44 | 50 | 38 | 72 | 64 | 62 | 64 | 50 | 65 | 80 | 63 | 82 | 55 | 63 | 80 | 74 | 82 | 94 | 82 | 97 | 96 |
| 40. Treasure Engine | 16 | 16 | 19 | 18 | 20 | 17 | 14 | 22 | 20 | 16 | 17 | 24 | 22 | 26 | 23 | 20 | 26 | 18 | 33 | 21 | 40 | 33 | 26 | 43 | 28 | 44 | 44 | 44 | 34 | 35 | 47 | 45 | 60 | 30 | 48 | 70 | 51 | 51 | 65 | - | 60 | 54 | 23 | 51 | 51 | 55 | 58 | 44 | 55 | 60 | 50 | 88 | 68 | 69 | 65 | 66 | 63 | 70 | 66 | 87 | 94 | 93 | 94 | 98 |
| 41. Astro Blasters | 26 | 23 | 32 | 23 | 6 | 35 | 29 | 39 | 29 | 25 | 16 | 20 | 24 | 28 | 39 | 18 | 34 | 37 | 29 | 82 | 15 | 20 | 28 | 28 | 22 | 31 | 17 | 48 | 44 | 55 | 45 | 43 | 24 | 59 | 53 | 26 | 47 | 46 | 32 | 40 | - | 45 | 68 | 67 | 60 | 62 | 71 | 48 | 64 | 44 | 67 | 26 | 60 | 61 | 72 | 68 | 80 | 46 | 52 | 82 | 66 | 91 | 99 | 96 |
| 42. Enchanted Court | 14 | 9 | 19 | 27 | 19 | 15 | 17 | 25 | 26 | 20 | 20 | 15 | 22 | 32 | 30 | 18 | 30 | 35 | 26 | 22 | 31 | 23 | 33 | 33 | 36 | 40 | 32 | 34 | 39 | 37 | 50 | 52 | 40 | 42 | 57 | 48 | 52 | 66 | 36 | 46 | 55 | - | 32 | 66 | 56 | 56 | 49 | 40 | 60 | 56 | 56 | 60 | 74 | 56 | 58 | 76 | 65 | 68 | 60 | 78 | 88 | 88 | 88 | 92 |
| 43. Space Time Chests | 18 | 23 | 18 | 15 | 15 | 12 | 15 | 14 | 10 | 28 | 30 | 38 | 20 | 39 | 12 | 40 | 24 | 13 | 27 | 15 | 30 | 28 | 48 | 50 | 20 | 47 | 40 | 32 | 31 | 34 | 47 | 25 | 51 | 28 | 38 | 46 | 60 | 31 | 66 | 77 | 32 | 68 | - | 53 | 39 | 62 | 35 | 56 | 42 | 57 | 44 | 75 | 32 | 80 | 42 | 72 | 58 | 84 | 83 | 78 | 96 | 97 | 98 | 100 |
| 44. Fangwild Hammers | 20 | 20 | 16 | 16 | 25 | 18 | 18 | 18 | 18 | 16 | 28 | 45 | 22 | 25 | 13 | 41 | 32 | 11 | 20 | 13 | 45 | 38 | 47 | 40 | 56 | 39 | 40 | 36 | 30 | 35 | 62 | 50 | 57 | 28 | 40 | 62 | 40 | 46 | 56 | 49 | 33 | 34 | 47 | - | 51 | 45 | 36 | 43 | 42 | 43 | 40 | 79 | 54 | 64 | 66 | 68 | 57 | 66 | 80 | 67 | 87 | 89 | 97 | 94 |
| 45. Matrix Hammers | 14 | 24 | 18 | 17 | 28 | 18 | 27 | 16 | 18 | 8 | 20 | 26 | 19 | 18 | 23 | 26 | 21 | 20 | 23 | 12 | 40 | 44 | 26 | 29 | 43 | 37 | 36 | 27 | 39 | 42 | 66 | 36 | 63 | 33 | 44 | 58 | 48 | 48 | 50 | 49 | 40 | 44 | 61 | 49 | - | 45 | 48 | 39 | 40 | 49 | 40 | 72 | 57 | 62 | 72 | 51 | 50 | 79 | 70 | 71 | 92 | 91 | 98 | 95 |
| 46. Workshop Thieves | 26 | 24 | 15 | 26 | 18 | 36 | 31 | 20 | 24 | 26 | 20 | 20 | 37 | 19 | 28 | 22 | 28 | 37 | 28 | 16 | 37 | 38 | 34 | 34 | 24 | 39 | 44 | 44 | 31 | 34 | 26 | 39 | 43 | 50 | 41 | 44 | 48 | 45 | 62 | 45 | 38 | 44 | 38 | 55 | 55 | - | 46 | 46 | 51 | 49 | 46 | 60 | 44 | 44 | 52 | 62 | 51 | 70 | 74 | 62 | 85 | 74 | 74 | 78 |
| 47. Blaster Barrage | 22 | 23 | 22 | 19 | 12 | 29 | 30 | 33 | 27 | 20 | 23 | 15 | 31 | 26 | 36 | 20 | 34 | 46 | 27 | 76 | 13 | 23 | 24 | 12 | 15 | 29 | 20 | 47 | 61 | 50 | 20 | 36 | 23 | 54 | 44 | 14 | 49 | 46 | 28 | 42 | 29 | 51 | 65 | 64 | 52 | 54 | - | 55 | 50 | 60 | 74 | 21 | 67 | 57 | 72 | 64 | 64 | 41 | 48 | 74 | 53 | 84 | 96 | 87 |
| 48. Iron Wall | 7 | 4 | 5 | 14 | 32 | 6 | 27 | 3 | 19 | 16 | 22 | 44 | 8 | 31 | 22 | 36 | 8 | 16 | 21 | 34 | 26 | 19 | 32 | 31 | 52 | 34 | 22 | 44 | 36 | 32 | 74 | 50 | 44 | 34 | 49 | 62 | 45 | 44 | 36 | 56 | 52 | 60 | 44 | 57 | 61 | 55 | 46 | - | 57 | 35 | 38 | 71 | 57 | 66 | 59 | 66 | 56 | 48 | 47 | 78 | 62 | 91 | 97 | 91 |
| 49. Hammer Time | 17 | 8 | 10 | 16 | 35 | 16 | 21 | 15 | 16 | 14 | 22 | 37 | 12 | 24 | 24 | 24 | 17 | 17 | 25 | 36 | 29 | 38 | 37 | 34 | 36 | 46 | 32 | 43 | 37 | 37 | 54 | 53 | 52 | 34 | 48 | 57 | 50 | 50 | 38 | 45 | 36 | 40 | 57 | 58 | 60 | 49 | 50 | 42 | - | 36 | 41 | 72 | 36 | 58 | 61 | 59 | 60 | 54 | 56 | 59 | 64 | 87 | 96 | 94 |
| 50. Mustafar Spears | 16 | 10 | 14 | 10 | 23 | 27 | 10 | 24 | 9 | 13 | 10 | 27 | 18 | 14 | 39 | 27 | 20 | 36 | 16 | 14 | 38 | 21 | 18 | 26 | 61 | 26 | 52 | 20 | 29 | 38 | 50 | 42 | 33 | 46 | 36 | 64 | 42 | 29 | 36 | 40 | 56 | 44 | 42 | 57 | 51 | 51 | 40 | 65 | 64 | - | 51 | 60 | 67 | 48 | 35 | 50 | 32 | 60 | 79 | 86 | 88 | 94 | 92 | 99 |
| 51. Fangwild Brawlers | 14 | 26 | 27 | 21 | 14 | 26 | 21 | 30 | 32 | 18 | 18 | 17 | 33 | 33 | 46 | 16 | 23 | 37 | 18 | 16 | 32 | 27 | 35 | 42 | 21 | 42 | 38 | 41 | 31 | 31 | 20 | 33 | 34 | 42 | 47 | 13 | 46 | 28 | 50 | 50 | 33 | 44 | 56 | 60 | 60 | 54 | 26 | 62 | 59 | 49 | - | 45 | 37 | 33 | 50 | 62 | 50 | 68 | 86 | 68 | 81 | 92 | 78 | 96 |
| 52. Lava Blasters | 8 | 22 | 28 | 7 | 40 | 24 | 8 | 27 | 12 | 16 | 12 | 50 | 39 | 4 | 32 | 30 | 44 | 36 | 34 | 22 | 9 | 12 | 10 | 14 | 62 | 15 | 22 | 17 | 41 | 28 | 54 | 44 | 24 | 56 | 38 | 78 | 26 | 17 | 35 | 12 | 74 | 40 | 25 | 21 | 28 | 40 | 79 | 29 | 28 | 40 | 55 | - | 82 | 64 | 18 | 51 | 35 | 57 | 76 | 94 | 86 | 92 | 80 | 100 |
| 53. Adrenaline Rush | 12 | 27 | 20 | 22 | 10 | 33 | 21 | 32 | 36 | 35 | 16 | 13 | 19 | 33 | 40 | 18 | 20 | 36 | 16 | 67 | 7 | 23 | 30 | 20 | 11 | 29 | 8 | 40 | 39 | 42 | 17 | 41 | 14 | 51 | 50 | 8 | 59 | 37 | 20 | 32 | 40 | 26 | 68 | 46 | 43 | 56 | 33 | 43 | 64 | 33 | 63 | 18 | - | 42 | 66 | 61 | 67 | 31 | 40 | 74 | 58 | 82 | 78 | 83 |
| 54. Hideout Bombers | 14 | 21 | 22 | 12 | 13 | 22 | 17 | 20 | 14 | 12 | 12 | 6 | 32 | 17 | 25 | 10 | 33 | 25 | 24 | 17 | 31 | 45 | 22 | 22 | 14 | 31 | 32 | 26 | 33 | 31 | 8 | 24 | 38 | 47 | 47 | 18 | 57 | 31 | 37 | 31 | 39 | 44 | 20 | 36 | 38 | 56 | 43 | 34 | 42 | 52 | 67 | 36 | 58 | - | 50 | 64 | 81 | 62 | 61 | 71 | 90 | 86 | 72 | 91 |
| 55. Comet Blades | 11 | 7 | 14 | 26 | 8 | 26 | 21 | 16 | 19 | 12 | 23 | 9 | 16 | 24 | 27 | 8 | 25 | 27 | 20 | 24 | 47 | 45 | 6 | 3 | 11 | 49 | 53 | 32 | 35 | 33 | 9 | 33 | 55 | 33 | 43 | 12 | 56 | 51 | 18 | 35 | 28 | 42 | 58 | 34 | 28 | 48 | 28 | 41 | 39 | 65 | 50 | 82 | 34 | 50 | - | 42 | 69 | 28 | 37 | 70 | 62 | 80 | 92 | 93 |
| 56. Fangwild Witches | 12 | 14 | 14 | 7 | 11 | 18 | 6 | 20 | 11 | 14 | 16 | 15 | 25 | 18 | 11 | 15 | 37 | 22 | 16 | 10 | 22 | 26 | 20 | 27 | 20 | 33 | 23 | 24 | 20 | 24 | 20 | 30 | 34 | 29 | 38 | 22 | 56 | 42 | 45 | 34 | 32 | 24 | 28 | 32 | 49 | 38 | 36 | 34 | 41 | 50 | 38 | 49 | 39 | 36 | 58 | - | 47 | 66 | 74 | 69 | 86 | 89 | 70 | 95 |
| 57. Comet Blaze | 12 | 4 | 12 | 21 | 1 | 18 | 15 | 16 | 18 | 21 | 15 | 11 | 30 | 14 | 18 | 12 | 18 | 32 | 17 | 12 | 32 | 28 | 27 | 19 | 11 | 32 | 46 | 21 | 17 | 25 | 5 | 27 | 49 | 31 | 25 | 10 | 46 | 32 | 37 | 37 | 20 | 35 | 42 | 42 | 50 | 49 | 36 | 44 | 40 | 68 | 50 | 65 | 33 | 19 | 31 | 53 | - | 46 | 53 | 53 | 73 | 78 | 56 | 79 |
| 58. Frozen Fortress | 7 | 10 | 11 | 2 | 22 | 10 | 4 | 10 | 7 | 5 | 6 | 12 | 16 | 6 | 10 | 26 | 17 | 6 | 16 | 3 | 8 | 18 | 24 | 26 | 52 | 18 | 9 | 17 | 22 | 26 | 49 | 29 | 24 | 30 | 48 | 53 | 32 | 62 | 20 | 30 | 55 | 32 | 16 | 34 | 21 | 30 | 59 | 52 | 46 | 40 | 32 | 42 | 69 | 38 | 72 | 34 | 55 | - | 57 | 56 | 76 | 65 | 88 | 84 |
| 59. Atlantis Freeze | 8 | 12 | 10 | 2 | 22 | 16 | 2 | 5 | 19 | 6 | 14 | 22 | 14 | 4 | 14 | 28 | 23 | 14 | 32 | 6 | 8 | 14 | 38 | 21 | 52 | 22 | 4 | 8 | 26 | 39 | 48 | 26 | 26 | 25 | 40 | 53 | 20 | 48 | 26 | 34 | 48 | 40 | 17 | 20 | 30 | 26 | 52 | 53 | 44 | 21 | 14 | 24 | 60 | 39 | 63 | 26 | 47 | 43 | - | 46 | 50 | 70 | 78 | 83 |
| 60. Eclipse Tide | 4 | 5 | 1 | 6 | 6 | 8 | 7 | 10 | 7 | 7 | 4 | 3 | 8 | 6 | 14 | 1 | 16 | 16 | 9 | 4 | 2 | 8 | 13 | 12 | 6 | 13 | 5 | 25 | 17 | 18 | 17 | 23 | 6 | 19 | 21 | 10 | 40 | 21 | 18 | 13 | 18 | 22 | 22 | 33 | 29 | 38 | 26 | 22 | 41 | 14 | 32 | 6 | 26 | 29 | 30 | 31 | 47 | 44 | 54 | - | 76 | 80 | 64 | 85 |
| 61. Atlantis Tide | 0 | 4 | 4 | 0 | 4 | 6 | 1 | 0 | 0 | 2 | 4 | 10 | 9 | 4 | 12 | 4 | 3 | 16 | 2 | 0 | 3 | 8 | 8 | 8 | 29 | 9 | 2 | 4 | 4 | 5 | 28 | 10 | 10 | 17 | 38 | 32 | 30 | 36 | 6 | 6 | 34 | 12 | 4 | 13 | 8 | 15 | 47 | 38 | 36 | 12 | 19 | 14 | 42 | 10 | 38 | 14 | 27 | 24 | 50 | 24 | - | 38 | 42 | 73 |
| 62. Eclipse Spears | 4 | 1 | 3 | 0 | 0 | 2 | 1 | 2 | 2 | 2 | 5 | 0 | 6 | 4 | 6 | 4 | 4 | 2 | 3 | 0 | 1 | 4 | 10 | 8 | 11 | 8 | 3 | 9 | 4 | 10 | 2 | 6 | 5 | 12 | 20 | 12 | 30 | 12 | 18 | 7 | 9 | 12 | 3 | 11 | 9 | 26 | 16 | 9 | 13 | 6 | 8 | 8 | 18 | 14 | 20 | 11 | 22 | 35 | 30 | 20 | 62 | - | 52 | 59 |
| 63. Space Time Bombs | 2 | 1 | 6 | 2 | 2 | 2 | 5 | 3 | 3 | 4 | 6 | 3 | 5 | 8 | 1 | 2 | 14 | 3 | 0 | 8 | 2 | 3 | 0 | 2 | 0 | 4 | 2 | 8 | 8 | 9 | 2 | 6 | 10 | 14 | 20 | 4 | 31 | 0 | 3 | 6 | 1 | 12 | 2 | 3 | 2 | 26 | 4 | 3 | 4 | 8 | 22 | 20 | 22 | 28 | 8 | 30 | 44 | 12 | 22 | 36 | 57 | 48 | - | 38 |
| 64. Eclipse Blasters | 3 | 0 | 0 | 1 | 0 | 1 | 1 | 0 | 0 | 0 | 5 | 1 | 3 | 2 | 3 | 1 | 0 | 6 | 2 | 1 | 0 | 4 | 5 | 4 | 3 | 5 | 3 | 7 | 8 | 10 | 2 | 4 | 1 | 5 | 14 | 2 | 15 | 4 | 4 | 2 | 4 | 8 | 0 | 6 | 5 | 22 | 13 | 9 | 6 | 1 | 4 | 0 | 17 | 9 | 7 | 5 | 21 | 16 | 17 | 15 | 27 | 41 | 62 | - |

</details>
