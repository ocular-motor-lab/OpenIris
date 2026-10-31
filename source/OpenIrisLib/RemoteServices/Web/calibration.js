// OpenIris web client: full-screen N-point calibration and validation.
//
// Calibration drives the Remote Linear method (CalibrationRemoteLinear.cs, by Ryan Ressmeyer) with its
// messages: for each target FixStart:x,y (target in degrees, y up) and FixStop, then Complete. OpenIris
// fits, for each eye, a linear map from the eye signal (source minus reference, pupil minus CR1 by
// default) to the targets and saves it in the calibration settings (LeftGxx...).
//
// Validation shows the targets again and compares them with the gaze from that map, computed here
// from the live data stream. It doesn't change anything in OpenIris.
"use strict";

const $ = (id) => document.getElementById(id);

// ---------------------------------------------------------------------------------------------
// Options, remembered in this browser.

const OPTIONS = {
  screenWidth: 53, distance: 57, points: "9", targetSize: 0.8,
  extentX: 10, extentY: 8, settle: 0.8, collect: 1.0,
};

function loadOptions() {
  let saved = {};
  try { saved = JSON.parse(localStorage.getItem("openiris.calibration")) || {}; } catch (e) { /* defaults */ }
  for (const key in OPTIONS) $(key).value = saved[key] !== undefined ? saved[key] : OPTIONS[key];
}

function readOptions() {
  const o = {};
  for (const key in OPTIONS) o[key] = key === "points" ? Number($(key).value) : Number($(key).value);
  try {
    const saved = {};
    for (const key in OPTIONS) saved[key] = $(key).value;
    localStorage.setItem("openiris.calibration", JSON.stringify(saved));
  } catch (e) { /* not saved */ }
  return o;
}

// ---------------------------------------------------------------------------------------------
// API

async function api(method, path, body) {
  const options = { method };
  if (body !== undefined) options.body = typeof body === "string" ? body : JSON.stringify(body);
  const response = await fetch("/api" + path, options);
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) throw new Error(data && data.error ? data.error : response.statusText);
  return data;
}

const METHOD = "Remote Linear";
const SIGNALS = ["None", "Pupil", "CR1", "CR2", "CR3", "CR4"];

async function getSettings() {
  const map = {};
  for (const s of await api("GET", "/settings")) map[s.Name] = s.Value;
  return map;
}

function setSetting(name, value) {
  return api("PUT", "/settings/" + encodeURIComponent(name), JSON.stringify(value));
}

// ---------------------------------------------------------------------------------------------
// Live data: rows are collected while a target is being recorded.

const stream = { columns: null, collecting: null };

function connectStream() {
  const socket = new WebSocket((location.protocol === "https:" ? "wss://" : "ws://") + location.host + "/ws/data");
  socket.onmessage = (event) => {
    const message = JSON.parse(event.data);
    if (message.type === "columns") {
      stream.columns = {};
      message.columns.forEach((c, i) => { stream.columns[c] = i; });
    } else if (message.type === "data" && stream.collecting) {
      stream.collecting.push(...message.rows);
    }
  };
  socket.onclose = () => setTimeout(connectStream, 1000);
}

// Eye signal of one row, as the Remote Linear calibration computes it: source minus reference, in
// pixels. Null if the eye or a reflection wasn't found (OpenIris writes 0 then).
function signal(row, eye, source, reference) {
  const point = (name) => {
    if (name === "None") return [0, 0];
    const prefix = eye + (name === "Pupil" ? "Pupil" : name);
    const x = row[stream.columns[prefix + "X"]], y = row[stream.columns[prefix + "Y"]];
    return x === null || y === null || (x === 0 && y === 0) ? null : [x, y];
  };
  const s = point(source), r = point(reference);
  return s && r ? [s[0] - r[0], s[1] - r[1]] : null;
}

function median(values) {
  const sorted = values.slice().sort((a, b) => a - b);
  const mid = sorted.length >> 1;
  return sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

// Median signal of each eye over the rows recorded on one target.
function medianSignals(rows, source, reference) {
  const result = {};
  for (const eye of ["Left", "Right"]) {
    const xs = [], ys = [];
    for (const row of rows) {
      const s = signal(row, eye, source, reference);
      if (s) { xs.push(s[0]); ys.push(s[1]); }
    }
    result[eye] = xs.length ? [median(xs), median(ys)] : null;
  }
  return result;
}

// Gaze in degrees from a signal, with the map of the Remote Linear calibration: the fit is
// [x y] = [sx sy 1] * [[Gxx Gxy] [Gyx Gyy] [XOffset YOffset]].
function gaze(map, s) {
  return [
    s[0] * map.Gxx + s[1] * map.Gyx + map.XOffset,
    s[0] * map.Gxy + s[1] * map.Gyy + map.YOffset,
  ];
}

function readMaps(settings) {
  const maps = {};
  for (const eye of ["Left", "Right"]) {
    const get = (n) => settings["Calibration." + eye + n];
    maps[eye] = { Gxx: get("Gxx"), Gxy: get("Gxy"), Gyx: get("Gyx"), Gyy: get("Gyy"), XOffset: get("XOffset"), YOffset: get("YOffset") };
  }
  return maps;
}

// ---------------------------------------------------------------------------------------------
// Targets

function targetGrid(n, ex, ey) {
  const points = [[0, 0]];
  const ring = [];
  if (n === 5) {
    ring.push([-ex, ey], [ex, ey], [-ex, -ey], [ex, -ey]);
  } else {
    for (const y of [ey, 0, -ey]) for (const x of [-ex, 0, ex]) if (x || y) ring.push([x, y]);
    if (n === 13) ring.push([-ex / 2, ey / 2], [ex / 2, ey / 2], [-ex / 2, -ey / 2], [ex / 2, -ey / 2]);
  }
  // Center first, the rest in random order so the subject can't anticipate.
  for (let i = ring.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [ring[i], ring[j]] = [ring[j], ring[i]];
  }
  return points.concat(ring);
}

const canvas = $("canvas");
const ctx = canvas.getContext("2d");
let options = null;

function resizeCanvas() {
  const ratio = window.devicePixelRatio || 1;
  canvas.width = Math.round(innerWidth * ratio);
  canvas.height = Math.round(innerHeight * ratio);
  ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
}
window.addEventListener("resize", resizeCanvas);

// Degrees (y up) to pixels of the screen, from its width and the viewing distance.
function toPx(x, y) {
  const pxPerCm = innerWidth / options.screenWidth;
  return [
    innerWidth / 2 + Math.tan((x * Math.PI) / 180) * options.distance * pxPerCm,
    innerHeight / 2 - Math.tan((y * Math.PI) / 180) * options.distance * pxPerCm,
  ];
}

function degToPx(deg) {
  return Math.tan((deg * Math.PI) / 180) * options.distance * (innerWidth / options.screenWidth);
}

function clear() {
  ctx.clearRect(0, 0, innerWidth, innerHeight);
}

// A target: a disc with a black center dot. scale > 1 while it draws attention, 1 while recording.
function drawTarget(x, y, scale, recording) {
  const [px, py] = toPx(x, y);
  const r = Math.max(4, degToPx(options.targetSize / 2)) * scale;
  ctx.fillStyle = recording ? "#ffffff" : "#e8e8e8";
  ctx.beginPath(); ctx.arc(px, py, r, 0, 2 * Math.PI); ctx.fill();
  ctx.fillStyle = "#000000";
  ctx.beginPath(); ctx.arc(px, py, Math.max(2, r * 0.18), 0, 2 * Math.PI); ctx.fill();
}

// ---------------------------------------------------------------------------------------------
// Running: Esc stops at any time.

let abort = null;

function wait(ms) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(resolve, ms);
    abort.signal.addEventListener("abort", () => { clearTimeout(timer); reject(new Error("Stopped")); }, { once: true });
  });
}

// Animates for a time, calling draw with the progress (0 to 1).
function animateFor(ms, draw) {
  return new Promise((resolve, reject) => {
    const start = performance.now();
    function frame(now) {
      if (abort.signal.aborted) { reject(new Error("Stopped")); return; }
      const t = Math.min(1, (now - start) / ms);
      clear();
      draw(t);
      if (t < 1) requestAnimationFrame(frame); else resolve();
    }
    requestAnimationFrame(frame);
  });
}

// Shows each target and records the data while the subject looks at it. onRecord(start|stop, point)
// lets calibration tell OpenIris. Returns the rows recorded on each target.
async function showTargets(points, onRecord) {
  const recorded = [];
  let previous = points[0];
  for (const point of points) {
    // Move from the previous target, then shrink to draw the eye to the center.
    const from = previous;
    await animateFor(250, (t) => drawTarget(from[0] + (point[0] - from[0]) * t, from[1] + (point[1] - from[1]) * t, 2, false));
    await animateFor(options.settle * 1000, (t) => drawTarget(point[0], point[1], 2 - t, false));

    clear();
    drawTarget(point[0], point[1], 1, true);
    stream.collecting = [];
    await onRecord("start", point);
    await wait(options.collect * 1000);
    await onRecord("stop", point);
    recorded.push({ point, rows: stream.collecting });
    stream.collecting = null;
    previous = point;
  }
  return recorded;
}

// ---------------------------------------------------------------------------------------------
// Displays: with several, the targets can go full screen on any of them. Needs the Window
// Management API (Chrome and Edge) and a secure page: http://localhost works, another computer's
// address doesn't. Without it, the targets go on the display of this window.

let screenDetails = null;

async function findDisplays(ask) {
  const note = $("displayNote");
  if (!("getScreenDetails" in window)) {
    $("findDisplays").style.display = "none";
    note.textContent = window.isSecureContext
      ? "This browser can't choose the display: move this window to it. Chrome and Edge can."
      : "To choose the display, open this page as http://localhost:9001 on the OpenIris computer. From here, move this window to the display.";
    return;
  }
  if (!ask) {
    // Only list them without asking if the browser already has the permission.
    try {
      const permission = await navigator.permissions.query({ name: "window-management" });
      if (permission.state !== "granted") return;
    } catch (e) {
      return;
    }
  }
  try {
    screenDetails = await window.getScreenDetails();
    screenDetails.addEventListener("screenschange", listDisplays);
    listDisplays();
    $("findDisplays").style.display = "none";
    note.textContent = "";
  } catch (error) {
    note.textContent = "The browser didn't allow listing the displays: move this window to the display.";
  }
}

function listDisplays() {
  const select = $("display");
  const previous = select.value || localStorage.getItem("openiris.display") || "";
  select.replaceChildren(...screenDetails.screens.map((s, i) => {
    const name = (s.label || "Display " + (i + 1)) + ` (${s.width} × ${s.height}${s.isPrimary ? ", primary" : ""}${s === screenDetails.currentScreen ? ", this window" : ""})`;
    return new Option(name, String(i));
  }));
  const index = screenDetails.screens.indexOf(screenDetails.currentScreen);
  select.value = previous !== "" && Number(previous) < screenDetails.screens.length ? previous : String(Math.max(0, index));
}

function chosenDisplay() {
  const value = $("display").value;
  if (!screenDetails || value === "") return undefined;
  try { localStorage.setItem("openiris.display", value); } catch (e) { /* not saved */ }
  return screenDetails.screens[Number(value)];
}

$("findDisplays").onclick = () => findDisplays(true);

async function enterStage(hint) {
  $("results").classList.remove("show");
  $("stage").classList.add("show");
  $("hint").textContent = hint;
  if (!document.fullscreenElement) {
    const screen = chosenDisplay();
    try {
      await document.documentElement.requestFullscreen(screen ? { screen } : undefined);
    } catch (e) {
      /* still works in the window */
    }
    // Wait for the full-screen window to have its size on the new display.
    await new Promise((r) => setTimeout(r, 300));
  }
  resizeCanvas();
  clear();
  abort = new AbortController();
}

function leaveStage() {
  $("stage").classList.remove("show");
  if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
}

document.addEventListener("keydown", (e) => {
  if (e.key === "Escape" && abort && !abort.signal.aborted) abort.abort();
});
document.addEventListener("fullscreenchange", () => {
  // Leaving full screen (Esc in most browsers) also stops.
  if (!document.fullscreenElement && abort && !abort.signal.aborted && !$("results").classList.contains("show")) abort.abort();
});

// ---------------------------------------------------------------------------------------------
// Calibration

async function waitForStatus(test, ms) {
  const end = performance.now() + ms;
  for (;;) {
    const status = await api("GET", "/status");
    if (test(status)) return status;
    if (performance.now() > end) throw new Error("OpenIris didn't respond in time.");
    await new Promise((r) => setTimeout(r, 100));
  }
}

async function calibrate() {
  options = readOptions();
  const source = $("source").value, reference = $("reference").value;
  await enterStage("Calibrating. Look at the center of each target. Esc to stop.");

  let started = false;
  try {
    const status = await api("GET", "/status");
    if (!status.Tracking) throw new Error("OpenIris is not tracking.");
    if (status.Calibrating) throw new Error("OpenIris is already calibrating.");
    if (status.CalibrationMethod !== METHOD) await setSetting("CalibrationMethod", METHOD);
    await setSetting("Calibration.SignalSource", source);
    await setSetting("Calibration.SignalReference", reference);

    await api("POST", "/calibration/start");
    started = true;
    await waitForStatus((s) => s.Calibrating, 3000);

    const message = async (m) => {
      const reply = (await api("POST", "/calibration/message", m)).Reply;
      if (reply !== "ack") throw new Error("Calibration: " + reply);
    };
    const points = targetGrid(options.points, options.extentX, options.extentY);
    const recorded = await showTargets(points, (step, p) =>
      step === "start" ? message(`FixStart:${+p[0].toFixed(3)},${+p[1].toFixed(3)}`) : message("FixStop"));

    // The reference (zero) is taken right after Complete, so the subject looks at the center.
    clear();
    drawTarget(0, 0, 1, true);
    await message("Complete");
    await waitForStatus((s) => !s.Calibrating, 5000);
    started = false;

    const maps = readMaps(await getSettings());
    showResults("Calibration", "Fit to the calibration targets. Validate to measure the real error.", recorded, maps, source, reference);
  } catch (error) {
    if (started) await api("POST", "/calibration/cancel").catch(() => {});
    leaveStage();
    showError(error);
  }
}

// ---------------------------------------------------------------------------------------------
// Validation

async function validate() {
  options = readOptions();
  await enterStage("Validation. Look at the center of each target. Esc to stop.");
  try {
    const status = await api("GET", "/status");
    if (!status.Tracking) throw new Error("OpenIris is not tracking.");
    const settings = await getSettings();
    if (status.CalibrationMethod !== METHOD || settings["Calibration.LeftGxx"] === undefined) {
      throw new Error("Calibrate first: validation uses the Remote Linear calibration.");
    }
    const source = settings["Calibration.SignalSource"], reference = settings["Calibration.SignalReference"];
    const points = targetGrid(options.points, options.extentX, options.extentY);
    const recorded = await showTargets(points, async () => {});
    showResults("Validation", "Gaze from the calibration compared with the targets.", recorded, readMaps(settings), source, reference);
  } catch (error) {
    leaveStage();
    showError(error);
  }
}

// ---------------------------------------------------------------------------------------------
// Results: targets with the gaze of each eye, and the error.

function showResults(title, subtitle, recorded, maps, source, reference) {
  $("hint").textContent = "";
  $("stage").style.cursor = "default";
  clear();

  const colors = { Left: "#2a50c8", Right: "#d42424" };
  const errors = { Left: [], Right: [] };
  for (const { point, rows } of recorded) {
    const [tx, ty] = toPx(point[0], point[1]);
    ctx.strokeStyle = "#000"; ctx.lineWidth = 1.5;
    ctx.beginPath(); ctx.moveTo(tx - 10, ty); ctx.lineTo(tx + 10, ty); ctx.moveTo(tx, ty - 10); ctx.lineTo(tx, ty + 10); ctx.stroke();

    const signals = medianSignals(rows, source, reference);
    for (const eye of ["Left", "Right"]) {
      if (!signals[eye] || maps[eye].Gxx === undefined) continue;
      const g = gaze(maps[eye], signals[eye]);
      errors[eye].push(Math.hypot(g[0] - point[0], g[1] - point[1]));
      const [gx, gy] = toPx(g[0], g[1]);
      ctx.strokeStyle = colors[eye]; ctx.fillStyle = colors[eye]; ctx.lineWidth = 1.5;
      ctx.beginPath(); ctx.moveTo(tx, ty); ctx.lineTo(gx, gy); ctx.stroke();
      ctx.beginPath(); ctx.arc(gx, gy, 5, 0, 2 * Math.PI); ctx.fill();
    }
  }

  $("resultsTitle").textContent = title;
  const rows = [`<tr><th colspan="5" style="font-weight:400;color:#555">${subtitle}</th></tr>`,
    "<tr><th></th><th>Targets</th><th>Mean error</th><th>Max error</th></tr>"];
  for (const eye of ["Left", "Right"]) {
    const e = errors[eye];
    if (!e.length) continue;
    const mean = e.reduce((a, b) => a + b, 0) / e.length;
    rows.push(`<tr><td style="color:${colors[eye]}">● ${eye} eye</td><td>${e.length} of ${recorded.length}</td><td>${mean.toFixed(2)}°</td><td>${Math.max(...e).toFixed(2)}°</td></tr>`);
  }
  if (rows.length === 2) rows.push(`<tr><td colspan="4">No eye data on the targets: check the tracking (pupil and ${reference}).</td></tr>`);
  $("resultsTable").innerHTML = rows.join("");
  $("results").classList.add("show");
}

$("resultsValidate").onclick = () => { $("stage").style.cursor = ""; validate(); };
$("resultsRedo").onclick = () => { $("stage").style.cursor = ""; calibrate(); };
$("resultsDone").onclick = () => { $("stage").style.cursor = ""; leaveStage(); refreshInfo(); };

// ---------------------------------------------------------------------------------------------
// Setup

function showError(error) {
  $("error").textContent = error && error.message !== "Stopped" ? error.message : "";
}

async function refreshInfo() {
  try {
    const settings = await getSettings();
    const status = await api("GET", "/status");
    const usingIt = status.CalibrationMethod === METHOD;
    $("fits").textContent = (usingIt ? "" : `OpenIris uses the ${status.CalibrationMethod} calibration now. Calibrate switches it to ${METHOD}. `) +
      (status.Tracking ? "" : "OpenIris is not tracking.");
    for (const [id, name, fallback] of [["source", "Calibration.SignalSource", "Pupil"], ["reference", "Calibration.SignalReference", "CR1"]]) {
      const select = $(id);
      const current = select.value || (usingIt ? settings[name] : null) || fallback;
      select.replaceChildren(...SIGNALS.map((s) => new Option(s === "None" ? "Nothing" : s, s, false, s === current)));
    }
  } catch (error) {
    showError(error);
  }
}

$("calibrate").onclick = () => { showError(null); calibrate(); };
$("validate").onclick = () => { showError(null); validate(); };

loadOptions();
findDisplays(false);
refreshInfo();
connectStream();
