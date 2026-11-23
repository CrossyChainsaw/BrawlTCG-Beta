# BrawlTCG deck analysis: 75 decks (v1.5.0 rules)

The 59 decks that ship with the game plus **16 new decks** built around the v1.5.0 cards played two full round robins: **111,000 games with Normal bots** (40 per matchup) and **13,875 games with Hard bots** (5 per matchup). The two rankings agree closely; see [Hard bots](#hard-bots-second-round-robin).

> **Note:** these games were played before two late v1.5.0 changes: chests craft **2** cards (was 3), and a player who gets no card damages **their own legends** first (the player only when they have none). Chest decks (Cosmic Lockdown, Treasure Hunt, Wild Axe, Frozen Tide, …) are probably a little weaker than shown here.

## What changed since the 64-deck analysis

- **New cards:** Fallout, Superfan Brynn, Mako's Reef, Art Class, Final Form Petra (with the new "When Eliminated" trigger).
- **Death's Hour** costs 5 (was 7).
- **Default attacks are +0:** Axe Swing, Hammer Swing and Cannon Blast lost their +1.
- **No card, take damage:** a player who gets no card at the start of their turn (full hand or empty deck) takes 1 damage.

## How this was done

1. **16 new decks**, each built around one new card or a changed card (3–4 decks for Mako's Reef, Final Form Petra and Superfan Brynn, 2 each for Fallout, Art Class and Death's Hour).
2. **Short tuning:** each new deck tried **16 single-card changes** against a fixed gauntlet of 16 shipped decks (ranks 1, 3, 5, 8 … 59 of the old list), 16 games per opponent. A change was kept only if it also won on a second, separate set of games. The deck's key card could never drop below 2–3 copies. This is much lighter than before (50 changes against all decks), so the new decks are probably still a little under their best.
3. **Round robin:** all 75 decks, 40 games per matchup, each deck going first in half. Both sides are the in-game **Normal** bot with 50 starting health and the starting-hand redraw. Games still going after 160 turns count as half a win each (only 329 of 111,000, 0.3%). An average game lasted 33.9 turns.

**Tiers:** **S** 70%+, **A** 60–70%, **B** 45–60%, **C** under 45%.

**Accuracy:** a single matchup (40 games) is only accurate to about ±15 points. An overall win rate averages 2,960 games and is accurate to about ±1.8 points.

## Key findings

- **Silent Galaxy still rules, and Superfan Brynn made it stronger.** The new **Galaxy Fans** deck (Silent Galaxy + Superfan Brynn + Cosmic spear legends) is **#1 at 79.2%**, ahead of Cosmic Lockdown (76.7%). Aurora Brynn, Orion, Wu Shang and Breaker Shang all use spears, so Superfan Brynn buffs almost the whole deck and hands out weapons. Four of the top five decks are Silent Galaxy decks. This is the biggest balance problem, as in both earlier analyses.
- **Superfan Brynn is strong outside Galaxy too.** **Superfan Spears** (Arctic/Cosmic) is **#17 at 64.3%**. That is the best Arctic deck by far: every other Arctic deck is at 54.5% or lower. Superfan Brynn fixes Arctic's lack of damage. It may be a bit strong at 4; watch it.
- **Fallout works as intended.** **Fallout Control** (Molten Kor walls, Iron Lady, Fallout) is **#16 at 64.6%**, the best new non-Galaxy deck. It beats most of the field but loses to the Galaxy decks (15–28%), which keep winning through the reset. Fallout at 7 looks right.
- **Final Form Petra is a good card in slow decks only.** The three Petra decks land at 44.5–47.1% (B/C border). Tuning *added* Petra copies to two decks (Final Form went to 4 copies, Hour Sweep picked one up), so the card itself is valued. Cost 9 is the problem against fast decks: Hideout Ninjas beats all three Petra decks about 90% of the time, and Hideout Rush, Cursed Kunai, Space Time Rush and Last Hour do almost as well. Against slow decks (Frozen Fortress, Fangwild Hammers) the Petra decks win about 90%.
- **Mako's Reef is the weakest new card, but the bot undersells it.** All three Reef decks besides Reef Ninjas finished last or near last (Reef Freeze 11.1%, Reef Swarm 20.8%, Reef Orbs 27.4%). Tuning even cut a Reef from Reef Swarm. **Caveat:** the bot scores a position partly by the HP of the legends on the board, so to the bot, Mako's Reef looks like throwing away HP on both sides. It doesn't see that its cheap legends can now trade with big ones. A human will play it better. **Reef Ninjas** (53.9%) shows the idea can work: 1-HP Ninja Spirits lose nothing under the Reef. Cost 7 is also late for a stage (same finding as Sozin's Comet and Full Eclipse).
- **Art Class is fine, not exciting.** Art School 44.3% and Art Garden 44.9%. Cheap legends are weak in a field this strong. It is a fair card at 3.
- **Death's Hour at 5 helped.** **Last Hour** (the shipped Death's Hour deck) climbed from #36 to **#28** (48.0% → 55.2%), the biggest gain of any deck. The new Hour decks are lower (43–45%).
- **The axe/hammer nerf hit hammer and axe decks.** The decks that lost the most are hammer/axe decks: Matrix Hammers −8.6 points, Rock Wall −8.2, Hammer Time −7.9, Fangwild Hammers −6.8, Wild Stampede −5.1, Hideout Brutes (#7 → #15). Wild Axe held up (65.8%, #13). Hammer decks were already weak, so they may need help now.
- **No-card damage ends games instead of stalling them.** Only 0.3% of games reached the 160-turn limit.

## Hard bots: second round robin

The same 75 decks (new decks as tuned above) played a second round robin with the in-game **Hard** bot on both sides. Hard looks a move ahead and plays out its best moves through the opponent's reply before choosing, so it uses combos and control cards better. It is about 25 times slower, so this run has only **5 games per matchup: 13,875 games**. An overall win rate is accurate to about **±5 points**; a single matchup can't be read on its own.

**What it shows:**

- **The Normal results hold.** The two rankings agree closely (correlation 0.97). The top two are the same (Galaxy Fans 80.0%, Cosmic Lockdown 79.2%), and Silent Galaxy decks are still 4 of the top 7.
- **Stun and control decks gain the most with a better pilot.** The four biggest gains are all Arctic stun decks: Frozen Fortress +9.1, Frozen Tide +9.0, Atlantis Snowfall +8.9, Atlantis Freeze +8.8. Frozen Tide moves into the A tier (63.5%). The Normal bot was underplaying Bubble, Ymir's Breath and Snowman Kor. Arctic is less weak than the Normal run suggested.
- **Fast Shadow decks also gain:** Hideout Rush is #3 (72.7%), Hideout Ninjas #4.
- **The new decks drop 1–7 points.** They were tuned against Normal bots, so the tuning partly fit the Normal bot's habits. The two strong ones hold: Superfan Spears 64.2% (#17) and Fallout Control 63.2% (#20). The Petra, Art Class and Hour decks fall to the C tier (39.7–43.0%).
- **Mako's Reef is still last** (Reef Freeze 10.9%, Reef Swarm 20.5%, Reef Orbs 22.4%) even with the better bot. Hard still scores positions with the same evaluation as Normal (legend HP counts), so the caveat above remains, but the stage now looks weak rather than just misplayed. Reef Ninjas stays mid-table (52.3%).
- Only 54 games (0.4%) reached the 160-turn limit.

| # | Tier | Deck | Hard bots | Normal bots | Change |
|---|---|---|---|---|---|
| 1 | **S** | Galaxy Fans *(new)* | 80.0% | 79.2% (#1) | +0.8 |
| 2 | **S** | Cosmic Lockdown | 79.2% | 76.7% (#2) | +2.5 |
| 3 | **S** | Hideout Rush | 72.7% | 68.4% (#8) | +4.3 |
| 4 | **S** | Hideout Ninjas | 71.6% | 69.3% (#6) | +2.3 |
| 5 | **S** | Cosmic Lancers | 71.6% | 68.1% (#9) | +3.5 |
| 6 | **S** | Galaxy Fists | 71.1% | 71.0% (#3) | +0.1 |
| 7 | **S** | Galaxy Lock | 71.0% | 70.9% (#5) | +0.1 |
| 8 | **S** | Treasure Hunt | 70.4% | 67.0% (#11) | +3.4 |
| 9 | **A** | Cursed Kunai | 69.0% | 65.3% (#14) | +3.7 |
| 10 | **A** | Wild Axe | 68.9% | 65.8% (#13) | +3.1 |
| 11 | **A** | Matrix Masters | 67.6% | 70.9% (#4) | -3.3 |
| 12 | **A** | Galaxy Lancers | 67.3% | 67.8% (#10) | -0.4 |
| 13 | **A** | Tombstone Control | 67.0% | 68.5% (#7) | -1.5 |
| 14 | **A** | Hideout Brutes | 65.1% | 65.1% (#15) | +0.0 |
| 15 | **A** | Scythe Executioners | 65.1% | 66.1% (#12) | -0.9 |
| 16 | **A** | Scorched Earth | 64.5% | 60.6% (#23) | +3.9 |
| 17 | **A** | Superfan Spears *(new)* | 64.2% | 64.3% (#17) | -0.1 |
| 18 | **A** | Galaxy Commanders | 63.6% | 60.7% (#22) | +2.9 |
| 19 | **A** | Frozen Tide | 63.5% | 54.5% (#29) | +9.0 |
| 20 | **A** | Fallout Control *(new)* | 63.2% | 64.6% (#16) | -1.3 |
| 21 | **A** | Kitsune Blades | 63.0% | 63.6% (#18) | -0.7 |
| 22 | **A** | Fangwild Garden | 62.4% | 62.6% (#20) | -0.2 |
| 23 | **A** | Galaxy Reapers | 61.2% | 63.1% (#19) | -1.9 |
| 24 | **B** | Ninja Swarm | 59.5% | 61.3% (#21) | -1.8 |
| 25 | **B** | Briar Garden | 59.1% | 60.0% (#24) | -0.9 |
| 26 | **B** | Space Time Giants | 56.5% | 57.8% (#26) | -1.3 |
| 27 | **B** | Matrix Witches | 56.2% | 51.2% (#32) | +5.1 |
| 28 | **B** | Sozin's Inferno | 55.1% | 58.2% (#25) | -3.1 |
| 29 | **B** | Rock Wall | 53.6% | 50.0% (#33) | +3.7 |
| 30 | **B** | Space Time Rush | 53.5% | 55.4% (#27) | -1.9 |
| 31 | **B** | Last Hour | 52.7% | 55.2% (#28) | -2.5 |
| 32 | **B** | Mustafar Burn | 52.6% | 49.9% (#34) | +2.6 |
| 33 | **B** | Workshop Forge | 52.4% | 49.8% (#35) | +2.6 |
| 34 | **B** | Reef Ninjas *(new)* | 52.3% | 53.9% (#30) | -1.6 |
| 35 | **B** | Atlantis Snowfall | 52.2% | 43.2% (#54) | +8.9 |
| 36 | **B** | Matrix Reapers | 50.5% | 48.0% (#37) | +2.6 |
| 37 | **B** | Treasure Engine | 50.5% | 47.5% (#39) | +3.0 |
| 38 | **B** | Deathcap Storm | 49.2% | 48.4% (#36) | +0.8 |
| 39 | **B** | Mustafar Walls | 49.0% | 51.3% (#31) | -2.2 |
| 40 | **B** | Enchanted Court | 47.7% | 42.5% (#55) | +5.2 |
| 41 | **C** | Fire Nation | 44.9% | 46.6% (#42) | -1.8 |
| 42 | **C** | Workshop Smiths | 44.3% | 47.0% (#41) | -2.7 |
| 43 | **C** | Space Time Chests | 44.3% | 40.0% (#57) | +4.4 |
| 44 | **C** | Wild Stampede | 43.2% | 46.4% (#43) | -3.2 |
| 45 | **C** | Petra Hideout *(new)* | 43.0% | 44.5% (#49) | -1.5 |
| 46 | **C** | Astro Blasters | 42.7% | 44.4% (#50) | -1.7 |
| 47 | **C** | Lava Blasters | 42.4% | 39.1% (#60) | +3.4 |
| 48 | **C** | Hour Hideout *(new)* | 42.2% | 43.3% (#53) | -1.2 |
| 49 | **C** | Fallout Blaze *(new)* | 42.0% | 45.0% (#47) | -3.0 |
| 50 | **C** | Hideout Claws | 41.6% | 47.8% (#38) | -6.2 |
| 51 | **C** | Art School *(new)* | 40.9% | 44.3% (#51) | -3.4 |
| 52 | **C** | Final Form *(new)* | 40.7% | 45.1% (#45) | -4.4 |
| 53 | **C** | Workshop Thieves | 40.4% | 39.3% (#59) | +1.2 |
| 54 | **C** | Shadow Giants *(new)* | 40.4% | 47.1% (#40) | -6.6 |
| 55 | **C** | Art Garden *(new)* | 40.1% | 44.9% (#48) | -4.7 |
| 56 | **C** | Ulgrim's Forge | 39.9% | 43.6% (#52) | -3.7 |
| 57 | **C** | Hour Sweep *(new)* | 39.7% | 45.1% (#46) | -5.3 |
| 58 | **C** | Axe Fans *(new)* | 39.2% | 45.1% (#44) | -6.0 |
| 59 | **C** | Hideout Bombers | 38.6% | 35.1% (#64) | +3.5 |
| 60 | **C** | Blaster Barrage | 38.1% | 40.3% (#56) | -2.2 |
| 61 | **C** | Frozen Fortress | 37.2% | 28.1% (#71) | +9.1 |
| 62 | **C** | Fangwild Brawlers | 36.1% | 36.3% (#63) | -0.2 |
| 63 | **C** | Fangwild Witches | 35.9% | 30.7% (#69) | +5.3 |
| 64 | **C** | Mustafar Spears | 35.1% | 39.6% (#58) | -4.4 |
| 65 | **C** | Iron Wall | 34.5% | 35.0% (#65) | -0.6 |
| 66 | **C** | Adrenaline Rush | 34.1% | 38.1% (#61) | -4.0 |
| 67 | **C** | Comet Blades | 33.5% | 34.2% (#66) | -0.7 |
| 68 | **C** | Atlantis Freeze | 33.1% | 24.3% (#73) | +8.8 |
| 69 | **C** | Fangwild Hammers | 32.8% | 36.3% (#62) | -3.5 |
| 70 | **C** | Matrix Hammers | 32.4% | 33.8% (#67) | -1.3 |
| 71 | **C** | Hammer Time | 30.8% | 33.1% (#68) | -2.3 |
| 72 | **C** | Comet Blaze | 25.7% | 29.5% (#70) | -3.8 |
| 73 | **C** | Reef Orbs *(new)* | 22.4% | 27.4% (#72) | -5.0 |
| 74 | **C** | Reef Swarm *(new)* | 20.5% | 20.8% (#74) | -0.3 |
| 75 | **C** | Reef Freeze *(new)* | 10.9% | 11.1% (#75) | -0.1 |

## Suggested changes

- **Silent Galaxy:** it is still the best card in the game. Ideas from before still apply: let non-Cosmic legends deal reduced damage instead of none, raise its cost, or make it last a number of turns.
- **Superfan Brynn:** watch it together with Silent Galaxy. If Galaxy is fixed, Superfan Spears at 64% is fine.
- **Mako's Reef:** it is last with both bots. Test it by hand, then try cost 6 or a cheaper/stronger version (the one-stage-per-turn rule would then be needed to stop the "Reef, attack, Matrix" combo).
- **Hammer decks:** consider giving Hammer something back (a hammer legend with real power, or a cheaper Hammer Gimp).

## Tier list, Normal bots (all 75 decks)

"Before" is the win rate and rank in the 64-deck analysis (old rules).

| # | Tier | Deck | Element | Win rate | Before | Origin |
|---|---|---|---|---|---|---|
| 1 | **S** | Galaxy Fans | Cosmic | 79.2% | – | **new** |
| 2 | **S** | Cosmic Lockdown | Cosmic | 76.7% | 77.5% (#1) | shipped |
| 3 | **S** | Galaxy Fists | Cosmic | 71.0% | 73.0% (#3) | shipped |
| 4 | **S** | Matrix Masters | Shadow | 70.9% | 70.7% (#4) | shipped |
| 5 | **S** | Galaxy Lock | Cosmic | 70.9% | 73.2% (#2) | shipped |
| 6 | **A** | Hideout Ninjas | Shadow | 69.3% | 70.7% (#5) | shipped |
| 7 | **A** | Tombstone Control | Shadow | 68.5% | 69.1% (#10) | shipped |
| 8 | **A** | Hideout Rush | Shadow | 68.4% | 67.0% (#12) | shipped |
| 9 | **A** | Cosmic Lancers | Cosmic | 68.1% | 70.6% (#6) | shipped |
| 10 | **A** | Galaxy Lancers | Cosmic | 67.8% | 69.5% (#8) | shipped |
| 11 | **A** | Treasure Hunt | Magic | 67.0% | 66.2% (#14) | shipped |
| 12 | **A** | Scythe Executioners | Cosmic | 66.1% | 69.2% (#9) | shipped |
| 13 | **A** | Wild Axe | Wild | 65.8% | 67.4% (#11) | shipped |
| 14 | **A** | Cursed Kunai | Shadow | 65.3% | 63.9% (#16) | shipped |
| 15 | **A** | Hideout Brutes | Shadow | 65.1% | 69.8% (#7) | shipped |
| 16 | **A** | Fallout Control | Fire | 64.6% | – | **new** |
| 17 | **A** | Superfan Spears | Arctic | 64.3% | – | **new** |
| 18 | **A** | Kitsune Blades | Shadow | 63.6% | 62.4% (#19) | shipped |
| 19 | **A** | Galaxy Reapers | Cosmic | 63.1% | 66.9% (#13) | shipped |
| 20 | **A** | Fangwild Garden | Nature | 62.6% | 64.9% (#15) | shipped |
| 21 | **A** | Ninja Swarm | Shadow | 61.3% | 57.2% (#25) | shipped |
| 22 | **A** | Galaxy Commanders | Cosmic | 60.7% | 62.7% (#17) | shipped |
| 23 | **A** | Scorched Earth | Fire | 60.6% | 61.1% (#21) | shipped |
| 24 | **A** | Briar Garden | Nature | 60.0% | 62.4% (#18) | shipped |
| 25 | **B** | Sozin's Inferno | Fire | 58.2% | 59.1% (#22) | shipped |
| 26 | **B** | Space Time Giants | Shadow | 57.8% | 62.1% (#20) | shipped |
| 27 | **B** | Space Time Rush | Shadow | 55.4% | 51.3% (#31) | shipped |
| 28 | **B** | Last Hour | Shadow | 55.2% | 48.0% (#36) | shipped |
| 29 | **B** | Frozen Tide | Arctic | 54.5% | 58.0% (#24) | shipped |
| 30 | **B** | Reef Ninjas | Shadow | 53.9% | – | **new** |
| 31 | **B** | Mustafar Walls | Fire | 51.3% | 55.4% (#27) | shipped |
| 32 | **B** | Matrix Witches | Cosmic | 51.2% | 53.9% (#28) | shipped |
| 33 | **B** | Rock Wall | Wild | 50.0% | 58.2% (#23) | shipped |
| 34 | **B** | Mustafar Burn | Fire | 49.9% | 49.0% (#33) | shipped |
| 35 | **B** | Workshop Forge | Fire | 49.8% | 56.1% (#26) | shipped |
| 36 | **B** | Deathcap Storm | Nature | 48.4% | 48.5% (#34) | shipped |
| 37 | **B** | Matrix Reapers | Shadow | 48.0% | 51.1% (#32) | shipped |
| 38 | **B** | Hideout Claws | Wild | 47.8% | 51.7% (#29) | shipped |
| 39 | **B** | Treasure Engine | Magic | 47.5% | 44.8% (#40) | shipped |
| 40 | **B** | Shadow Giants | Shadow | 47.1% | – | **new** |
| 41 | **B** | Workshop Smiths | Cosmic | 47.0% | 48.0% (#35) | shipped |
| 42 | **B** | Fire Nation | Fire | 46.6% | 46.1% (#38) | shipped |
| 43 | **B** | Wild Stampede | Wild | 46.4% | 51.5% (#30) | shipped |
| 44 | **B** | Axe Fans | Wild | 45.1% | – | **new** |
| 45 | **B** | Final Form | Shadow | 45.1% | – | **new** |
| 46 | **B** | Hour Sweep | Shadow | 45.1% | – | **new** |
| 47 | **B** | Fallout Blaze | Fire | 45.0% | – | **new** |
| 48 | **C** | Art Garden | Nature | 44.9% | – | **new** |
| 49 | **C** | Petra Hideout | Shadow | 44.5% | – | **new** |
| 50 | **C** | Astro Blasters | Cosmic | 44.4% | 44.6% (#41) | shipped |
| 51 | **C** | Art School | Magic | 44.3% | – | **new** |
| 52 | **C** | Ulgrim's Forge | Fire | 43.6% | 46.4% (#37) | shipped |
| 53 | **C** | Hour Hideout | Shadow | 43.3% | – | **new** |
| 54 | **C** | Atlantis Snowfall | Arctic | 43.2% | 45.4% (#39) | shipped |
| 55 | **C** | Enchanted Court | Magic | 42.5% | 43.5% (#42) | shipped |
| 56 | **C** | Blaster Barrage | Shadow | 40.3% | 41.3% (#47) | shipped |
| 57 | **C** | Space Time Chests | Cosmic | 40.0% | 43.5% (#43) | shipped |
| 58 | **C** | Mustafar Spears | Fire | 39.6% | 40.3% (#50) | shipped |
| 59 | **C** | Workshop Thieves | Magic | 39.3% | 41.3% (#46) | shipped |
| 60 | **C** | Lava Blasters | Fire | 39.1% | 38.2% (#52) | shipped |
| 61 | **C** | Adrenaline Rush | Nature | 38.1% | 36.8% (#53) | shipped |
| 62 | **C** | Fangwild Hammers | Nature | 36.3% | 43.1% (#44) | shipped |
| 63 | **C** | Fangwild Brawlers | Nature | 36.3% | 40.2% (#51) | shipped |
| 64 | **C** | Hideout Bombers | Shadow | 35.1% | 36.3% (#54) | shipped |
| 65 | **C** | Iron Wall | Nature | 35.0% | 41.1% (#48) | shipped |
| 66 | **C** | Comet Blades | Fire | 34.2% | 35.3% (#55) | shipped |
| 67 | **C** | Matrix Hammers | Wild | 33.8% | 42.4% (#45) | shipped |
| 68 | **C** | Hammer Time | Nature | 33.1% | 41.0% (#49) | shipped |
| 69 | **C** | Fangwild Witches | Magic | 30.7% | 33.0% (#56) | shipped |
| 70 | **C** | Comet Blaze | Fire | 29.5% | 31.6% (#57) | shipped |
| 71 | **C** | Frozen Fortress | Arctic | 28.1% | 31.1% (#58) | shipped |
| 72 | **C** | Reef Orbs | Wild | 27.4% | – | **new** |
| 73 | **C** | Atlantis Freeze | Arctic | 24.3% | 29.4% (#59) | shipped |
| 74 | **C** | Reef Swarm | Arctic | 20.8% | – | **new** |
| 75 | **C** | Reef Freeze | Arctic | 11.1% | – | **new** |

## The 16 new decks (after tuning)

- **Reef Swarm:** 12× Essence, 3× Mako's Reef, 1× Bubble, 4× Art Class, 1× Fox Spirit, 4× Dander, 3× Atlantean Ada, 3× Aqua Blasters, 1× Chikara, 4× Nightmare Claws, 3× Winter Daggers, 1× Holy Covenant
- **Reef Ninjas:** 12× Essence, 4× Mako's Reef, 3× Forgeborne Chest, 2× Art Class, 3× Caspian, 4× Ninja Spirit, 4× Dullahan Jiro, 2× Necro Aura, 4× Shadow Edge, 2× Mahou Shoujo
- **Final Form:** 15× Essence, 2× Vial of Crows, 2× Dark Duo, 1× Promotion II, 2× Tombstone, 3× Caspian, 3× Raymesis, 4× Final Form Petra, 1× The Rock, 3× Sleight of Hand, 1× Rippers, 3× Remnant of Fate
- **Petra Hideout:** 13× Essence, 3× Evil Hideout, 1× Bouncy Bomb, 2× Tombstone, 3× Caspian, 3× Raymesis, 2× Final Form Petra, 4× Dander, 1× Buzz Axe, 2× Sleight of Hand, 3× Rippers, 3× Nightmare Claws
- **Fallout Control:** 14× Essence, 2× Dragon's Chest, 3× Fallout, 3× Iron Lady, 3× Heatblast, 1× Ulgrim, 3× Molten Kor, 2× Fury Shang, 3× Searing Blade, 2× Candlelit Scythe, 4× Magma Spear
- **Fallout Blaze:** 12× Essence, 2× Mustafar, 2× Fire Nation, 1× Dragon's Chest, 3× Fallout, 2× Heatblast, 1× Ulgrim, 1× Molten Kor, 1× Jhala, 2× Hothead Jiro, 3× Fury Shang, 2× Seven, 4× Magma Spear, 4× Ancestor's Flame
- **Superfan Spears:** 13× Essence, 2× Water Tribe, 1× Cosmic Chest, 4× Superfan Brynn, 3× Orion, 3× Wu Shang, 3× Aurora Brynn, 2× Atlantean Ada, 3× Alpine Kaya, 3× Shattering Star, 3× Glacier's Grace
- **Axe Fans:** 13× Essence, 2× Evil Hideout, 2× Adrenaline, 1× A-Oooooo, 4× Superfan Brynn, 3× Rayman, 2× Jhala, 4× The Minotaur, 3× Axe of Regrowth, 4× Buzz Axe, 2× Boiling Point
- **Galaxy Fans:** 13× Essence, 3× Silent Galaxy, 2× Cosmic Chest, 3× Superfan Brynn, 3× Orion, 3× Wu Shang, 4× Aurora Brynn, 3× Breaker Shang, 2× Sweet Magi, 4× Shattering Star
- **Art School:** 13× Essence, 2× Fangwild, 4× Chest, 1× Witch Party, 4× Art Class, 2× Witch Scarlet, 3× Arcadia, 3× Fait, 1× Law of the Land, 2× Remnant of Fate, 2× Amethyst Scythe, 3× Holy Covenant
- **Art Garden:** 12× Essence, 2× Fangwild, 3× Adrenaline, 3× Art Class, 3× Briar Rose, 3× Forest Guardian, 3× Forest Spirit, 3× Faerie Queen, 4× Law of the Land, 4× Piercing Regret
- **Hour Sweep:** 13× Essence, 3× Orb, 4× Death's Hour, 3× Caspian, 4× Ninja Spirit, 3× Hellshot Hattori, 3× Dullahan Jiro, 1× Final Form Petra, 2× Revolvers, 4× Shadow Edge
- **Hour Hideout:** 14× Essence, 3× Evil Hideout, 1× Orb, 2× Wild Chest, 2× Death's Hour, 3× Caspian, 4× Ninja Spirit, 1× Dullahan Jiro, 4× Dander, 4× Nightmare Claws, 2× Shadow Edge
- **Reef Freeze:** 12× Essence, 3× Mako's Reef, 4× Snowball, 2× Bubble, 3× Sunken Chest, 2× Snowman Kor, 4× Atlantean Ada, 4× Alpine Kaya, 3× Aqua Blasters, 3× Glacier's Grace
- **Reef Orbs:** 13× Essence, 4× Mako's Reef, 4× Orb, 2× Bubble, 3× Art Class, 2× Forest Guardian, 3× Yumiko, 3× Fox Spirit, 3× Primrose Mallet, 3× Chikara
- **Shadow Giants:** 15× Essence, 3× Vial of Crows, 1× Chest, 1× Forgeborne Chest, 2× Promotion I, 1× Death's Hour, 2× Tombstone, 3× Dark Mage, 1× Plague Knight, 2× Kitsune Hattori, 2× Final Form Petra, 2× Sleight of Hand, 3× Necro Aura, 2× Shadow Edge

### Tuning changes

- Reef Swarm: 16.8% -> 19.9% vs gauntlet; kept 2: -Mako's Reef +Holy Covenant, -Fox Spirit +Chikara
- Reef Ninjas: 40.2% -> 48.2% vs gauntlet; kept 1: -Essence +Forgeborne Chest
- Final Form: 34.4% -> 47.1% vs gauntlet; kept 3: -Rippers +Final Form Petra, -Sleight of Hand +The Rock, -Essence +Promotion II
- Petra Hideout: 31.6% -> 45.3% vs gauntlet; kept 3: -Sleight of Hand +Buzz Axe, -Essence +Tombstone, -Sleight of Hand +Bouncy Bomb
- Fallout Control: 58.4% -> 61.7% vs gauntlet; kept 1: -Essence +Ulgrim
- Fallout Blaze: 34.4% -> 44.9% vs gauntlet; kept 3: -Hothead Jiro +Molten Kor, -Heatblast +Dragon's Chest, -Mustafar +Ulgrim
- Superfan Spears: 62.1% -> 67.2% vs gauntlet; kept 2: -Atlantean Ada +Wu Shang, -Glacier's Grace +Cosmic Chest
- Axe Fans: 44.7% -> 44.7% vs gauntlet; kept 0: 
- Galaxy Fans: 81.2% -> 81.2% vs gauntlet; kept 0: 
- Art School: 45.9% -> 48.2% vs gauntlet; kept 1: -Amethyst Scythe +Law of the Land
- Art Garden: 43.9% -> 47.7% vs gauntlet; kept 1: -Essence +Law of the Land
- Hour Sweep: 33.2% -> 40.0% vs gauntlet; kept 1: -Orb +Final Form Petra
- Hour Hideout: 24.6% -> 37.5% vs gauntlet; kept 3: -Shadow Edge +Wild Chest, -Orb +Wild Chest, -Death's Hour +Dullahan Jiro
- Reef Freeze: 7.2% -> 9.0% vs gauntlet; kept 1: -Snowman Kor +Atlantean Ada
- Reef Orbs: 24.0% -> 24.0% vs gauntlet; kept 0: 
- Shadow Giants: 33.2% -> 49.0% vs gauntlet; kept 3: -Kitsune Hattori +Forgeborne Chest, -Shadow Edge +Plague Knight, -Sleight of Hand +Chest
