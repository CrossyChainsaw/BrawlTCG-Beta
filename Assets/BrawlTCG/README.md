# BrawlTCG – Unity port

Port of the WinForms game in `BrawlTCG-alpha/`. Press Play in any scene: `App` bootstraps itself
(`RuntimeInitializeOnLoadMethod`) and builds the whole UI from code, so no scene setup is needed.

## Layout
- `Core/` – pure C# rules engine (own assembly, `noEngineReferences`). Cards, effects, attacks,
  catalogues, `Game` (rules), `GameCommand` (every player action), deck rules, network protocol.
- `Unity/` – presentation: `App` (canvas, screens, toasts), `MenuScreen` (P2P host/join, relay server,
  hot-seat), `DeckBuilderScreen`, `MatchScreen` (board, drag & drop, detail view, targeting, overlays),
  `CardView`, `NetSession` (TCP), `DeckStore`.
- `../Resources/BrawlTCG/Art/` – card art, named after the WinForms resource keys.

## Networking
Lockstep: both clients run the same `Game` with the same decks and a shared seed (`seedA ^ seedB`),
and exchange only `CMD:` lines (serialized `GameCommand`s). Random effects need no extra messages.
Port 5000 by default. The existing `BrawlTCG-Server` relay works unchanged for Client-Server mode.

## Decks
Stored in `Application.persistentDataPath/Decks/<name>.txt`, one card id per line (same format as
the WinForms deck files, so old `deckPlayer1.txt` files can be dropped into that folder).
Two starter decks are created on first launch.

## Controls
Drag cards from your hand onto a zone / legend. Click a card to zoom; click an attack, then click a
target if one is needed. Space or the END TURN button ends the turn. Right-click / Esc cancels.
