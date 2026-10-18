# UX principles applied to 3.14

Reviewed 2026-10-18 against [Laws of UX](https://lawsofux.com/). This is an implementation assessment, not proof of universal usability. Principles are contextual; performance and psychological effects require observation with users.

| Principle | Application / evidence |
|---|---|
| Aesthetic-Usability Effect | Neutral, consistent controls; usability still tested separately. |
| Choice Overload | Related output tools grouped. |
| Chunking | Picker, library, editor, export groups. |
| Cognitive Bias | Explicit PASS/FAIL ratios; no blanket accessibility claim. |
| Cognitive Load | Semantic role labels remain visible. |
| Doherty Threshold | Live local editing; latency not benchmarked. |
| Fitts’s Law | Larger copy/contrast targets beside swatches. |
| Flow | Global shortcut; direct sampling. |
| Goal-Gradient Effect | Preview/export readily available; no artificial progress. |
| Hick’s Law | Primary output actions lead the grouped toolbar. |
| Jakob’s Law | Escape, Space, standard Undo/Redo, familiar dialogs. |
| Law of Common Region | Bounded workspace groups. |
| Law of Proximity | Controls beside affected values. |
| Law of Prägnanz | Simple consistent shapes. |
| Law of Similarity | Shared control templates. |
| Law of Uniform Connectedness | Grouped picker target selector. |
| Mental Model | Explicit Base versus role target. |
| Miller’s Law | Five labeled semantic roles; no arbitrary item limit. |
| Occam’s Razor | Existing WPF components; no dependency added. |
| Paradox of the Active User | Contextual help and empty states. |
| Pareto Principle | Core picking/editing/export emphasized. |
| Parkinson’s Law | Direct commands; no required wizard. |
| Peak-End Rule | Copy/export completion feedback; recoverable failures. |
| Postel’s Law | Normalize supported input; reject damaged storage safely. |
| Selective Attention | Strong selected-role state. |
| Serial Position Effect | Primary picking first; output actions at end. |
| Tesler’s Law | Advanced color formats disclosed; validation retained. |
| Von Restorff Effect | Charcoal primary actions within neutral chrome. |
| Working Memory | Visible HEX, roles, locks, save status. |
| Zeigarnik Effect | Persisted drafts; no manipulative unfinished-task prompts. |

## This change

Kept output actions grouped and directly available. Increased per-role action heights from 24 to 30 logical units, widened library overflow targets, honored button content alignment, truncated long snapshot titles with full-name tooltips, and returned keyboard focus to the originating control after utility dialogs close.

## Verification and limits

Build, 621 core assertions, 27 feature assertions. Includes preview fitting the fixed window. App renders inspected. Real user testing, timed latency, physical keyboard/input, HDR, secure screens and additional display-scaling configurations were not verified this turn. Existing capture limitations remain documented in README.

References: [Hick’s Law](https://lawsofux.com/hicks-law/), [Fitts’s Law](https://lawsofux.com/fittss-law/), [Peak-End Rule](https://lawsofux.com/peak-end-rule/), [Postel’s Law](https://lawsofux.com/postels-law/). Design applications above are our judgment, not claims of measured psychological outcomes.
