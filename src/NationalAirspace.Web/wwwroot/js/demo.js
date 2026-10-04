// Demo airspace dashboard: refreshes SIMULATED data every 30 seconds. No operational data is involved.
(function () {
  "use strict";
  var root = document.querySelector("[data-demo-url]");
  if (!root || !window.fetch) return;
  var url = root.getAttribute("data-demo-url");
  var labels = document.getElementById("level-labels");
  var levelNames = ["Normal", "Elevated", "High"];
  var icons = { Normal: "bi-check-circle-fill", Elevated: "bi-exclamation-triangle-fill", High: "bi-exclamation-octagon-fill" };
  var textClass = { Normal: "text-success", Elevated: "text-warning", High: "text-danger" };
  function levelOf(v) { return typeof v === "number" ? levelNames[v] : v; }
  function label(level) { return labels ? labels.getAttribute("data-" + level.toLowerCase()) : level; }

  function apply(data) {
    Object.keys(data).forEach(function (k) {
      var el = root.querySelector('[data-kpi="' + k + '"]');
      if (el) el.textContent = data[k];
    });
    var gen = root.querySelector("[data-generated]");
    if (gen) gen.textContent = new Date(data.generatedAtUtc).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false });

    data.sectors.forEach(function (s) {
      var level = levelOf(s.level);
      var g = root.querySelector('[data-sector="' + s.code + '"]');
      if (g) {
        g.setAttribute("class", "lvl-" + level);
        g.querySelector("rect").setAttribute("class", "sector lvl-" + level);
        g.querySelector("[data-load]").textContent = s.loadPercent + "%";
        g.setAttribute("data-tip", s.name + " — " + s.demand + " / " + s.capacity + " (" + s.loadPercent + "%) — " + label(level));
      }
      var row = root.querySelector('[data-sector-row="' + s.code + '"]');
      if (row) {
        var fill = row.querySelector(".bar-fill");
        fill.className = "bar-fill lvl-bg-" + level;
        fill.style.width = Math.min(100, s.loadPercent) + "%";
        row.querySelector("[data-load]").innerHTML = '<i class="bi ' + icons[level] + '" aria-hidden="true"></i> ' + s.loadPercent + "%";
      }
    });

    data.airports.forEach(function (a) {
      var row = root.querySelector('[data-airport="' + a.code + '"]');
      if (!row) return;
      var level = levelOf(a.level);
      row.querySelector('[data-f="arrivals"]').textContent = a.arrivals;
      row.querySelector('[data-f="departures"]').textContent = a.departures;
      row.querySelector('[data-f="averageDelayMinutes"]').textContent = a.averageDelayMinutes;
      row.querySelector('[data-f="level"]').innerHTML = '<i class="bi ' + icons[level] + " " + textClass[level] + '" aria-hidden="true"></i> ' + label(level);
    });

    var list = root.querySelector("[data-notifications]");
    if (list) {
      list.innerHTML = "";
      data.notifications.forEach(function (n) {
        var div = document.createElement("div");
        div.className = "notif " + n.severity;
        var icon = document.createElement("i");
        icon.className = "bi " + (n.severity === "warning" ? "bi-exclamation-triangle" : n.severity === "info" ? "bi-info-circle" : "bi-shield-check");
        icon.setAttribute("aria-hidden", "true");
        var span = document.createElement("span");
        span.textContent = n.message;
        div.appendChild(icon);
        div.appendChild(span);
        list.appendChild(div);
      });
    }
  }

  function refresh() {
    if (document.hidden) return;
    fetch(url, { headers: { Accept: "application/json" } })
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (d) { if (d && d.isDemo) apply(d); })
      .catch(function () { /* keep last snapshot */ });
  }
  setInterval(refresh, 30000);
})();
