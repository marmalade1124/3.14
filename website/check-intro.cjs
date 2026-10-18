const fs = require("node:fs");
const vm = require("node:vm");
const assert = require("node:assert/strict");
const source = fs.readFileSync(__dirname + "/dist/app.js", "utf8").split("// A decorative first-visit reveal;")[1];
function run({seen=false, reduced=false, hash="", storageFails=false} = {}) {
  let appended=0, removed=0, fallback, timerCleared=false;
  const listeners = new Map(), media = new Map(), elementEvents = new Map();
  const intro = {setAttribute(){},remove(){removed++;},addEventListener(name,fn){elementEvents.set(name,fn);}};
  const context = {document:{createElement(){return intro;},body:{append(){appended++;}},addEventListener(name,fn){listeners.set(name,fn);},removeEventListener(name){listeners.delete(name);}},sessionStorage:{getItem(){if(storageFails)throw Error();return seen?"1":null;},setItem(){if(storageFails)throw Error();}},reducedMotion:{matches:reduced,addEventListener(n,f){media.set(n,f);},removeEventListener(n){media.delete(n);}},location:{hash},CSS:{supports(){return true;}},setTimeout(fn){fallback=fn;return 1;},clearTimeout(){timerCleared=true;}};
  vm.runInNewContext("// A decorative first-visit reveal;"+source,context);
  return {get appended(){return appended;},get removed(){return removed;},get cleared(){return timerCleared;},listeners,media,elementEvents,intro,fallback};
}
assert.equal(run().appended,1);
assert.equal(run({seen:true}).appended,0);
assert.equal(run({reduced:true}).appended,0);
assert.equal(run({hash:"#workflow"}).appended,0);
assert.equal(run({storageFails:true}).appended,1);
for(const event of ["keydown","pointerdown","wheel","touchstart"]){const r=run();r.listeners.get(event)();assert.equal(r.removed,1);assert.equal(r.listeners.size,0);assert.equal(r.cleared,true);}
const fallback=run();fallback.fallback();assert.equal(fallback.removed,1);
const end=run();end.elementEvents.get("animationend")({target:end.intro});assert.equal(end.removed,1);
const pref=run();pref.media.get("change")();assert.equal(pref.removed,1);
console.log("20 intro assertions passed");
