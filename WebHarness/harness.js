(() => {
  "use strict";

  const protocolVersion = 1;
  const expectedOrigin = window.location.origin;
  const frame = document.getElementById("game");
  const mode = document.getElementById("mode");
  const resend = document.getElementById("resend");
  const connection = document.getElementById("connection");
  const progress = document.getElementById("progress");
  const completionStatus = document.getElementById("completion-status");
  const completionScore = document.getElementById("completion-score");
  const completionAttempt = document.getElementById("completion-attempt");
  const completionCompetencies = document.getElementById("completion-competencies");
  const completionTimeouts = document.getElementById("completion-timeouts");

  const launchConfig = Object.freeze({
    PseudonymousLearnerId: "local-learner-001",
    SessionId: "local-session-001",
    CourseId: "course-innovation",
    ModuleId: "module-pitching",
    LessonId: "lesson-smart-school-garden",
    ScenarioId: "smart-school-garden",
    Language: "en",
    AttemptNumber: 1,
    TimerMode: "Normal",
    ReducedMotion: false,
    MusicVolume: 0.6,
    SfxVolume: 0.7,
    ContentVersion: 1,
    LaunchReference: "lref_localHarness1234"
  });

  function addProgress(summary) {
    const item = document.createElement("li");
    item.textContent = summary;
    progress.prepend(item);
    while (progress.childElementCount > 12) progress.lastElementChild.remove();
  }

  function sendLaunch() {
    if (!frame.contentWindow) return;
    const missing = mode.value === "missing-config";
    frame.contentWindow.postMessage({
      version: protocolVersion,
      type: "pitch-simulator.lms.launch",
      payload: missing ? null : launchConfig
    }, expectedOrigin);
    connection.textContent = missing
      ? "Missing Config mode sent."
      : "Launch configuration sent.";
    addProgress(`Launch event sent (${mode.options[mode.selectedIndex].text}).`);
  }

  function showCompletion(payload) {
    const competencyCount = Array.isArray(payload.CompetencyScores)
      ? payload.CompetencyScores.length
      : 0;
    completionStatus.textContent = String(payload.CompletionStatus || "submitted");
    completionScore.textContent = Number.isFinite(payload.OverallScore)
      ? String(payload.OverallScore)
      : "—";
    completionAttempt.textContent = Number.isInteger(payload.AttemptNumber)
      ? String(payload.AttemptNumber)
      : "—";
    completionCompetencies.textContent = String(competencyCount);
    completionTimeouts.textContent = Number.isInteger(payload.TimeoutCount)
      ? String(payload.TimeoutCount)
      : "—";
  }

  function resultStatus() {
    switch (mode.value) {
      case "expired": return "expired";
      case "missing-config": return "missing-config";
      case "failure": return "failure";
      default: return "success";
    }
  }

  window.addEventListener("message", event => {
    if (event.origin !== expectedOrigin || event.source !== frame.contentWindow) return;
    const message = event.data;
    if (!message || message.version !== protocolVersion || typeof message.type !== "string") return;

    if (message.type === "pitch-simulator.lms.ready") {
      connection.textContent = "Embedded build connected.";
      addProgress("Embedded bridge ready.");
      sendLaunch();
      return;
    }

    if (message.type !== "pitch-simulator.lms.completion-submit") return;
    if (!Number.isInteger(message.requestId) || !message.payload || typeof message.payload !== "object") return;

    showCompletion(message.payload);
    const status = resultStatus();
    addProgress(`Completion received; replying with ${status}.`);
    frame.contentWindow.postMessage({
      version: protocolVersion,
      type: "pitch-simulator.lms.completion-result",
      requestId: message.requestId,
      status
    }, expectedOrigin);
  });

  resend.addEventListener("click", sendLaunch);
  frame.addEventListener("load", () => addProgress("Embedded build frame loaded."));
})();
