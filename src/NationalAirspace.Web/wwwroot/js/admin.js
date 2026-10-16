// Admin enhancements. Every screen works without JavaScript; this adds confirmations,
// the rich-text editor, the media picker and small conveniences.
(function () {
  "use strict";
  document.documentElement.classList.remove("no-js");
  var script = document.currentScript;

  // ---- confirmation modal for form[data-confirm] ----
  var confirmEl = document.getElementById("confirmModal");
  var pendingForm = null;
  document.addEventListener("submit", function (e) {
    var form = e.target;
    if (!form.matches || !form.matches("form[data-confirm]") || form.dataset.confirmed === "1" || !confirmEl || !window.bootstrap) return;
    e.preventDefault();
    pendingForm = form;
    confirmEl.querySelector("[data-confirm-text]").textContent = form.getAttribute("data-confirm");
    bootstrap.Modal.getOrCreateInstance(confirmEl).show();
  }, true);
  if (confirmEl) {
    confirmEl.querySelector("[data-confirm-ok]").addEventListener("click", function () {
      if (!pendingForm) return;
      pendingForm.dataset.confirmed = "1";
      bootstrap.Modal.getOrCreateInstance(confirmEl).hide();
      pendingForm.submit();
    });
  }

  // ---- filters submit on change ----
  document.addEventListener("change", function (e) {
    if (e.target.matches && e.target.matches("[data-autosubmit]") && e.target.form) e.target.form.submit();
  });

  // ---- show the language tab that contains the first validation error ----
  function activateErrorTabs(root) {
    (root || document).querySelectorAll(".lang-tabs .nav-link").forEach(function (l) { l.classList.remove("has-error"); });
    var first = null;
    document.querySelectorAll(".tab-pane").forEach(function (pane) {
      if (!pane.querySelector(".is-invalid, .input-validation-error, .field-validation-error:not(:empty)")) return;
      var link = document.querySelector('[data-bs-target="#' + pane.id + '"]');
      if (link) { link.classList.add("has-error"); if (!first) first = link; }
    });
    if (first && window.bootstrap) bootstrap.Tab.getOrCreateInstance(first).show();
  }
  activateErrorTabs();
  if (window.jQuery) {
    jQuery(document).on("invalid-form.validate", "form", function () { setTimeout(activateErrorTabs, 0); });
  }

  // ---- rich text (Quill), loaded only when needed ----
  var sources = document.querySelectorAll("textarea[data-richtext]");
  function load(tag, attrs) {
    return new Promise(function (resolve, reject) {
      var el = document.createElement(tag);
      Object.keys(attrs).forEach(function (k) { el[k] = attrs[k]; });
      el.onload = resolve; el.onerror = reject;
      document.head.appendChild(el);
    });
  }
  if (sources.length && script) {
    load("link", { rel: "stylesheet", href: script.getAttribute("data-quill-css") })
      .then(function () { return load("script", { src: script.getAttribute("data-quill-js") }); })
      .then(function () { sources.forEach(enhance); })
      .catch(function () { /* the plain textarea keeps working */ });
  }
  function enhance(textarea) {
    var wrap = document.createElement("div");
    wrap.className = "richtext-wrap" + (textarea.classList.contains("is-invalid") ? " is-invalid" : "");
    wrap.setAttribute("dir", textarea.getAttribute("dir") || document.documentElement.dir);
    var editor = document.createElement("div");
    editor.innerHTML = textarea.value; // server-sanitized HTML
    wrap.appendChild(editor);
    textarea.parentNode.insertBefore(wrap, textarea.nextSibling);
    textarea.classList.add("is-enhanced");
    var quill = new Quill(editor, {
      theme: "snow",
      modules: { toolbar: [[{ header: [2, 3, 4, false] }], ["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], [{ align: [] }], ["link", "blockquote"], ["clean"]] }
    });
    var label = document.querySelector('label[for="' + textarea.id + '"]');
    if (label) quill.root.setAttribute("aria-label", label.textContent.trim());
    function sync() { textarea.value = quill.getLength() <= 1 ? "" : quill.getSemanticHTML(); }
    quill.on("text-change", sync);
    if (textarea.form) textarea.form.addEventListener("submit", sync);
  }

  // ---- media picker ----
  var pickerEl = document.getElementById("mediaPickerModal");
  var activeField = null;
  function loadPicker(url) {
    var body = pickerEl.querySelector("[data-picker-body]");
    fetch(url, { headers: { "X-Requested-With": "fetch" }, credentials: "same-origin" })
      .then(function (r) { if (!r.ok) throw new Error(r.status); return r.text(); })
      .then(function (html) { body.innerHTML = html; })
      .catch(function () { body.innerHTML = '<div class="alert alert-danger mb-0" role="alert">' + (pickerEl.getAttribute("data-error-text") || "Error") + "</div>"; });
  }
  function setPreview(field, html) { field.querySelector("[data-media-preview]").innerHTML = html; }
  document.addEventListener("click", function (e) {
    var pick = e.target.closest("[data-media-pick]");
    if (pick && pickerEl && window.bootstrap) {
      activeField = pick.closest("[data-media-field]");
      var url = new URL(pickerEl.getAttribute("data-picker-url"), location.origin);
      url.searchParams.set("kind", activeField.getAttribute("data-media-field"));
      loadPicker(url.toString());
      bootstrap.Modal.getOrCreateInstance(pickerEl).show();
      return;
    }
    var clear = e.target.closest("[data-media-clear]");
    if (clear) {
      var field = clear.closest("[data-media-field]");
      field.querySelector("[data-media-value]").value = "";
      var file = field.querySelector('input[type="file"]');
      if (file) file.value = "";
      var span = document.createElement("span");
      span.className = "text-muted small";
      span.textContent = pickerEl ? pickerEl.getAttribute("data-empty-text") : "";
      setPreview(field, ""); field.querySelector("[data-media-preview]").appendChild(span);
      return;
    }
    if (!pickerEl || !pickerEl.contains(e.target)) return;
    var choice = e.target.closest("[data-media-id]");
    if (choice && activeField) {
      activeField.querySelector("[data-media-value]").value = choice.getAttribute("data-media-id");
      var preview = activeField.querySelector("[data-media-preview]");
      preview.textContent = "";
      if (choice.getAttribute("data-media-url")) {
        var img = document.createElement("img");
        img.src = choice.getAttribute("data-media-url"); img.alt = ""; img.className = "media-thumb";
        preview.appendChild(img);
      } else {
        var doc = document.createElement("span");
        doc.className = "media-doc"; doc.textContent = choice.getAttribute("data-media-name");
        preview.appendChild(doc);
      }
      bootstrap.Modal.getOrCreateInstance(pickerEl).hide();
      return;
    }
    var link = e.target.closest("[data-picker-body] a[href]");
    if (link) { e.preventDefault(); loadPicker(link.href); }
  });
  if (pickerEl) {
    pickerEl.addEventListener("submit", function (e) {
      var form = e.target;
      if (!form.matches("[data-picker-search]")) return;
      e.preventDefault();
      var url = new URL(form.action, location.origin);
      new FormData(form).forEach(function (v, k) { url.searchParams.set(k, v); });
      loadPicker(url.toString());
    });
  }

  // ---- icon picker: fills the icon text field, which stays editable by hand ----
  document.querySelectorAll("[data-icon-picker]").forEach(function (picker) {
    var input = document.getElementById(picker.getAttribute("data-icon-picker"));
    if (!input) return;
    var preview = picker.querySelector("[data-icon-preview]");
    var items = Array.prototype.slice.call(picker.querySelectorAll("[data-icon]"));
    var fallback = "ph-squares-four";
    function sync() {
      var value = input.value.trim();
      preview.className = "ph-light " + (/^ph-[a-z0-9-]+$/.test(value) ? value : fallback);
      items.forEach(function (b) { b.setAttribute("aria-selected", b.getAttribute("data-icon") === value ? "true" : "false"); });
    }
    items.forEach(function (b) {
      b.addEventListener("click", function () { input.value = b.getAttribute("data-icon"); input.dispatchEvent(new Event("change", { bubbles: true })); sync(); });
    });
    picker.querySelector("[data-icon-clear]").addEventListener("click", function () { input.value = ""; sync(); });
    picker.querySelector("[data-icon-search]").addEventListener("keydown", function (e) {
      if (e.key === "Enter") e.preventDefault();
    });
    picker.querySelector("[data-icon-search]").addEventListener("input", function (e) {
      var q = e.target.value.trim().toLowerCase();
      items.forEach(function (b) { b.hidden = q !== "" && b.getAttribute("data-keywords").toLowerCase().indexOf(q) === -1; });
    });
    input.addEventListener("input", sync);
    sync();
  });
})();
