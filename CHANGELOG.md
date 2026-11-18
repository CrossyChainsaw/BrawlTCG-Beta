# Changelog

## v1.2.0 Beta

Four new cards, three reworks, avatars and a batch of quality-of-life fixes. **v1.2.0 can't play online with v1.1.0 or older.** From now on, versions with the same major.minor number (1.2.x) can always play each other, and a mismatch tells you who needs to update.

Closes issues **#34**, **#36**, **#38**, **#39**, **#40**, **#41**, **#43**, **#44**, **#45**, **#46**, **#49**, **#50** and **#51**.

### New cards

- **Sozin's Comet** (Stage, Fire, 7) (#50). While in play, Fire legends get +5/+5.
- **Full Eclipse** (Stage, Arctic, 7) (#51). While in play, Arctic legends get +5/+5.
- **Tombstone** (Battle, Shadow, 6) (#46). Eliminates an opposing legend.
- **A-Oooooo** (Battle, Wild, 6) (#45). All your Wild legends get +4 Attack.

When Sozin's Comet or Full Eclipse leaves play, the +5/+5 goes away again, but a legend never dies from it: it keeps at least 1 HP.

### Card reworks

- **Fangwild** (#43). At the start of every turn, all Magic and Nature legends on the board gain +2 max health (and +2 HP). It used to heal them by 2. It now costs 4 (was 3).
- **Start-turn stages hit the whole board.** Mustafar, Atlantis and Fangwild now affect every legend on the board (both players) at the start of every turn, as their text says, instead of only the side whose turn starts. To make up for it, Mustafar deals 1 damage (was 2). Atlantis stays at 1.
- **Evil Hideout** (#49). Now a Wild card, and only Shadow and Wild legends get the +3 Power. Fire legends no longer benefit.

### Tapped is now called Stunned

- **"Tapped" is renamed to "Stunned"** everywhere: the ribbon on the card, the effects list, Bubble ("Stun a legend") and Ymir's Breath ("Stun all opposing legends"). A stunned legend can't attack and its abilities are off.
- **Stunned legends can be attacked now.** Before, a tapped legend couldn't be targeted by attacks, so stunning an enemy also protected it. Battle cards and weapons could already be played on them, Tombstone included.

### Online

- **Version check** (#34). When you connect, both games compare versions. If the major or minor number differs, you see who is outdated, for example "Version mismatch: you are on v1.2.0, Bob is on v1.1.0 (outdated). Bob needs to update."
- **Press Enter to join** (#38) after typing a room code or LAN address.

### Profile

- **Avatars** (#44). Click the circle next to your name on the main menu to pick a legend portrait instead of your initial. Your opponent sees it on the match screen and in the versus intro.

### Match screen

- **No accidental end turn** (#39). END TURN and Space don't work while the YOUR TURN banner is still showing.
- **Look through a discard pile.** Click either player's discard pile to see every card in it, newest first. Hover a card to enlarge it, click it for its details.

### Deck builder

- **Reopens on the deck you last had open** (#40).
- **Mentioned cards** (#36). A card that names other cards (Witch Party, Did Shaw Win Yet?, Ulgrim...) shows them under MENTIONED CARDS, and attacks that craft a card show a small thumbnail. Hover to see the full card, click to select it.
- **Equal buttons** (#41). NEW DECK, RENAME, SAVE and DELETE are now the same size in a 2×2 grid. The stray rectangles on top of gold buttons are gone.

---

## v1.1.0 Beta

Timers, a versus intro, two rule fixes and deck renaming. **Two rules changed, so v1.1.0 can't play online with v1.0.x.** An older version is refused when it connects ("incompatible version"), instead of going out of sync mid-match.

Closes issues **#17**, **#18**, **#19**, **#20**, **#21**, **#25**, **#26**, **#27**, **#28** and **#29**.

### Rule changes

- **Tapped legends' abilities are turned off** (#29). A tapped legend's "while in play" ability doesn't trigger until it is untapped. Its card detail shows ABILITIES · OFF WHILE TAPPED.
- **No elemental bonus without damage** (#21). An attack that deals 0 damage of its own (for example after The Finger) no longer gets the +1 per matching-element weapon.

### Timers

- **60 seconds per turn** (#17). A countdown next to END TURN and a bar along the bottom of the centre strip. It turns red at 10 seconds, ticks for the last 5, and ends your turn at 0. Online you also see roughly how long your opponent has left. In local matches the clock pauses while the options menu is open.
- **30 seconds to choose your starting hand** (#28). When time runs out, the cards you've marked so far go back.

### Match screen

- **Versus intro** (#26). Both players slide in, VS slams down, and the screen says who goes first. Click to skip.
- **See whether your opponent is still choosing their starting hand** (#27). After you choose, you wait on the hand screen until they're done, and the first turn starts right after.
- **Your cards are darkened during your opponent's turn** (#19), online.
- **Weapon burns** on attacks are now a flame with the number in it, instead of an orange oval (#20). The deck builder shows it too.
- **Volume slider** in the options menu (Esc), under the sound on/off switch (#18).

### Deck builder

- **Rename decks** (#25). NEW DECK and RENAME ask for the name in a pop-up. The name text box is gone. Renaming keeps unsaved changes, and the main menu remembers the deck under its new name.

---

## v1.0.1 Beta

A small update to the match screen. No rule changes: v1.0.1 and v1.0 can play each other online.

Closes issue **#14** (show the Silent Galaxy icon when inspecting a legend).

### Match screen

- **EFFECTS section.** Clicking a legend shows everything affecting it below its abilities, one row each with an icon and its source:
  - **Can't deal damage** (Silent Galaxy): red sword with a slash.
  - **Tapped**: a gold card turned on its side. The diagonal TAPPED text on the board stays.
  - **Burned**: an orange flame.
  - **ATK changes**: a green sword for a buff, a red one for a debuff.
  - **Max HP changes**: a green heart for a buff, a red one for a debuff. Healing is not listed.
  - **Already attacked** this turn.
- When there is no room for all of them, rows go two to a line, and **"+N more"** opens the full list. The panel grows taller when it needs to.
- This replaces the ON THIS LEGEND list.
- **Burned** legends show a flame badge on the board instead of the BURNED tag.
- **Essence holds 44 crystals** (was 12), in rows of 15, 14 and 15. Past 44, crystals turn **gold** from the first one on (54 Essence shows 10 gold). Spending uses up gold first: a gold crystal turns back to purple, then purple crystals go dim.

### Fixes

- After **Enchantress** used an Enchant attack, the weapons under her could be drawn on top of her. Weapons that slide along a stack now stay under their legend.
- **Faerie Queen** now reads "all **your** Nature legends get +1/+1". The effect always worked that way; only the text was wrong.

---

## v1.0 Beta

The first version numbered as a release. It adds three new rules (a bigger health pool, a starting-hand redraw and paid discards). It rebalances Ninja Spirit and the Promotion cards, and replaces every bundled deck with six decks picked by a 105,600-game bot tournament. It also adds a lot of clarity to the match screen: what affects a legend and why, where a card came from, which cards you can't play right now, and a consistent ABILITIES layout on every card.

Closes issues **#10** (Silent Galaxy marker), **#11** (decks from the alpha) and **#12** (easy online play).

### Contents

1. [Rule changes](#1-rule-changes)
2. [Buffs and nerfs](#2-buffs-and-nerfs)
3. [Decks](#3-decks)
4. [Match screen](#4-match-screen)
5. [Card detail and abilities](#5-card-detail-and-abilities)
6. [Local (hot-seat) mode](#6-local-hot-seat-mode)
7. [Deck builder](#7-deck-builder)
8. [Home screen](#8-home-screen)
9. [Fixes](#9-fixes)
10. [Online play and compatibility](#10-online-play-and-compatibility)
11. [Under the hood](#11-under-the-hood)

---

### 1. Rule changes

- **Starting health is 50** (was 30). Games last longer and comebacks are more likely.
- **Starting hand redraw.** After both players draw their 7 cards, each one picks any of them to shuffle back into their deck and draws as many new ones. This happens once per match, before the first turn.
  - The new cards take the places of the cards they replace, so the rest of the hand doesn't move.
  - Online, both players choose at the same time.
- **Discard from your hand.** Drop a card from your hand on **your discard pile** during your turn to discard it.
  - It costs the card's Essence; 0-cost cards are free.
  - None of its effects trigger.
- **Decks need 40 cards to be saved** (was 20). This matches the 40 cards needed to play.

### 2. Buffs and nerfs

| Card | Change | Before | After | Why |
|---|---|---|---|---|
| **Promotion I** | Buff | 5 Essence, +5 max HP | 5 Essence, **+15 max HP** | At 1 HP per Essence it was never worth playing. Now about 3 HP per Essence. |
| **Promotion II** | Buff | 10 Essence, +10 max HP | **8 Essence**, **+25 max HP** | Same reason. It's now a strong late-game swing you can actually reach. |
| **Ninja Spirit** | Nerf | 1 Essence | **2 Essence** | The only legend that attacks without a weapon. Every other legend needs a legend plus a weapon (about 2 Essence and 2 cards) to attack, so it was underpriced. |
| **All players** | Global | 30 starting health | **50 starting health** | See [Rule changes](#1-rule-changes). |

Promotions also heal you for the amount they add.

Text-only change:
- **Fangwild** now reads "All Magic and Nature Legends heal 2" (was "will gain 2 health"). What it does hasn't changed.

**How the Ninja Spirit change was checked:** a bot tournament with each version. At cost 2, the decks built around it lose about 3–4 points of win rate (Hideout Rush 80.6% → 77.9%) and every other deck is unaffected. It stays strong, but no longer free.

### 3. Decks

The game now comes with **exactly six decks**. Each one is handed out once: a deck you delete stays deleted, and a deck you already have with the same name is never overwritten.

| Deck | Tactic | Main type | Tournament |
|---|---|---|---|
| **Cosmic Lockdown** | All-Cosmic, so Silent Galaxy stops only the opponent's damage. Aurora Brynn and Artemis hit hard. | Cosmic legends, spears | #1, 80.1% |
| **Treasure Hunt** | Card advantage: chests and Witch Party, plus crafting legends (Breaker Shang, Fait, Ulgrim). | Battle cards | #2, 74.5% |
| **Hideout Rush** | Evil Hideout gives +3 ATK to cheap Shadow and Wild legends that rush in early. The fastest deck. | Shadow / Wild, katars | #3, 74.0% |
| **Wild Axe** | Dander early, The Minotaur and Raymesis late, plus chests for reach. | Wild, katars and axes | #4, 71.8% |
| **Scorched Earth** | All Fire under Mustafar: every non-Fire legend loses 2 HP each turn, while Fire legends are immune. | Fire, scythes | #5, 69.2% |
| **Frozen Tide** | Arctic control under Atlantis: The Rock and Snowman Kor hold the line while the stage drains non-Arctic legends. | Arctic, hammers | #10, 62.0%, kept because it plays so differently |

**How they were picked:**
- 16 tactic decks were built (rush, lockdown, card advantage, Mustafar burn, Atlantis control, scythe executions, tap control, ramp, swarm, board wipes, blasters and more).
- Each was tuned by simulation within its own theme.
- They then played a full round robin against each other, the 7 old starter decks and the 10 decks from the alpha: 33 decks, 200 games per matchup, 105,600 games.
- The tournament ran with 50 health and Ninja Spirit at 2, before the Promotion buff; none of these decks run Promotions.

**Removed:**
- the seven per-element starter decks (*Starter - Fire* … *Starter - Shadow*);
- the alpha decks and the earlier Claude I–III decks, which only existed during development of this version.

On launch, old copies of removed or re-tuned decks are deleted, but only while they are still exactly as the game created them. **A deck you edited is yours and is never touched.**

### 4. Match screen

**Unplayable cards are darkened.** Any card in your hand that you can afford but can't play right now is darkened, and lights up again once it becomes playable:
- legends while your field already has 5 legends;
- weapons while none of your legends can carry them;
- battle cards with no legend to target on their side: yours for heals and boosts, the opponent's for damage and taps.

**Silent Galaxy marker:** the "NO STRIKES" label is replaced by an icon, a white sword crossed by a red bar inside a red ring. It sits in the ability-badge row on legends that can't deal damage. (#10)

**Crafted cards:** cards made during the match get a small **hammer in their type pill**, in the card's own colours. This includes Yumiko's orbs, Dark Duo legends, chest cards, Seven's weapons, Workshop cards, summoned Ninja Spirits, and stages set by Ulgrim, Molten Kor, Mastermind or Atlantean Ada.

**Discard pile:** your discard pile is now a drop zone. It lights up while you drag a card you can afford to discard.

**Starting hand screens:**
- **KEEP OR REDRAW:** your 7 cards are shown large. Click a card to mark it "BACK TO DECK"; the button reads **KEEP HAND** or **REDRAW N CARDS**.
- **YOUR NEW HAND:** each replaced card sinks and fades out, then its replacement rises into the same slot while turning over, marked **NEW**. A line lists the cards that went back.
- While you wait for the other player, the centre strip says so.

**Other:**
- The **HOW TO PLAY** box explains discarding.
- The **stage panel** shows its ability lines with their trigger icons.

### 5. Card detail and abilities

**ON THIS LEGEND:** clicking a legend lists everything currently affecting it, each with its source:
- stat changes, green for buffs and red for debuffs. For example "+5 ATK +5 HP Super Saiyan", "-16 ATK Cody Travis", or an aura from Fury Shang or a stage;
- statuses: tapped by Bubble, burned by Heatblast, can't deal damage (Silent Galaxy), already attacked.

**Every card has an ABILITIES section:**
- Each "Start Turn / When Played / While In Play" line starts with its trigger icon: green arrow, gold bolt or purple ∞.
- This applies to stages, battle cards and Essence too, laid out like the legends'.
- The rest of the card's text (targets, how weapons and stages work) sits next to the card.

**CRAFTED BY:** crafted cards show a copper chip naming what made them, e.g. "Dark Duo" or "Yumiko · Craft 3 Orbs".

### 6. Local (hot-seat) mode

- Your hand is **no longer re-dealt from the deck every turn**. After **START MY TURN**, the cards you already held turn over in place one by one, and only new cards (your draw, crafted cards) fly in from the deck.
- Cards dealt from the deck now leave it face down and turn over mid-flight, so they're never shown face up on the deck pile. This also applies to the opening deal online.
- Starting hands: Alice chooses, the device is passed to Bob (**SEE MY HAND**), Bob chooses, then Alice's first turn starts.

### 7. Deck builder

- **Unsaved changes warning:** leaving, switching decks or creating a new deck with unsaved changes now asks first. You can **save and continue** (when the deck can be saved), **continue without saving**, or **keep editing**. If the deck can't be saved yet, the dialog says why.
- Decks need **40 cards** to be saved.
- The card preview uses the same **ABILITIES** layout with trigger icons as the match.

### 8. Home screen

- The footer shows the build version: **v1.0 BETA · UNITY EDITION** (was a hard-coded "v0.1"). It now follows the version set in Project Settings.

### 9. Fixes

- **Broken visuals from the second Play in the Unity editor.** Rounded panels, glows, icons and effect frames broke on every run after the first, and sounds could stay silent. The project skips the script reload between Play runs, so the game kept references to drawings Unity had already thrown away. These are now rebuilt at the start of every run. Builds were never affected.

### 10. Online play and compatibility

- **Online rooms.** Click **Create Room** to get a 4-digit code, and your friend enters it under **Join Room**. Matches go through Unity Relay, so nobody opens a port on their router or shares their IP address.
  - The room disappears as soon as your friend joins, or within 30 seconds if the host closes the game.
  - **LAN Host** and **LAN Join** keep the direct connection for playing without internet. The old **Server** option is gone, since rooms replace it.
- The network version is now **BTCG-UNITY-2**. The new rules (50 health, discards, starting-hand redraw) make older builds incompatible, so **both players need this version**. A mismatched build is refused with a clear "incompatible version" message instead of falling out of sync.
- Both players choose their starting hands at the same time. Each player's reshuffle uses its own random generator, so the result is identical whichever choice arrives first. This was tested in 1,000 random games that applied the choices in opposite orders on the two clients, with 0 desyncs.

### 11. Under the hood

- **Core (rules):**
  - new commands `DiscardCard` and `RedrawStartingHand`, added at the end so existing command numbers are unchanged;
  - `Game.StartingHandRedraw` and `ChoosingStartingHands`;
  - every stat change, tap and burn records its source (`LegendCard.Modifiers`, `TappedBy`, `BurnedBy`);
  - `Card.CraftedBy`.

  The source tracking only feeds the display and changes no rules: 2,000 random games ran with identical results before and after it.
- **Decks:**
  - `StarterDecks.Premade` holds the six bundled decks;
  - `StarterDecks.Retired` lists earlier decks by their exact cards, so untouched copies can be cleaned up;
  - `StarterDecks.All` (the old starters) is kept as a benchmark for the tests and the simulator.
- **UI:**
  - `CardFlight.ShowBackFirst` gives a real card turn-over, showing the back first;
  - `TriggerIcons.PaintAbilities`, `SplitCardText`, `PaintCraftedChip` and the hammer glyph;
  - every static cache of runtime-made sprites resets on Play.
- **Tests:** scenario checks for every new rule, plus 2,000 random lockstep games per run, half of them with starting-hand redraws applied in opposite orders. All pass with 0 crashes and 0 desyncs.
