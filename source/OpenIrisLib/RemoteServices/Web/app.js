// OpenIris web client: control page. Talks to the REST API (/api) and the live data WebSocket (/ws/data).
"use strict";

const $ = (id) => document.getElementById(id);

// ---------------------------------------------------------------------------------------------
// API

async function api(method, path, body) {
  const options = { method };
  if (body !== undefined) {
    options.body = typeof body === "string" ? body : JSON.stringify(body);
  }
  const response = await fetch("/api" + path, options);
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) throw new Error(data && data.error ? data.error : response.statusText);
  return data;
}

function showError(error) {
  $("error").textContent = error ? error.message || String(error) : "";
}

// Runs a command from a button; errors show under the controls.
function command(button, action) {
  $(button).addEventListener("click", async () => {
    showError(null);
    try {
      await action();
      await refreshStatus();
    } catch (error) {
      showError(error);
    }
  });
}

// ---------------------------------------------------------------------------------------------
// Status

let status = null;

async function refreshStatus() {
  try {
    status = await api("GET", "/status");
  } catch (error) {
    status = null;
  }
  renderStatus();
}

function renderStatus() {
  const state = $("state");
  if (!status) {
    state.textContent = "Not connected";
    state.className = "pill bad";
    for (const b of document.querySelectorAll("main button")) b.disabled = true;
    return;
  }

  const s = status;
  state.textContent = s.Recording ? "Recording" : s.Calibrating ? "Calibrating" : s.Tracking ? "Tracking" : s.Processing ? "Processing video" : "Not tracking";
  state.className = "pill " + (s.Recording ? "rec" : s.Tracking ? "ok" : "");
  $("info").textContent = [s.EyeTrackerSystem, s.EyeTrackingPipeline, "Calibration: " + s.CalibrationMethod, s.Version].filter(Boolean).join(" · ");
  document.title = (s.Recording ? "● " : "") + "OpenIris";

  $("startTracking").disabled = !s.NotStarted;
  $("stopTracking").disabled = s.NotStarted;
  $("startRecording").disabled = !s.Tracking || s.Recording;
  $("stopRecording").disabled = !s.Recording;
  $("recordEvent").disabled = !s.Tracking;
  $("startCalibration").disabled = !s.Tracking || s.Calibrating;
  $("cancelCalibration").disabled = !s.Calibrating;
  $("resetReference").disabled = !s.Tracking || s.Calibrating;

  const sessionName = $("sessionName");
  if (document.activeElement !== sessionName) sessionName.value = s.SessionName || "";

  $("grabberStatus").textContent = s.GrabberStatus + (s.HeadTrackerStatus ? " " + s.HeadTrackerStatus : "");
  $("processorStatus").textContent = s.ProcessorStatus;
  $("recorderStatus").textContent = s.RecorderStatus;

  for (const eye of ["Left", "Right"]) {
    $("eye" + eye).style.display = s.Eye === "Both" || s.Eye === eye ? "" : "none";
  }

  if (s.LastRecordedFile !== lastFile || s.Recording !== lastRecording) {
    lastFile = s.LastRecordedFile;
    lastRecording = s.Recording;
    refreshFiles();
  }
}

// ---------------------------------------------------------------------------------------------
// Commands

command("startTracking", () => api("POST", "/tracking/start"));
command("stopTracking", () => api("POST", "/tracking/stop"));
command("startRecording", () => api("POST", "/recording/start"));
command("stopRecording", async () => {
  if (!confirm("Stop the recording? If another program started it, that program may not expect it to stop.")) return;
  await api("POST", "/recording/stop");
});
command("startCalibration", () => api("POST", "/calibration/start"));
command("cancelCalibration", () => api("POST", "/calibration/cancel"));
command("resetReference", () => api("POST", "/reference/reset"));

async function recordEvent() {
  const input = $("eventMessage");
  const result = await api("POST", "/events", input.value);
  input.value = "";
  input.placeholder = "Saved at frame " + result.FrameNumber;
}
command("recordEvent", recordEvent);
$("eventMessage").addEventListener("keydown", (e) => {
  if (e.key === "Enter") recordEvent().catch(showError);
});

$("sessionName").addEventListener("change", (e) => {
  api("PUT", "/settings/SessionName", JSON.stringify(e.target.value)).catch(showError);
});

// ---------------------------------------------------------------------------------------------
// Files of the last recording

let lastFile = null;
let lastRecording = null;

async function refreshFiles() {
  const box = $("files");
  try {
    const result = await api("GET", "/files/last");
    if (result.Recording) {
      box.textContent = "Recording...";
    } else if (!result.Files.length) {
      box.textContent = "None";
    } else {
      box.replaceChildren(...result.Files.map((name) => {
        const a = document.createElement("a");
        a.href = "/api/files/" + encodeURIComponent(name);
        a.textContent = name;
        a.download = name;
        return a;
      }));
    }
  } catch (error) {
    box.textContent = error.message;
  }
}

// ---------------------------------------------------------------------------------------------
// Eye images: each eye asks for the next image when the last one arrives, up to 15 per second.

function startImageLoop(eye) {
  const box = document.querySelector("#eye" + eye + " .image");
  const img = document.createElement("img");
  img.alt = eye + " eye";
  let url = null;

  async function next() {
    const started = performance.now();
    let wait = 1000;
    if (status && (status.Tracking || status.Processing) && $("eye" + eye).style.display !== "none") {
      try {
        const response = await fetch(`/api/images/${eye.toLowerCase()}?overlay=${$("overlay").checked}`, { cache: "no-store" });
        if (response.ok) {
          const blob = await response.blob();
          if (url) URL.revokeObjectURL(url);
          url = URL.createObjectURL(blob);
          img.src = url;
          if (img.parentNode !== box) box.replaceChildren(img);
          wait = Math.max(0, 66 - (performance.now() - started));
        } else if (response.status === 503) {
          wait = 30;
        }
      } catch (error) {
        // Not connected; try again later.
      }
    } else if (img.parentNode === box && !(status && status.Tracking)) {
      box.innerHTML = "<span>No image</span>";
    }
    setTimeout(next, wait);
  }
  next();
}

// ---------------------------------------------------------------------------------------------
// Settings: the whole list, editable, plus the pupil and reflection thresholds under each eye.

let settings = [];

async function refreshSettings() {
  try {
    settings = await api("GET", "/settings");
  } catch (error) {
    return;
  }
  renderThresholds();
  if ($("settingsBox").open) renderSettings();
}

function setSetting(name, value) {
  return api("PUT", "/settings/" + encodeURIComponent(name), JSON.stringify(value))
    .then(refreshSettings)
    .catch((error) => { showError(error); refreshSettings(); });
}

function renderThresholds() {
  for (const eye of ["Left", "Right"]) {
    const box = document.querySelector("#eye" + eye + " .thresholds");
    const found = settings.filter((s) => s.Group === "Tracking" && /Threshold/.test(s.Name) && s.Name.endsWith(eye + "Eye") && s.Type === "integer");
    box.replaceChildren(...found.map((s) => {
      const row = document.createElement("div");
      row.className = "threshold";
      const label = s.Name.replace("Tracking.", "").replace(eye + "Eye", "").replace(/([a-z])([A-Z])/g, "$1 $2").toLowerCase();
      row.innerHTML = `<span></span><button class="small">−</button><b></b><button class="small">+</button>`;
      row.children[0].textContent = label.charAt(0).toUpperCase() + label.slice(1);
      row.children[0].title = s.Description || "";
      row.children[2].textContent = s.Value;
      row.children[1].onclick = () => setSetting(s.Name, Math.max(0, s.Value - 1));
      row.children[3].onclick = () => setSetting(s.Name, Math.min(255, s.Value + 1));
      return row;
    }));
  }
}

function renderSettings() {
  const container = $("settings");
  // Don't rebuild while editing a value.
  if (container.contains(document.activeElement)) return;

  const groups = {};
  for (const s of settings) (groups[s.Group] = groups[s.Group] || []).push(s);

  container.replaceChildren(...Object.keys(groups).map((group) => {
    const box = document.createElement("div");
    box.className = "settings-group";
    const title = document.createElement("h3");
    title.textContent = { General: "General", System: "Eye tracking system", Tracking: "Tracking pipeline", Calibration: "Calibration" }[group] || group;
    box.appendChild(title);

    let category = null;
    for (const s of groups[group].slice().sort((a, b) => (a.Category || "").localeCompare(b.Category || ""))) {
      if (s.Category !== category) {
        category = s.Category;
        const c = document.createElement("div");
        c.className = "category";
        c.textContent = (category || "").replace(/^[A-Z]\) /, "");
        box.appendChild(c);
      }
      box.appendChild(settingRow(s));
    }
    return box;
  }));
}

function settingRow(s) {
  const row = document.createElement("label");
  row.className = "setting";
  row.title = s.Description || "";
  const name = document.createElement("span");
  name.textContent = s.Name.replace(/^\w+\./, "");
  row.appendChild(name);

  let input;
  if (s.Type === "boolean") {
    input = document.createElement("input");
    input.type = "checkbox";
    input.checked = !!s.Value;
    input.onchange = () => setSetting(s.Name, input.checked);
  } else if (s.Options && s.Options.length) {
    input = document.createElement("select");
    for (const o of s.Options) input.add(new Option(o, o, false, o === String(s.Value)));
    input.onchange = () => setSetting(s.Name, input.value);
  } else {
    input = document.createElement("input");
    input.type = s.Type === "integer" || s.Type === "number" ? "number" : "text";
    if (s.Type === "number") input.step = "any";
    input.value = s.Value === null ? "" : s.Value;
    input.onchange = () => setSetting(s.Name, input.type === "number" ? Number(input.value) : input.value);
  }
  input.disabled = s.IsReadOnly;
  row.appendChild(input);
  return row;
}

$("settingsBox").addEventListener("toggle", () => { if ($("settingsBox").open) renderSettings(); });

// ---------------------------------------------------------------------------------------------
// Live trace

// Panels as in the OpenIris window. Signals are columns of the data file (docs/user-manual/data-file.md).
const PANELS = [
  { name: "Horizontal", unit: "deg", minSpan: 10, series: [["LeftH", "left"], ["RightH", "right"]], on: true },
  { name: "Vertical", unit: "deg", minSpan: 10, series: [["LeftV", "left"], ["RightV", "right"]], on: true },
  { name: "Torsion", unit: "deg", minSpan: 4, series: [["LeftT", "left"], ["RightT", "right"]], on: true },
  { name: "Pupil", unit: "px", minSpan: 10, series: [["LeftPupilWidth", "left"], ["RightPupilWidth", "right"]], on: false },
  { name: "Pupil X", unit: "px", minSpan: 10, series: [["LeftPupilX", "left"], ["RightPupilX", "right"]], on: false },
  { name: "Pupil Y", unit: "px", minSpan: 10, series: [["LeftPupilY", "left"], ["RightPupilY", "right"]], on: false },
  { name: "Head velocity", unit: "deg/s", minSpan: 10, series: [["HeadRollVelocity", "#3b82f6"], ["HeadPitchVelocity", "#ef4444"], ["HeadYawVelocity", "#22a05a"]], on: false },
];

const trace = {
  columns: null,     // name -> index in the rows
  time: [],          // seconds of each sample
  values: {},        // column -> array of values, same length as time
  paused: false,
  received: 0,       // samples in the last second, for the rate shown
  lastRow: null,
};

function savedPanels() {
  try { return JSON.parse(localStorage.getItem("openiris.panels")) || null; } catch (e) { return null; }
}
(function initPanels() {
  const saved = savedPanels();
  const toggles = $("panelToggles");
  for (const panel of PANELS) {
    if (saved) panel.on = saved.includes(panel.name);
    const label = document.createElement("label");
    const box = document.createElement("input");
    box.type = "checkbox";
    box.checked = panel.on;
    box.onchange = () => {
      panel.on = box.checked;
      try { localStorage.setItem("openiris.panels", JSON.stringify(PANELS.filter((p) => p.on).map((p) => p.name))); } catch (e) { /* not saved */ }
      resizeTrace();
    };
    label.append(box, " " + panel.name);
    toggles.appendChild(label);
    toggles.append(" ");
  }
})();

function connectStream() {
  const socket = new WebSocket((location.protocol === "https:" ? "wss://" : "ws://") + location.host + "/ws/data");
  socket.onmessage = (event) => {
    const message = JSON.parse(event.data);
    if (message.type === "columns") {
      trace.columns = {};
      message.columns.forEach((c, i) => { trace.columns[c] = i; });
      clearTrace();
    } else if (message.type === "data") {
      addRows(message.rows);
    }
  };
  socket.onclose = () => setTimeout(connectStream, 1000);
}

function clearTrace() {
  trace.time = [];
  trace.values = {};
  for (const panel of PANELS) for (const [column] of panel.series) trace.values[column] = [];
}

function addRows(rows) {
  if (!trace.columns || !rows.length) return;
  trace.received += rows.length;
  trace.lastRow = rows[rows.length - 1];
  if (trace.paused) return;

  const c = trace.columns;
  for (const row of rows) {
    // Seconds of whichever eye there is.
    const t = row[c.LeftSeconds] || row[c.RightSeconds] || 0;
    const last = trace.time.length ? trace.time[trace.time.length - 1] : null;
    // A new video or a restart: time goes back.
    if (last !== null && t < last - 0.5) clearTrace();
    trace.time.push(t);
    for (const column in trace.values) {
      const v = row[c[column]];
      trace.values[column].push(v === null || v === undefined ? NaN : v);
    }
  }

  // Keep 60 s (the longest span).
  const newest = trace.time[trace.time.length - 1];
  let drop = 0;
  while (drop < trace.time.length && trace.time[drop] < newest - 60) drop++;
  if (drop > 0) {
    trace.time.splice(0, drop);
    for (const column in trace.values) trace.values[column].splice(0, drop);
  }
}

function showLatestValues() {
  const row = trace.lastRow;
  if (!row || !trace.columns) return;
  const c = trace.columns;
  const show = (v) => (v === null || v === undefined ? "–" : v.toFixed(1) + "°");
  for (const eye of ["Left", "Right"]) {
    const box = $("eye" + eye);
    box.querySelector("[data-v=H]").textContent = show(row[c[eye + "H"]]);
    box.querySelector("[data-v=V]").textContent = show(row[c[eye + "V"]]);
    box.querySelector("[data-v=T]").textContent = show(row[c[eye + "T"]]);
  }
}

const canvas = $("trace");
const ctx = canvas.getContext("2d");
const PANEL_HEIGHT = 140;

function resizeTrace() {
  const panels = PANELS.filter((p) => p.on).length;
  const ratio = window.devicePixelRatio || 1;
  const width = canvas.clientWidth;
  const height = Math.max(1, panels) * PANEL_HEIGHT;
  canvas.style.height = height + "px";
  canvas.width = Math.round(width * ratio);
  canvas.height = Math.round(height * ratio);
  ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
}
window.addEventListener("resize", resizeTrace);

function cssColor(name) {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

function drawTrace() {
  const width = canvas.clientWidth;
  const panels = PANELS.filter((p) => p.on);
  const span = Number($("span").value);
  const colors = { left: cssColor("--left"), right: cssColor("--right") };
  const grid = cssColor("--grid");
  const muted = cssColor("--muted");
  const left = 64, right = 12;
  const plotWidth = width - left - right;

  ctx.clearRect(0, 0, width, panels.length * PANEL_HEIGHT);
  ctx.font = "11px system-ui, sans-serif";

  const n = trace.time.length;
  const newest = n ? trace.time[n - 1] : 0;
  const start = newest - span;
  let first = 0;
  while (first < n && trace.time[first] < start) first++;

  panels.forEach((panel, p) => {
    const top = p * PANEL_HEIGHT + 8;
    const height = PANEL_HEIGHT - 24;

    // Vertical range: the visible data, at least minSpan, centered.
    let lo = Infinity, hi = -Infinity;
    for (const [column] of panel.series) {
      const values = trace.values[column] || [];
      for (let i = first; i < n; i++) {
        const v = values[i];
        if (v === v) { if (v < lo) lo = v; if (v > hi) hi = v; }
      }
    }
    if (lo === Infinity) { lo = -panel.minSpan / 2; hi = panel.minSpan / 2; }
    if (hi - lo < panel.minSpan) { const mid = (hi + lo) / 2; lo = mid - panel.minSpan / 2; hi = mid + panel.minSpan / 2; }
    const pad = (hi - lo) * 0.08; lo -= pad; hi += pad;
    const y = (v) => top + height - ((v - lo) / (hi - lo)) * height;

    // Grid and labels.
    ctx.strokeStyle = grid;
    ctx.fillStyle = muted;
    ctx.lineWidth = 1;
    const step = niceStep((hi - lo) / 4);
    ctx.textAlign = "right";
    ctx.textBaseline = "middle";
    for (let v = Math.ceil(lo / step) * step; v <= hi; v += step) {
      const yy = Math.round(y(v)) + 0.5;
      ctx.beginPath(); ctx.moveTo(left, yy); ctx.lineTo(width - right, yy); ctx.stroke();
      ctx.fillText(+v.toFixed(6) + "", left - 6, yy);
    }
    ctx.save();
    ctx.translate(12, top + height / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.textAlign = "center";
    ctx.fillText(`${panel.name} (${panel.unit})`, 0, 0);
    ctx.restore();

    // Each series: the min and max of the samples that fall in each pixel column, so all
    // the samples count however many there are.
    ctx.save();
    ctx.beginPath(); ctx.rect(left, top, plotWidth, height); ctx.clip();
    ctx.lineWidth = 1.5;
    for (const [column, color] of panel.series) {
      const values = trace.values[column] || [];
      ctx.strokeStyle = colors[color] || color;
      ctx.beginPath();
      let pen = false, px = -1, mn = 0, mx = 0;
      const flush = () => {
        if (px < 0) return;
        const x = left + px + 0.5;
        if (pen) ctx.lineTo(x, y(mn)); else { ctx.moveTo(x, y(mn)); pen = true; }
        if (mx !== mn) ctx.lineTo(x, y(mx));
      };
      for (let i = first; i < n; i++) {
        const v = values[i];
        const col = Math.floor(((trace.time[i] - start) / span) * plotWidth);
        if (v !== v) { flush(); px = -1; pen = false; continue; }
        if (col !== px) { flush(); px = col; mn = mx = v; }
        else { if (v < mn) mn = v; if (v > mx) mx = v; }
      }
      flush();
      ctx.stroke();
    }
    ctx.restore();
  });

  // Time axis under the last panel.
  if (panels.length) {
    const yy = panels.length * PANEL_HEIGHT - 8;
    ctx.fillStyle = muted;
    ctx.textAlign = "center";
    ctx.textBaseline = "alphabetic";
    const step = niceStep(span / 5);
    for (let s = 0; s <= span + 1e-9; s += step) {
      ctx.fillText(s === 0 ? "now" : "-" + +s.toFixed(3) + " s", left + plotWidth - (s / span) * plotWidth, yy);
    }
  }
}

function niceStep(raw) {
  const power = Math.pow(10, Math.floor(Math.log10(raw)));
  const f = raw / power;
  return (f < 1.5 ? 1 : f < 3.5 ? 2 : f < 7.5 ? 5 : 10) * power;
}

$("pause").addEventListener("click", () => {
  trace.paused = !trace.paused;
  $("pause").textContent = trace.paused ? "Resume" : "Pause";
});

function animate() {
  drawTrace();
  showLatestValues();
  requestAnimationFrame(animate);
}

setInterval(() => {
  $("rate").textContent = trace.received ? trace.received + " samples/s" : "";
  trace.received = 0;
}, 1000);

// ---------------------------------------------------------------------------------------------

resizeTrace();
refreshStatus();
refreshSettings();
setInterval(refreshStatus, 500);
setInterval(refreshSettings, 2000);
startImageLoop("Left");
startImageLoop("Right");
connectStream();
animate();
