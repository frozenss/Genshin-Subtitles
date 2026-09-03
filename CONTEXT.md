# Live subtitle overlay

GI-Subtitles captures in-game text, matches it to a language pack, and shows a translation over the game. Optional voice playback follows one designated capture.

## Language

**Overlay**:
The always-on-top layer drawn over the game. It can show subtitles and short-lived hints; it is not a log.
_Avoid_: Main window, 主窗口 (that name also fits the settings window)

**Hint**:
A short-lived overlay notice that a hotkey or button ran.
_Avoid_: 操作日志, 侧边框, log (the overlay is not a log); Capture region preview (that outline is not a hint)

**Capture region preview**:
A short-lived outline drawn over the capture region so the operator can see where OCR will read.
_Avoid_: Hint, 按键提示 (those name the text confirmation of an action)

**Activity log**:
The complete visible record of what the pipeline did: operator actions, background jobs, detection results, and matched subtitle lookups.
_Avoid_: 操作日志 when you mean a hint; debug console; log4net log (the file logger is not this)

**Capture region**:
The screen rectangle OCR reads from.
_Avoid_: 识别框 as the name of a pair; Region2 when you mean a second pair

**Display region**:
The screen rectangle where that capture's subtitle is placed.
_Avoid_: 字幕窗 when you mean the pair; overlay when you mean placement rather than the window

**Region pair**:
One capture region bound to one display region.
_Avoid_: Secondary region, Region2 (those names currently mean a one-shot fallback probe)

**Fallback probe**:
A one-shot secondary capture used after repeated failures on the primary capture, then abandoned.
_Avoid_: Second region pair; 第二识别区域 when describing today's Region2 behaviour

**Voice-primary region**:
The one capture region allowed to trigger voice playback.
_Avoid_: 主区域 without 配音; primary region when you mean the fallback probe's primary capture

**OCR cadence**:
The live timing stack that decides how often frames are sampled and when OCR is allowed to run.
_Avoid_: 识图频率 (that name implies a single Hz)
