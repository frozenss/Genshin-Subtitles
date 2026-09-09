# Test-build releases

Fork test builds are published as pre-release GitHub Releases, downloadable without a GitHub login. Rationale and rejected alternatives: `docs/adr/0013-fork-test-builds-are-prereleases.md`.

## One-time setup

Forks ship with GitHub Actions disabled, and enabling does not backfill events that already happened. Before the first tag push: `git push origin personal`, then enable workflows in the repo's Actions tab ("I understand my workflows, go ahead and enable them"), then tag. If a tag was already pushed before enabling, delete and re-push it (`git push origin :refs/tags/<tag>` then push it again), or dispatch `Build Release` with that tag and `publish=true`. The workflow that runs is the version at the tag's commit — the deploy-website guard only protects tags cut from commits that contain it.

## Publish a test build

```
git tag -a 1.6.12-fork.1 -m "Fork test build, not an official release. <what changed, what to test>"
git push origin 1.6.12-fork.1
```

Push the tag to `origin` only — never `upstream`. Never `git push --tags` against either remote: this clone carries upstream's bare tags (`1.6.x`), and pushing them would fire one non-prerelease release per tag. Branch pushes are inert — `release.yml` triggers on tag pushes and manual dispatch only, and `build.yml` listens to `master`/`main` only. `.github/workflows/release.yml` builds the MSI + ZIP + SHA256 and publishes the Release marked **Pre-release**; the website deploy never runs (gated to `qew21/Genshin-Subtitles`).

## Tag rules

- Format `<AssemblyVersion>-fork.N` (e.g. `1.6.12-fork.1`). The numeric part must equal `AssemblyVersion` (three fields) — the release job rejects mismatches.
- Suffix is `-fork.N`, never `-pre` (upstream's own prerelease vocabulary).
- Annotated tags only (`-a`): the message is the intended release body and must open with the fork notice. The runner's checkout can drop the annotated message (the pushed tag arrives as a plain commit ref, so the body falls back to generated notes); `release.yml` therefore prepends the notice to every `-fork.*` release as a guarantee. Verify the published body anyway.
- Do not bump `AssemblyVersion` ahead of upstream for a test build. The in-app updater reads upstream's stable manifest; staying at-or-behind upstream's number is what eventually shepherds test users back to official builds.

## Self-test without publishing

Run `Build Release` via workflow_dispatch with `publish=false`; download the artifact from the run (requires GitHub login — fine for yourself, unusable for anonymous users).

## Error reference

Commands that misfire, worst first, and what happens:

- `git push upstream …` — anything (branch, tag, `--tags`, `--mirror`) pushes fork content onto upstream; a bare tag would release **on upstream** and deploy to `2langs.com`. Lack of write access blocks this today; never rely on that.
- `git push --mirror` (to `origin` too) — rewrites `origin`'s branches and pushes every local ref.
- `git push --tags` (either remote) or `git push origin <bare-tag>` — one release per bare tag, published **without** the pre-release mark. The deploy-website guard stops the website, not the release.
- workflow_dispatch with a bare tag and `publish=true` — same as above, without any tag push.
- Tag pushed before Actions was enabled — nothing runs; enabling does not backfill. Delete and re-push the tag, or dispatch.
- Tag numeric part ≠ `AssemblyVersion` — the validate step fails the run; no release. Benign.
- Lightweight tag (no `-a`) — the release body falls back to auto-generated notes and loses the required "fork test build" first line.

Recovery: `git push origin :refs/tags/<tag>` deletes the remote tag — the Release survives, so delete it too (`gh api repos/frozenss/Genshin-Subtitles/releases --jq '.[] | "\(.id) \(.tag_name)"'` for the id, then `gh api -X DELETE repos/frozenss/Genshin-Subtitles/releases/<id>`), and `git tag -d <tag>` locally.

## After upstream merges

Keep the test release. Pre-releases never claim the "Latest" badge, links stay valid, and the release records which code the testers ran.
