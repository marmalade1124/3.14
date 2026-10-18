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
document.querySelectorAll("[data-palette]").forEach(button => button.addEventListener("click", () => {active = button.dataset.palette; render(); status.textContent = "";}));
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
