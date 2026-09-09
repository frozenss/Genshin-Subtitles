# Test-build releases

Fork test builds are published as pre-release GitHub Releases, downloadable without a GitHub login. Rationale and rejected alternatives: `docs/adr/0013-fork-test-builds-are-prereleases.md`.

## Publish a test build

```
git tag -a 1.6.12-fork.1 -m "Fork test build, not an official release. <what changed, what to test>"
git push origin 1.6.12-fork.1
```

Push the tag to `origin` only — never `upstream`, and no `git push --tags` against `upstream`. `.github/workflows/release.yml` builds the MSI + ZIP + SHA256 and publishes the Release marked **Pre-release**; the website deploy never runs (gated to `qew21/Genshin-Subtitles`).

## Tag rules

- Format `<AssemblyVersion>-fork.N` (e.g. `1.6.12-fork.1`). The numeric part must equal `AssemblyVersion` (three fields) — the release job rejects mismatches.
- Suffix is `-fork.N`, never `-pre` (upstream's own prerelease vocabulary).
- Annotated tags only: the message becomes the release body, and its first line must say it is a fork test build, not an official release.
- Do not bump `AssemblyVersion` ahead of upstream for a test build. The in-app updater reads upstream's stable manifest; staying at-or-behind upstream's number is what eventually shepherds test users back to official builds.

## Self-test without publishing

Run `Build Release` via workflow_dispatch with `publish=false`; download the artifact from the run (requires GitHub login — fine for yourself, unusable for anonymous users).

## After upstream merges

Keep the test release. Pre-releases never claim the "Latest" badge, links stay valid, and the release records which code the testers ran.
