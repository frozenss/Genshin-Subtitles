# Git branches

This clone's GitHub default is `personal`. Remotes: `origin` is the personal fork `frozenss/Genshin-Subtitles`; `upstream` is `qew21/Genshin-Subtitles` (see `docs/agents/issue-tracker.md`).

## Lines

- **`personal`**: fork mainline. Land finished work here.
- **`master`**: fast-forward only from `upstream/master`. No feature commits.
- **`feature/<name>`**: short-lived, branched from `personal`, deleted locally and on `origin` after merge into `personal`.

`personal` is the mainline, not a branch-name prefix. Use `feature/<name>`, not `personal/feature/<name>`.

Tiny finished fork-only chores (docs, scripts, personal defaults) commit on `personal`. Multi-session, discardable, or reviewable work uses `feature/<name>`.

## Upstream PRs

Branch from updated `master`, push to `origin`, open the PR against `qew21/Genshin-Subtitles`.

## Sync from upstream

```
git fetch upstream
git checkout master
git merge --ff-only upstream/master
git checkout personal
git merge master
```

If `personal` is private to this clone and a linear history is wanted, `git rebase master` on `personal` instead of the last merge.

Keep `master` a fast-forward of `upstream/master`. Do not merge `personal` into `master`.
