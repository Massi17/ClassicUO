# Custom Changes Log

Tracks modifications to **pre-existing** ClassicUO files (anything with commits before ours — check with `git log --follow -- <path>` if unsure). Brand-new files need no entry here: they can't collide with an upstream update.

Each row below is what, at the next `upstream` sync, tells us which incoming changes are likely to conflict with something we did on purpose.

| File | Reason | Date |
|---|---|---|
| `src/ClassicUO.Client/Game/GameObjects/Entity.cs` | New `MagicShield` (`ushort`) field, populated from the server's magic-shield sub-command, feeding the magic-shield HP-bar rendering feature. | 2026-09-16 |
| `src/ClassicUO.Client/Game/Managers/HealthLinesManager.cs` | Overhead health line now renders a purple shield segment via the new shared formula, and its green fill width is computed live from `Hits`/`HitsMax` instead of reading `Entity.HitsPercentage` (needed regardless, to add the shield segment correctly; also avoids `HitsPercentage` going briefly stale when `HitsMax` transiently hits `0`, since its only writer is gated on `HitsMax > 0`). | 2026-09-16 |
| `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs` | Adds the shared `BaseHealthBarGump.CalculateShieldSegments` formula and renders the magic-shield segment on both health-bar gump classes (`HealthBarGumpCustom`, the modern line-skin bar, and `HealthBarGump`, the classic gump-art bar). | 2026-09-16 |
| `src/ClassicUO.Client/Network/PacketHandlers.cs` | New `0xBF` sub-command `0x4D53` parses the server's magic-shield value into `Entity.MagicShield`. | 2026-09-16 |
