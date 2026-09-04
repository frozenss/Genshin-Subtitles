# Live subtitle overlay

GI-Subtitles captures in-game text, matches it to a language pack, and shows a translation over the game. Optional voice playback follows one designated capture.

## Language

**Overlay**:
The always-on-top layer drawn over the game. It can show subtitles and short-lived hints; it is not a log.
_Avoid_: Main window, 主窗口 (that name also fits the settings window)

**Hint**:
A short-lived overlay notice that a hotkey or button ran. It is not a notice that OCR produced a new subtitle.
_Avoid_: 操作日志, 侧边框, log (the overlay is not a log); Region-pair preview (that outline is not a hint); announcing each OCR or subtitle change

**Region-pair preview**:
A short-lived outline of every region pair's capture region and display region, drawn together over the game. It also outlines a set dark-screen display or dialogue-option display, and the live dark-screen candidate when that scan is on and no dark-screen display is set.
_Avoid_: Capture region preview (that named a capture-only outline); Hint; 预览全部 as a second concept (that is the button label for this)

**Activity log**:
The complete visible record of what the pipeline did this session: operator actions, background jobs, detection results, and matched subtitle lookups.
_Avoid_: 操作日志 when you mean a hint; debug console; log4net log (the file logger is not this); overlay when you mean this record

**Capture region**:
The screen rectangle OCR reads from.
_Avoid_: 识别框 as the name of a pair; Region2 when you mean a second pair

**Display region**:
The screen rectangle where that capture's subtitle is placed. It is a rectangle of its own, not an offset of the capture region.
_Avoid_: 字幕窗 when you mean the pair; overlay when you mean placement rather than the window; Pad, 字幕偏移 when you mean this rectangle

**Display-region adjust**:
The settings-armed state in which one display region accepts mouse drag — a region pair's, or the optional dark-screen display or dialogue-option display. Off, the overlay is click-through.
_Avoid_: dragging subtitles at any time; hover handle; treating this as a hint

**Region pair**:
One capture region bound to one display region. The live overlay keeps a list of pairs; every pair with a valid capture region runs at the same time.
_Avoid_: Secondary region, Region2, 第二识别区域 (those named a one-shot fallback, not a pair)

**Fallback probe**:
Today's one-shot secondary capture after repeated misses on the primary capture, then abandoned. The region-pair model does not include this; a second pair is a real pair.
_Avoid_: Second region pair; 第二识别区域

**Voice-primary region**:
The one capture region among region pairs allowed to trigger voice playback. The operator designates which region pair that capture belongs to; it defaults to the first pair. Other pairs never speak. Dark-screen subtitles and the dialogue-choice echo may still speak when voice playback is on; they do not change this designation.
_Avoid_: 主区域 without 配音; primary region when you mean the fallback probe's primary capture; letting another pair speak because this one is empty

**OCR cadence**:
The live timing stack that samples frames on one clock, diffs each capture region against its own previous frame, and runs OCR only for regions that changed. Dark-screen scan and dialogue-option scan ride the same clock and the same serial OCR queue; they do not pause region pairs. OCR is one engine on one queue.
_Avoid_: 识图频率 (that name implies a single Hz); per-pair timer; using this name for the settings-window 最小识图间隔 control (that lever is OCR interval); whole-beat preemption

**OCR interval**:
The minimum time between OCR engine runs on the single serial queue. In the settings window this lever is labelled 最小识图间隔; it is the one user-facing control of OCR cadence.
_Avoid_: 识图频率; OCR cadence (that name is the whole stack); per-pair interval

**Dark-screen scan**:
An extra capture on the OCR cadence that hunts a central text band on a mostly-dark frame. It is not a region pair.
_Avoid_: treating it as a region pair; Region2; skipping all pairs for the rest of the beat

**Dark-screen display**:
The optional persistent rectangle for dark-screen subtitles. Unset, the text follows the detected candidate band.
_Avoid_: a fifth region pair; binding dark-screen text to a pair's display region

**Dialogue-option scan**:
A Genshin-only extra capture that locates the right-side choice list. It is not a region pair. Off by default.
_Avoid_: overlay-translating every option in place; treating it as a region pair; skipping all pairs for the rest of the beat

**Dialogue-choice echo**:
The short-lived translation of the option the operator just selected. Default: one extra line above the voice-primary pair's subtitle body, not a replacement. A set dialogue-option display detaches it there.
_Avoid_: Hint; replacing the voice-primary subtitle body; showing the echo in two places at once

**Dialogue-option display**:
The optional persistent rectangle for the dialogue-choice echo. Unset, the echo stays on the voice-primary pair. Set, the echo appears only there.
_Avoid_: a region pair; duplicating the echo on the voice-primary pair
