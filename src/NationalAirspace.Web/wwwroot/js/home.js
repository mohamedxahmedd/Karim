// Home page motion (GSAP + ScrollTrigger + SplitText + MotionPath).
// Pure enhancement: without JavaScript, or with reduced motion, every element is simply visible.
(function () {
  "use strict";
  var root = document.documentElement;

  function release() { root.classList.remove("anim-pending"); }

  if (!window.gsap || !window.ScrollTrigger) { release(); return; }
  gsap.registerPlugin.apply(gsap, [window.ScrollTrigger, window.SplitText, window.MotionPathPlugin].filter(Boolean));

  var rtl = root.dir === "rtl";
  var side = rtl ? -1 : 1; // multiply horizontal offsets so motion follows the reading direction
  var introAllowed = !root.hasAttribute("data-anim-timeout");
  var mm = gsap.matchMedia();

  mm.add("(prefers-reduced-motion: no-preference)", function () {
    try {
      setup();
    } catch (err) {
      // Never leave the page half-hidden because of an animation error.
      gsap.set(".hero-content > *, .section-head, .section-head-row > .btn, [data-anim]", { clearProps: "all" });
      if (window.console) console.error(err);
    }
    release();
  });
  mm.add("(prefers-reduced-motion: reduce)", release);

  function setup() {
    heroRadar();
    heroIntro();
    sectionHeads();
    cards();
    statistics();
    aboutVisual();
    visionBars();
    serviceTilt();
    scrollProgress();
    if (document.fonts && document.fonts.ready) document.fonts.ready.then(function () { ScrollTrigger.refresh(); });
    window.addEventListener("load", function () { ScrollTrigger.refresh(); });
  }

  // ---------- split helpers ----------
  // Arabic letters must stay joined, so text is only ever split into lines and words, never characters.
  function splitWords(el) {
    if (!window.SplitText) return null;
    return SplitText.create(el, { type: "lines,words", mask: "lines", linesClass: "split-line", wordsClass: "split-word" });
  }

  // ---------- hero: live radar ----------
  function heroRadar() {
    document.querySelectorAll(".hero-radar").forEach(function (svg) {
      var origin = "1180 400";
      gsap.to(svg.querySelector(".radar-sweep"), { rotation: 360, svgOrigin: origin, duration: 9, ease: "none", repeat: -1 });
      gsap.to(svg.querySelectorAll(".radar-routes path"), { strokeDashoffset: -160, duration: 6, ease: "none", repeat: -1 });
      gsap.fromTo(svg.querySelector(".radar-ping"), { attr: { r: 5 }, opacity: .8 },
        { attr: { r: 140 }, opacity: 0, duration: 3.2, ease: "power2.out", repeat: -1, repeatDelay: .4 });
      svg.querySelectorAll(".radar-blip").forEach(function (blip, i) {
        gsap.to(blip, { attr: { r: 6 }, fillOpacity: 1, duration: .5, ease: "sine.inOut", yoyo: true, repeat: -1, repeatDelay: 2.4, delay: i * 1.1 });
      });

      if (window.MotionPathPlugin) {
        svg.querySelectorAll("[data-route]").forEach(function (plane, i) {
          var route = svg.querySelector(plane.getAttribute("data-route"));
          if (!route) return;
          var duration = 13 + i * 4;
          var tl = gsap.timeline({ repeat: -1, delay: 1.2 + i * 2.6, repeatDelay: 1.5 });
          tl.to(plane, { motionPath: { path: route, align: route, alignOrigin: [.5, .5], autoRotate: true }, duration: duration, ease: "none" }, 0)
            .fromTo(plane, { opacity: 0 }, { opacity: 1, duration: 1.2 }, 0)
            .to(plane, { opacity: 0, duration: 1.2 }, duration - 1.2);
        });
      }

      // Depth: layers drift with the pointer and the whole radar eases away on scroll.
      if (window.matchMedia("(pointer: fine)").matches) {
        var layers = [
          { el: svg.querySelector(".radar-grid"), depth: 10 },
          { el: svg.querySelector(".radar-routes"), depth: 22 },
          { el: svg.querySelector(".radar-aircraft"), depth: 30 },
        ].filter(function (l) { return l.el; }).map(function (l) {
          return { depth: l.depth, x: gsap.quickTo(l.el, "x", { duration: 1.2, ease: "power3" }), y: gsap.quickTo(l.el, "y", { duration: 1.2, ease: "power3" }) };
        });
        var hero = svg.closest(".hero");
        hero.addEventListener("pointermove", function (e) {
          var r = hero.getBoundingClientRect();
          var nx = (e.clientX - r.left) / r.width - .5, ny = (e.clientY - r.top) / r.height - .5;
          // The svg is mirrored in RTL, so mirror the pointer too.
          layers.forEach(function (l) { l.x(nx * l.depth * side); l.y(ny * l.depth); });
        });
      }
    });

    var hero = document.querySelector(".hero");
    if (!hero) return;
    gsap.to(".hero-bg", { yPercent: 18, scale: 1.06, ease: "none", scrollTrigger: { trigger: hero, start: "top top", end: "bottom top", scrub: true } });
    gsap.to(".hero .container", { yPercent: -22, autoAlpha: .2, ease: "none", scrollTrigger: { trigger: hero, start: "top top", end: "bottom top", scrub: true } });
  }

  // ---------- hero: entrance ----------
  // Jump a slide's previous entrance to its end state so a new one never starts from a half-finished pose.
  function finishSlide(slide) {
    if (slide._tl) { slide._tl.progress(1).kill(); slide._tl = null; }
  }

  function slideTimeline(slide) {
    finishSlide(slide);
    var content = slide.querySelector(".hero-content");
    if (!content) return gsap.timeline();
    var title = content.querySelector("h1, .hero-title");
    var kicker = content.querySelector(".hero-kicker");
    var tl = gsap.timeline({ defaults: { ease: "power4.out" } });
    gsap.set(content.children, { autoAlpha: 1 });
    if (kicker) {
      tl.fromTo(kicker, { "--line": 0, autoAlpha: 0, x: 24 * side }, { "--line": 1, autoAlpha: 1, x: 0, duration: .9 }, 0);
    }
    if (title) {
      var split = title._split || (title._split = splitWords(title));
      if (split) tl.from(split.words, { yPercent: 115, duration: 1.1, stagger: .055 }, .1);
      else tl.from(title, { y: 40, autoAlpha: 0, duration: 1 }, .1);
    }
    tl.from(content.querySelectorAll("p"), { y: 24, autoAlpha: 0, duration: 1 }, .45)
      .from(content.querySelectorAll(".hero-actions .btn"), { y: 22, autoAlpha: 0, duration: .9, stagger: .1 }, .6);
    slide._tl = tl;
    return tl;
  }

  function heroIntro() {
    var hero = document.querySelector(".hero");
    if (!hero) return;
    var first = hero.querySelector(".carousel-item.active");
    var radar = first && first.querySelector(".hero-radar");

    if (introAllowed) {
      var start = function () {
        var tl = gsap.timeline();
        if (radar) {
          tl.from(radar.querySelectorAll(".radar-ring"), { scale: .3, opacity: 0, svgOrigin: "1180 400", duration: 1.6, ease: "expo.out", stagger: .08 }, 0)
            .from(radar.querySelectorAll(".radar-axis, .radar-center, .radar-blips"), { opacity: 0, duration: 1 }, .3)
            .from(radar.querySelector(".radar-routes"), { opacity: 0, duration: 1.4 }, .5);
        }
        tl.add(slideTimeline(first), .15);
        tl.from(hero.querySelectorAll(".carousel-indicators, .carousel-control-prev, .carousel-control-next"), { autoAlpha: 0, duration: .8 }, .9);
      };
      gsap.set(first.querySelectorAll(".hero-content > *"), { autoAlpha: 0 });
      // Split the title only once the web font is in, so line breaks are measured correctly (capped wait).
      var fontsReady = document.fonts && document.fonts.ready ? document.fonts.ready : Promise.resolve();
      Promise.race([fontsReady, new Promise(function (r) { setTimeout(r, 700); })]).then(start);
    }

    hero.addEventListener("slide.bs.carousel", function (e) {
      if (!e.relatedTarget) return;
      finishSlide(e.relatedTarget);
      gsap.set(e.relatedTarget.querySelectorAll(".hero-content > *"), { autoAlpha: 0 });
    });
    hero.addEventListener("slid.bs.carousel", function (e) {
      if (e.relatedTarget) slideTimeline(e.relatedTarget);
    });
  }

  // ---------- section headings ----------
  function sectionHeads() {
    document.querySelectorAll("main .section .section-head").forEach(function (head) {
      var kicker = head.querySelector(".section-kicker");
      var title = head.querySelector("h2");
      var text = head.querySelector("p");
      var action = head.parentElement.classList.contains("section-head-row") ? head.parentElement.querySelector(":scope > .btn") : null;
      var tl = gsap.timeline({ paused: true, defaults: { ease: "power4.out" } });
      gsap.set(head, { autoAlpha: 1 });
      if (kicker) tl.fromTo(kicker, { "--line": 0, autoAlpha: 0 }, { "--line": 1, autoAlpha: 1, duration: .8 }, 0);
      if (title) {
        var split = splitWords(title);
        if (split) tl.from(split.words, { yPercent: 115, duration: 1, stagger: .045 }, .05);
        else tl.from(title, { y: 30, autoAlpha: 0, duration: .9 }, .05);
      }
      if (text) tl.from(text, { y: 18, autoAlpha: 0, duration: .9 }, .3);
      if (action) tl.from(action, { x: -30 * side, autoAlpha: 0, duration: .9 }, .35);
      ScrollTrigger.create({ trigger: head, start: "top 85%", once: true, onEnter: function () { tl.play(); } });
    });
  }

  // ---------- cards and tiles (everything that used the CSS .reveal fallback) ----------
  function cards() {
    var items = gsap.utils.toArray("main .reveal");
    items.forEach(function (el) { el.classList.remove("reveal", "in"); el.setAttribute("data-anim", ""); });
    gsap.set(items, { autoAlpha: 0, y: 56 });
    ScrollTrigger.batch(items, {
      start: "top 88%",
      once: true,
      onEnter: function (batch) {
        gsap.to(batch, { autoAlpha: 1, y: 0, duration: 1.1, ease: "power3.out", stagger: .1, clearProps: "transform" });
        var icons = [];
        batch.forEach(function (el) { el.querySelectorAll(".icon-tile, .stat > i").forEach(function (i) { icons.push(i); }); });
        if (icons.length) gsap.from(icons, { scale: .3, rotation: -25 * side, autoAlpha: 0, duration: 1, ease: "back.out(2)", stagger: .1, delay: .25 });
        var steps = [];
        batch.forEach(function (el) { el.querySelectorAll(".process-step").forEach(function (s) { steps.push(s); }); });
        if (steps.length) gsap.from(steps, { y: 40, autoAlpha: 0, duration: .9, ease: "power3.out", stagger: .14 });
      },
    });
  }

  // ---------- statistics count up ----------
  function statistics() {
    document.querySelectorAll(".stat-value").forEach(function (el) {
      var match = /^(\D*)(\d[\d,]*)(\D*)$/.exec(el.textContent.trim());
      if (!match) return; // non-numeric values (e.g. Arabic-Indic digits) stay as written
      var target = parseInt(match[2].replace(/,/g, ""), 10);
      var commas = match[2].indexOf(",") !== -1;
      var from = target >= 1900 && target <= 2100 ? target - 30 : 0; // years count from a nearby year, not from zero
      var state = { v: from };
      var render = function () {
        var n = Math.round(state.v);
        el.textContent = match[1] + (commas ? n.toLocaleString("en-US") : String(n)) + match[3];
      };
      render();
      ScrollTrigger.create({
        trigger: el, start: "top 90%", once: true,
        onEnter: function () { gsap.to(state, { v: target, duration: 2.2, ease: "power3.out", onUpdate: render }); },
      });
    });
  }

  // ---------- about: curtain reveal + parallax ----------
  function aboutVisual() {
    document.querySelectorAll("main .about-visual").forEach(function (box) {
      gsap.fromTo(box, { clipPath: "inset(0% 0% 100% 0% round 20px)" }, {
        clipPath: "inset(0% 0% 0% 0% round 20px)", duration: 1.5, ease: "expo.inOut",
        scrollTrigger: { trigger: box, start: "top 80%", once: true },
      });
      var img = box.querySelector(":scope > img");
      if (img) gsap.fromTo(img, { scale: 1.25, yPercent: -6 }, { scale: 1.05, yPercent: 6, ease: "none", scrollTrigger: { trigger: box, start: "top bottom", end: "bottom top", scrub: true } });
      var stamp = box.querySelector(".about-stamp");
      if (stamp) gsap.from(stamp, { y: 40, autoAlpha: 0, duration: 1, ease: "power3.out", scrollTrigger: { trigger: box, start: "top 60%", once: true } });
    });
  }

  // ---------- vision / mission: the gold rule grows in ----------
  function visionBars() {
    document.querySelectorAll("main .vm-card").forEach(function (card) {
      gsap.fromTo(card, { "--bar": 0 }, { "--bar": 1, duration: 1.2, ease: "expo.out", delay: .3, scrollTrigger: { trigger: card, start: "top 85%", once: true } });
    });
  }

  // ---------- service cards: gentle 3D tilt that follows the pointer ----------
  function serviceTilt() {
    if (!window.matchMedia("(hover: hover) and (pointer: fine)").matches) return;
    document.querySelectorAll("#services .card-namc").forEach(function (card) {
      gsap.set(card, { transformPerspective: 900 });
      var rx = gsap.quickTo(card, "rotationX", { duration: .6, ease: "power3" });
      var ry = gsap.quickTo(card, "rotationY", { duration: .6, ease: "power3" });
      card.addEventListener("pointermove", function (e) {
        var r = card.getBoundingClientRect();
        rx(((e.clientY - r.top) / r.height - .5) * -7);
        ry(((e.clientX - r.left) / r.width - .5) * 7);
      });
      card.addEventListener("pointerleave", function () { rx(0); ry(0); });
    });
  }

  // ---------- reading progress ----------
  function scrollProgress() {
    var bar = document.createElement("div");
    bar.className = "scroll-progress";
    bar.setAttribute("aria-hidden", "true");
    document.body.appendChild(bar);
    gsap.to(bar, { scaleX: 1, ease: "none", scrollTrigger: { start: 0, end: "max", scrub: .3 } });
  }
})();
