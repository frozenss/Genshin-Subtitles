# Live subtitle overlay

GI-Subtitles captures in-game text, matches it to a language pack, and shows a translation over the game. Optional voice playback follows one designated capture.

## Language

**Overlay**:
The always-on-top layer drawn over the game. It can show subtitles and short-lived hints; it is not a log.
_Avoid_: Main window, 主窗口 (that name also fits the settings window)

**Game**:
A title the operator selects. Each game owns one overlay layout and its own language packs. The running overlay uses the last applied game; browsing another title in settings does not swap the layout.
_Avoid_: treating the overlay layout as shared across titles; calling the language-pack files the overlay layout; swapping layout on the game dropdown; requiring a restart to change game

**Overlay layout**:
One game's region pairs, voice-primary designation, extra-path displays, and extra-path scan toggles. Switching games swaps this layout; it is not shared.
_Avoid_: Config; RegionPairs as the name of this concept; per-game settings when you mean language-pack URLs; a second overlay

**Hint**:
A short-lived overlay notice that a hotkey or button ran. It is not a notice that OCR produced a new subtitle, and it is not an activity log row. It appears on one monitor — the one carrying the voice-primary region's display, falling back to the primary monitor only when no region is set — never straddling two monitors.
_Avoid_: 操作日志, 侧边框, log (the overlay is not a log); Region-pair preview (that outline is not a hint); announcing each OCR or subtitle change; treating hint expiry as a new row; pinning the hint to the primary screen when a display region exists; centering the hint on the combined desktop

**Region-pair preview**:
A short-lived outline of every region pair's capture region and display region, drawn together over the game. It also outlines a set dark-screen display (labelled 暗屏) or dialogue-option display (labelled 选项), and the live dark-screen candidate (labelled 检测带) when that scan is on and no dark-screen display is set. Extra paths are not numbered as pairs.
_Avoid_: Capture region preview (that named a capture-only outline); Hint; 预览全部 as a second concept (that is the button label for this); labelling extra paths 对 N

**Activity log**:
The complete visible record of what the pipeline did this session: operator actions, background jobs, detection results, and matched subtitle lookups.
_Avoid_: 操作日志 when you mean a hint; debug console; log4net log (the file logger is not this); overlay when you mean this record

**Activity log row**:
One snapshot in the activity log: the time, which region pair or extra path (or a global row), the job or operator action, and the result. Deleting or renumbering pairs later does not rewrite it. The job is not the hint copy (识别中 is a result, not the job name).
_Avoid_: Hint; a live pointer to a region pair; putting the window's empty-state sentence in the log

**Global row**:
An activity log row that is not about one region pair or extra path. Start/stop recognition, hide/show subtitles, and voice speed are this. The window labels the region-pair column 全局.
_Avoid_: labelling 暗屏 or 对话选项 as 全局; a boxing row when a pair was actually boxed

**Recognition result**:
What one pipeline run concluded for one capture: no text found, text the pack could not match, or the matched subtitle with its header and content. Two runs with different OCR text can conclude the same result.
_Avoid_: OCR text (that is the run's input); 查询结果 when you mean only the matched subtitle

**Result tag**:
The bracketed marker that opens one result line of an activity log row, naming which pipeline output the line carries: OCR text, the matched source, or the translation. A dual-output translation is still one tagged line whose content stacks the two languages; the detection-miss line carries no tag, and the match-miss line borrows the source tag.
_Avoid_: coloring the whole line when you mean the tag; a second tag for the second output language; putting a tag on the detection-miss line; counting the tag as part of the job

**Result fold**:
Keeping the current subtitle and skipping voice replay when a run's recognition result is the same as what that pair already shows. The run itself still happens and is still recorded; only the re-apply is skipped.
_Avoid_: caching the query result (the match cache is a forever map from OCR text; this is one pair's latest result, replaced by the next different one); turning this off with the log de-noise checkbox (that lever is view-only); treating subtitle idle timeout expiry as a fold (expiry clears the fold so the same line can return)

**Subtitle idle timeout**:
A global preference, in whole seconds, for how long a region-pair or dark-screen subtitle body may stay after its last newly applied recognition result before that surface is cleared and its result fold forgotten. Zero means off (bodies stay until replaced or otherwise cleared). Each surface times out on its own clock. It is not the operator hide/show mute, and it does not cover the dialogue-choice echo, hints, or region-pair preview.
_Avoid_: 隐藏字幕; 字幕自动隐去 as the canonical name; applying it to the dialogue-choice echo (that keeps its own fixed duration); writing an activity log row on expiry; stopping voice when a body clears; pausing the clock because recognition stopped; one shared clock that wipes every surface together

**Repeat row**:
An activity log row whose recognition result is the same as that pair's previous row. It is recorded like any other row; hiding it is a view choice and never a deletion.
_Avoid_: a double-write bug; 重复识别 as a separate job kind

**Log de-noise**:
The default-on settings checkbox that hides repeat rows in the activity log window. It changes what the window shows, not what the log records, and it does not touch the overlay.
_Avoid_: a second filtered copy of the log; applying it to the result fold; deleting rows at write time

**Copy-on-select**:
The activity log window's rule that a mouse-made, non-empty in-cell text selection is copied to the clipboard the moment the left button is released, silently. Plain clicks, keyboard selections, and row selection never auto-copy; whole rows copy through Ctrl+C or the context menu.
_Avoid_: the settings window's click-a-language-pack-URL-to-copy (a different feature); auto-copying a row on click; a visible copy confirmation

**Follow-tail**:
The activity log window's rule that the viewport stays pinned to the newest visible content only while it is already at the bottom — including after the operator jumps there with 有新记录. While the operator has scrolled up to read older rows, updates must not move the viewport.
_Avoid_: scrolling the list on every update regardless of position; per-region-pair scroll panes; treating 有新记录 as a badge that does not return the operator to the bottom and resume pinning

**Capture region**:
The screen rectangle OCR reads from.
_Avoid_: 识别框 as the name of a pair; Region2 when you mean a second pair

**Display region**:
The screen rectangle where that capture's subtitle is placed. It is a rectangle of its own, not an offset of the capture region.
_Avoid_: 字幕窗 when you mean the pair; overlay when you mean placement rather than the window; Pad, 字幕偏移 when you mean this rectangle

**Region adjust**:
The settings-armed state in which one existing region accepts mouse drag — a region pair's capture region or display region (grabbing a box drags that box's own rectangle), or the optional dark-screen display or dialogue-option display (display only; their bands are detected, not user rectangles). It does not create a rectangle. Off, the overlay is click-through.
_Avoid_: Display-region adjust (the pre-extension name; it now drags captures too); dragging subtitles at any time; hover handle; treating this as a hint; using adjust to pull the first dark-screen or dialogue-option display; dragging a detected band

**Region pair**:
One capture region bound to one display region. The live overlay keeps the current game's list of pairs; every pair with a valid capture region runs at the same time.
_Avoid_: Secondary region, Region2, 第二识别区域 (those named a one-shot fallback, not a pair); one shared pair list across games

**Fallback probe**:
Today's one-shot secondary capture after repeated misses on the primary capture, then abandoned. The region-pair model does not include this; a second pair is a real pair.
_Avoid_: Second region pair; 第二识别区域

**Voice-primary region**:
The region pair whose capture may trigger voice playback. Designation is that pair, not a list index: default is the first pair; an empty list has none; deleting the designated pair rebinds to the first remaining pair; deleting another pair does not move it. Other pairs never speak. Dark-screen subtitles and the dialogue-choice echo may still speak when voice playback is on; they do not change this designation.
_Avoid_: 主区域 without 配音; primary region when you mean the fallback probe's primary capture; letting another pair speak because this one is empty; treating the designation as “whoever is currently index 0”

**OCR cadence**:
The live timing stack that samples frames on one clock, diffs each capture region against its own previous frame, and runs OCR only for regions that changed. Dark-screen scan and dialogue-option scan ride the same clock and the same serial OCR queue; they do not pause region pairs. OCR is one engine on one queue.
_Avoid_: 识图频率 (that name implies a single Hz); per-pair timer; using this name for the settings-window 最小识图间隔 control (that lever is OCR interval); whole-beat preemption

**OCR interval**:
The minimum time between OCR engine runs on the single serial queue. In the settings window this lever is labelled 最小识图间隔; it is the one user-facing control of OCR cadence.
_Avoid_: 识图频率; OCR cadence (that name is the whole stack); per-pair interval

**Dark-screen scan**:
An extra capture on the OCR cadence that hunts a central text band on a mostly-dark frame. It is not a region pair. On or off is this game's overlay layout, not a global preference.
_Avoid_: treating it as a region pair; Region2; skipping all pairs for the rest of the beat; one on/off for every game

**Dark-screen display**:
The optional persistent rectangle for dark-screen subtitles. Unset, the text follows the detected candidate band.
_Avoid_: a fifth region pair; binding dark-screen text to a pair's display region

**Dialogue-option scan**:
A Genshin-only extra capture that locates the right-side choice list. It is not a region pair. Off by default. Its on/off and display belong to Genshin's overlay layout; other games do not scan or draw it.
_Avoid_: overlay-translating every option in place; treating it as a region pair; skipping all pairs for the rest of the beat; leaving the boxed display on screen after leaving Genshin

**Dialogue-choice echo**:
The short-lived translation of the option the operator just selected. Default: one extra line above the voice-primary pair's subtitle body, not a replacement. A set dialogue-option display detaches it there.
_Avoid_: Hint; replacing the voice-primary subtitle body; showing the echo in two places at once; putting it under subtitle idle timeout (echo keeps its own fixed duration)

**Dialogue-option display**:
The optional persistent rectangle for the dialogue-choice echo. Unset, the echo stays on the voice-primary pair. Set, the echo appears only there.
_Avoid_: a region pair; duplicating the echo on the voice-primary pair
