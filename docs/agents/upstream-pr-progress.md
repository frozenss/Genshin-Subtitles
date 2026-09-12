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

Grill consensus (already settled): full upstreamable surface over time; doc-first batches; Issue+PR together (Draft for large); English for PR/branch surfaces; hard-exclude fork/AI/agent/`-fork.N`/ADR 0013; build/CI as small separate PRs; organize locally and review before a dedicated submit session.

## Status

**Phase:** Organize clean `pr/*` branches on the fork (no upstream Issue/PR yet).

**Overall:** PR-01 and PR-02 pushed to `origin`. Manifest written. **PR-03 region-pairs-core cut in progress** (subagent).

## Done

| Item | Evidence |
| --- | --- |
| Plan doc | `docs/agents/upstream-pr-plan.md` |
| File transplant manifest PR-03…09 | `docs/agents/upstream-pr-file-manifest.md` |
| `origin/pr/msbuild-script` | `1188d58` — `chore: add VS MSBuild build script` (no fork README banner) |
| `origin/pr/deploy-repo-guard` | `378d430` — deploy `github.repository` guard only (no `-fork.N` notes) |
| Parent review of PR-01/02 | Diffs match allow lists; forbid strings absent |

## In progress

| Item | Owner / note |
| --- | --- |
| `pr/region-pairs-core` | Subagent building from `master` per manifest (surgical `LiveOverlaySession`, no wholesale personal tip copy) |

## Next actions (after PR-03 lands)

1. Parent-review `pr/region-pairs-core`: forbid list, manifest INCLUDE/EXCLUDE, build/`GI-Test` for region-pair / hint / OCR-interval.
2. Update this progress file + branch table with tip SHA.
3. Only then schedule PR-04+ (stack on reviewed PR-03 head; do not parallelize with PR-03).
4. Submit session (later, explicit user ask): upstream Issue+PR per plan checklist — start with small Ready PRs (01/02), Draft for PR-03.

## Blockers / confirmations

| When | Confirm before acting |
| --- | --- |
| Before upstream submit session | User explicitly asks to open Issues/PRs on `qew21/Genshin-Subtitles` |
| PR-01 local Release build on naked `master` | May hit Screenshot `v4.5.2` reference assemblies on some machines; TFM `v4.8` is planned with **PR-03** — do not silently expand PR-01 |
| PR-03 scope disputes | Re-read manifest “EXCLUDE / stub” for extra-paths, activity-log, idle timeout, `RegionAdjustTrace` |
| Parallel product PRs | **Not allowed** until PR-03 is reviewed; 07a/b/c parallel only after PR-06 is stable |

Nothing else is waiting on the user for the current organize phase unless the PR-03 subagent reports an ambiguity.

## Branch table (fork `origin`)

| Branch | Tip | Upstream Issue/PR | Notes |
| --- | --- | --- | --- |
| `pr/msbuild-script` | `1188d58` | — | Ready for later submit |
| `pr/deploy-repo-guard` | `378d430` | — | Ready for later submit |
| `pr/region-pairs-core` | _(pending)_ | — | In progress |
| `pr/extra-paths` … `pr/subtitle-idle-timeout` | — | — | Not started |

`master` should remain ff-only of `upstream/master` (last known `cda7fa6`).

## Last updated

- **When:** 2026-09-12 (progress doc created; PR-03 subagent running; fork agent docs committing to `origin/personal`)
- **By:** agent session after user asked for progress handoff doc + PR-03 kickoff
- **personal docs commit:** pending in same turn (see tip after push)
