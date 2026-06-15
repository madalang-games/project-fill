Fetch GitHub issues for this project and cache them locally for commit reference.

Arguments: $ARGUMENTS — optional: `open` | `closed` | `all` | label name (default: `open`)

## Repo
`madalang-games/project-fill`

## Steps
1. Parse `$ARGUMENTS`:
   - If empty or `open` → `--state open`
   - If `closed` → `--state closed`
   - If `all` → `--state all`
   - Otherwise treat as label filter → `--label "$ARGUMENTS" --state open`
2. Run: `gh issue list --repo madalang-games/project-fill {state_flag} --limit 100 --json number,title,state,labels,assignees`
3. Write result to `.claude/issues.cache.md` (overwrite every time)
4. Report: count fetched + file path, nothing else

## Output format for .claude/issues.cache.md
```
# GitHub Issues Cache
<!-- repo: madalang-games/project-fill | updated: {YYYY-MM-DD HH:MM} -->

| # | Title | Labels | State |
|---|-------|--------|-------|
| 42 | 로그인 버그 수정 | bug, priority:high | open |
| 38 | 아이템 드롭 로직 개선 | feat | open |
```

## Rules
- Always overwrite `.claude/issues.cache.md` — never append
- Never commit `.claude/issues.cache.md` (it is gitignored)
- If `gh` not authenticated, print: `ERROR: gh auth login required` and stop
- If no issues found, write the header only + `<!-- no issues -->`
