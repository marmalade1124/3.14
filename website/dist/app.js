"use strict";
const palettes = {
  graphite: {background: "#EEF0EB", surface: "#FFFFFF", "primary-text": "#272E28", "secondary-text": "#586256", accent: "#333735"},
  clay: {background: "#F4EDE7", surface: "#FFFCF8", "primary-text": "#392C26", "secondary-text": "#6D5950", accent: "#914F3E"},
  moss: {background: "#EBF0E8", surface: "#FCFDF9", "primary-text": "#273526", "secondary-text": "#53634E", accent: "#42634B"},
  blue: {background: "#EDF0F6", surface: "#FCFDFF", "primary-text": "#273047", "secondary-text": "#56627B", accent: "#3E5694"}
};
let active = "graphite";
const demo = document.querySelector("#demo");
const status = document.querySelector("#copy-status");
function luminance(hex) {
  const rgb = hex.match(/[a-f0-9]{2}/gi).map(value => parseInt(value, 16) / 255).map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4);
  return rgb[0] * .2126 + rgb[1] * .7152 + rgb[2] * .0722;
}
function render() {
  const p = palettes[active];
  const names = {background: "bg", surface: "surface", "primary-text": "text", "secondary-text": "secondary", accent: "accent"};
  Object.entries(p).forEach(([role, hex]) => demo.style.setProperty("--" + names[role], hex));
  document.querySelectorAll("[data-palette]").forEach(button => button.setAttribute("aria-pressed", String(button.dataset.palette === active)));
  const values = [luminance(p["primary-text"]), luminance(p.background)].sort((a,b) => b-a);
  const ratio = (values[0] + .05) / (values[1] + .05);
  document.querySelector("#contrast-ratio").textContent = ratio.toFixed(2) + ":1";
  document.querySelector("#contrast-result").textContent = ratio >= 4.5 ? "AA normal text · Pass" : "AA normal text · Fail";
}
document.querySelectorAll("[data-palette]").forEach(button => button.addEventListener("click", event => {demo.classList.toggle("pointer-transition", event.detail > 0);active = button.dataset.palette; render(); status.textContent = "";}));
document.querySelector("#copy-css").addEventListener("click", async () => {
  const output = ":root {\n" + Object.entries(palettes[active]).map(([role, hex]) => "  --color-" + role + ": " + hex + ";").join("\n") + "\n}";
  try {await navigator.clipboard.writeText(output); status.textContent = "Sample CSS copied. Ready for your next idea.";}
  catch {status.textContent = "Clipboard unavailable here. Select and copy the CSS below."; const text = document.createElement("textarea");text.readOnly = true;text.value = output;text.setAttribute("aria-label", "Sample CSS to copy");text.style.cssText = "width:100%;height:140px;margin-top:10px";const previous = document.querySelector("#manual-css");if(previous)previous.remove();text.id = "manual-css";status.after(text);text.focus();text.select();}
});
document.querySelector("#demo-button").addEventListener("click", () => {
  const value = document.querySelector("#demo-input").value.trim();
  document.querySelector("#idea-status").textContent = value ? "A little beginning: “" + value + "”. Demo only; not saved." : "Add a name above to try this preview. Nothing is saved.";
});
render();

// Progressive enhancement: content is visible if JS or IntersectionObserver is unavailable.
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
const revealTargets = document.querySelectorAll(".hero-copy, .hero-note, .app-section, .section-heading, .steps li, .playground-copy, .demo-wrap, .details > div, .download > div, .faq > h2");
let revealObserver;
if (!reducedMotion.matches && "IntersectionObserver" in window) {
  revealObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      entry.target.classList.add("reveal-ready");
      entry.target.classList.remove("reveal-pending");
      revealObserver.unobserve(entry.target);
    });
  }, {threshold: .08});
  revealTargets.forEach(element => {element.classList.add("reveal-pending");revealObserver.observe(element);});
}
function finishReveals() {
  if (revealObserver) revealObserver.disconnect();
  revealTargets.forEach(element => {element.classList.remove("reveal-pending");element.classList.add("reveal-ready");});
}
document.addEventListener("keydown", () => {document.documentElement.classList.add("keyboard-input");finishReveals();}, {capture:true});
document.addEventListener("pointerdown", () => document.documentElement.classList.remove("keyboard-input"), {capture:true});
reducedMotion.addEventListener("change", event => {if(event.matches)finishReveals();});

// A decorative first-visit reveal; never gate navigation, input or reduced-motion access.
let introSeen = false;
try {introSeen = sessionStorage.getItem("pi-intro-seen-v1") === "1";} catch {}
if (!introSeen && !reducedMotion.matches && !location.hash && CSS.supports("mask-image", "radial-gradient(circle, transparent, black)")) {
  const intro = document.createElement("div");
  intro.className = "intro-screen";
  intro.setAttribute("aria-hidden", "true");
  intro.innerHTML = '<svg class="intro-mark" viewBox="0 0 100 100" fill="none"><circle class="intro-ring" cx="50" cy="50" r="34" pathLength="100" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"/><circle class="intro-point" cx="76" cy="25" r="5" fill="currentColor" stroke="var(--page)" stroke-width="3"/></svg>';
  document.body.append(intro);
  try {sessionStorage.setItem("pi-intro-seen-v1", "1");} catch {}
  const skipEvents = ["keydown", "pointerdown", "wheel", "touchstart"];
  function closeIntro() {
    intro.remove();
    clearTimeout(failsafe);
    skipEvents.forEach(name => document.removeEventListener(name, closeIntro, true));
    reducedMotion.removeEventListener("change", closeIntro);
  }
  const failsafe = setTimeout(closeIntro, 1400);
  skipEvents.forEach(name => document.addEventListener(name, closeIntro, {capture:true, passive:true}));
  reducedMotion.addEventListener("change", closeIntro);
  intro.addEventListener("animationend", event => {if(event.target === intro)closeIntro();});
}
