# Upstream PR plan (fork-only)

Plan for contributing product work from `personal` to `qew21/Genshin-Subtitles`.
This file stays on the fork (`docs/agents/`). Do not open a PR that adds it upstream.

Execution: organize and review branches locally first; open upstream Issues + PRs in a **separate session**. English only for branch names, commit subjects, Issue/PR titles, and PR bodies.

## Ground rules

| Rule | Detail |
| --- | --- |
| Base | Branch every PR head from updated local `master` (`ff-only` of `upstream/master`). |
| Remote | Push heads to `origin` only. Open PRs against `qew21/Genshin-Subtitles` `master`. |
| Head names | `pr/<short-topic>` on the fork. Upstream does not keep this name after merge; the maintainer chooses merge strategy and squash message. |
| Topology | Prefer independent PRs from `master`. Stack only when the later PR cannot build or review without the earlier. |
| Issues | Open an upstream Issue and PR together; link both ways. Use **Draft** for large product PRs. Small chore/fix PRs may be Ready. |
| Rebuild | Do **not** wholesale cherry-pick `personal` onto `master`. Recreate clean commits (or surgically transplant allowed paths) so forbid-listed files never appear. |
| Shared files | `LiveOverlaySession*.cs`, `MainWindow.*`, `SettingsWindow.*`, `Config.cs`, string resources, and `*.csproj` are touched by many items — edit only the hunks for that PR. |
| Language | English for anything that appears on the upstream PR. |

## Hard forbid list (every PR)

Never include:

- README / README_CN **fork notice** (“personal fork” / 个人 fork)
- `docs/agents/**`, `AGENTS.md`, `CONTEXT.md` agent wiring, `.agents/`, `skills-lock.json`
- `.gitignore` entries only for AI tooling / `.scratch/` / `tmp/` (unless a product PR truly needs an unrelated ignore)
- `docs/adr/0013-fork-test-builds-are-prereleases.md` and any `-fork.N` release-notes copy
- Fork runbooks that only document personal release tags
- `feature/prototype-*` branches or prototype-only code
- This plan file

## Suggested PR inventory

Order is dependency-friendly. Batch in separate sessions; do not require finishing all in one go.

### PR-01 — MSBuild script

| Field | Value |
| --- | --- |
| Branch | `pr/msbuild-script` |
| Title | `chore: add VS MSBuild build script` |
| Depends on | — |
| Draft? | No |
| Allow | `scripts/Build.ps1`; README.md / README_CN.md **build section only** (PowerShell `Build.ps1` instructions; no fork banner) |
| Forbid | Fork notice lines; any other README churn; agent docs |
| ADRs | — |
| Validate | `pwsh -File scripts/Build.ps1 -Configuration Release` builds `GI-Subtitles` (ignore unrelated WixSharp SDK noise if present) |

### PR-02 — Deploy repository guard

| Field | Value |
| --- | --- |
| Branch | `pr/deploy-repo-guard` |
| Title | `chore: guard website deploy to upstream repository only` |
| Depends on | — (independent of PR-01) |
| Draft? | No |
| Allow | `.github/workflows/release.yml` hunk that requires `github.repository == 'qew21/Genshin-Subtitles'` for `deploy-website` |
| Forbid | `-fork.N` release-notes prepend; ADR 0013; any fork branding |
| ADRs | — |
| Validate | Workflow YAML still parses; comment explains forks must not deploy the stable channel |

### PR-03 — Region pairs core (+ hint + OCR interval)

| Field | Value |
| --- | --- |
| Branch | `pr/region-pairs-core` |
| Title | `feat: region pairs with shared OCR cadence and settings` |
| Depends on | — (first large product PR; optionally note PR-01 if README build text helps reviewers) |
| Draft? | **Yes** |
| Allow (themes) | Region-pair model/store; one OCR cadence; voice-primary on the pair; pair settings UI; display-region adjust + pair preview; overlay hint + hint screen selection; OCR interval setting; related `Screenshot/` selection helpers; `Screenshot.csproj` TFM `v4.8` if still required; matching `GI-Test` tests; string resources for these controls; minimal `csproj` compile/page entries |
| Forbid | Activity log; extra-path pipeline; layout persistence; idle timeout; adjust-trace diagnostics; fork-only docs |
| ADRs | `0001`, `0002`, `0004`, `0005`, `0007` (and any glossary-only notes those ADRs need — do not add full `CONTEXT.md` unless scrubbed and agreed) |
| Validate | VS MSBuild Debug/Release for `GI-Subtitles` + `GI-Test`; run region-pair / hint / OCR-interval tests |

### PR-04 — Extra paths

| Field | Value |
| --- | --- |
| Branch | `pr/extra-paths` |
| Title | `feat: dark-screen and dialogue options as extra paths` |
| Depends on | PR-03 (stack or wait until merged) |
| Draft? | **Yes** |
| Allow | Extra-path sample/body; dark-screen / dialogue scans as extra paths; pin displays in settings/preview; gates follow applied game; related session/MainWindow/Settings hunks; tests (`TestLiveOverlaySessionExtraPaths*`, etc.) |
| Forbid | Activity log window; idle timeout; fork docs |
| ADRs | `0006` |
| Validate | Build + extra-path tests; manual smoke: toggle scans, pin displays, switch game without restart |

### PR-05 — Per-game overlay layout

| Field | Value |
| --- | --- |
| Branch | `pr/overlay-layout-per-game` |
| Title | `feat: persist overlay layout per game` |
| Depends on | PR-03; preferably PR-04 if extra-path displays are part of the layout record |
| Draft? | Yes if diff is large; else Ready |
| Allow | `OverlayLayoutRecord` / `OverlayLayoutPersistence`; swap layout on Apply; applied-game session tests |
| Forbid | Activity log; fork docs |
| ADRs | `0009` |
| Validate | Build + `TestOverlayLayoutPersistence` / applied-game tests |

### PR-06 — Activity log core

| Field | Value |
| --- | --- |
| Branch | `pr/activity-log-core` |
| Title | `feat: activity log window for operator actions and pipeline jobs` |
| Depends on | PR-03; PR-04 strongly recommended so rows can name extra paths |
| Draft? | **Yes** |
| Allow | Activity log row model; window XAML/code; pipeline job recording; session hooks; basic tests; strings; `csproj` entries |
| Forbid | Result-tag coloring; follow-tail polish; fold/de-noise if they can land in PR-07; fork docs |
| ADRs | `0003`, `0008` |
| Validate | Build + activity-log core tests |

### PR-07 — Activity log enhancements (split if needed)

Split into separate heads if any one PR grows hard to review:

| Sub | Branch | Title | ADRs |
| --- | --- | --- | --- |
| 07a | `pr/activity-log-fold-miss` | `feat: fold identical results and keep subtitle on match miss` | `0010` |
| 07b | `pr/activity-log-copy-tags` | `feat: activity log copy selection, result tags, and category stripes` | `0011`, `0016` (keep `0012` file marked superseded) |
| 07c | `pr/activity-log-follow-tail` | `feat: activity log follow-tail and virtualization fixes` | `0014` |

| Field | Value |
| --- | --- |
| Depends on | PR-06 |
| Draft? | Yes if stacked as one; Ready OK for small follow-ups after 06 merges |
| Allow | Only files for that sub-theme (filter, projection, composer, follow-tail, tag views, related tests) |
| Forbid | Unrelated session features; fork docs |
| Validate | Matching `GI-Test` cases for each sub-theme |

### PR-08 — Region adjust polish (+ optional Debug trace)

| Field | Value |
| --- | --- |
| Branch | `pr/region-adjust-polish` |
| Title | `feat: arm-once region adjust and Debug-only adjust trace` |
| Depends on | PR-03 |
| Draft? | Optional |
| Allow | Arm-once / either-frame drag; `WS_DISABLED` clear; `AdjustMouseGuard`; Debug-only `RegionAdjustTrace` / diagnostics; optional `scripts/Verify-Release-No-AdjustTrace.ps1` **after scrubbing fork issue numbers** |
| Forbid | Fork release runbooks; ADR 0013 |
| ADRs | Update `0005` only if the decision text must change |
| Validate | Debug build shows trace when enabled; Release build has no diagnostic-only strings (script or manual check) |

### PR-09 — Subtitle idle timeout

| Field | Value |
| --- | --- |
| Branch | `pr/subtitle-idle-timeout` |
| Title | `feat: subtitle idle timeout clears fold and bodies` |
| Depends on | PR-03; PR-04 for dark-screen body clear; PR-07a if fold semantics are already upstream |
| Draft? | Optional |
| Allow | Idle-timeout store/settings; clear region-pair and dark-screen bodies; fold clear on expiry; tests; strings |
| Forbid | Fork docs |
| ADRs | `0015` |
| Validate | Build + idle-timeout tests |

## Optional later

- Scrubbed product glossary (`CONTEXT.md`) as a docs-only PR — only if upstream wants it and agent/fork references are removed.
- Further README polish beyond the build section — never reintroduce the fork banner.

## Per-PR checklist (submit session)

1. `git fetch upstream` && ff `master` to `upstream/master`.
2. Create `pr/<topic>` from that `master`.
3. Transplant **allow** paths only; diff against forbid list before push.
4. Commit with English conventional subject matching the PR title theme.
5. Push to `origin`; open upstream Issue + PR (Draft if large); cross-link.
6. PR body: `## Summary`, `## Validation` (mirror recent upstream style).
7. Do not push to `upstream` remotes as a write target.

## Out of scope for upstream

Personal fork process, agent skills, triage labels, fork test-build releases (`-fork.N`), and this plan.
