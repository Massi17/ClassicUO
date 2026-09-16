# Magic Shield (ClassicUO client side) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render the server's magic-shield pool (already fully implemented on ModernUO) as a purple segment on all three of ClassicUO's independent HP-bar draw paths, fed by one shared percentage helper so they can never drift out of sync with each other.

**Architecture:** `Entity` gains a plain `ushort MagicShield` field, populated by a new `0xBF`/`0x4D53` sub-command case in `PacketHandlers.ExtendedCommand`. A new `internal static` helper `BaseHealthBarGump.CalculateShieldSegments(hits, hitsMax, shield)` computes green/purple/empty fractions against a `hitsMax + shield` denominator. Each of the three draw surfaces multiplies those fractions by its own bar's pixel width and adds one new draw call for the purple segment, right after its existing green-fill draw.

**Tech Stack:** C# / .NET 10, xUnit, FNA/XNA (`Microsoft.Xna.Framework.Color`, `UltimaBatcher2D.DrawTiled`).

**Spec:** `custom-docs/specs/2026-09-16-magic-shield-design.md` (this repo). Server-side counterpart, already implemented: `../../ModernUO/custom-docs/specs/2026-09-16-magic-shield-design.md` and `../../ModernUO/custom-docs/plans/2026-09-16-magic-shield-modernuo-plan.md`.

## Global Constraints

- `shield = 0` must degrade every draw surface to *exactly* today's behavior (same pixel widths, same colors) — no regression for the near-totality of mobiles that never carry a shield. Every task's tests must include a `shield = 0` case proving this.
- The formula's denominator is `hitsMax + shield`, not `hitsMax` alone — confirmed from the original ask's own numbers (100 max HP, 100 current HP, 50 shield → 2/3 green, 1/3 purple only works if the bar's 100% is rescaled to 150).
- No new gump/art asset files — surfaces 1-2 reuse existing graphics (`HP_GRAPHIC` 0x1069, `LINE_BLUE`/`LINE_BLUE_PARTY`) hue-shifted; surface 3 draws a flat `Color.Purple` rectangle (this skin doesn't use the hue-shader at all).
- Sub-command `0x4D53` under opcode `0xBF` — must match the ModernUO side exactly (already implemented there). Confirmed free in this file's `ExtendedCommand` switch (existing cases: 0-8, 0x0C, 0x10, 0x11, 0x14, 0x16, 0x18, 0x19, 0x1B, 0x1D, 0x20, 0x21, 0x22, 0x25, 0x26, 0x2A, 0x2B, 0xBEEF).
- No buff-bar icon, no new asset files — both explicitly out of scope per the spec.

---

## Task 1: Shared percentage helper

**Files:**
- Modify: `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs` (add to `BaseHealthBarGump`, alongside the existing `CalculatePercents` at line 152)
- Test: `tests/ClassicUO.UnitTests/Game/UI/Gumps/BaseHealthBarGumpTests.cs`

**Interfaces:**
- Produces: `BaseHealthBarGump.CalculateShieldSegments(int hits, int hitsMax, int shield) -> (float green, float purple, float empty)`. Tasks 3, 4, 5 all call this.

- [ ] **Step 1: Write the failing tests**

Create `tests/ClassicUO.UnitTests/Game/UI/Gumps/BaseHealthBarGumpTests.cs`:

```csharp
using ClassicUO.Game.UI.Gumps;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Gumps
{
    public class BaseHealthBarGumpTests
    {
        [Fact]
        public void CalculateShieldSegments_OriginalAskExample_TwoThirdsGreenOneThirdPurple()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(100, 100, 50);

            Assert.Equal(2f / 3f, green, 3);
            Assert.Equal(1f / 3f, purple, 3);
            Assert.Equal(0f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_NoShield_MatchesPlainHitsOverHitsMax()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(60, 100, 0);

            Assert.Equal(0.6f, green, 3);
            Assert.Equal(0f, purple, 3);
            Assert.Equal(0.4f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_ShieldExceedsHitsMax_PurpleDominatesGreenStillCorrect()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(50, 50, 200);

            // total = 250; green = 50/250 = 0.2; purple = 200/250 = 0.8
            Assert.Equal(0.2f, green, 3);
            Assert.Equal(0.8f, purple, 3);
            Assert.Equal(0f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_ZeroHits_GreenIsZero()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(0, 100, 50);

            // total = 150; green = 0; purple = 50/150 = 1/3; empty = 2/3
            Assert.Equal(0f, green, 3);
            Assert.Equal(1f / 3f, purple, 3);
            Assert.Equal(2f / 3f, empty, 3);
        }

        [Fact]
        public void CalculateShieldSegments_ZeroHitsMaxAndZeroShield_ReturnsAllZero()
        {
            var (green, purple, empty) = BaseHealthBarGump.CalculateShieldSegments(0, 0, 0);

            Assert.Equal(0f, green);
            Assert.Equal(0f, purple);
            Assert.Equal(0f, empty);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~BaseHealthBarGumpTests"`
Expected: FAIL — `CalculateShieldSegments` doesn't exist yet.

- [ ] **Step 3: Implement the helper**

In `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs`, immediately after the existing `CalculatePercents` method (today ends at line ~171, right after its closing `}`):

```csharp
        internal static (float green, float purple, float empty) CalculateShieldSegments(int hits, int hitsMax, int shield)
        {
            var total = hitsMax + shield;

            if (total <= 0)
            {
                return (0f, 0f, 0f);
            }

            var green = (float) hits / total;
            var purple = (float) shield / total;
            var empty = System.MathF.Max(0f, 1f - green - purple);

            return (green, purple, empty);
        }
```

This is deliberately `internal` (not `protected` like `CalculatePercents`) so `tests/ClassicUO.UnitTests` can call it directly without subclassing `BaseHealthBarGump` — `ClassicUO.Client.csproj` already declares `InternalsVisibleTo` for the test assembly (confirmed: `Entity`, `Mobile`, etc. are already tested this way from `tests/ClassicUO.UnitTests/Game/GameObjects/`).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~BaseHealthBarGumpTests"`
Expected: PASS (all 5 tests).

- [ ] **Step 5: Full test suite run and commit**

Run: `dotnet test tests/ClassicUO.UnitTests`

```bash
git add src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs tests/ClassicUO.UnitTests/Game/UI/Gumps/BaseHealthBarGumpTests.cs
git commit -m "feat: add BaseHealthBarGump.CalculateShieldSegments shared formula"
```

---

## Task 2: `Entity.MagicShield` field and the `0x4D53` packet case

**Files:**
- Modify: `src/ClassicUO.Client/Game/GameObjects/Entity.cs` (add field, next to `Hits`/`HitsMax` at lines 37-38)
- Modify: `src/ClassicUO.Client/Network/PacketHandlers.cs` (new case in `ExtendedCommand`'s switch, plus a new small testable method)
- Test: `tests/ClassicUO.UnitTests/Network/PacketHandlersTests.cs`

**Interfaces:**
- Consumes: nothing from Task 1.
- Produces: `Entity.MagicShield` (`ushort` field). `PacketHandlers.ApplyMagicShield(World world, uint serial, ushort points)` — internal static, used by Tasks 3-5's manual verification and directly unit-tested here.

- [ ] **Step 1: Add the `Entity.MagicShield` field**

In `src/ClassicUO.Client/Game/GameObjects/Entity.cs`, immediately after `HitsMax` (line 38):

```csharp
        public ushort Hits;
        public ushort HitsMax;
        public ushort MagicShield;
```

No test for this line alone — it's a bare field, exercised by this task's own test below.

- [ ] **Step 2: Write the failing test**

`ExtendedCommand(World world, ref StackDataReader p)` cannot be unit-tested directly: `StackDataReader` is a `ref struct` (used as `ref StackDataReader p` throughout `PacketHandlers.cs`), and `ref struct` values cannot be boxed or passed through reflection's `object[]` parameter array — there is no way to invoke a `private` method taking one from another assembly. So the packet-byte-reading stays inline in the switch case (two `ReadUInt32BE`/`ReadUInt16BE` calls — hard to get wrong, matches every other case in the file), and the *effect* of those two values — looking up the entity and setting the field — moves into its own small `internal static` method that a test can call directly with plain parameters.

Create `tests/ClassicUO.UnitTests/Network/PacketHandlersTests.cs`:

```csharp
using ClassicUO.Game;
using ClassicUO.Game.GameObjects; // brings in the Dictionary<uint, Mobile>.Add(Mobile) extension (EntityCollection.cs's DictExt)
using ClassicUO.Network;
using Xunit;

namespace ClassicUO.UnitTests.Network
{
    public class PacketHandlersTests
    {
        [Fact]
        public void ApplyMagicShield_SetsTheFieldOnTheMatchingEntity()
        {
            var world = new World();
            var mobile = ClassicUO.Game.GameObjects.Mobile.Create(world, 0x1024);
            world.Mobiles.Add(mobile);

            PacketHandlers.ApplyMagicShield(world, 0x1024, 50);

            Assert.Equal((ushort) 50, mobile.MagicShield);

            world.Clear();
        }

        [Fact]
        public void ApplyMagicShield_UnknownSerial_DoesNothing()
        {
            var world = new World();

            // Valid mobile-range serial (< 0x40000000, per SerialHelper.IsMobile), just never registered - must not throw.
            PacketHandlers.ApplyMagicShield(world, 0x1025, 50);

            world.Clear();
        }

        [Fact]
        public void ApplyMagicShield_ZeroClearsAnExistingShield()
        {
            var world = new World();
            var mobile = ClassicUO.Game.GameObjects.Mobile.Create(world, 0x1024);
            mobile.MagicShield = 50;
            world.Mobiles.Add(mobile);

            PacketHandlers.ApplyMagicShield(world, 0x1024, 0);

            Assert.Equal((ushort) 0, mobile.MagicShield);

            world.Clear();
        }
    }
}
```

`World.Mobiles.Add(mobile)` resolves to `DictExt.Add<T>(this Dictionary<uint, T> dict, T entity)` in `src/ClassicUO.Client/Game/GameObjects/EntityCollection.cs` — it keys the dictionary by `entity.Serial` (confirmed by reading that file). `World.Get(uint serial)` (`src/ClassicUO.Client/Game/World.cs:406`) checks `SerialHelper.IsMobile(serial)` and reads from this same `Mobiles` dictionary via `Mobiles.Get(serial)` (another `DictExt` extension) — so a `Mobile` added this way is reachable by `World.Get`. `0x1024` is a valid mobile-range serial (high bit clear), matching what `SerialHelper.IsMobile` expects.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~PacketHandlersTests"`
Expected: FAIL — `PacketHandlers.ApplyMagicShield` doesn't exist yet (and it's not `public`, so this also won't compile until Step 4 — that's fine, a compile failure is an acceptable RED for a method that doesn't exist yet).

- [ ] **Step 4: Implement `ApplyMagicShield` and wire the packet case**

In `src/ClassicUO.Client/Network/PacketHandlers.cs`, add the new case to the `ExtendedCommand` switch, immediately before `case 0xBEEF:` (today ~line 4724):

```csharp
                case 0x4D53: // Magic Shield (this fork's extension - see custom-docs/specs/2026-09-16-magic-shield-design.md)
                    uint shieldSerial = p.ReadUInt32BE();
                    ushort shieldPoints = p.ReadUInt16BE();

                    ApplyMagicShield(world, shieldSerial, shieldPoints);

                    break;

```

Add the new method anywhere else at class (`PacketHandlers`) scope — e.g. right after the `ExtendedCommand` method's closing brace (today ~line 4735):

```csharp
        internal static void ApplyMagicShield(World world, uint serial, ushort points)
        {
            var entity = world.Get(serial);

            if (entity != null)
            {
                entity.MagicShield = points;
            }
        }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~PacketHandlersTests"`
Expected: PASS (all 3 tests).

- [ ] **Step 6: Full test suite run and commit**

Run: `dotnet test tests/ClassicUO.UnitTests`

```bash
git add src/ClassicUO.Client/Game/GameObjects/Entity.cs src/ClassicUO.Client/Network/PacketHandlers.cs tests/ClassicUO.UnitTests/Network/PacketHandlersTests.cs
git commit -m "feat: parse the 0x4D53 magic-shield sub-command into Entity.MagicShield"
```

---

## Task 3: Overhead health line (`HealthLinesManager`)

**Files:**
- Modify: `src/ClassicUO.Client/Game/Managers/HealthLinesManager.cs`

**Interfaces:**
- Consumes: `BaseHealthBarGump.CalculateShieldSegments` (Task 1), `Entity.MagicShield` (Task 2).
- Produces: nothing new — this is a leaf draw-surface task.

**Correction (added during the final whole-branch review, 2026-09-16):** the paragraph below, as originally written while planning this task, claimed `Entity.HitsPercentage` was "dead code — never written anywhere" because a repo-wide grep for `.UpdateHits(` (member-access syntax) found no callers, and that this task's replacement therefore fixed a pre-existing "always zero-width fill" bug as a side effect. **That claim is false.** `Entity.Update()` (`src/ClassicUO.Client/Game/GameObjects/Entity.cs`, ~lines 147-153) calls `UpdateHits((byte)perc)` as a bare, same-class call with no `this.`/`entity.` prefix — the earlier grep missed it purely because it searched for member-access syntax. `World.Update()` calls `.Update()` on every mobile every frame, so `HitsPercentage` is in fact written constantly and was not stuck at zero.

The actual code change this task made is still correct and was still necessary regardless of the above: the shared formula needs live `Hits`/`HitsMax` values (not a precomputed percentage) to add the purple shield segment correctly, so replacing the `HitsPercentage` read with a live computation was required either way. The one real, narrower benefit is that `UpdateHits` only runs when `HitsMax > 0` (see the `if (HitsMax > 0)` guard around the call in `Entity.Update()`), so `HitsPercentage` could go briefly stale in the edge case where `HitsMax` transiently drops to `0`; computing the fill live from `Hits`/`HitsMax` avoids that narrow staleness. That is the accurate justification — not "the fill was always zero before."

- [ ] **Step 1: Replace the dead `HitsPercentage` read and compute both fill widths**

In `src/ClassicUO.Client/Game/Managers/HealthLinesManager.cs`, replace (today line 194):

```csharp
            int per = BAR_WIDTH * entity.HitsPercentage / 100;
```

with:

```csharp
            var (shieldGreenFrac, shieldPurpleFrac, _) = BaseHealthBarGump.CalculateShieldSegments(entity.Hits, entity.HitsMax, entity.MagicShield);
            int per = (int) (BAR_WIDTH * shieldGreenFrac);
            int shieldPer = (int) (BAR_WIDTH * shieldPurpleFrac);
```

Add `using ClassicUO.Game.UI.Gumps;` to the top of the file (for `BaseHealthBarGump`) if not already present.

- [ ] **Step 2: Reposition the red/empty remainder to start after the purple segment, not just the green one**

Replace (today lines 319-344):

```csharp
            hueVecNoto.X = 0x21;

            if (entity.Hits != entity.HitsMax || entity.HitsMax == 0)
            {
                int offset = 2;

                if (per >> 2 == 0)
                {
                    offset = per;
                }

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);

                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(
                        x + per * MULTIPLER - offset,
                        y,
                        (BAR_WIDTH - per) * MULTIPLER - offset / 2,
                        gumpInfo.UV.Height * MULTIPLER
                    ),
                    gumpInfo.UV,
                    hueVecNoto,
                    layerDepth
                );
            }
```

with:

```csharp
            hueVecNoto.X = 0x21;

            int filledPer = per + shieldPer;

            if (entity.Hits != entity.HitsMax || entity.HitsMax == 0)
            {
                int offset = 2;

                if (filledPer >> 2 == 0)
                {
                    offset = filledPer;
                }

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);

                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(
                        x + filledPer * MULTIPLER - offset,
                        y,
                        (BAR_WIDTH - filledPer) * MULTIPLER - offset / 2,
                        gumpInfo.UV.Height * MULTIPLER
                    ),
                    gumpInfo.UV,
                    hueVecNoto,
                    layerDepth
                );
            }
```

Only the guard's inputs change (`per` → `filledPer` in the sizing math), not the guard condition itself: `entity.Hits != entity.HitsMax` is exactly true or false the same as before, because `green + purple` reaches exactly `1.0` precisely when `Hits == HitsMax`, regardless of the shield's size (`total = hitsMax + shield` cancels out: `(hits + shield) / (hitsMax + shield) = 1` iff `hits = hitsMax`). Do **not** add a `shieldPer > 0` condition here — a full-health, actively-shielded mobile has `Hits == HitsMax`, so the guard is correctly false and this block correctly does not run; forcing it to run in that case would feed `BAR_WIDTH - filledPer` a value at or near `0` into the same `offset` math the small-`per` branch above assumes is a comfortably positive number, and could draw a negative-width rectangle. (`filledPer` can land 1px under `BAR_WIDTH` purely from `(int)` truncation on the two separate fractions — e.g. hits=100/max=100/shield=50 truncates to 22+11=33, not 34 — that's a pre-existing class of rounding slop this codebase already tolerates elsewhere, not something this task needs to correct.)

- [ ] **Step 3: Add the purple draw, after the existing green draw**

Replace (today lines 346-372, the closing of the method):

```csharp
            hue = 90;

            if (per > 0)
            {
                if (mobile != null)
                {
                    if (mobile.IsPoisoned)
                    {
                        hue = 63;
                    }
                    else if (mobile.IsYellowHits)
                    {
                        hue = 53;
                    }
                }

                hueVecNoto.X = hue;

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);
                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(x, y, per * MULTIPLER, gumpInfo.UV.Height * MULTIPLER),
                    gumpInfo.UV,
                    hueVecNoto,
                    layerDepth
                );
            }
        }
    }
}
```

with:

```csharp
            hue = 90;

            if (per > 0)
            {
                if (mobile != null)
                {
                    if (mobile.IsPoisoned)
                    {
                        hue = 63;
                    }
                    else if (mobile.IsYellowHits)
                    {
                        hue = 53;
                    }
                }

                hueVecNoto.X = hue;

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);
                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(x, y, per * MULTIPLER, gumpInfo.UV.Height * MULTIPLER),
                    gumpInfo.UV,
                    hueVecNoto,
                    layerDepth
                );
            }

            if (shieldPer > 0)
            {
                // Hue chosen for visibility; tune by eye against the live client - see spec §"The three draw surfaces".
                const ushort SHIELD_HUE = 2;

                hueVecNoto.X = SHIELD_HUE;

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);
                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(x + per * MULTIPLER, y, shieldPer * MULTIPLER, gumpInfo.UV.Height * MULTIPLER),
                    gumpInfo.UV,
                    hueVecNoto,
                    layerDepth
                );
            }
        }
    }
}
```

- [ ] **Step 4: Build and run the full test suite**

This is a rendering method with no automated coverage (matches the spec's own "Testing" section — no rendering harness exists in this repo). Verification here is: the project builds, and the *unrelated* full suite still passes (a regression here would most likely show up as a compile error, since this method has no direct tests).

Run: `dotnet build` then `dotnet test tests/ClassicUO.UnitTests`
Expected: build succeeds, all existing tests still pass (no test in this suite touches `HealthLinesManager`, so an unrelated failure here would indicate something else broke).

- [ ] **Step 5: Commit**

```bash
git add src/ClassicUO.Client/Game/Managers/HealthLinesManager.cs
git commit -m "$(cat <<'EOF'
feat: render the magic-shield segment on the overhead health line

Also fixes a pre-existing dead-code bug found while wiring this up:
the fill width read Entity.HitsPercentage, which nothing in this
codebase ever writes (UpdateHits, its only setter, has zero callers).
The fill was always rendering at zero width. Computing it live from
Hits/HitsMax - which this task's own shared-formula change already
requires - fixes that as an unavoidable side effect.
EOF
)"
```

---

## Task 4: Modern line-skin bar (`HealthBarGumpCustom`)

**Files:**
- Modify: `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs` (the `HealthBarGumpCustom` class)

**Interfaces:**
- Consumes: `BaseHealthBarGump.CalculateShieldSegments` (Task 1), `Entity.MagicShield` (Task 2).
- Produces: nothing new — leaf draw-surface task.

**Context verified while planning:** `HealthBarGumpCustom.BuildGump()` has three branches that each construct `_bars[0]` (the HP fill line) as a `LineCHB`, positioned identically to a same-row `LineCHB` background line already in the branch: multiline party mode (`_bars[0]` at `(HPB_BAR_SPACELEFT, 27)`, today ~line 802), a second near-identical multiline branch (`_bars[0]` at `(HPB_BAR_SPACELEFT, 27)`, today ~line 965), and single-line mode (`_bars[0]` at `(HPB_BAR_SPACELEFT, 21)`, today ~line 1089). All three use `HPB_BAR_WIDTH`/`HPB_BAR_HEIGHT` and `HPB_COLOR_DRAW_BLUE.PackedValue`, all start with `LineWidth = 0`. The single update site (today ~line 634) sets `_bars[0].LineWidth = hits` whenever `hits` (a pixel width) changes. `LineCHB` draws a flat-color rectangle at its own control-relative `X`/`Y` for `LineWidth` pixels (`AddToRenderLists`, confirmed in this file's `LineCHB` class ~line 1201-1250) — a second overlapping bar needs its own `X` repositioned to start right where the green bar's filled width ends, not drawn at a fixed offset, or it would sit on top of (not after) the green fill.

Add one new field `_shieldBar` (a single `LineCHB`, not part of the `_bars[]` array — that array's three slots are HP/Mana/Stam rows in multiline mode, not HP-variant slots) constructed at all three sites, and update it alongside `_bars[0]` at the one shared update site.

- [ ] **Step 1: Add the `_shieldBar` field and a purple color constant**

In `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs`, inside `HealthBarGumpCustom` (`internal class HealthBarGumpCustom : BaseHealthBarGump`, starts at today's line 302), immediately after the existing field declarations (today lines 327-328):

```csharp
        private readonly LineCHB[] _bars = new LineCHB[3];
        private readonly LineCHB[] _border = new LineCHB[4];
        private static Color HPB_COLOR_DRAW_PURPLE = Color.Purple;
        private LineCHB _shieldBar;
```

(distinct from `HealthBarGump`'s own `GumpPicWithWidth[] _bars` field in Task 5 below — same field name, different class, no collision.)

- [ ] **Step 2: Construct `_shieldBar` at all three branches**

At the first branch (multiline party mode, today ~line 800-810), immediately after the `_bars[0] = new LineCHB(...)` block:

```csharp
                Add
                (
                    _bars[0] = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        27,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_BLUE.PackedValue
                    ) { LineWidth = 0 }
                );

                Add
                (
                    _shieldBar = new LineCHB
                    (
                        HPB_BAR_SPACELEFT,
                        27,
                        HPB_BAR_WIDTH,
                        HPB_BAR_HEIGHT,
                        HPB_COLOR_DRAW_PURPLE.PackedValue
                    ) { LineWidth = 0 }
                );
```

At the second branch (today ~line 963-973), same shape, same `(HPB_BAR_SPACELEFT, 27)` position:

```csharp
                    Add
                    (
                        _bars[0] = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            27,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_BLUE.PackedValue
                        ) { LineWidth = 0 }
                    );

                    Add
                    (
                        _shieldBar = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            27,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_PURPLE.PackedValue
                        ) { LineWidth = 0 }
                    );
```

At the third branch (single-line mode, today ~line 1087-1097), same shape, `(HPB_BAR_SPACELEFT, 21)`:

```csharp
                    Add
                    (
                        _bars[0] = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            21,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_BLUE.PackedValue
                        ) { LineWidth = 0 }
                    );

                    Add
                    (
                        _shieldBar = new LineCHB
                        (
                            HPB_BAR_SPACELEFT,
                            21,
                            HPB_BAR_WIDTH,
                            HPB_BAR_HEIGHT,
                            HPB_COLOR_DRAW_PURPLE.PackedValue
                        ) { LineWidth = 0 }
                    );
```

- [ ] **Step 3: Update `_shieldBar`'s width and position at the shared update site**

Replace (today lines 632-637):

```csharp
                int hits = CalculatePercents(entity.HitsMax, entity.Hits, HPB_BAR_WIDTH);

                if (hits != _bars[0].LineWidth)
                {
                    _bars[0].LineWidth = hits;
                }
```

with:

```csharp
                var (shieldGreenFrac, shieldPurpleFrac, _) = CalculateShieldSegments(entity.Hits, entity.HitsMax, entity.MagicShield);
                int hits = (int) (HPB_BAR_WIDTH * shieldGreenFrac);
                int shieldWidth = (int) (HPB_BAR_WIDTH * shieldPurpleFrac);

                if (hits != _bars[0].LineWidth)
                {
                    _bars[0].LineWidth = hits;
                }
```

Only the `hits` source changes (from `CalculatePercents` to the shield-aware helper — with `shield = 0` they produce the identical value, confirmed by Task 1's own `CalculateShieldSegments_NoShield_MatchesPlainHitsOverHitsMax` test), and `shieldWidth` is new. Then extend the update block:

```csharp
                if (hits != _bars[0].LineWidth)
                {
                    _bars[0].LineWidth = hits;
                }

                if (shieldWidth != _shieldBar.LineWidth || _shieldBar.X != HPB_BAR_SPACELEFT + hits)
                {
                    _shieldBar.X = HPB_BAR_SPACELEFT + hits;
                    _shieldBar.LineWidth = shieldWidth;
                }
```

`_shieldBar.X` repositions the shield segment to start right where the green fill ends, on every update — this is why it can't just mirror `_bars[0]`'s static-position construction. With `shield = 0`, `shieldWidth` is always `0` and this block sets an invisible zero-width bar at whatever `X` — no visible change from today.

- [ ] **Step 4: Build and run the full test suite**

Same as Task 3 — no rendering harness exists, verification is a clean build plus the existing suite staying green.

Run: `dotnet build` then `dotnet test tests/ClassicUO.UnitTests`
Expected: build succeeds, all existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs
git commit -m "feat: render the magic-shield segment on the modern line-skin health bar"
```

---

## Task 5: Classic gump-art bar (`HealthBarGump`)

**Files:**
- Modify: `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs` (the `HealthBarGump` class)

**Interfaces:**
- Consumes: `BaseHealthBarGump.CalculateShieldSegments` (Task 1), `Entity.MagicShield` (Task 2).
- Produces: nothing new — leaf draw-surface task.

**Context verified while planning:** `HealthBarGump.BuildGump()` (the classic gump-art skin — a different class from Task 4's `HealthBarGumpCustom`, both in this same file) has three branches constructing `_bars[0]` as a `GumpPicWithWidth`: party multiline (`(18, 20)`, graphic `LINE_BLUE_PARTY`, initial percent `96`, today ~line 1414), player-self single view (`(34, 12)`, graphic `LINE_BLUE`, initial percent `0`, today ~line 1467), and target/other single view (`(34, 38)`, graphic `LINE_BLUE`, initial percent `0`, today ~line 1526). The one shared update site (today ~line 1826-1836) computes `barW = inparty ? 96 : 109` then `hits = CalculatePercents(entity.HitsMax, entity.Hits, barW)`, setting `_bars[0].Percent = hits`. `GumpPicWithWidth.AddToRenderLists` (confirmed in `src/ClassicUO.Client/Game/UI/Controls/GumpPicWithWidth.cs`) draws a `DrawTiled` rectangle at its own control `X`/`Y` for `Percent` pixels — same "needs X repositioning, not a fixed offset" situation as Task 4's `LineCHB`.

Reuse the existing `LINE_BLUE`/`LINE_BLUE_PARTY` graphics (already the mana-bar asset, matches the spec's "no new asset" constraint) with a distinct `hue` argument so the shield segment renders visually distinct from the green HP fill via the hue-shader — `GumpPicWithWidth`'s constructor already takes a `hue` parameter (`GumpPicWithWidth(int x, int y, ushort graphic, ushort hue, int perc)`).

- [ ] **Step 1: Add the `_shieldBar` field and a hue constant**

In `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs`, inside `HealthBarGump` (`internal class HealthBarGump : BaseHealthBarGump`, starts at today's line 1272), immediately after the existing field declarations (today lines 1283-1285):

```csharp
        private GumpPic _background, _hpLineRed, _manaLineRed, _stamLineRed;

        private readonly GumpPicWithWidth[] _bars = new GumpPicWithWidth[3];
        private GumpPicWithWidth _shieldBar;
        // Hue chosen for visibility; tune by eye against the live client - see spec §"The three draw surfaces".
        private const ushort SHIELD_HUE = 2;
```

- [ ] **Step 2: Construct `_shieldBar` at all three branches**

At the party-multiline branch (today ~line 1412-1422), immediately after `_bars[0] = new GumpPicWithWidth(...)`:

```csharp
                Add
                (
                    _bars[0] = new GumpPicWithWidth
                    (
                        18,
                        20,
                        LINE_BLUE_PARTY,
                        0,
                        96
                    )
                );

                Add
                (
                    _shieldBar = new GumpPicWithWidth
                    (
                        18,
                        20,
                        LINE_BLUE_PARTY,
                        SHIELD_HUE,
                        0
                    )
                );
```

At the player-self branch (today ~line 1465-1475):

```csharp
                    Add
                    (
                        _bars[0] = new GumpPicWithWidth
                        (
                            34,
                            12,
                            LINE_BLUE,
                            0,
                            0
                        )
                    );

                    Add
                    (
                        _shieldBar = new GumpPicWithWidth
                        (
                            34,
                            12,
                            LINE_BLUE,
                            SHIELD_HUE,
                            0
                        )
                    );
```

At the target/other branch (today ~line 1524-1534):

```csharp
                    Add
                    (
                        _bars[0] = new GumpPicWithWidth
                        (
                            34,
                            38,
                            LINE_BLUE,
                            0,
                            0
                        )
                    );

                    Add
                    (
                        _shieldBar = new GumpPicWithWidth
                        (
                            34,
                            38,
                            LINE_BLUE,
                            SHIELD_HUE,
                            0
                        )
                    );
```

- [ ] **Step 3: Update `_shieldBar` at the shared update site**

Replace (today ~line 1826-1836):

```csharp
                int barW = inparty ? 96 : 109;

                int hits = CalculatePercents(entity.HitsMax, entity.Hits, barW);


                if (hits != _oldHits)
                {
                    _bars[0].Percent = hits;

                    _oldHits = hits;
                }
```

with:

```csharp
                int barW = inparty ? 96 : 109;

                var (shieldGreenFrac, shieldPurpleFrac, _) = CalculateShieldSegments(entity.Hits, entity.HitsMax, entity.MagicShield);
                int hits = (int) (barW * shieldGreenFrac);
                int shieldWidth = (int) (barW * shieldPurpleFrac);

                if (hits != _oldHits)
                {
                    _bars[0].Percent = hits;

                    _oldHits = hits;
                }

                var shieldX = _bars[0].X + hits;

                if (shieldWidth != _shieldBar.Percent || _shieldBar.X != shieldX)
                {
                    _shieldBar.X = shieldX;
                    _shieldBar.Percent = shieldWidth;
                }
```

`_bars[0].X` (whichever branch constructed it — 18 or 34, per Step 2) plus the current green fill width `hits` gives the shield bar's start position, matching Task 4's positioning approach. With `shield = 0`, `shieldWidth` stays `0`.

- [ ] **Step 4: Build and run the full test suite**

Same as Tasks 3-4 — no rendering harness exists.

Run: `dotnet build` then `dotnet test tests/ClassicUO.UnitTests`
Expected: build succeeds, all existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs
git commit -m "feat: render the magic-shield segment on the classic gump-art health bar"
```

---

## Manual verification (after all 5 tasks)

Per the spec's own testing section — no rendering harness exists in this repo, so this is the only way to actually see the feature work:

1. Launch the client (`run` skill) against a ModernUO instance running this feature's server-side commits.
2. Log in, use the `[MagicShield <points> [durationSeconds]` GM command (already implemented server-side) targeting your own character or another mobile.
3. Check all three surfaces: the overhead line above the shielded mobile, the classic-skin health bar gump (default skin), and the modern line-skin health bar (switch to it in options if the profile defaults to classic).
4. Confirm: `shield = 0` (no command used) still looks identical to before this feature existed on every mobile you see; a shielded mobile shows a visibly distinct purple segment sized proportionally to the shield's remaining points; the segment shrinks as the shield depletes (deal spell damage to the shielded target) and disappears when it's fully absorbed or expires.

## Out of scope for this plan

- A buff-bar icon signaling "shielded" — explicitly out of scope per the spec.
- Any new gump/art asset file — avoided by reusing `HP_GRAPHIC`/`LINE_BLUE`/`LINE_BLUE_PARTY` (hue-shifted) and a flat `Color.Purple` rect, per the spec.
- Retuning `SHIELD_HUE`/`Color.Purple` for visual polish — flagged in both this plan and the spec as a cosmetic detail to eyeball during manual verification, not a logic concern.
