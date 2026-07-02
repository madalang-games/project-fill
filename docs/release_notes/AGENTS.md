# Release Notes Management Policy

This directory manages Store Listing Release Notes for Project Fill across all supported languages.

## Directory Structure
- `vX.Y.Z/`: Directory for a specific version.
- `vX.Y.Z/release_note.txt`: The definitive plain-text release note with language tags.
- `vX.Y.Z/snapshots/`: (Optional) Point-in-time drafts.

## File Format
The `release_note.txt` uses language tags for Google Play Console compatibility:
```text
<en-US>
Contents here...
</en-US>
<ko-KR>
내용...
</ko-KR>
<ar>
المحتوى...
</ar>
```

## AI Agent Workflow
1. **Diff Analysis:** Compare with previous version using git.
2. **Drafting:** Max 4 lines summary. Benefit-driven bullet points.
3. **Translation:** Ensure all 15 supported languages are covered using full language-REGION tags (e.g., `ru-RU`, `es-ES`, `pt-PT`, `fr-FR`, `de-DE`, `it-IT`, `tr-TR`). Required list: `en-US`, `ko-KR`, `zh-CN`, `zh-TW`, `ja-JP`, `ru-RU`, `es-ES`, `pt-PT`, `fr-FR`, `de-DE`, `th`, `ar`, `it-IT`, `tr-TR`, `id`.
4. **Metadata:** Append current Git HEAD commit hash at the bottom.

## Release History
| Version | Release Date | Summary | Commit |
|---------|--------------|---------|--------|
| v1.0.3  | 2026-07-02   | Stability Polish & Clearer Purchase Feedback | `67545c7` |
| v1.0.2  | 2026-06-29   | Store Launch, Rewarded Ads & Smoother Play | `9dfe2c7` |
| v1.0.1  | 2026-06-19   | Weekly Missions, Shop/Lobby Revamp & Reward Clarity | `ea40894` |
| v1.0.0  | 2026-06-12   | Initial MVP Release | `c22b90c` |

> Per-version `vX.Y.Z/` directories are added when a release note is drafted. If none are present in this folder, none have been committed yet — regenerate from the commits listed above.
