# Upstream PR file transplant manifest (PR-03 … PR-09)

Fork-only companion to [`upstream-pr-plan.md`](upstream-pr-plan.md).
Compare base: local `master` @ `cda7fa6` (= `upstream/master`) vs `personal` tip.
Do **not** open a PR that adds this file upstream.

**Method:** recreate each `pr/*` head from updated `master`. Prefer whole-file ADD for paths absent on master; surgically edit shared files. Never wholesale cherry-pick `personal`.

**Master reality check (important):**
- Master has **no** `GI-Subtitles/Core/Overlay/` tree and **no** `LiveOverlaySession`.
- Master still has `Services/OCR/RecognitionRegionFallback.cs` + `GI-Test/TestRecognitionRegionFallback.cs` (delete in PR-03).
- Master already has dark-screen / dialogue-option **detectors** and inline MainWindow scan loops; personal rewires them through `ExtraPathSample` / session slots in PR-04.
- Master `Screenshot/Screenshot.csproj` is `v4.5.2`; personal is `v4.8` (include with PR-03).
- Master `Config.cs` has no `Contains` / `Remove` (add with PR-03 for layout/store).

**Hard forbid (every PR):** see plan — `docs/agents/**`, `AGENTS.md`, `CONTEXT.md`, ADR **0013**, fork README banner, `-fork.N` notes, this manifest.

---

## Context size & parallel safety

| PR | Size | Parallel subagents? | Notes |
| --- | --- | --- | --- |
| PR-03 | **XL** | **No** (foundation) | Touches almost every shared file; deletes fallback |
| PR-04 | **L** | Only after PR-03 exists | Extra-path entanglement in session + MainWindow |
| PR-05 | **M** | After PR-03; prefer after PR-04 | Mostly persistence + `ApplyGame` |
| PR-06 | **L** | After PR-03; prefer after PR-04 | OperatorActions activity-log half |
| PR-07a | **M** | After PR-06 | Fold / match-miss / de-noise |
| PR-07b | **M** | After PR-06; OK parallel with 07a/07c if careful on `ActivityLogWindow` | Tags + copy |
| PR-07c | **S–M** | After PR-06; OK parallel with 07a/07b if careful on `ActivityLogWindow` | Follow-tail |
| PR-08 | **M** | After PR-03; independent of 04–07 if adjust basics exist | Trace + mouse guard |
| PR-09 | **M** | After PR-03; prefer after PR-04 (+07a if fold already upstream) | Idle timeout |

**Safe parallel only for:** PR-07a / 07b / 07c **after** PR-06 is stable, with one owner of `ActivityLogWindow.*` merge. Do not parallelize PR-03 with anything.

---

## Shared-file cut legend

When a later section says “surgical”, cut **only** the named symbols/regions. Personal tip often has everything intertwined — transplanting the whole file into an early PR will drag later features.

---

## PR-03 — Region pairs core (+ hint + OCR interval)

| Field | Value |
| --- | --- |
| Branch | `pr/region-pairs-core` |
| Title | `feat: region pairs with shared OCR cadence and settings` |
| Depends on | — (first large product PR) |
| Size | XL |

### Whole-file ADD from personal

**Core overlay (region-pair / cadence / hint / OCR interval only — see surgical session note):**

- `GI-Subtitles/Core/Overlay/OverlayRect.cs`
- `GI-Subtitles/Core/Overlay/RegionPair.cs` (and `RegionPairRecord` / `LegacyRegionSlots` as defined in `IRegionPairStore.cs`)
- `GI-Subtitles/Core/Overlay/IRegionPairStore.cs`
- `GI-Subtitles/Core/Overlay/PairFrameSample.cs`
- `GI-Subtitles/Core/Overlay/PairSubtitleBody.cs`
- `GI-Subtitles/Core/Overlay/RegionOutline.cs`
- `GI-Subtitles/Core/Overlay/IOcrIntervalStore.cs`
- `GI-Subtitles/Core/Overlay/ConfigOcrIntervalStore.cs`
- `GI-Subtitles/Core/Overlay/HintScreenSelection.cs`
- `GI-Subtitles/Views/RegionPairSettings.cs` (`RegionPairSettings`, `RegionPairCard`)
- `GI-Subtitles/Views/OverlayHintChrome.cs`
- `docs/adr/0001-region-pairs-share-one-ocr-clock.md`
- `docs/adr/0002-voice-plays-only-from-designated-capture.md`
- `docs/adr/0004-display-region-is-boxed-not-padded.md`
- `docs/adr/0005-overlay-click-through-except-region-adjust.md`
- `docs/adr/0007-voice-play-requests-come-from-the-session.md`

**Config map helpers (absent on master):**

- `GI-Subtitles/Core/Config/IConfigMap.cs`
- `GI-Subtitles/Core/Config/AppConfigMap.cs`

**Persistence backbone (personal store already depends on these):**

- `GI-Subtitles/Core/Overlay/OverlayLayoutRecord.cs`
- `GI-Subtitles/Core/Overlay/OverlayLayoutPersistence.cs`
- `GI-Subtitles/Core/Overlay/ConfigRegionPairStore.cs` — **strip** every `RegionAdjustTrace.StoreWrite(...)` call (PR-08). Keep pair / voice-primary / next-id / legacy migrate. Extra-path display/scan accessors may land here early as inert storage used by PR-04; do **not** wire session extra-path runtime yet.

**Optional stub types to keep `Beat` compiling without PR-04 runtime:**

- Prefer: `Beat(params PairFrameSample[])` only in PR-03; defer `ExtraPathSample.cs` / `ExtraPathBody.cs` to PR-04.
- If you keep personal’s `Beat(ExtraPathSample extra, …)` signature, ADD `ExtraPathSample.cs` with `None` only and make `ApplyExtraPathSample` a no-op until PR-04.

### Whole-file TAKE from personal

Rare. Prefer surgical for all shared UI/session files. Exception: if a new file is entirely in-scope (list above), TAKE = ADD.

### DELETE on this PR (upstream files removed by personal)

- `GI-Subtitles/Services/OCR/RecognitionRegionFallback.cs`
- `GI-Test/TestRecognitionRegionFallback.cs`
- Remove their `<Compile Include=...>` entries from both csprojs.

### Surgical shared files

#### `LiveOverlaySession.cs` (NEW on master — author a PR-03-shaped file from personal)

**INCLUDE:**
- Constants: `DefaultOcrIntervalMs`, `UiMin/MaxOcrIntervalMs`, `EngineFloorOcrIntervalMs`, `OcrIntervalConfigKey`, `PreviewDurationMs`, `EnginePairCap`, `SettingsPairCap`
- Ctors that take `IOcrIntervalStore` / `IRegionPairStore` / `Func<DateTime>` (**no** idle-timeout store overload yet — or accept and ignore until PR-09)
- Pair list API: `Pairs`, `LoadPairs` / `PersistPairs` / `MigrateLegacy`, `TryStartAdd` / `SetAddCapture` / `SetAddDisplay` / `AbortAdd` / `TryCommitAdd` / `DeletePair`, `SetCapture` / `SetDisplay` / `ClearCapture`, `GetCapture` / `GetDisplay`, `SetVoicePrimary`, `VoicePrimaryId`, `TryGetVoicePrimaryCapture`
- OCR cadence: `Beat(params PairFrameSample[])`, `EnqueueOcr*`, `TryBeginOcr` / `TryStartNextOcr`, `CompleteOcr` (**pair slots only**), `BusyOcrSlot` / `OcrQueue`, `EngineOcrIntervalMs`, `ResetOcrInterval`, `OpenOcrIntervalSettings` / `OcrIntervalSettingsView` / `ApplyCommittedOcrInterval`
- Subtitle bodies: `PairBodies`, `ApplyPairResult` (keep `_lastResults` SameAs fold + match-miss keep — required for typewriter UX; dedicated fold ADR/tests wait for PR-07a)
- Voice: `TakeVoicePlayRequest`, `EmitVoicePlayRequest`, `VoicePlayRequest`, `NoteVoicePlaybackEnded`, match cache helpers
- Preview / pair adjust: `PreviewCaptureRegion`, `PreviewChanged`, `PreviewOutlines`, `TryToggleRegionAdjust` (**pair only**), `CancelRegionAdjust`, `AdjustChanged`, `AdjustOutlines`, `ArmedTarget` / `ArmedPairId` / `IsClickThrough`, `OverlayAdjustTarget` enum **with at least `None` + `Pair`**
- Hint display candidates: `HintDisplayCandidates`
- `HasValidCapture`, `RecognitionRunning` / `SubtitlesVisible` flags as needed by MainWindow

**EXCLUDE / stub for later PRs:**
- Extra-path fields & methods: `DarkScreen*`, `DialogueOption*`, `DialogueChoiceEcho`, `ApplyExtraPathSample`, `DarkScreenOcrSlot` / `DialogueOptionsOcrSlot`, `TryToggleDarkScreenDisplayAdjust`, `TryToggleDialogueOptionDisplayAdjust`, `SetDarkScreen*` / `SetDialogueOption*`, `ApplyGame` / `AppliedGame` / `AllowsDialogueOptionScan` (PR-04 / PR-05)
- Idle timeout: `SubtitleIdleTimeout*`, `ISubtitleIdleTimeoutStore`, `ExpireIdleSubtitlesIfNeeded`, `OpenSubtitleIdleTimeoutSettings` (PR-09)
- All `RegionAdjustTrace.*` calls (PR-08)
- Activity-log calls from `CompleteOcr` (`WritePipelineForSlot`) — no-op until PR-06

#### `LiveOverlaySession.OperatorActions.cs` (NEW — PR-03 hint slice only)

**INCLUDE:**
- Hint constants (`HintDurationMs`, `HintResource*`), `HintChanged`, `HintVisible` / `HintResourceKey` / `HintFormatArguments`
- Operator entry points that update **hints**: `StartRecognition`, `StopRecognition`, `HideSubtitles`, `ShowSubtitles`, `CaptureRegionSelected` (+ overload), `CaptureRegionSelectionCancelled`, `Refresh`, `ChangeVoiceSpeed`
- `ExpireHintIfNeeded`, `ClearHint`, and the **hint half** of `WriteOperatorAction` (set hint fields + raise `HintChanged`)

**EXCLUDE:**
- `_activityLog`, `ActivityLog`, `ActivityLogChanged`
- `AppendActivityLogRow`, `WritePipelineForSlot`, `WritePipelineResult`, `WriteDialogueChoiceRow`, `WriteLanguagePackRow`, `RememberVoiceLogRow` / `ClearPendingVoiceLog`
- `NoteOcrMiss` / `NoteMatchMiss` / `NoteVoicePlaybackStarted` / language-pack note APIs (PR-06)
- Types `ActivityLogRow`, `OperatorJob`, `ActivityLogScope` (PR-06) — do not reference them yet
- Make `WriteOperatorAction` stop after hint update (no log append)

#### `MainWindow.xaml`

**INCLUDE from personal:**
- Root `Canvas` `OverlayCanvas` + click-through defaults (`ShowActivated="False"`, `Focusable="False"`, `IsHitTestVisible="False"`)
- Multi-pair capable `SubtitleText` / `HeaderPanel` / `HeaderText` / `PlaybackSpeedBadge` layout (personal moved off single centered Grid)
- Hint host is code-driven via `OverlayHintChrome` (no XAML control required)

**DEFER:**
- `DarkScreenText` element → PR-04 (keep master’s dialogue/header arrangement for dark-screen until rewired, or leave master’s dark-screen drawing path intact)
- Do not copy personal wholesale if it deletes master’s working dark-screen UI without PR-04 session support

**Practical cut:** Port canvas + pair subtitle hosts + hint-ready shell; keep master dark-screen / dialogue-choice drawing until PR-04 replaces it with `DarkScreenBody` / `DialogueChoiceEcho`.

#### `MainWindow.xaml.cs`

**INCLUDE:**
- Construct `LiveOverlaySession` with `ConfigOcrIntervalStore` + `ConfigRegionPairStore` (no idle store)
- `OverlayHintChrome` + `OnHintChanged` / `ApplyHintChrome` + `HintScreenSelection`
- Replace single-region + `RecognitionRegionFallback` OCR path with per-pair capture → `PairFrameSample[]` → `session.Beat(...)` → `TryBeginOcr` / `CompleteOcr`
- Render `session.PairBodies` (multi subtitle)
- Voice via `TakeVoicePlayRequest` (ADR 0007)
- Preview outline rendering (`PreviewChanged`)
- Pair adjust arming UI sufficient for ADR 0005 basics (hit-mode toggle). Prefer **simple** hit-test toggle; full `AdjustMouseGuard` / `RegionAdjustDiagnostics` / `WS_DISABLED` clear → PR-08
- Hotkeys / notify hooks calling session `StartRecognition` / `StopRecognition` / etc.
- `Screenshot` TFM consumers if any API surface changed

**EXCLUDE:**
- `CollectExtraPathSample`, dark-screen/dialogue enqueue into session slots, `ApplyDarkScreenOverlay` / session echo path → PR-04 (retain master scan methods until then)
- `ActivityLogWindow` field / `ShowActivityLog` → PR-06
- Idle timeout construction → PR-09
- `RegionAdjustTrace` / `AdjustMouseGuard` / `RegionAdjustDiagnostics` → PR-08
- `ApplyGame` layout swap beyond whatever settings already did for language packs → PR-05

#### `SettingsWindow.xaml`

**INCLUDE:**
- Tab `Tab_RegionPairs`: `AddRegionPairButton`, `Btn_PreviewAll`, `RegionPairEmptyState`, `RegionPairCards` (+ card template: box capture/display, delete, adjust, voice-primary)
- Config row: `OcrIntervalTextBox`, `OcrIntervalOutOfRangeWarning`
- `VoicePrimaryHint`
- Remove or stop relying on master “second region / Pad as subtitle placement” controls that contradict ADR 0004 (display region boxed). Pad labels may remain only if still used elsewhere; do not keep Region2 UI.

**EXCLUDE:**
- `SubtitleIdleTimeoutTextBox` → PR-09
- `RecognizeDarkScreenSubtitlesCheckBox` / `DarkScreenDisplayRow` / `DialogueOptionScanPanel` / adjust-pin buttons → PR-04 (master may still have a simple dark-screen checkbox — leave master’s simple toggle until PR-04 layout-aware UI)
- `LogDenoiseCheckBox`, `Btn_ActivityLog` → PR-06 / 07a
- ExtraPath_* button rows → PR-04

#### `SettingsWindow.xaml.cs`

**INCLUDE:**
- `RegionPairSettings _pairSettings`, card refresh, `AddRegionPair_Click`, `PreviewAll_Click`, `AdjustRegion_Click`, delete / designate voice-primary, Esc cancel adjust
- `BindOcrIntervalSettings` / `OcrIntervalTextBox_LostFocus` / `UpdateOcrIntervalWarning`
- `UpdateVoicePrimaryHint`

**EXCLUDE:**
- `BindSubtitleIdleTimeoutSettings` → PR-09
- `RefreshAppliedLayoutUi` / extra-path display rows / dark-screen & dialogue handlers → PR-04 / PR-05
- `OpenActivityLog_Click` / `OpenActivityLogRequested` → PR-06
- `LogDenoiseCheckBox` handlers → PR-07a

#### `Config.cs`

**INCLUDE only:**
- `Contains(string key)`
- `Remove(string key)`
(needed by `OverlayLayoutPersistence` / `AppConfigMap`)

Do not drag unrelated churn.

#### `INotifyIcon.cs` (NotifyIcon implementation)

**INCLUDE:**
- `SetSession(LiveOverlaySession)`
- Region-pair box/add helpers: `AddRegionPair`, pair capture/display prompts using `RegionPair_*` masks
- Stop using `Region2` / fallback for recognition

**EXCLUDE:**
- Tray `activityLogItem` / `SetActivityLogOpener` → PR-06
- `BoxDarkScreenDisplay` / `BoxDialogueOptionDisplay` → PR-04

#### `GI-Subtitles.csproj`

**INCLUDE compile/page entries for all PR-03 ADDs; DELETE `RecognitionRegionFallback.cs`.**
Do **not** add ActivityLog / ExtraPath runtime / idle / adjust-trace compiles yet (unless stub ExtraPathSample as above).

#### `GI-Test.csproj`

Add PR-03 tests only; remove `TestRecognitionRegionFallback.cs`.

#### `Screenshot/Screenshot.csproj`

Bump `TargetFrameworkVersion` `v4.5.2` → `v4.8`.

#### `Screenshot/RegionSelectionWindow.*` / `Screenshot.cs`

Include selection **prompt** helper changes used by pair boxing (`Prompt` on selection window) if personal differs from master — required for `RegionPair_Box*Mask` flows.

#### String resources (`Strings.zh-CN.xaml`, `Strings.en-US.xaml`, `Strings.ja-JP.xaml`)

**INCLUDE keys:**
- `Tab_RegionPairs`, `Btn_AddRegionPair`, `Btn_PreviewAll`, `RegionPair_*`, `Btn_BoxRegion`, `Btn_DeletePair`, `Btn_AdjustRegion`, `Btn_SetVoicePrimary`, `Btn_VoicePrimaryOn`
- `Config_OcrInterval_Label`, `Config_OcrInterval_OutOfRange`
- `Hint_*`

**EXCLUDE keys:** `ActivityLog_*`, `Btn_ActivityLog`, `Tray_ActivityLog`, `Config_LogDenoise*`, `ExtraPath_*`, `Config_SubtitleIdleTimeout_*`, and any PR-04-only dark-screen pin copy if not yet wired.

### Tests (exact paths)

- `GI-Test/TestLiveOverlaySessionRegionPairs.cs`
- `GI-Test/TestLiveOverlaySessionVoicePrimary.cs`
- `GI-Test/TestLiveOverlaySessionOcrInterval.cs`
- `GI-Test/TestLiveOverlaySessionHint.cs`
- `GI-Test/TestHintScreenSelection.cs`
- `GI-Test/TestLiveOverlaySessionPreviewAndAdjust.cs` — **only** pair preview/adjust cases; strip extra-path adjust cases or gate them to PR-04
- `GI-Test/TestRegionPairSettings.cs` — pair-only cases

### ADRs

`docs/adr/0001`, `0002`, `0004`, `0005`, `0007` (listed above). **Not** 0003/0006/0008/0009/0010–0015/0013.

### Explicit EXCLUDE

- Activity log window + row model + pipeline logging
- Extra-path runtime (dark-screen/dialogue as session slots) — keep master inline scans working
- Per-game `ApplyGame` swap tests / ADR 0009 narrative (storage files may land early)
- Idle timeout
- AdjustMouseGuard / RegionAdjustTrace / Verify-Release script
- Fold/de-noise dedicated ADR + tests (production SameAs keep may already be in `ApplyPairResult`)
- Fork docs / ADR 0013

### Risk notes

- **Compile break:** any leftover reference to `RecognitionRegionFallback`, `notify.Region2` recognition path, or ActivityLog types.
- **Behavioral gap:** until PR-04, dark-screen/dialogue must keep working via retained master MainWindow paths; do not delete those scans when deleting fallback.
- **Do not** copy personal `MainWindow.xaml.cs` wholesale — it assumes ExtraPath + ActivityLog + Idle + AdjustTrace.
- `ConfigRegionPairStore` on personal always writes `OverlayLayouts`; shipping it in PR-03 means PR-05 is mostly Apply/swap + ADR/tests, not greenfield storage.
- Stacking: PR-04/06/08 all edit `LiveOverlaySession*` — keep PR-03 session surface minimal.

---

## PR-04 — Extra paths

| Field | Value |
| --- | --- |
| Branch | `pr/extra-paths` |
| Title | `feat: dark-screen and dialogue options as extra paths` |
| Depends on | **PR-03** (stack or wait for merge) |
| Size | L |

### Whole-file ADD from personal

- `GI-Subtitles/Core/Overlay/ExtraPathSample.cs`
- `GI-Subtitles/Core/Overlay/ExtraPathBody.cs`
- `docs/adr/0006-dark-screen-and-dialogue-options-are-extra-paths.md`

(Detectors `DarkScreenSubtitleDetector.cs` / `DialogueOptionDetector.cs` already exist on master — TAKE only if personal has required fixes; otherwise leave.)

### Surgical shared files

#### `LiveOverlaySession.cs`

**INCLUDE:**
- Constants `DarkScreenScanIntervalMs`, `DialogueOptionScanIntervalMs`, `DarkScreenOcrSlot`, `DialogueOptionsOcrSlot`, `DialogueChoiceEchoDurationMs`, `DialogueChoiceEchoPrefix`
- State + props: `DarkScreenBody`, `DarkScreenDisplay`, `DialogueOptionDisplay`, `DarkScreenScanOn`, `DialogueOptionScanOn`, `DialogueChoiceEcho`, `AllowsDialogueOptionScan` / `IsAppliedGenshin` (scan gates), `ApplyGame` **scan/display reload pieces** needed so toggles follow applied game (full layout ADR still PR-05)
- `Beat(ExtraPathSample extra, params PairFrameSample[] samples)`, `ApplyExtraPathSample`, `ApplyDarkScreenResult`, `ClearDarkScreen`, `ShowDialogueChoiceEcho`, `ClearEcho` / `ExpireEchoIfNeeded`, `EmitExtraPathVoice`
- `CompleteOcr` branches for dark-screen / dialogue-option slots
- `SetDarkScreenDisplay` / `ClearDarkScreenDisplay` / `SetDialogueOptionDisplay` / `ClearDialogueOptionDisplay` / `SetDarkScreenScan` / `SetDialogueOptionScan`
- `TryToggleDarkScreenDisplayAdjust` / `TryToggleDialogueOptionDisplayAdjust`
- `OverlayAdjustTarget.DarkScreenDisplay` / `DialogueOptionDisplay`
- Preview outlines for extra paths in `BuildPreviewOutlines`
- `VoicePlayRequest` extra-path flag / ctor overload

**EXCLUDE:** activity-log dialogue rows (`WriteDialogueChoiceRow`) until PR-06; idle clear of dark-screen body until PR-09; adjust-trace calls until PR-08.

#### `LiveOverlaySession.OperatorActions.cs`

Only if pipeline logging already present (PR-06 stacked): allow `ActivityLogScope.DarkScreen` / `DialogueOptions` in `WritePipelineForSlot`. If PR-06 not merged, leave no-ops.

#### `MainWindow.xaml`

**INCLUDE:** `DarkScreenText`; ensure `DialogueChoiceText` is the personal detached echo host (not only header sibling).

#### `MainWindow.xaml.cs`

**INCLUDE:**
- `CollectExtraPathSample`, dark-screen / dialogue scan timers feeding `ExtraPathSample`
- `SampleRegionPairsAndMaybeOcr(extra)` → `Beat(extra, samples)`
- `ApplyDarkScreenOverlay`, `ApplyDialogueChoiceEchoOverlay`
- Extra-path voice queue handling (`_pendingExtraPathVoiceKey`, `VoicePlayRequest.ExtraPath`)
- Outline brushes for dark-screen / dialogue preview
- Remove obsolete master “preempt OCR entirely” control flow in favor of shared queue

**EXCLUDE:** ActivityLog window; idle timeout; AdjustMouseGuard-only polish (PR-08) unless required for extra-path display drag.

#### `SettingsWindow.xaml` / `.xaml.cs`

**INCLUDE:**
- `RecognizeDarkScreenSubtitlesCheckBox`, `DarkScreenDisplayRow`, `DarkScreenDisplayStatus`, `AdjustDarkScreenDisplayButton`, box/clear buttons
- `DialogueOptionScanPanel`, `RecognizeDialogueOptionsCheckBox`, `DialogueOptionDisplayRow`, `AdjustDialogueOptionDisplayButton`
- Handlers: `RecognizeDarkScreenSubtitlesCheckBox_Checked`, `RecognizeDialogueOptionsCheckBox_Checked`, `RefreshExtraPathDisplayRows`, `BoxDarkScreenDisplay_Click`, clear/adjust counterparts
- `PreviewAll_Click` passes `darkScreenScanOn`

#### `INotifyIcon.cs`

**INCLUDE:** `BoxDarkScreenDisplay`, `BoxDialogueOptionDisplay` (+ clear helpers if present).

#### `ConfigRegionPairStore.cs`

Ensure dark-screen/dialogue display + scan read/write used (already in personal). Still strip `RegionAdjustTrace` if PR-08 not in stack.

#### `GI-Subtitles.csproj` / `GI-Test.csproj`

Add ExtraPath compiles + tests below.

#### Strings

**INCLUDE:** `ExtraPath_*`, `Config_RecognizeDarkScreenSubtitles*`, `Config_RecognizeDialogueOptions*` (update copy to “not a region pair” wording from personal).

### Tests

- `GI-Test/TestLiveOverlaySessionExtraPaths.cs`
- `GI-Test/TestLiveOverlaySessionExtraPathRepeat.cs`
- Keep using existing `GI-Test/TestDarkScreenSubtitleDetector.cs` (already on master)

### ADRs

`docs/adr/0006-dark-screen-and-dialogue-options-are-extra-paths.md`

### Explicit EXCLUDE

- Activity log window; idle timeout; ADR 0009 packaging if not needed; adjust-trace; fork docs

### Risk notes

- PR-03 MainWindow hybrid (master scans) **must** be replaced carefully — leaving both systems double-fires OCR.
- Dialogue options are Genshin-gated (`AllowsDialogueOptionScan`); wrong gate breaks other games.
- Preview labels 暗屏 / 选项 / 检测带 must not renumber as pairs.

---

## PR-05 — Per-game overlay layout

| Field | Value |
| --- | --- |
| Branch | `pr/overlay-layout-per-game` |
| Title | `feat: persist overlay layout per game` |
| Depends on | PR-03; **preferably PR-04** (extra-path fields live on the layout record) |
| Size | M |

### Whole-file ADD

- `docs/adr/0009-overlay-layout-is-per-game.md`
- If PR-03 did **not** already add them: `OverlayLayoutRecord.cs`, `OverlayLayoutPersistence.cs` (and wire `ConfigRegionPairStore`)

### Whole-file TAKE

Usually none if persistence landed in PR-03 — edit in place.

### Surgical shared files

#### `LiveOverlaySession.cs`

**INCLUDE:** `AppliedGame`, `ApplyGame(string game)` (switch store game, `LoadPairs`, `LoadExtraPathDisplays` / scans, `ClearInFlightOverlayState`), genshin gates tied to applied game not settings dropdown.

#### `ConfigRegionPairStore.cs`

**INCLUDE:** `SwitchGame`, `OverlayLayoutPersistence.TryMigrate` on ctor, per-game read/write (personal tip).

#### `MainWindow.xaml.cs` / `SettingsWindow.xaml.cs`

**INCLUDE:** on confirmed game apply/restart path, call `session.ApplyGame(...)` / `RefreshAppliedLayoutUi` so browsing another title in the dropdown does **not** swap layout until Apply.

#### `IConfigMap` / `Config.Contains` / `Remove`

Already from PR-03; needed for migrate `RemoveGlobal`.

#### Strings

Only if apply/layout help text changed (usually none).

### Tests

- `GI-Test/TestOverlayLayoutPersistence.cs`
- `GI-Test/TestLiveOverlaySessionAppliedGame.cs`

### ADRs

`docs/adr/0009-overlay-layout-is-per-game.md`

### Explicit EXCLUDE

Activity log; idle timeout; fork docs; adjust-trace.

### Risk notes

- Migrating existing user `Region` / `Region2` / `Pad` / flat `RegionPairs` into `OverlayLayouts` is one-shot — test `TryMigrate`.
- If PR-04 missing, layout record still has extra-path fields but UI may not edit them.

---

## PR-06 — Activity log core

| Field | Value |
| --- | --- |
| Branch | `pr/activity-log-core` |
| Title | `feat: activity log window for operator actions and pipeline jobs` |
| Depends on | PR-03; **PR-04 strongly recommended** (scopes DarkScreen / DialogueOptions) |
| Size | L |

### Whole-file ADD from personal

- `GI-Subtitles/Core/Overlay/ActivityLogRow.cs`
- `GI-Subtitles/Core/Overlay/OperatorJob.cs` (includes `ActivityLogScope`)
- `GI-Subtitles/Core/Overlay/ActivityLogResultProjection.cs` — include **composer** (`ActivityLogResultComposer`) for readable result column; colored tag **views** can wait for 07b if you simplify the result cell to plain text in PR-06
- `GI-Subtitles/Views/ActivityLogWindow.xaml`
- `GI-Subtitles/Views/ActivityLogWindow.xaml.cs` — **core append/bind only**; see surgical note
- `docs/adr/0003-activity-log-is-not-the-log4net-file.md`
- `docs/adr/0008-activity-log-rows-are-a-funnel-projection.md`

### Surgical shared files

#### `LiveOverlaySession.OperatorActions.cs`

**INCLUDE full personal logging half:**
- `_activityLog`, `ActivityLog`, `ActivityLogChanged`
- `AppendActivityLogRow`, `WriteOperatorAction` log append, `WritePipelineForSlot` / `WritePipelineResult`
- `NoteOcrMiss`, `NoteMatchMiss`, `NoteVoicePlaybackStarted`, language-pack note APIs
- `WriteDialogueChoiceRow` (needs PR-04)
- `RememberVoiceLogRow` / `ClearPendingVoiceLog` / `IncludeVoiceJob` behavior

#### `LiveOverlaySession.cs`

**INCLUDE:** `CompleteOcr` → `WritePipelineForSlot(...)` + `IsRepeatResult` (repeat flag recorded even if UI de-noise is later).

#### `MainWindow.xaml.cs`

**INCLUDE:** create `ActivityLogWindow`, `ShowActivityLog`, tray/settings opener wiring; call language-pack / voice note APIs where master only logged to log4net.

#### `SettingsWindow.xaml` / `.xaml.cs`

**INCLUDE:** `Btn_ActivityLog` + `OpenActivityLog_Click` / `OpenActivityLogRequested` (not LogDenoise yet).

#### `INotifyIcon.cs`

**INCLUDE:** tray `activityLogItem`, `SetActivityLogOpener`.

#### `ActivityLogWindow.*` (PR-06-shaped)

**INCLUDE:** list columns Time / RegionPair / Job / Result; empty state; subscribe `ActivityLogChanged`; basic projection rows.

**STRIP for later:**
- Copy-on-select mouse handlers / copy menu behavior → 07b
- `ActivityLogResultTextBlock` colored layer → 07b (plain TextBox OK in 06)
- `NewRecordsButton` follow-tail → 07c
- Repeat opacity trigger can wait for 07a (or harmlessly include style without filter)

#### csproj

Add ActivityLog compiles + `Page` for `ActivityLogWindow.xaml`.

#### Strings

**INCLUDE:** `ActivityLog_Title`, columns, scopes (`Global` / `Pair` / `VoicePrimary` / `DarkScreen` / `DialogueOptions`), job labels, basic result keys (`DetectionMiss`, language-pack result keys), `ActivityLog_Empty`, `Btn_ActivityLog`, `Tray_ActivityLog`.

**EXCLUDE:** tag keys / copy / new-records / repeat badge / log-denoise → 07*.

### Tests

- `GI-Test/TestLiveOverlaySessionActivityLog.cs`
- `GI-Test/TestLiveOverlaySessionPipelineLog.cs`
- `GI-Test/TestDuplicatePipelineLogDiagnosis.cs` (if it asserts core pipeline funnel)

### ADRs

`0003`, `0008`

### Explicit EXCLUDE

- `ActivityLogRowFilter`, follow-tail types, result tag colors/controls, copy-on-select tests
- Idle timeout; adjust-trace; fork docs

### Risk notes

- Without PR-04, extra-path scopes never appear — rows still work for pairs/global.
- Personal `ActivityLogWindow` already contains 07b/07c features — **must** trim or reviewers see a huge dump.
- OperatorActions is the worst merge point with PR-03 hint code — re-apply hint+log together carefully.

---

## PR-07a — Fold identical results + match miss keep (+ log de-noise view)

| Field | Value |
| --- | --- |
| Branch | `pr/activity-log-fold-miss` |
| Title | `feat: fold identical results and keep subtitle on match miss` |
| Depends on | PR-06 |
| Size | M |

### Whole-file ADD

- `GI-Subtitles/Core/Overlay/ActivityLogRowFilter.cs`
- `docs/adr/0010-identical-results-fold-log-stays-complete.md`

### Surgical shared files

#### `LiveOverlaySession.cs` / `ApplyPairResult`

Confirm SameAs fold + match-miss keep + forced-miss behavior match ADR 0010 (may already be present since PR-03). Adjust only if PR-03 shipped a simplified apply.

#### `SettingsWindow.xaml` / `.xaml.cs`

**INCLUDE:** `LogDenoiseCheckBox` + handlers; wire filter to activity log window.

#### `ActivityLogWindow.xaml.cs`

Apply `ActivityLogRowFilter` when projecting visible rows (hide repeats; do not delete).

#### Strings

`Config_LogDenoise`, `Config_LogDenoise_Tooltip`, `ActivityLog_RepeatBadge` (if shown).

### Tests

- `GI-Test/TestLiveOverlaySessionResultFold.cs`
- `GI-Test/TestLiveOverlaySessionMatchMissKeep.cs`
- `GI-Test/TestActivityLogRowFilter.cs`

### ADRs

`0010`

### Explicit EXCLUDE

Copy/tags/follow-tail; session features unrelated to fold.

### Risk notes

- De-noise must remain **view-only** (ADR 0008 / 0010) — never skip `AppendActivityLogRow`.

---

## PR-07b — Copy selection + result tags

| Field | Value |
| --- | --- |
| Branch | `pr/activity-log-copy-tags` |
| Title | `feat: activity log copy selection and result tags` |
| Depends on | PR-06 |
| Size | M |

### Whole-file ADD

- `GI-Subtitles/Views/ActivityLogResultTagColors.cs`
- `GI-Subtitles/Views/ActivityLogResultTextBlock.cs`
- `GI-Test/ActivityLogResultComposerHarness.cs`
- `docs/adr/0011-activity-log-selection-per-cell-plus-row-copy.md`
- `docs/adr/0012-activity-log-result-tags-colored-overlay.md`

(Ensure `ActivityLogResultProjection.cs` / composer complete if PR-06 shipped a stub.)

### Surgical shared files

#### `ActivityLogWindow.xaml`

**INCLUDE:** layered result cell (`ActivityLogResultTextBlock` + transparent selection `TextBox`), `LogCopyMenu`, `LogResultSelectionTextBox` style, repeat opacity trigger compatible with colored layer.

#### `ActivityLogWindow.xaml.cs`

**INCLUDE:** copy-on-select mouse-up helpers, Ctrl+C / context menu row copy, selection rules from ADR 0011.

#### Strings

`ActivityLog_Result_Tag_Ocr`, `_Original`, `_Translation`, `ActivityLog_Result_Quoted`, `ActivityLog_Result_MatchMiss`, `ActivityLog_Copy`.

#### csproj

Compile tag color + result text block.

### Tests

- `GI-Test/TestActivityLogResultComposer.cs`
- `GI-Test/TestActivityLogResultColoredLayer.cs`
- `GI-Test/TestActivityLogWindowCopyOnSelect.cs`
- `GI-Test/TestActivityLogWindowAppendProjection.cs` (if projection/tag focused)

### ADRs

`0011`, `0012`

### Explicit EXCLUDE

Follow-tail / virtualization-only fixes (07c); fold filter (07a) except coexistence.

### Risk notes

- Glyph alignment between colored layer and selection TextBox is DPI-sensitive — keep personal template.
- Parallel with 07c: merge `ActivityLogWindow` carefully.

---

## PR-07c — Follow-tail + virtualization

| Field | Value |
| --- | --- |
| Branch | `pr/activity-log-follow-tail` |
| Title | `feat: activity log follow-tail and virtualization fixes` |
| Depends on | PR-06 |
| Size | S–M |

### Whole-file ADD

- `GI-Subtitles/Core/Overlay/ActivityLogFollowTail.cs`
- `docs/adr/0014-follow-tail-is-operator-viewport.md`

### Surgical shared files

#### `ActivityLogWindow.xaml`

**INCLUDE:** `NewRecordsButton`; ListView virtualization attrs (`VirtualizingPanel.*`, pixel scroll) if not already in PR-06.

#### `ActivityLogWindow.xaml.cs`

**INCLUDE:** follow-tail sticky flag wiring; `有新记录` jump + re-arm; ignore extent-only ScrollChanged; operator wheel/scrollbar updates flag.

#### Strings

`ActivityLog_NewRecords`

### Tests

- `GI-Test/TestActivityLogFollowTail.cs`
- `GI-Test/TestActivityLogWindowFollowTailPin.cs`
- `GI-Test/TestActivityLogWindowVirtualization.cs`

### ADRs

`0014`

### Explicit EXCLUDE

Tag/copy/fold features; unrelated session code.

### Risk notes

- WPF `ScrollChanged` false positives are the whole bug — do not “simplify” to scroll-on-every-append.

---

## PR-08 — Region adjust polish (+ Debug trace)

| Field | Value |
| --- | --- |
| Branch | `pr/region-adjust-polish` |
| Title | `feat: arm-once region adjust and Debug-only adjust trace` |
| Depends on | PR-03 (extra-path adjust targets better with PR-04) |
| Size | M |

### Whole-file ADD

- `GI-Subtitles/Core/Overlay/AdjustMouseGuard.cs`
- `GI-Subtitles/Core/Overlay/RegionAdjustTrace.cs`
- `GI-Subtitles/Views/RegionAdjustDiagnostics.cs`
- `scripts/Verify-Release-No-AdjustTrace.ps1` — **scrub fork issue numbers** before upstream
- Update `docs/adr/0005-overlay-click-through-except-region-adjust.md` only if decision text must change

### Surgical shared files

#### `MainWindow.xaml.cs`

**INCLUDE:**
- `ApplyOverlayHitMode`, `WS_DISABLED` clear helper
- Mouse down/move/up paths using `AdjustMouseGuard.DownExitReason` / `MoveExitReason` / `UpExitReason`
- `RegionAdjustDiagnostics.HitModeApplied` / `DisabledBitCleared`
- Arm-once / either-frame drag behavior for pair (+ extra-path displays if PR-04 present)

#### `LiveOverlaySession.cs` / `ConfigRegionPairStore.cs`

**INCLUDE:** restore `RegionAdjustTrace` arm/capture/display/store-write calls from personal.

#### csproj

Compile the three new types; no Release-only defines that leave trace strings if script forbids them.

### Tests

- `GI-Test/TestAdjustMouseGuard.cs`
- `GI-Test/TestRegionAdjustTrace.cs`

### ADRs

Update `0005` only if needed. **Never** ADR 0013.

### Explicit EXCLUDE

Fork release runbooks; activity-log work; idle timeout.

### Risk notes

- Trace must be Debug-only; run verify script on Release.
- Click-through regressions are user-visible — manual smoke required.

---

## PR-09 — Subtitle idle timeout

| Field | Value |
| --- | --- |
| Branch | `pr/subtitle-idle-timeout` |
| Title | `feat: subtitle idle timeout clears fold and bodies` |
| Depends on | PR-03; PR-04 for dark-screen body clear; PR-07a if fold semantics already upstream |
| Size | M |

### Whole-file ADD

- `GI-Subtitles/Core/Overlay/ISubtitleIdleTimeoutStore.cs`
- `GI-Subtitles/Core/Overlay/ConfigSubtitleIdleTimeoutStore.cs`
- `docs/adr/0015-subtitle-idle-timeout-clears-fold.md`

### Surgical shared files

#### `LiveOverlaySession.cs`

**INCLUDE:**
- Constants `Default/Min/MaxSubtitleIdleTimeoutSeconds`, `SubtitleIdleTimeoutConfigKey`
- `_idleTimeoutStore`, `_subtitleIdleTimeoutSeconds`, `_pairLastAppliedAt`, `_darkScreenLastAppliedAt`
- `SubtitleIdleTimeoutSeconds`, `SetSubtitleIdleTimeoutSeconds`, `ClampSubtitleIdleTimeoutSeconds`
- `OpenSubtitleIdleTimeoutSettings` / `SubtitleIdleTimeoutSettingsView` / `ApplyCommittedSubtitleIdleTimeout`
- `NotePairApplied` / `NoteDarkScreenApplied`, `ExpireIdleSubtitlesIfNeeded`, `ClearPairSubtitle`, `ClearDarkScreenSubtitleBody` (clears fold via `_lastResults[i]=null`)
- Call expire from `Tick` / `Beat` / `CompleteOcr` the same way personal does
- Ctors taking `ISubtitleIdleTimeoutStore`

**EXCLUDE:** do not clear dialogue-choice echo via idle timeout (echo keeps fixed duration).

#### `MainWindow.xaml.cs`

Pass `ConfigSubtitleIdleTimeoutStore` into session ctor; ensure UI timer calls `session.Tick()`.

#### `SettingsWindow.xaml` / `.xaml.cs`

**INCLUDE:** `SubtitleIdleTimeoutTextBox`, bind/commit handlers, help label.

#### Strings

`Config_SubtitleIdleTimeout_Label`, `_Unit`, `_Help`

#### csproj

Compile idle-timeout store types + tests.

### Tests

- `GI-Test/TestLiveOverlaySessionSubtitleIdleTimeout.cs`
- `GI-Test/TestLiveOverlaySessionSubtitleIdleTimeoutSettings.cs`

### ADRs

`0015`

### Explicit EXCLUDE

Fork docs; activity-log rows on expiry (must **not** write log rows); adjust-trace.

### Risk notes

- Expiry must clear fold so the same line can reappear (ADR 0015).
- Zero means off.
- Without PR-04, dark-screen body clear path may be incomplete.

---

## Cross-PR entanglement cheat sheet (`LiveOverlaySession`)

| Concern | Earliest PR | Personal symbols |
| --- | --- | --- |
| Pairs + OCR queue + voice-primary | 03 | `Beat(PairFrameSample[])`, `ApplyPairResult`, `SetVoicePrimary` |
| Hint overlay | 03 | OperatorActions hint half + `HintScreenSelection` |
| OCR interval settings | 03 | `IOcrIntervalStore`, `OcrIntervalSettingsView` |
| Extra-path pipeline | 04 | `ExtraPathSample`, `ApplyExtraPathSample`, slot −1/−2 |
| Per-game layout swap | 05 | `ApplyGame`, `OverlayLayoutPersistence` |
| Activity log writes | 06 | `WritePipelineForSlot`, `AppendActivityLogRow` |
| Fold / match-miss / de-noise view | 07a | `PairRecognitionResult.SameAs`, `ActivityLogRowFilter` |
| Result tags / copy | 07b | composer tags + window controls |
| Follow-tail | 07c | `ActivityLogFollowTail` |
| Adjust trace / mouse guard | 08 | `RegionAdjustTrace`, `AdjustMouseGuard` |
| Idle timeout | 09 | `ExpireIdleSubtitlesIfNeeded`, `ISubtitleIdleTimeoutStore` |

**Hardest cut:** PR-03 must invent a session that compiles **without** dragging OperatorActions activity-log types or ExtraPath runtime, while MainWindow must **not** lose master dark-screen/dialogue until PR-04. Prefer authoring a reduced `LiveOverlaySession*.cs` over copying personal tip files.

---

## Global forbid reminder

Never transplant: `docs/agents/**`, `AGENTS.md`, `CONTEXT.md`, `.agents/`, `skills-lock.json`, ADR **0013**, fork README banners, `-fork.N` release notes, this manifest.
