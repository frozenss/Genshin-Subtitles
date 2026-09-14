# Upstream PR progress (fork-only)

Living handoff for slicing `personal` into clean `pr/*` heads for `qew21/Genshin-Subtitles`.
**Do not** open a PR that adds this file upstream.

## Session close rule (mandatory)

At the **end of every agent/user conversation** that touches upstream-PR work (planning, branch cuts, review, submit):

1. Update **this file** before the turn ends: Status, Done, In progress, Next actions, Blockers / confirmations, Branch table, and “Last updated”.
2. Keep claims honest: only mark Done when tool output or review supports it.
3. If the working tree has progress-doc or plan/manifest edits worth keeping across sessions, **commit and push them to `origin/personal`** (fork only — never write to `upstream`). Mention the commit SHA in “Last updated”.
4. Do **not** open upstream Issues/PRs unless the user explicitly starts a submit session.
5. Point readers at the canonical docs below instead of duplicating long inventories here.

New sessions: read this file first, then the linked docs for the next action.

## Canonical docs (read in order)

| Doc | Role |
| --- | --- |
| [`upstream-pr-progress.md`](upstream-pr-progress.md) | **This file** — status, next step, confirmations |
| [`upstream-pr-plan.md`](upstream-pr-plan.md) | Ground rules, forbid list, PR-01…09 inventory, submit checklist |
| [`upstream-pr-file-manifest.md`](upstream-pr-file-manifest.md) | Precise allow/exclude paths and surgical cuts for PR-03…09 |
| [`git-branches.md`](git-branches.md) | `personal` / `master` / `pr/<topic>` conventions |

Grill consensus (settled): full upstreamable surface over time; doc-first batches; Issue+PR together (Draft for large); English for PR/branch surfaces; hard-exclude fork/AI/agent/`-fork.N`/ADR 0013; build/CI as small separate PRs; organize locally and review before a dedicated submit session.

## Status

**Phase:** **Submit opened** against `qew21/Genshin-Subtitles`. English Issues + PRs created with cross-links. Gap review previously clean.

**Overall:** Independent Ready chores + Draft product stack:

`#31` → `#32` → `#33` → `#34` → `#35` → `#36` → `#37` → `#38` → `#39` (plus Ready `#29`, `#30`).

ExtraPath-early on PR-03 remains **accepted**.

**PR-07b rewrite (done 2026-09-14):** Upstream `#36` / `pr/activity-log-copy-tags` tip `57e609f` now ships ADR **0011** + **0016** (category stripes); ADR **0012** kept superseded. Dependents restacked: `#37` `70ce2d2`, `#38` `08a61d6`, `#39` `37702d6`. Issue `#25` + PR `#36` titles/bodies updated in English.

## Upstream Issue ↔ PR map

| Series | Branch tip | Issue | PR | State | Depends on PR |
| --- | --- | --- | --- | --- | --- |
| PR-01 MSBuild | `1188d58` `pr/msbuild-script` | [#18](https://github.com/qew21/Genshin-Subtitles/issues/18) | [#29](https://github.com/qew21/Genshin-Subtitles/pull/29) | Ready | — |
| PR-02 Deploy guard | `378d430` `pr/deploy-repo-guard` | [#19](https://github.com/qew21/Genshin-Subtitles/issues/19) | [#30](https://github.com/qew21/Genshin-Subtitles/pull/30) | Ready | — |
| PR-03 Region pairs | `a9ecd04` `pr/region-pairs-core` | [#20](https://github.com/qew21/Genshin-Subtitles/issues/20) | [#31](https://github.com/qew21/Genshin-Subtitles/pull/31) | Draft | — (stack root) |
| PR-04 Extra paths | `8f8fdd2` `pr/extra-paths` | [#21](https://github.com/qew21/Genshin-Subtitles/issues/21) | [#32](https://github.com/qew21/Genshin-Subtitles/pull/32) | Draft | #31 |
| PR-05 Overlay layout | `6dfefa4` `pr/overlay-layout-per-game` | [#22](https://github.com/qew21/Genshin-Subtitles/issues/22) | [#33](https://github.com/qew21/Genshin-Subtitles/pull/33) | Draft | #32 |
| PR-06 Activity log core | `6c5704c` `pr/activity-log-core` | [#23](https://github.com/qew21/Genshin-Subtitles/issues/23) | [#34](https://github.com/qew21/Genshin-Subtitles/pull/34) | Draft | #33 |
| PR-07a Fold/miss | `d3b7ff4` `pr/activity-log-fold-miss` | [#24](https://github.com/qew21/Genshin-Subtitles/issues/24) | [#35](https://github.com/qew21/Genshin-Subtitles/pull/35) | Draft | #34 |
| PR-07b Copy/tags/stripes | `57e609f` `pr/activity-log-copy-tags` | [#25](https://github.com/qew21/Genshin-Subtitles/issues/25) | [#36](https://github.com/qew21/Genshin-Subtitles/pull/36) | Draft (ADR 0016) | #35 |
| PR-07c Follow-tail | `70ce2d2` `pr/activity-log-follow-tail` | [#26](https://github.com/qew21/Genshin-Subtitles/issues/26) | [#37](https://github.com/qew21/Genshin-Subtitles/pull/37) | Draft | #36 |
| PR-08 Adjust polish | `08a61d6` `pr/region-adjust-polish` | [#27](https://github.com/qew21/Genshin-Subtitles/issues/27) | [#38](https://github.com/qew21/Genshin-Subtitles/pull/38) | Draft | #37 |
| PR-09 Idle timeout | `37702d6` `pr/subtitle-idle-timeout` | [#28](https://github.com/qew21/Genshin-Subtitles/issues/28) | [#39](https://github.com/qew21/Genshin-Subtitles/pull/39) | Draft | #38 |

Cross-link map also posted on upstream [#18](https://github.com/qew21/Genshin-Subtitles/issues/18) and [#20](https://github.com/qew21/Genshin-Subtitles/issues/20); each Issue has an Implementation PR comment; each PR body has **Closes / Depends on PR / Next PR**.

**Suggested merge order:** `#29` and `#30` anytime; product stack `#31` → … → `#39`.

## Done

| Item | Tip | Notes |
| --- | --- | --- |
| Organize PR-01…09 | see branch table | Agent tests per segment; ExtraPath-early accepted |
| Synth gap review | vs `personal` | No real product gaps |
| Upstream Issues #18–#28 | English | Paired 1:1 with PRs |
| Upstream PRs #29–#39 | English | Ready: #29–#30; Draft: #31–#39 |
| Rewrite `#36` onto ADR 0016 | `57e609f` | One clean commit from `#35`; force-pushed; Issue/PR English updated |
| Restack `#37`–`#39` | `70ce2d2` / `08a61d6` / `37702d6` | Force-pushed; restack comments on PRs |
| Validate activity-log tests | — | `#36`: 31 passed; `#37`: 46 passed (`FullyQualifiedName~ActivityLog`) |

## In progress

| Item | Owner / note |
| --- | --- |
| Upstream review / merge | Maintainer; babysit only if user asks |

## Next actions

1. Respond to upstream review on Ready `#29`/`#30` and Draft stack starting at `#31`.
2. After merges: ff `master` ← `upstream/master`, merge into `personal`.
3. Do not mark Drafts Ready unless user asks or review requests it.

## Blockers / confirmations

| When | Confirm before acting |
| --- | --- |
| Mark Draft → Ready | User ask or maintainer request |
| Force-push / restack `pr/*` | User ask (e.g. after parent squash-merge) |

## Branch table (fork `origin`)

| Branch | Tip | Upstream Issue/PR | Notes |
| --- | --- | --- | --- |
| `pr/msbuild-script` | `1188d58` | #18 / #29 | Ready |
| `pr/deploy-repo-guard` | `378d430` | #19 / #30 | Ready |
| `pr/region-pairs-core` | `a9ecd04` | #20 / #31 | Draft stack root |
| `pr/extra-paths` | `8f8fdd2` | #21 / #32 | Draft |
| `pr/overlay-layout-per-game` | `6dfefa4` | #22 / #33 | Draft |
| `pr/activity-log-core` | `6c5704c` | #23 / #34 | Draft |
| `pr/activity-log-fold-miss` | `d3b7ff4` | #24 / #35 | Draft |
| `pr/activity-log-copy-tags` | `57e609f` | #25 / #36 | Draft; ADR 0011+0016 |
| `pr/activity-log-follow-tail` | `70ce2d2` | #26 / #37 | Draft; restacked |
| `pr/region-adjust-polish` | `08a61d6` | #27 / #38 | Draft; restacked |
| `pr/subtitle-idle-timeout` | `37702d6` | #28 / #39 | Draft tip; restacked |

`master` last known ff of `upstream/master`: `cda7fa6`.

## Last updated

- **When:** 2026-09-14 (submit session: rewrite `#36` to ADR 0016; restack `#37`–`#39`)
- **By:** parent agent; force-pushed tips; English `#25`/`#36`; plan/manifest/progress + CONTEXT category stripe on personal
- **personal docs tip:** see commit after this file is pushed
