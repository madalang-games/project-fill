# PlayerSettings Icons

## Files
| file | class | role |
|------|-------|------|
| `AppIcon2.png` | asset | App icon source sprite (1024×1024) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AppIcon2` | sprite asset | Current app icon source; used in Unity PlayerSettings icon slots |

## Cross-refs
| type | refs |
|------|------|
| Consumed by | `Unity.PlayerSettings` app icon configuration |

## Rules
- Keep icon sources square PNGs unless a platform-specific export requires another format.
- Preserve matching `.meta` files so Unity import settings and GUIDs remain stable.
