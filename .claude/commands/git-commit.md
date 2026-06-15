Stage and commit changes grouped by logical work unit.

Arguments: $ARGUMENTS — optional: issue number (e.g. `42`), type override, or scope hint.

## Convention
```
{type}#{issue}: {한글 메시지}
{type}: {한글 메시지}
```
Types: `feat` `fix` `refactor` `docs` `test` `chore` `build`

- Issue number: from `$ARGUMENTS` only — never fetch remotely.
- Message: Korean, concise, no verbose explanation.
- Subject line ≤ 50 chars.

## Rules
- Never stage: `.env*`, files in `.gitignore` (gitignored = do not stage; reading for reference is allowed)
- Always stage generated files: `*/generated/*`, `*/Generated/*` (must be included in commits to ensure consistency)
- Use `git -C {path}` syntax — never `cd && git`
- Multiple distinct work units → separate commits (do not squash)
- If issue number is not provided, check in order: current branch name (e.g., `feature/123-xyz`) → Read `.claude/issues.cache.md` with Read tool (file is gitignored but exists locally; only run `/git-issue` if Read returns file-not-found) → `TODO-List/` files.

## Steps
1. `git status` + `git diff --stat` → identify all changed files
2. Group by work nature → define commit units
3. Per unit:
   a. Stage only its files/hunks
   b. Commit with convention format
4. Report: `{hash} {message}` per commit, nothing else
