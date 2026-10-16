// Loaded in <head> on the home page only, before first paint: marks the page so the
// elements home.js animates start hidden instead of flashing in and then animating.
(function () {
  "use strict";
  var root = document.documentElement;
  if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  root.classList.add("anim-pending");
  // Safety net: if the animation scripts fail to load, never leave content hidden.
  window.setTimeout(function () {
    if (!root.classList.contains("anim-pending")) return;
    root.classList.remove("anim-pending");
    root.setAttribute("data-anim-timeout", "");
  }, 2500);
})();
