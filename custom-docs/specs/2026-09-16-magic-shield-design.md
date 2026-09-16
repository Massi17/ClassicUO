# Magic shield: HP-bar rendering (design)

Server-side counterpart: `../../ModernUO/custom-docs/specs/2026-09-16-magic-shield-design.md` (the mechanic — absorb pool, depletion, network packet). This doc owns only how the three HP-bar drawing surfaces render it.

## Goal

Server grants a Mobile a depletable "magic shield" point pool (see the ModernUO doc). This doc covers rendering it as a purple segment appended to the HP bar, on all three of ClassicUO's independent HP-bar draw paths, using a single shared percentage formula so the three stay visually consistent.

## Data

`Entity` (or `Mobile`, wherever `Hits`/`HitsMax` already live) gains:

```csharp
public ushort MagicShield { get; set; }
```

Populated by the `0xBF` sub-command handler (server side defined in the ModernUO doc) — a new `case` in the `ExtendedCommand` switch in `Network/PacketHandlers.cs` (~line 4340 today), reading Serial + shield points and setting the field on the matching `World.Mobiles`/`World.Player` entity, then invalidating whatever currently triggers a bar redraw on `Hits` change.

## Shared percentage formula

Rather than let each of the three draw sites compute this independently (and risk drifting out of sync), one shared static helper — alongside `BaseHealthBarGump.CalculatePercents` (`Game/UI/Gumps/HealthBarGump.cs:152`) is the natural home:

```csharp
public static (float green, float purple, float empty) CalculateShieldSegments(int hits, int hitsMax, int shield)
{
    var total = hitsMax + shield;
    if (total <= 0)
    {
        return (0f, 0f, 0f);
    }

    var green = (float)hits / total;
    var purple = (float)shield / total;
    var empty = MathF.Max(0f, 1f - green - purple);
    return (green, purple, empty);
}
```

Confirmed semantics from the original ask: 100 max HP, 100 current HP, 50 shield → `total = 150`, green = 2/3, purple = 1/3. With `shield = 0` this degrades to exactly today's behavior (green = `hits/hitsMax`, no purple segment) — no regression for every mobile that never has a shield.

## The three draw surfaces

Each gets a third draw call for the purple segment, inserted between the existing green-fill draw and the existing red/empty draw, sized by `purple * <bar's pixel width>`:

1. **`Game/Managers/HealthLinesManager.cs`** (`DrawHealthLine`, ~line 178) — overhead bar drawn above mobiles. Today: green/poison/yellow fill (~346-372) then red remainder (~319-344), both `DrawTiled` calls over `HP_GRAPHIC` (0x1069) with different hues. Add a third `DrawTiled` call with a purple hue, positioned right after the fill block, width `purple * BAR_WIDTH` (`BAR_WIDTH = 34`, line 13).
2. **`Game/UI/Gumps/HealthBarGump.cs`, `HealthBarGump`** (classic gump-art skin, line 1272) — fill today is `GumpPicWithWidth` (`_bars[0]`) over a red background (`_hpLineRed`, `LINE_RED`/`LINE_RED_PARTY`). Reuse the existing `LINE_BLUE`/`LINE_BLUE_PARTY` asset (already shipped for the mana bar) as the shield segment's graphic, hue-shifted purple via the standard hue-shader draw path — no new gump asset needed.
3. **`Game/UI/Gumps/HealthBarGump.cs`, `HealthBarGumpCustom`** (modern line skin, line 302) — fill is a solid-color `LineCHB` (`_bars[0]`, line 1201/1222). Add a third `LineCHB` with a literal purple RGB color (this skin draws flat rects, no hue-shader involved), same pattern as `_bars[0]`.

Exact hue index (surfaces 1-2) and RGB constant (surface 3) are tuned visually during implementation, not pinned here — they don't affect layout/logic, only appearance.

## Explicitly out of scope

- A buff-bar icon (`BuffGump`) signaling "shielded" — the original ask was about the HP bar only; add later if wanted, as its own small change (see `custom-docs/design/effects-ui.md` §1 for the existing buff-icon plumbing if that's ever picked up).
- Any new gump/art asset file — both hue-shifted-existing-asset (surfaces 1-2) and flat-color (surface 3) avoid needing one, consistent with `custom-docs/design/effects-ui.md` §3's loose-file override mechanism being unnecessary here.

## Testing

`tests/ClassicUO.UnitTests`:

- `CalculateShieldSegments`: the 100/100/50 case from the original ask (2/3, 1/3, 0), `shield = 0` (matches today's plain green/red split exactly, i.e. no regression), shield alone exceeding `hitsMax` by a large margin (purple dominates, green still correct), `hits = 0` edge case.
- `0xBF` sub-command parsing: known byte payload → `Entity.MagicShield` set to the expected value.

Manual: launch the client (`run` skill) against a ModernUO test instance, trigger the shield via whatever test hook the server side exposes, and eyeball all three surfaces (overhead line, classic gump bar, modern line-skin bar) before calling the feature done — no rendering test harness exists in this repo to automate that part.
