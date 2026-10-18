# 3.14 companion website

Dependency-free HTML, CSS and JavaScript in `dist/`. The palette selector uses four prepared samples; it does not perform native capture or claim to be the desktop generator. No analytics, remote fonts, API keys or storage.

## Run locally

From the repository root:

```powershell
python -m http.server 4173 --directory website/dist --bind 127.0.0.1
```

Open http://127.0.0.1:4173. Clipboard requires a secure context (HTTPS or localhost); if unavailable, selectable CSS appears. Native desktop picking is available only in the Windows app. Build links point to GitHub Actions artifacts, not a nonexistent release/installer.

## Verification

Run `node --check website/dist/app.js`. Browser QA passed for desktop and narrow viewport (375 CSS px, no horizontal overflow), palette changes, keyboard Space activation, text-pair contrast updates, CSS copy feedback, sample input and FAQ. Physical mobile and screen-reader testing remain unverified. Images are app-render screenshots. The desktop app itself remains source-of-truth for capability and capture limitations.

## Design

Light-only material grays, charcoal actions, system typography, one circular sampling mark, restrained sections and generous spacing. Responsive layout and keyboard focus. Reduced motion disables smooth scrolling. Prepared palette interaction is explicitly labeled as a demo. No invented users, ratings, pricing or trademark claims.
