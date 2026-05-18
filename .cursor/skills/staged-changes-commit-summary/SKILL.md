---
name: staged-changes-commit-summary
description: >-
  Reads git staged changes (index); if the index is empty, stages all working-tree
  changes (git add -A at repo root) then proceeds. Produces a Chinese Conventional
  Commit summary with type(scope) title plus one line per feature with affected
  files (filename-only), shows it in a fenced code block, and commits by default
  via git commit -F. Use when the user asks to commit, summarize staged changes,
  write a commit message, or 提交 commit.
---

# Staged Changes → Commit Summary

## When to use

Apply when the user wants a commit message or changelog-style summary **from the index after any auto-stage** (see Direct commit). Summary content always reflects **what ends up staged** for the commit.

## Steps

1. **Ensure there is something to summarize**
   - If `git diff --cached --name-only` is non-empty, use the index as-is.
   - If the index is **empty** but the working tree has changes, run **`git add -A`** from the **repository root**, then re-check `git diff --cached`. If still empty, refuse (nothing to commit).
   - Prefer `git diff --cached` for reading. If huge, start with `--stat` and `--name-only`, then targeted hunks.

2. **Read the diff**
   - Understand behavioral changes (feature, fix, refactor, build, test) from hunks, not only filenames.
   - Treat renames as one logical change when appropriate.

3. **Group into “features / operations”**
   - One bullet = one user-visible or reviewable unit.
   - Merge tiny edits in the same area; split unrelated concerns.

4. **Resolve “影响文件” display names**
   - List **filename only** (last path segment), never repo-relative or full paths.
   - Use prefix wildcards when many files share a prefix (e.g. `StepPayloadBuilder*`).
   - If duplicate filenames exist in different folders, disambiguate in the bullet text, not with paths.

5. **Choose `scope` (ZeppStepTool)**
   - `wpf` — `src/ZeppStepTool.Wpf/` UI, services, assets
   - `test` — `tests/ZeppStepTool.Tests/`
   - `build` — `build.yaml`, `build.ps1`, `version.json`
   - `legacy` — `legacy/`
   - `chore` — `.gitignore`, repo metadata
   - `core` — cross-cutting or dominant mixed changes

6. **First line type**
   - Default: **`feat(<scope>): <中文一句话总结>`**
   - Use Conventional Commits types when clearer: `fix`, `refactor`, `chore`, `docs`, `perf`, `test`
   - Format: **`<type>(<scope>): <中文一句话总结>`** (per project convention)

## Output format (required)

### Message contents

```text
<type>(<scope>): <中文总结，概括本次暂存区整体价值>

- <功能或操作描述 1>；影响文件：<file1>；<file2>
- <功能或操作描述 2>；影响文件：<file1>
```

Rules:

- **Title**: one line; Chinese summary; no trailing period unless natural.
- **Body**: each line `- ` + ends with **`；影响文件：`** + **`；`-separated filenames** (filename-only).
- **Language**: Chinese for descriptions; English only for code identifiers.

### Presentation (required)

- Show the final message in one **` ```text `** fenced block.
- Keep each `-` line reasonably short; do not break mid-bullet.

## Follow-up options (required)

After the fenced block, append **“下一步”** in Chinese with numbered options:

1. **已直接提交（默认）**：已按暂存区执行提交并报告 `git` 结果。
2. **仅输出不提交**：用户明确要求时不执行 `git commit`。
3. **修改提交说明后再提交**：按用户修改重新生成并提交。

## Direct commit (default behavior)

Execute direct commit by default. Skip only when the user explicitly requests output-only (e.g. “只生成提交说明，不提交”).

1. **Re-check staged state** at repo root. If index empty but working tree has changes, run **`git add -A`** once, then re-check. If still empty, refuse.
2. **Safety**: Do **not** commit secrets (`.env`, `credentials.json`, passwords). If staged, warn and exclude or ask user.
3. **Write message** to a UTF-8 temp file (e.g. Windows `%TEMP%\zepp-step-tool-commit-msg.txt`, Unix `/tmp/zepp-step-tool-commit-msg.txt`). Do not commit the temp file.
4. From repository root:

```bash
git commit -F "<path-to-utf8-message-file>"
```

5. **Staging rule**: Do **not** run extra `git add` after the index was already non-empty at flow start. Only `git add -A` when index was empty. Do **not** amend unless user explicitly asked.
6. Report `git` stdout/stderr and success/failure. Note if auto-stage ran.

## Git safety (align with user rules)

- NEVER update git config
- NEVER run destructive git commands unless explicitly requested
- NEVER skip hooks (`--no-verify`) unless explicitly requested
- NEVER commit unless user asked to commit (this skill is invoked for commit intent)
- NEVER force push to main/master
- Use HEREDOC or `-F` file for multiline messages (UTF-8)

## Commands reference

```bash
git status
git diff --cached --stat
git diff --cached --name-only
git diff --cached
git add -A
git commit -F /path/to/utf8-message.txt
```

## Example (ZeppStepTool)

```text
feat(wpf): 修复步数提交 data_json 并支持记住账号密码

- 修正日期/步数仅替换捕获组，避免破坏编码模板；影响文件：StepPayloadBuilder.cs；ZeppStepService.cs
- 使用 DPAPI 本地保存账号密码；影响文件：CredentialStore.cs；MainWindow.xaml.cs
- 新增单元测试覆盖 payload 构建；影响文件：StepPayloadBuilderTests.cs
```
