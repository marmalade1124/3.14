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

## Motion
One-time section reveals use IntersectionObserver with opacity/transform (500 ms, 60 ms child stagger). Hover/press feedback takes 140 ms; pointer palette color transitions take 220 ms. Palette color changes deliberately repaint the small demo, preserving real inputs and selectable text. Keyboard actions finish reveals immediately and disable transitions. Reduced-motion preferences remove movement; content stays visible without JavaScript or IntersectionObserver. No animation library or perpetual motion.

The homepage plays a first-session circular introduction: a 450 ms SVG ring draw, then a 650 ms expanding radial mask revealing the page. It is decorative, ignores pointer hit testing, skips deep links and reduced-motion preferences, and dismisses on input. A 1.4 second failsafe removes it even if mask animation support fails. No cookies or network request; a sessionStorage flag avoids repeat playback.

Run intro lifecycle checks with: node website/check-intro.cjs (from the repository root). Browser verified the ring appearing, full page becoming visible, and session repeat skipping.

