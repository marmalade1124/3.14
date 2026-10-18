# UX implementation checklist

2026-10-18. Full shortlist delivered subject to the later fixed-window instruction. No account/cloud/payment features added.

| # | Request | Delivery |
|---|---|---|
| 1 | Undo/redo | 50 session steps, toolbar/shortcuts; grouped wheel edits. |
| 2 | Apply/Cancel | Apply commits; Cancel/Escape/backdrop restore opening color. |
| 3 | Explicit picker target | Base/Selected role; no focus-triggered target switch. |
| 4 | Nearby copy feedback | Copied badge, brief animation, polite live announcement. |
| 5 | Copy-only action | Separate Copy; combined swatch behavior retained. |
| 6 | Lock semantics | Lock regen label/help; explicit edits remain possible. |
| 7 | Contrast badges | Per-role pair status links to detailed checks. |
| 8 | Draft/save distinction | Draft/saved/failure/disabled states in footer. |
| 9 | Adaptive spacing | Superseded by fixed 1160×760 and two main screens. |
| 10 | Typography | Shared family/size resources for body/headings/code. |
| 11 | App themes | Light-only neutral workspace; legacy preferences migrate safely. Palette variants remain independent. |
| 12 | Purposeful motion | Copy/save-state/page feedback; reduced motion respected. |
| 13 | High Contrast/keyboard | System-color resources and keyboard cues; OS/screen-reader verification remains outstanding. |
| 14 | Shortcut recorder | Recording, validation, conflict feedback and prior-registration recovery. |
| 15 | Search/sort | Palette search, newest/name sorting, favorites. |
| 16 | Library management | Rename, duplicate, delete with undo. |
| 17 | Window behavior | Fixed size supersedes maximize/Snap; minimize/close, dragging and system menu retained. |
| 18 | First-use help | Dismissible hint, reopen via Settings. |
| 19 | Advanced color input | RGB/HSL/OKLCH disclosure with validation/gamut notice. |
| 20 | Perceptual generation | OKLCH tones, deterministic hue variation, sRGB chroma reduction, preserved locks. |
| 21 | Export preview | Exact CSS/JSON review, copy and file export. |
| 22 | Palette import | Versioned JSON/legacy exports, rejection before mutation, collision-safe names. |
| 23 | Library recovery | Full backup, validated confirmed restore, original archive including damaged files. |
| 24 | Palette snapshots | Last 20 generated/saved states; persisted and undoable on opening. |

## Actual verification

621 core assertions and 24 feature assertions pass. Build succeeds. Light app, settings and advanced-editor renders inspected. The latest full native-pointer run is not a pass: native hit testing at the wheel point identified an overlapping browser renderer. Logs preserve this failure.

Multi-monitor/mixed-DPI/HDR/protected/elevated screens, OS High Contrast switching, screen-reader use, file dialogs and native shortcut recording still require interactive/hardware verification. No claim is made for configurations not tested. See README for limitations and startup/recovery instructions.
