// Public site enhancements. Everything here is optional: pages work without JavaScript.
(function () {
  "use strict";
  document.documentElement.classList.remove("no-js");
  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // Reveal-on-scroll for elements marked .reveal
  var reveal = document.querySelectorAll(".reveal");
  if ("IntersectionObserver" in window && !reduceMotion) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add("in"); io.unobserve(e.target); }
      });
    }, { rootMargin: "0px 0px -8% 0px" });
    reveal.forEach(function (el) { io.observe(el); });
  } else {
    reveal.forEach(function (el) { el.classList.add("in"); });
  }

  // Hero carousel: no auto-rotation for users who prefer reduced motion
  var hero = document.getElementById("heroCarousel");
  if (hero && window.bootstrap) {
    var carousel = bootstrap.Carousel.getOrCreateInstance(hero, { interval: reduceMotion ? false : 8000, pause: "hover" });
    if (reduceMotion) carousel.pause();
  }

  // Back to top
  var toTop = document.querySelector(".back-to-top");
  if (toTop) {
    window.addEventListener("scroll", function () { toTop.classList.toggle("show", window.scrollY > 600); }, { passive: true });
    toTop.addEventListener("click", function () { window.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" }); });
  }

  // Gallery lightbox (links still open the full image without JS)
  var lightbox = document.getElementById("lightbox");
  if (lightbox && window.bootstrap) {
    var modal = new bootstrap.Modal(lightbox);
    var img = lightbox.querySelector("img");
    var caption = lightbox.querySelector(".caption");
    var items = Array.prototype.slice.call(document.querySelectorAll("[data-lightbox]"));
    var index = 0;
    function show(i) {
      index = (i + items.length) % items.length;
      img.src = items[index].getAttribute("href");
      img.alt = items[index].getAttribute("data-caption") || "";
      caption.textContent = items[index].getAttribute("data-caption") || "";
    }
    items.forEach(function (a, i) {
      a.addEventListener("click", function (e) { e.preventDefault(); show(i); modal.show(); });
    });
    lightbox.querySelectorAll("[data-step]").forEach(function (b) {
      b.addEventListener("click", function () { show(index + parseInt(b.getAttribute("data-step"), 10)); });
    });
    lightbox.addEventListener("keydown", function (e) {
      var rtl = document.documentElement.dir === "rtl";
      if (e.key === "ArrowRight") show(index + (rtl ? -1 : 1));
      if (e.key === "ArrowLeft") show(index + (rtl ? 1 : -1));
    });
  }

  // Share helpers
  document.querySelectorAll("[data-copy-link]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      if (!navigator.clipboard) return;
      navigator.clipboard.writeText(window.location.href).then(function () {
        var label = btn.querySelector("span");
        if (!label) return;
        var original = label.textContent;
        label.textContent = btn.getAttribute("data-copied") || original;
        setTimeout(function () { label.textContent = original; }, 2000);
      });
    });
  });
  document.querySelectorAll("[data-print]").forEach(function (btn) {
    btn.addEventListener("click", function () { window.print(); });
  });

  // Auto-submit filter selects
  document.querySelectorAll("[data-autosubmit]").forEach(function (el) {
    el.addEventListener("change", function () { el.form && el.form.submit(); });
  });
})();
