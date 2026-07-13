/* LedgerFlow — front-end interactions (mockup only, no backend) */
(function () {
  "use strict";

  /* Animated count-up for anything with [data-count] */
  function animateCounts() {
    document.querySelectorAll("[data-count]").forEach(function (el) {
      var target = parseFloat(el.getAttribute("data-count"));
      var prefix = el.getAttribute("data-prefix") || "";
      var suffix = el.getAttribute("data-suffix") || "";
      var decimals = parseInt(el.getAttribute("data-decimals") || "0", 10);
      var dur = 900 + Math.random() * 500;
      var start = performance.now();
      function tick(now) {
        var p = Math.min((now - start) / dur, 1);
        var eased = 1 - Math.pow(1 - p, 3);
        var v = target * eased;
        el.textContent =
          prefix + v.toLocaleString("en-US", {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals,
          }) + suffix;
        if (p < 1) requestAnimationFrame(tick);
      }
      requestAnimationFrame(tick);
    });
  }

  /* Build a smooth area + line chart into an <svg data-line="v1,v2,..."> */
  function drawLineCharts() {
    document.querySelectorAll("svg[data-line]").forEach(function (svg) {
      var vals = svg.getAttribute("data-line").split(",").map(Number);
      var w = 100, h = 34;
      var max = Math.max.apply(null, vals) * 1.1;
      var min = Math.min.apply(null, vals) * 0.9;
      var span = max - min || 1;
      var pts = vals.map(function (v, i) {
        var x = (i / (vals.length - 1)) * w;
        var y = h - ((v - min) / span) * h;
        return [x, y];
      });
      var d = pts.map(function (p, i) { return (i ? "L" : "M") + p[0].toFixed(1) + " " + p[1].toFixed(1); }).join(" ");
      var area = d + " L" + w + " " + h + " L0 " + h + " Z";
      var stroke = svg.getAttribute("data-stroke") || "var(--accent-2)";
      var id = "g" + Math.random().toString(36).slice(2, 8);
      svg.setAttribute("viewBox", "0 0 " + w + " " + h);
      svg.setAttribute("preserveAspectRatio", "none");
      svg.innerHTML =
        '<defs><linearGradient id="' + id + '" x1="0" y1="0" x2="0" y2="1">' +
        '<stop offset="0" stop-color="' + stroke + '" stop-opacity="0.35"/>' +
        '<stop offset="1" stop-color="' + stroke + '" stop-opacity="0"/></linearGradient></defs>' +
        '<path d="' + area + '" fill="url(#' + id + ')"/>' +
        '<path d="' + d + '" fill="none" stroke="' + stroke + '" stroke-width="1.6" ' +
        'stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"/>';
    });
  }

  /* Grouped/soft bar chart into <svg data-bars="v1,v2,..."> */
  function drawBarCharts() {
    document.querySelectorAll("svg[data-bars]").forEach(function (svg) {
      var vals = svg.getAttribute("data-bars").split(",").map(Number);
      var labels = (svg.getAttribute("data-labels") || "").split(",");
      var w = 320, h = 150, pad = 22, gap = 10;
      var max = Math.max.apply(null, vals) * 1.15 || 1;
      var bw = (w - pad) / vals.length - gap;
      var stroke = svg.getAttribute("data-stroke") || "var(--accent)";
      var body = "";
      for (var g = 1; g <= 3; g++) {
        var gy = h - pad - ((h - pad * 1.4) * g) / 3;
        body += '<line x1="' + pad + '" y1="' + gy.toFixed(1) + '" x2="' + w + '" y2="' + gy.toFixed(1) +
          '" stroke="var(--border-soft)" stroke-width="1"/>';
      }
      vals.forEach(function (v, i) {
        var bh = ((v / max) * (h - pad * 1.4));
        var x = pad + i * (bw + gap);
        var y = h - pad - bh;
        body += '<rect x="' + x.toFixed(1) + '" y="' + y.toFixed(1) + '" width="' + bw.toFixed(1) +
          '" height="' + bh.toFixed(1) + '" rx="4" fill="' + stroke + '" opacity="' + (0.55 + 0.4 * (v / max)) + '">' +
          '<animate attributeName="height" from="0" to="' + bh.toFixed(1) + '" dur="0.7s" fill="freeze"/>' +
          '<animate attributeName="y" from="' + (h - pad) + '" to="' + y.toFixed(1) + '" dur="0.7s" fill="freeze"/></rect>';
        if (labels[i]) {
          body += '<text x="' + (x + bw / 2).toFixed(1) + '" y="' + (h - 6) + '" fill="var(--text-faint)" ' +
            'font-size="9" text-anchor="middle">' + labels[i] + '</text>';
        }
      });
      svg.setAttribute("viewBox", "0 0 " + w + " " + h);
      svg.innerHTML = body;
    });
  }

  /* Donut into <svg data-donut="v1,v2,..." data-colors="c1,c2,..."> */
  function drawDonuts() {
    document.querySelectorAll("svg[data-donut]").forEach(function (svg) {
      var vals = svg.getAttribute("data-donut").split(",").map(Number);
      var colors = (svg.getAttribute("data-colors") || "").split(",");
      var total = vals.reduce(function (a, b) { return a + b; }, 0) || 1;
      var cx = 60, cy = 60, r = 46, sw = 16;
      var circ = 2 * Math.PI * r;
      var offset = 0;
      var body = '<circle cx="' + cx + '" cy="' + cy + '" r="' + r + '" fill="none" stroke="var(--panel-2)" stroke-width="' + sw + '"/>';
      vals.forEach(function (v, i) {
        var frac = v / total;
        var len = frac * circ;
        body += '<circle cx="' + cx + '" cy="' + cy + '" r="' + r + '" fill="none" stroke="' + (colors[i] || "var(--accent)") +
          '" stroke-width="' + sw + '" stroke-linecap="round" ' +
          'stroke-dasharray="' + len.toFixed(1) + " " + (circ - len).toFixed(1) + '" ' +
          'stroke-dashoffset="' + (-offset).toFixed(1) + '" transform="rotate(-90 ' + cx + " " + cy + ')"/>';
        offset += len;
      });
      var center = svg.getAttribute("data-center") || "";
      var sub = svg.getAttribute("data-sub") || "";
      if (center) {
        body += '<text x="' + cx + '" y="' + (cy - 2) + '" text-anchor="middle" fill="var(--text)" font-size="20" font-weight="700">' + center + '</text>';
        body += '<text x="' + cx + '" y="' + (cy + 15) + '" text-anchor="middle" fill="var(--text-faint)" font-size="9">' + sub + '</text>';
      }
      svg.setAttribute("viewBox", "0 0 120 120");
      svg.innerHTML = body;
    });
  }

  /* Occasional "live" nudge on elements marked [data-live] */
  function liveTicker() {
    var live = document.querySelectorAll("[data-live]");
    if (!live.length) return;
    setInterval(function () {
      var el = live[Math.floor(Math.random() * live.length)];
      var base = parseFloat(el.getAttribute("data-count") || el.textContent.replace(/[^0-9.]/g, "")) || 0;
      var next = base + Math.round(Math.random() * 3);
      var prefix = el.getAttribute("data-prefix") || "";
      var suffix = el.getAttribute("data-suffix") || "";
      el.setAttribute("data-count", next);
      el.textContent = prefix + next.toLocaleString("en-US") + suffix;
      el.animate(
        [{ color: "var(--accent-2)" }, { color: "var(--text)" }],
        { duration: 1200, easing: "ease-out" }
      );
    }, 3500);
  }

  /* Simple tab groups: [data-tabs] buttons toggle .active */
  function tabs() {
    document.querySelectorAll("[data-tabs]").forEach(function (group) {
      group.querySelectorAll("button").forEach(function (b) {
        b.addEventListener("click", function () {
          group.querySelectorAll("button").forEach(function (x) { x.classList.remove("active"); });
          b.classList.add("active");
        });
      });
    });
  }

  /* Mobile: reveal nothing fancy; just keep clicks from navigating dead links */
  function guardDeadLinks() {
    document.querySelectorAll('a[href="#"], .nav-item[data-noop]').forEach(function (el) {
      el.addEventListener("click", function (e) { e.preventDefault(); });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    animateCounts();
    drawLineCharts();
    drawBarCharts();
    drawDonuts();
    liveTicker();
    tabs();
    guardDeadLinks();
  });
})();
