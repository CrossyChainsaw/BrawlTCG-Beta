# BrawlTCG – Beta (Unity)

A two-player trading card game featuring Brawlhalla legends, weapons and stages.

This is the **Unity port of the original WinForms game, BrawlTCG-alpha**
([play the alpha on itch.io](https://crossychainsaw.itch.io/brawltcg-alpha)).
The rules, cards, attacks and effects were carried over 1:1, with the known bugs of the WinForms version
fixed and a few missing features added (see [Changes from the WinForms version](#changes-from-the-winforms-version)).

> Fan-made project, not affiliated with Brawlhalla or Blue Mammoth Games. Card art is fan art; see [CREDITS.txt](CREDITS.txt).

---

## Running the game

1. Open this folder in **Unity 6 (6000.6.9f1)**.
2. Open `Assets/Scenes/SampleScene` and press **Play**. The game builds its own UI, so no scene setup is needed.

To make a standalone build: **File → Build Profiles → Windows → Build** (pick a folder outside `Assets`, e.g. `Builds/`).

Six ready-made decks are added on launch, so you can play straight away: *Cosmic Lockdown*, *Treasure Hunt*, *Hideout Rush*, *Wild Axe*, *Scorched Earth* and *Frozen Tide*. Each one is handed out once: a deleted deck stays deleted, and a deck you already have with the same name is never overwritten. Decks that older versions handed out (the per-element starters and earlier premade decks) are removed on launch, unless you edited them.

## Game modes

| Mode | How |
|---|---|
| **Hot-Seat** | Two players on one PC. The screen hides between turns so you can pass the device. |
| **P2P / LAN** | One player clicks **Host Game**, the other enters the host's IP and clicks **Join Game** (port 5000). |
| **Client-Server** | Both players connect to the `BrawlTCG-Server` relay from the original project. |

To test networking on one PC: run a build and press Play in the editor at the same time, then host in one and join `127.0.0.1` in the other.

## How to play

- Each player starts with **50 health** (30 in the WinForms version), **7 cards** and **1 Essence** on the field. The host goes first.
- At the start of your turn you draw a card, and your Essence is refilled to the number of Essence cards you have in play.
- **Drag cards from your hand** onto the board:
  - **Essence** → your Essence zone (one per turn).
  - **Legend** → your field (max 5).
  - **Weapon** → one of your legends that uses that weapon type.
  - **Stage** → the Stage slot (one stage for both players).
  - **Battle card** → a legend (or anywhere on a field if it needs no target).
- **Click a legend** to zoom in and use an attack. Attacks need weapons stacked on the legend; some burn (discard) weapons when used.
- Weapons that match the legend's element deal **elemental bonus damage**.
- **Discard a card** by dropping it from your hand on your discard pile. It costs the card's Essence, and none of its effects trigger.
- You can only hit the enemy player directly when they have no legends on the field.
- **End your turn** with the END TURN button or **Space**. Right-click or Esc cancels.
- Bring the other player to 0 health to win.

## Deck building

Click **EDIT DECK** in the menu. A deck needs at least **10 Essence** and **20 cards** to save, and **40 cards** to play.
Max 4 copies of each card (Essence is unlimited).

Decks are saved as `.txt` files (one card id per line, the same format as the WinForms version) in the game's data folder.
Use **Open Decks Folder** in the menu to find it. Old `deckPlayer1.txt` / `deckPlayer2.txt` files can be copied in.

---

## Changes from the WinForms version

### Bugs fixed

| # | WinForms bug | Fix in this version |
|---|---|---|
| 1 | The elemental bonus only worked for attacks needing 1 weapon (a counter reset every loop). | Holding the required amount of matching-element weapons now adds that many damage, for any attack. |
| 2 | Attacks with a second weapon (Pink Roses, Meltdown) crashed when a battle card was stacked on the legend. | Battle cards are ignored when counting weapons. No crash. |
| 3 | Gimp attacks (Scythe / Hammer Gimp) relied on integer overflow to avoid hitting the player. | They are now a proper knock-out attack that can never target the player. |
| 4 | The zoomed card created new attack buttons every time it was repainted. | Buttons are created once. |
| 5 | Cards from chests, crafting and spawning had no owner, so Fury Shang and Faerie Queen never triggered for them. | Every generated card gets its owner. |
| 6 | A legend whose "when played" effect changes the stage got the new stage's effect applied twice. | Applied once. |
| 7 | Multi-hit attacks (Storm, Gauntlet Dsig) burned weapons on every hit, so later targets got less damage. | Weapons burn once per attack; every target takes full damage. |
| 9 | Attacks were looked up by name over the network ("Slash" exists twice). | Attacks are sent by position. |
| 10 | Unused/dead code (end-turn effects, abilities, unused stats and events). | Not ported. |
| 11 | A `:` in a player name broke the network handshake. | Names are encoded safely. |
| – | Hitting a burned legend with a normal attack removed the burn. | Burn stays until the legend dies. |
| – | Spawn Ninja Spirit could put a 6th legend on the field. | Blocked at 5 legends. |
| – | Space Time's extra draw used a different hand limit than the normal draw. | Both use the 15-card limit. |
| – | The first turn after The Workshop was played gave both a normal draw and the random card. | Only the random card, as the card text says. |
| – | The opponent busy-waited (froze) while waiting for randomly generated cards over the network. | Both players share a random seed, so random cards are identical without waiting. |

Stage start-of-turn effects still only affect the active player's legends. That is intended: each player's legends are hit once per round.

### Rule changes

- **Silent Galaxy** now reads "Only Cosmic legends can deal damage". Attacks have a category (Strike, Craft, Draw, Set, Summon, Boost, Hex), and the stage only blocks Strikes, so other legends can still craft, set stages, summon, buff and curse. In the original it blocked every attack.
- Card text uses three keywords consistently: **Craft** (a card goes into your hand), **Set** (a stage is put into play) and **Summon** (a legend goes onto your field).

### New features

- **END TURN button** (Space still works).
- **Game over screen** with the winner, plus handling for an opponent leaving or disconnecting.
- **Hot-seat mode** for two players on one PC.
- **Ready-made decks**, and named decks with New / Save / Delete in the deck builder.

### Other differences

- Pop-up message boxes are now short on-screen messages.
- Decks are picked from a list instead of a file dialog.
- Attacks that need a target show a "choose a target" banner and highlight valid targets.
- Discard piles show their top card and a card count.
- The Unity version cannot play against the WinForms version (the network messages changed with the fixes).

---

## Project structure

```
Assets/BrawlTCG/Core/    Rules engine (pure C#, no Unity code): cards, effects, attacks, turns, decks, network protocol
Assets/BrawlTCG/Unity/   Unity side: menu, deck builder, match board, card visuals, networking
Assets/Resources/BrawlTCG/Art/   Card art
```

Every player action is a command applied to the rules engine. In multiplayer both players run the same engine with the
same decks and random seed and only send each other their commands (lockstep).

## Credits

Card art is fan art by many artists; sources are listed in [CREDITS.txt](CREDITS.txt).
