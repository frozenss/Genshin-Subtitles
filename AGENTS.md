## Agent skills

### Issue tracker

Issues live in GitHub Issues on the personal fork `frozenss/Genshin-Subtitles` (`origin`; not `upstream`). See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/` at repo root). See `docs/agents/domain.md`.

### Git branches

Default working branch is `personal` (fork mainline). `master` fast-forwards `upstream/master` only. Feature work, upstream PRs, and syncing: see `docs/agents/git-branches.md`.

### Test-build releases

Fork test builds ship as pre-release GitHub Releases from `-fork.N` tags pushed to `origin` only. See `docs/agents/test-build-releases.md`.

### Other

update issue status after implement.
