(() => {
    let installPrompt, idleTimer, warningTimer, countdownTimer;
    let listeners = [];
    let activityReference, lastActivitySentAt = 0;

    window.addEventListener("beforeinstallprompt", event => { event.preventDefault(); installPrompt = event; });
    const showInstallRequirement = message => {
        const box = document.getElementById("install-requirement-message");
        if (!box) return;
        box.textContent = message;
        box.hidden = false;
    };

    window.romsInstall = {
        checkRequirements: () => {
            if (window.matchMedia("(display-mode: standalone)").matches) return;
            if (!window.isSecureContext) {
                const button = document.getElementById("install-roms-button");
                if (button) {
                    button.disabled = true;
                    button.textContent = "HTTPS required to install";
                }
                showInstallRequirement("This public address uses HTTP. Browsers only install ARCWorks Restaurant Suite from a secure HTTPS address or from localhost.");
            }
        },
        prompt: async () => {
            if (!window.isSecureContext) {
                showInstallRequirement("Installation is blocked because this public address is not HTTPS.");
            } else if (installPrompt) {
                await installPrompt.prompt();
                await installPrompt.userChoice;
                installPrompt = undefined;
            } else {
                showInstallRequirement("The browser has not made installation available yet. Use Chrome or Edge, interact with ARCWorks Restaurant Suite for a moment, then try again or use the browser menu.");
            }
        }
    };

    window.romsConnection = {
        ref: null,
        onlineHandler: null,
        offlineHandler: null,
        observer: null,

        init: function(dotNetRef) {
            this.dispose();
            this.ref = dotNetRef;

            const renderStatus = state => {
                const indicator = document.getElementById("roms-connection-indicator");
                if (!indicator) return;

                const label = state === "Offline"
                    ? "Connection lost"
                    : state === "Reconnecting"
                        ? "Reconnecting"
                        : "Connected";

                indicator.classList.remove("status-connected", "status-reconnecting", "status-offline");
                indicator.classList.add(`status-${state.toLowerCase()}`);
                indicator.textContent = `● ${label}`;
            };

            const updateStatus = () => {
                const isOnline = navigator.onLine;
                const modal = document.getElementById("components-reconnect-modal");

                let state = "Connected";
                if (!isOnline) {
                    state = "Offline";
                } else if (modal) {
                    if (modal.classList.contains("components-reconnect-failed") || modal.classList.contains("components-resume-failed")) {
                        state = "Offline";
                    } else if (modal.classList.contains("components-reconnect-show") || modal.classList.contains("components-reconnect-paused") || modal.classList.contains("components-reconnect-retrying") || modal.hasAttribute("open")) {
                        state = "Reconnecting";
                    }
                }

                // Browser connectivity must be reflected locally because an
                // offline Blazor circuit cannot deliver a .NET callback.
                renderStatus(state);

                if (!this.ref) return;
                try {
                    this.ref.invokeMethodAsync("UpdateConnectionState", state).catch(() => {});
                } catch (e) {
                    // Ignore disposed handle
                }
            };

            this.onlineHandler = updateStatus;
            this.offlineHandler = updateStatus;

            window.addEventListener("online", this.onlineHandler);
            window.addEventListener("offline", this.offlineHandler);

            const modal = document.getElementById("components-reconnect-modal");
            if (modal) {
                this.observer = new MutationObserver(updateStatus);
                this.observer.observe(modal, { attributes: true, attributeFilter: ["class", "open"] });
            }

            updateStatus();
        },

        dispose: function() {
            if (this.onlineHandler) {
                window.removeEventListener("online", this.onlineHandler);
                this.onlineHandler = null;
            }
            if (this.offlineHandler) {
                window.removeEventListener("offline", this.offlineHandler);
                this.offlineHandler = null;
            }
            if (this.observer) {
                this.observer.disconnect();
                this.observer = null;
            }
            this.ref = null;
        }
    };

    const clearTimers = () => { clearTimeout(idleTimer); clearTimeout(warningTimer); clearInterval(countdownTimer); };
    const hideWarning = () => { const warning = document.getElementById("session-timeout-warning"); if (warning) warning.hidden = true; };
    const reset = (formId, idleMinutes) => {
        clearTimers();
        hideWarning();
        const totalMs = idleMinutes * 60 * 1000;
        warningTimer = setTimeout(() => {
            const warning = document.getElementById("session-timeout-warning");
            const counter = document.getElementById("session-timeout-countdown");
            if (!warning || !counter) return;
            let seconds = 60;
            counter.textContent = seconds.toString();
            warning.hidden = false;
            countdownTimer = setInterval(() => { seconds -= 1; counter.textContent = Math.max(0, seconds).toString(); }, 1000);
        }, Math.max(0, totalMs - 60_000));
        idleTimer = setTimeout(() => document.getElementById(formId)?.requestSubmit(), totalMs);
    };

    window.romsSession = {
        start: (formId, idleMinutes, dotNetReference) => {
            window.romsSession.stop();
            activityReference = dotNetReference;
            const onActivity = () => {
                reset(formId, idleMinutes);
                const now = Date.now();
                if (activityReference && now - lastActivitySentAt >= 60_000) {
                    lastActivitySentAt = now;
                    activityReference.invokeMethodAsync("RecordActivity").catch(() => {});
                }
            };
            ["pointerdown", "keydown", "touchstart"].forEach(name => {
                document.addEventListener(name, onActivity, { passive: true });
                listeners.push([name, onActivity]);
            });
            document.getElementById("session-timeout-continue")?.addEventListener("click", onActivity);
            listeners.push(["continue", onActivity]);
            reset(formId, idleMinutes);
        },
        stop: () => {
            clearTimers();
            listeners.forEach(([name, handler]) => {
                if (name === "continue") document.getElementById("session-timeout-continue")?.removeEventListener("click", handler);
                else document.removeEventListener(name, handler);
            });
            listeners = [];
            activityReference = null;
            lastActivitySentAt = 0;
            hideWarning();
        }
    };
})();
