// Minimal hover tooltip for chart marks: any element with data-tip="text" (and its .hit target) shows a tooltip.
(function () {
  "use strict";
  var tip = document.createElement("div");
  tip.className = "viz-tip";
  tip.setAttribute("role", "status");
  document.body.appendChild(tip);
  function move(e) {
    var x = e.clientX + 14, y = e.clientY + 14;
    var r = tip.getBoundingClientRect();
    if (x + r.width > window.innerWidth - 8) x = e.clientX - r.width - 14;
    if (y + r.height > window.innerHeight - 8) y = e.clientY - r.height - 14;
    tip.style.left = x + "px";
    tip.style.top = y + "px";
  }
  document.addEventListener("pointerover", function (e) {
    var el = e.target.closest && e.target.closest("[data-tip]");
    if (!el) return;
    tip.textContent = el.getAttribute("data-tip");
    tip.classList.add("show");
    var mark = el.getAttribute("data-mark") && document.getElementById(el.getAttribute("data-mark"));
    if (mark) mark.classList.add("hover");
    move(e);
  });
  document.addEventListener("pointermove", function (e) { if (tip.classList.contains("show")) move(e); });
  document.addEventListener("pointerout", function (e) {
    var el = e.target.closest && e.target.closest("[data-tip]");
    if (!el) return;
    tip.classList.remove("show");
    var mark = el.getAttribute("data-mark") && document.getElementById(el.getAttribute("data-mark"));
    if (mark) mark.classList.remove("hover");
  });
})();
