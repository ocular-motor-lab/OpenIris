// OpenIris web client: the N-point calibration targets when OpenIris controls them (calibration
// Mode RemoteDisplay). Every page of the client loads this: while OpenIris has a target, it draws
// it over the whole page, then goes back to the page. OpenIris sends the target to draw on
// /ws/calibration (CalibrationTargetStream.cs), with the screen's width and the viewing distance
// from its settings. Esc stops the calibration.
//
// A page can't go full screen by itself when a calibration starts (browsers allow it only after a
// click), so the targets fill the browser window. To have them full screen, put the client in full
// screen first (the Full screen button, or F11): it stays so while using the client.
"use strict";

(() => {
  const stage = document.createElement("div");
  stage.className = "targets-stage";
  const canvas = document.createElement("canvas");
  const hint = document.createElement("div");
  hint.className = "hint";
  hint.textContent = "Esc stops the calibration.";
  stage.append(canvas, hint);
  document.body.append(stage);

  const ctx = canvas.getContext("2d");
  let target = null;

  function resizeCanvas() {
    const ratio = window.devicePixelRatio || 1;
    canvas.width = Math.round(canvas.clientWidth * ratio);
    canvas.height = Math.round(canvas.clientHeight * ratio);
    ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    draw();
  }
  window.addEventListener("resize", resizeCanvas);

  // Degrees to pixels of this screen, with the screen's width and the distance OpenIris sends.
  // The whole screen's width, so the size is right also when the window doesn't cover it.
  function px(deg, t) {
    return Math.tan((deg * Math.PI) / 180) * t.distanceMm * (screen.width / t.screenWidthMm);
  }

  function draw() {
    const width = canvas.clientWidth, height = canvas.clientHeight;
    ctx.clearRect(0, 0, width, height);
    if (!target) return;
    const x = width / 2 + px(target.x, target);
    const y = height / 2 - px(target.y, target);
    const r = Math.max(4, px(target.size / 2, target)) * target.scale;
    ctx.fillStyle = target.recording ? "#ffffff" : "#e8e8e8";
    ctx.beginPath(); ctx.arc(x, y, r, 0, 2 * Math.PI); ctx.fill();
    ctx.fillStyle = "#000000";
    ctx.beginPath(); ctx.arc(x, y, Math.max(2, r * 0.18), 0, 2 * Math.PI); ctx.fill();
  }

  function show(message) {
    const showing = target !== null;
    target = message && message.type === "target" ? message : null;
    stage.classList.toggle("show", target !== null);
    if (target && !showing) resizeCanvas();
    else requestAnimationFrame(draw);
  }

  function connect() {
    const socket = new WebSocket((location.protocol === "https:" ? "wss://" : "ws://") + location.host + "/ws/calibration");
    socket.onmessage = (event) => show(JSON.parse(event.data));
    socket.onclose = () => {
      show(null);
      setTimeout(connect, 1000);
    };
  }

  function stop() {
    if (target) fetch("/api/calibration/cancel", { method: "POST" }).catch(() => {});
  }
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") stop();
  });
  // In full screen, Esc leaves full screen instead (the page doesn't get the key).
  document.addEventListener("fullscreenchange", () => {
    if (!document.fullscreenElement) stop();
  });

  // -------------------------------------------------------------------------------------------
  // Full screen on a chosen display, on pages with the button (the control page). Choosing the
  // display needs the Window Management API (Chrome and Edge) and a secure page: http://localhost
  // works, another computer's address doesn't. Without it, full screen is on this window's display.

  const $ = (id) => document.getElementById(id);
  const button = $("fullScreen");
  if (button) {
    let screenDetails = null;

    async function findDisplays(ask) {
      const note = $("displayNote");
      if (!("getScreenDetails" in window)) {
        $("findDisplays").style.display = "none";
        note.textContent = window.isSecureContext
          ? "This browser can't choose the display: move this window to it. Chrome and Edge can."
          : "This browser can't choose the display from this address: move this window to the display.";
        return;
      }
      if (!ask) {
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
    button.onclick = async () => {
      if (document.fullscreenElement) {
        document.exitFullscreen().catch(() => {});
        return;
      }
      const screen = chosenDisplay();
      try {
        await document.documentElement.requestFullscreen(screen ? { screen } : undefined);
      } catch (e) {
        /* stays in the window */
      }
    };
    document.addEventListener("fullscreenchange", () => {
      button.textContent = document.fullscreenElement ? "Leave full screen" : "Full screen";
    });

    findDisplays(false);
  }

  connect();
})();
