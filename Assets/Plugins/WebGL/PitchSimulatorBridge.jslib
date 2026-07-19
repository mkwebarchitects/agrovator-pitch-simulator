mergeInto(LibraryManager.library, {
  $PitchSimulatorLmsBridge: {
    initialized: false,
    launchConfigJson: "",

    initialize: function () {
      if (PitchSimulatorLmsBridge.initialized) return;
      PitchSimulatorLmsBridge.initialized = true;

      window.addEventListener("message", function (event) {
        if (event.source !== window.parent || event.origin !== window.location.origin) return;
        var message = event.data;
        if (!message || message.version !== 1 || typeof message.type !== "string") return;

        if (message.type === "pitch-simulator.lms.launch") {
          PitchSimulatorLmsBridge.launchConfigJson = message.payload && typeof message.payload === "object"
            ? JSON.stringify(message.payload)
            : "";
          return;
        }

        if (message.type !== "pitch-simulator.lms.completion-result") return;
        var request = PitchSimulatorLmsBridge.pending[message.requestId];
        if (!request) return;
        delete PitchSimulatorLmsBridge.pending[message.requestId];

        if (message.status === "success") {
          SendMessage(request.receiver, "OnLmsSubmissionSucceeded", String(message.requestId));
        } else {
          var safeStatus = message.status === "expired" || message.status === "missing-config"
            ? message.status
            : "failure";
          SendMessage(request.receiver, "OnLmsSubmissionFailed", String(message.requestId) + "|" + safeStatus);
        }
      });

      if (window.parent !== window) {
        window.parent.postMessage({
          version: 1,
          type: "pitch-simulator.lms.ready"
        }, window.location.origin);
      }
    },

    pending: {}
  },

  PitchSimulatorBridge_GetLaunchConfigJson__deps: ["$PitchSimulatorLmsBridge"],
  PitchSimulatorBridge_GetLaunchConfigJson: function () {
    PitchSimulatorLmsBridge.initialize();
    return stringToNewUTF8(PitchSimulatorLmsBridge.launchConfigJson);
  },

  PitchSimulatorBridge_SubmitCompletion__deps: ["$PitchSimulatorLmsBridge"],
  PitchSimulatorBridge_SubmitCompletion: function (completionJsonPointer, requestId, receiverNamePointer) {
    PitchSimulatorLmsBridge.initialize();
    var receiver = UTF8ToString(receiverNamePointer);
    var payload;
    try {
      payload = JSON.parse(UTF8ToString(completionJsonPointer));
    } catch (error) {
      SendMessage(receiver, "OnLmsSubmissionFailed", String(requestId) + "|failure");
      return;
    }

    if (window.parent === window) {
      SendMessage(receiver, "OnLmsSubmissionFailed", String(requestId) + "|missing-config");
      return;
    }

    PitchSimulatorLmsBridge.pending[requestId] = { receiver: receiver };
    window.parent.postMessage({
      version: 1,
      type: "pitch-simulator.lms.completion-submit",
      requestId: requestId,
      payload: payload
    }, window.location.origin);
  },

  PitchSimulatorBridge_CancelSubmission__deps: ["$PitchSimulatorLmsBridge"],
  PitchSimulatorBridge_CancelSubmission: function (requestId) {
    PitchSimulatorLmsBridge.initialize();
    delete PitchSimulatorLmsBridge.pending[requestId];
  },

  PitchSimulatorViewportWidth: function () {
    var canvas = document.getElementById("unity-canvas");
    return Math.max(1, Math.round(canvas ? canvas.clientWidth : window.innerWidth));
  },

  PitchSimulatorViewportHeight: function () {
    var canvas = document.getElementById("unity-canvas");
    return Math.max(1, Math.round(canvas ? canvas.clientHeight : window.innerHeight));
  },

  PitchSimulatorDevicePixelRatioTimes100: function () {
    return Math.max(100, Math.round((window.devicePixelRatio || 1) * 100));
  }
});
