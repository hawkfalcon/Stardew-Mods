# How to verify the BetterJunimos fixes

All fixes are static source edits in this repo (12 files, see `git diff`). Nothing has
been compiled or run — this checklist is the plan to prove the fixes work in-game.

---

## 1. Build the mod

Requirements: **.NET 6 SDK** (or Visual Studio 2022+ with the .NET 6 workload), and a
Stardew Valley install with SMAPI. `Pathoschild.Stardew.ModBuildConfig` auto-detects
the game path (set `StardewModsAPI` env var or edit the csproj `<ModFolderPath>` if
needed).

```
cd BetterJunimos
dotnet build -c Release
```

- Success looks like: `Build succeeded` and a `BetterJunimos/bin/Release/net6.0/` output.
- The build also validates that every symbol/signature we use exists (that's the main
  thing static review could NOT check — compile it and fix whatever the compiler flags).

## 2. Install & launch

```
# copy the built folder into your game's Mods dir
cp -r BetterJunimos/bin/Release/net6.0 <game>/Mods/BetterJunimos
# launch the game through SMAPI
<game>/SMAPI installer or Steam launch with SMAPI
```

- The SMAPI console should show `BetterJunimos 3.1.2` loading with **no red errors**.
- If anything looks wrong: upload the log to https://smapi.io/log and share the link.

## 3. Config sanity

In `<game>/Mods/BetterJunimos/config.json` confirm the new toggle exists:

```json
"PlantMixedSeeds": false
```

and that `MaxJunimos` / `MaxRadius` are whatever you want to test with.

---

## 4. Per-issue in-game tests

The numbers in brackets are the GitHub issues. Test on a fresh save or a test farm;
use the SMAPI console commands the mod already ships (`bj_list_huts`,
`bj_list_actions`, `bj_list_abilities`, `bj_unlock`, `bj_reset_cooldowns`).

### Spawning / caps / lag — the big ones

| Test | Expected | Issues |
|---|---|---|
| Build a hut next to mature crops; save & reload, just stand there | Junimos come out **within ~1–3 seconds** of loading, every hut, without pressing J | #98 #99 #93 |
| Set `MaxJunimos = 3`, `MaxRadius = 3` on a huge field | Never more than **3 junimos total** per hut; watch with the Junimo Tracker or just count | #92 #101 |
| Set `MaxJunimos = 1` | Exactly **1** junimo comes out — no flickering/despawn loop, no repeated meep sounds | #92 #101 |
| Clear the field (nothing to harvest/plant/water) | **No junimos come out at all** while nothing is actionable | #100 #101 |
| Unlock "Unlimited Junimos" (`bj_unlock UnlimitedJunimos`), then set `MaxJunimos = 3` | Only 3 spawn — the config wins over the unlock | #101 |
| Day with no crops in range; let crops mature mid-day (or use `debug growcrops`), wait | The hut **activates mid-day** instead of staying dead | #99 |
| 2 huts, one with nothing in range at 6:10 AM | The idle hut activates later as soon as work exists | #99 |

### Harvesting behavior

| Test | Expected | Issues |
|---|---|---|
| Lone melon/pumpkin/cauliflower mature, not in a 3x3 block | **Harvested** normally | #93 |
| A full 3x3 (or larger) block of the same giant crop | **Left unharvested** (giant-crop chance preserved); harvested on the 28th if `HarvestEverythingOn28th` | #93 |
| Harvest a melon field with `AvoidHarvestingGiants` | Option stays default-on but no longer blocks *all* melon harvests | #93 |
| Put **Mixed Seeds** and **Mixed Flower Seeds** in the hut chest | Not planted while `PlantMixedSeeds` is off; planted when you flip it on (GMCM or config + restart) | #97 |
| Manually fertilize a tile with Speed-Gro, seeds planted by junimos on it | Junimos **don't re-fertilize or wipe it**; that tile grows at speed-gro rate | #44 |
| Tiles junimos fertilize themselves | They grow at the correct boosted rate (incl. Deluxe Speed-Gro) | #44 |
| Have **Botanist** profession; junimos harvest wild-seed crops (Spring/Summer/Fall/Winter Seeds) | Harvested forage in the hut is **iridium** | #89 |
| Junimos harvest salmonberries/blackberries from bushes | **Not** iridium (matches vanilla) | #89 |

### Location support

| Test | Expected | Issues |
|---|---|---|
| Ginger Island with "Buildable Ginger Island Farm" | Hut placed there spawns junimos; pressing J in that location spawns too | #95 |
| Immersive Farm 2 / modded farm | Works, and the fps stays normal (the old per-frame scan is gone) | #100 |
| Greenhouse crops | Still worked (regression check — old behavior preserved) | — |

### Errors / lost huts

| Test | Expected | Issues |
|---|---|---|
| Move a hut with the carpenter while junimos are out | At most **one** "Could not find hut" warning line, not a flood; lost junimo despawns within ~10 game-minutes | #103 |
| Reload a save from a session where a hut was moved | No warning spam on load | #103 |
| Install **Athletics** + **Archaeology** (modded skill XP) with `GiveExperience` on | No error spam; junimos still harvest; XP for the ones that work still granted | #94 |

### Tracker (perfection) popup

| Test | Expected | Issues |
|---|---|---|
| Click a scarecrow / owl statue / farmhouse deco near a hut | **No** tracker popup | #108 |
| Press A on a gamepad 10 tiles from a hut | **No** tracker popup | #108 |
| Click directly on a Junimo Hut | Tracker **does** open (and toggles the hut info if you have the menu keybind) | #105 |
| Mobile: click the Dwarf King statue | No hut info popup; junimos still come out of huts | #105 |

---

## 5. If a test fails

1. Reproduce it once, then grab the SMAPI log: https://smapi.io/log (drag the
   `SMAPI-latest.txt` / `SMAPI-crash.txt` from `<game>/` onto the page, paste the link).
2. Note which test, which config values, and whether it reproduces after a fresh
   day (reload) — save-load behavior is the key trigger for the spawning bugs.
3. Open the issue or paste it here with the log link. The fixes are all in
   `Patches/JunimoHutPatches.cs` (spawning/caps), `Patches/JunimoHarvesterPatches.cs`
   (harvest/Botanist), `Abilities/Base/*` (planting/fertilizing/XP), so regressions
   will point at one of those files.

## 6. Regression sanity (things that must NOT break)

- Junimos stop at 7:10 PM (or 12 AM with "work in evenings").
- No junimos in winter/rain unless those toggles are on.
- Raisins still give double harvests.
- Co-op: only the host spawns junimos (as before).
- `bj_reset_cooldowns` still clears failed-action cooldowns.

---

## 7. What the diff actually does (read this before or instead of playing)

This is the "review on paper" map. Each row: the change → why → how you'd observe it failing if it's wrong.

### `Patches/JunimoHutPatches.cs` (the core of the fix)

| Change | Why | Failure signature if wrong |
|---|---|---|
| `PatchSearchAroundHut` caches the full-grid scan per hut for 60 ticks, keyed by `(hut, radius, tileX, tileY)`; invalidated on day start, hut-chest menu close, building add/remove, dayUpdate | The scan ran every frame (the #100/#101 lag); 60-tick cooldown ≈ 1s is invisible in gameplay | Lag returns; or stale "no work" up to ~1s after moving a hut |
| `ReplaceJunimoHutUpdate` + `updateWhenFarmNotCurrentLocation` postfixes call `JunimoSpawnHelper.TrySpawnJunimo`; the non-current-location method also has a prefix that pins `junimoSendOutTimer` ≥ 1000 so **vanilla's own spawn driver (which hard-codes 3 junimos) never fires** | #98/#99/#93 (spawning depended on the 6:10 AM `performTenMinuteAction` priming the timer; some huts never activated) and #92/#101 (vanilla driver ignores your cap) | Junimos don't come out; or >MaxJunimos junimos appear |
| `TrySpawnJunimo`: prune dead/orphaned junimos, despawn excess above cap, spawn only when work exists, pace 1/sec via `Game1.ticks`, quit at 7:10 PM / midnight, winter/rain/event gates | #92/#101 (cap now absolute, no spawns when idle), #103 (orphans can't block a hut) | Too many junimos; or junimos come out with nothing to do |
| `ReplaceJunimoTimerNumber` postfix: removes junimos no longer in the world, **despawns junimos whose hut no longer exists** (global sweep every 10 min), re-adds greenhouse junimos to `myJunimos`, pokes all to work | #103 (lost-hut junimos now vanish within 10 game-minutes instead of at next day start); greenhouse regression safety | "Could not find hut" spam continues; junimos frozen forever |
| `ReplaceJunimoHutdayUpdate` postfix: force `shouldSendOutJunimos=true`, set radius, reset caches | #99 (huts must activate regardless of save-load state) | Some huts idle all session |

### `Patches/JunimoHarvesterPatches.cs`

| Change | Why | Failure signature |
|---|---|---|
| `PatchTryToHarvestHere`: when the hut lookup fails, fall back to vanilla instead of freezing the junimo | #103 | Junimo stuck in place (it was: `return false` = cancel vanilla, do nothing) |
| `PatchJunimoHarvesterAddItemToHut` (new, on `tryToAddItemToHut`): Botanist → iridium for `GreensCategory` items, excluding bush yields 815/296/410 | #89. Runs *before* the item enters the chest, so stored quality is right | Wild-seed crops come out non-iridium; or (worse) salmonberries become iridium |

### `BetterJunimos.cs`

| Change | Why | Failure signature |
|---|---|---|
| `ShowPerfectionTracker`: cursor tile only when the mouse is actually visible/near the player, else `GetGrabTile()` — mirrors vanilla `pressActionButton` | #108/#105 (tracker fired on scarecrows, statues, walls, 10 tiles away) | Tracker still fires off-hut; or no longer fires on the hut itself |
| Cache invalidation wired to menu close / day start / building change | #99/#100 | Stale "no work" after chest edits |
| `SpawnJunimoCommand` works in any location containing your hut | #95 (Ginger Island farm) | J key does nothing on the island |

### The rest

| File | Change | Why |
|---|---|---|
| `Utils/JunimoProgression.cs` | `BonusMaxJunimos` deleted; `MaxJunimosUnlocked = BaseMaxJunimos` (config always wins) | #92/#101 feedback loop |
| `Utils/Util.cs` | `GetAllFarms()` no longer copies the list (hot path); `GetHutFromId` warns **once per id per session**; null-safe id lookup | #103 spam |
| `Abilities/Base/FertilizeAbility.cs` | Fertilizer stored as a qualified id + vanilla-identical speed formula (`GetFertilizerSpeedBoost`, paddy +0.25, Agriculturist +0.1); skips tiles that already have fertilizer (`HasFertilizer`) | #44 (junimos wiped manual fertilizer / miscalculated speed) |
| `Abilities/Base/HarvestCropsAbility.cs` | `gainExperience` wrapped in try/catch (#94); `AvoidHarvestingGiants` only skips crops that can actually form a 3×3 giant (#93 half) | modded-skill error spam; melon fields never harvested |
| `Abilities/Base/HarvestForageCropsAbility.cs` | `gainExperience` guarded | #94 |
| `Abilities/Base/PlantCropsAbility.cs` | Mixed Seeds / Mixed Flower Seeds gated behind new `PlantMixedSeeds` toggle | #97 |
| `ModConfig.cs` + `i18n/default.json` | the `PlantMixedSeeds` option + GMCM text | #97 |
| `Abilities/JunimoAbilities.cs` | `ItemInHut`/`UpdateHutItems` never throw on never-scanned huts | robustness (#44-adjacent crash) |

### The honest limits

- **Compile** catches typos/signature mistakes — the #1 risk in a Harmony patch mod (the diff touches 12 files; one wrong method name = silent non-patch or crash).
- **Play-testing** catches behavioral mistakes — and the failure signatures above are all *visible in-game* (count junimos, watch the log).
- What can't be verified without a game: nothing structural, but the proof is in the session. The 20-minute script in section 4 covers every changed subsystem.
