# 3.14 UI and UX shortlist

2026-10-18. Original prioritized shortlist. The user subsequently requested implementation; current delivery is tracked in IMPLEMENTATION-CHECKLIST.md. Fixed sizing supersedes adaptive resizing and maximize/Snap suggestions. Favor clarity, forgiving editing, quiet surfaces and progressive disclosure over decorative trends.

## Highest value

| Before | After | Why |
|---|---|---|
| Color edits and regeneration have no undo | Undo/redo for role edits, picking and generation | Make experimentation safe; group a slider drag as one undo step. |
| Escape and Done both retain wheel edits | Explicit Apply/Cancel; Escape cancels and restores the opening color | Distinguish accepting from abandoning a change. |
| Focusing Base color changes the screen-picker target | Explicit Base/Selected role picker target control | Avoid focus changes silently changing the next action. |
| Copy confirmation appears in the footer | Brief Copied feedback near the clicked swatch, announced accessibly | Make successful copying obvious without searching the screen. |
| Swatch click both copies and opens the editor | Keep the requested combined behavior, plus a dedicated Copy-only action | Provide a faster path when the user only wants the code. |
| Lock semantics need explanatory text | Label locks as regeneration locks; explain that direct edits still work | Match the control's promise to its behavior. |
| Contrast results live on the preview page | Compact per-role issue badges linking to the failing pairs | Catch a problem while editing without declaring universal accessibility. |
| Draft autosaves while library saving is explicit | Distinguish Draft saved locally from Saved palette | Prevent uncertainty about which version is reusable. |

## Polish and everyday use

| Before | After | Why |
|---|---|---|
| Some large windows leave generous empty areas | Tune spacing at a few window sizes, retaining the no-page-scroll layout | Make larger windows useful without shrinking text at minimum size. |
| Font roles exist but sizes/weights are set in several places | A small shared typography scale | Keep headings, labels, values and buttons consistent. |
| Editor uses a fixed light theme | Optional system/light/dark app theme independent of palette variant | Match the desktop without changing generated colors. |
| Motion is limited to a preview fade | Restrained feedback for copied/saved/tab state; respect reduced motion | Explain state changes without delaying editing or keyboard actions. |
| Custom colors and focus styling use fixed values | Verify Windows High Contrast and keyboard-only cues | Preserve visible controls for users with different vision needs. |
| Shortcut editing requires typing its syntax | A shortcut recorder with occupied/reserved-key feedback | Prevent formatting errors and explain registration failures. |
| Saved palettes are a simple list | Search, sort, and clear favorite filtering | Help a growing personal collection stay navigable. |
| Saving a new name creates another palette | Explicit Rename, Duplicate, and Delete with undo | Make library management predictable and reversible. |
| The title bar has minimize/close and double-click maximize | Consider maximize/restore and native Windows menu/Snap support | Add familiar Windows behavior if window management needs grow. |
| First use relies on explanatory copy | One dismissible hint for the shortcut, picker target and regeneration locks | Teach the core workflow without a tour or forced onboarding. |

## Optional, after the core feels settled

| Before | After | Why |
|---|---|---|
| HEX is the main editing format | RGB/HSL and optional OKLCH under an Advanced disclosure | Add precision without expanding the default editor. |
| Neutral generation uses HSL | Evaluate OKLCH-based tonal generation with gamut handling | Offer more predictable perceived lightness; validate rather than promise better palettes. |
| Export actions immediately copy or open a file dialog | Small export preview with token names and output format | Help users understand exactly what they will paste. |
| JSON export has no corresponding import | Versioned, validated palette import | Support local sharing and moving palettes between machines. |
| Recovery requires manually restoring a backup file | In-app backup/export-library and restore workflow | Reduce the cost of recovering saved work while preserving damaged originals. |
| Recent history records picked colors | Optional named palette snapshots or recent generation history | Revisit a promising combination instead of trying to recreate it. |

## Verification to prioritize alongside design

Run the real picker on mixed-DPI monitors, negative monitor coordinates and 100–200% scaling. Exercise keyboard-only navigation, screen readers, long palette names, storage failure states, high contrast, and reduced motion. Current native-input integration tests can be affected by concurrent desktop activity, so run them with an idle mouse and keyboard.

No accounts, cloud sync, subscriptions, payment systems or AI services are proposed.

## Sources and interpretation

- [Microsoft: Windows title-bar design](https://learn.microsoft.com/en-us/windows/apps/design/basics/titlebar-design): retain recognizable caption behavior and clear draggable/interactive regions.
- [Nielsen Norman Group: ten usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/): timely feedback, consistency, user control, error prevention and recognition.
- [Nielsen Norman Group: user control and freedom](https://www.nngroup.com/articles/user-control-and-freedom/): support undo and a clear way to leave an editing flow.
- [W3C: target size minimum](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum): useful target-size guidance. WCAG is a web standard; applying its principles to this desktop app does not establish desktop conformance.

These recommendations are design judgments, not a measured ranking of 2026 visual trends. The subsequent implementation is tracked separately with its actual verification limits.
