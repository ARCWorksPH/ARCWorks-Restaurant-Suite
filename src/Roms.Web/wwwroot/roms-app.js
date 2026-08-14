(() => {
    // Blazor enhanced navigation can encounter the page-level script more than
    // once without replacing the browser JavaScript heap. Keep exactly one
    // guard per live runtime. A copied browser profile receives cookies and
    // storage, but it does not receive this in-memory marker or instance ID.
    if (window.__arcworksSessionGuardLoaded) return;
    window.__arcworksSessionGuardLoaded = true;

    let installPrompt, idleTimer, warningTimer, countdownTimer;
    let listeners = [];
    let lastActivitySentAt = 0, lastMeaningfulActivityAt = 0, heartbeatTimer;
    let applicationInstanceId, ownsWindowLease = false, sessionBootstrapInProgress = false;
    let forcedLogoutInProgress = false;
    let lastLeaseStatus = 0;

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

    const clearTimers = () => {
        clearTimeout(idleTimer);
        clearTimeout(warningTimer);
        clearInterval(countdownTimer);
        clearInterval(heartbeatTimer);
    };
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
        createApplicationInstanceId: () => {
            const namePrefix = "ARCWORKS-INSTANCE:";
            const existing = window.name.startsWith(namePrefix)
                ? window.name.slice(namePrefix.length)
                : "";
            if (existing.length === 64 && /^[0-9A-F]+$/.test(existing)) return existing;
            const instanceBytes = new Uint8Array(32);
            crypto.getRandomValues(instanceBytes);
            const created = Array.from(instanceBytes, byte => byte.toString(16).padStart(2, "0")).join("").toUpperCase();
            // window.name belongs to this live top-level browsing context and
            // survives an ordinary reload. It is not included in copied cookie,
            // localStorage, or sessionStorage data.
            window.name = `${namePrefix}${created}`;
            return created;
        },
        revokeDuplicate: async (_formId, reason = "session-replay") => {
            if (forcedLogoutInProgress) return;
            forcedLogoutInProgress = true;
            window.romsSession.stop();
            try {
                await fetch("/Account/ForcedSessionLogout", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: { "X-ARCWorks-Forced-Logout": "session-replay" }
                });
            } finally {
                window.location.replace(`/Account/Login?reason=${encodeURIComponent(reason)}`);
            }
        },
        start: async (formId, idleMinutes, instanceId) => {
            window.romsSession.stop();
            applicationInstanceId = instanceId;

            const recordActivity = async () => {
                try {
                    const status = await postLease("/security/session/touch", applicationInstanceId);
                    if (status === 409) window.romsSession.revokeDuplicate(formId);
                } catch {
                    // A transient network failure is handled by the connection UI.
                }
            };
            const onActivity = () => {
                lastMeaningfulActivityAt = Date.now();
                reset(formId, idleMinutes);
                const now = lastMeaningfulActivityAt;
                if (now - lastActivitySentAt >= 60_000) {
                    lastActivitySentAt = now;
                    recordActivity();
                }
            };
            ["pointerdown", "keydown", "touchstart", "input", "change", "scroll"].forEach(name => {
                document.addEventListener(name, onActivity, { passive: true });
                listeners.push([name, onActivity]);
            });
            const onFocus = () => { if (document.visibilityState === "visible") onActivity(); };
            document.addEventListener("visibilitychange", onFocus, { passive: true });
            window.addEventListener("focus", onActivity, { passive: true });
            listeners.push(["visibilitychange", onFocus], ["window-focus", onActivity]);
            document.getElementById("session-timeout-continue")?.addEventListener("click", onActivity);
            listeners.push(["continue", onActivity]);
            reset(formId, idleMinutes);
            lastMeaningfulActivityAt = Date.now();
            await recordActivity();
            heartbeatTimer = setInterval(() => {
                if (Date.now() - lastMeaningfulActivityAt <= 75_000) recordActivity();
            }, 60_000);
        },
        isWindowLeaseOwner: () => ownsWindowLease,
        diagnostics: () => ({
            ownsWindowLease,
            lastLeaseStatus,
            hasForm: !!document.getElementById("session-timeout-form"),
            instanceLength: applicationInstanceId?.length ?? 0
        }),
        verifyNow: async () => {
            if (!applicationInstanceId) return false;
            return (await postLease("/security/session/touch", applicationInstanceId)) === 204;
        },
        stop: () => {
            clearTimers();
            listeners.forEach(([name, handler]) => {
                if (name === "continue") document.getElementById("session-timeout-continue")?.removeEventListener("click", handler);
                else if (name === "window-focus") window.removeEventListener("focus", handler);
                else document.removeEventListener(name, handler);
            });
            listeners = [];
            lastActivitySentAt = 0;
            lastMeaningfulActivityAt = 0;
            applicationInstanceId = null;
            ownsWindowLease = false;
            hideWarning();
        }
    };

    const postLease = async (url, instanceId) => {
        const antiforgeryToken = document.querySelector("#session-timeout-form input[name='__RequestVerificationToken']")?.value;
        const response = await fetch(`${url}/${encodeURIComponent(instanceId)}`, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                ...(antiforgeryToken ? { "RequestVerificationToken": antiforgeryToken } : {})
            },
            body: ""
        });
        lastLeaseStatus = response.status;
        return lastLeaseStatus;
    };
    const bootstrapAuthenticatedSession = async () => {
        const form = document.getElementById("session-timeout-form");
        if (!form || applicationInstanceId || sessionBootstrapInProgress) return;
        sessionBootstrapInProgress = true;
        try {
            const instanceId = window.romsSession.createApplicationInstanceId();
            const status = await postLease("/security/session/register", instanceId);
            if (status === 409) {
                window.romsSession.revokeDuplicate(form.id);
                return;
            }
            if (status === 401) {
                // The cookie may still decrypt after the authoritative database
                // session has been revoked. Remove that stale cookie immediately
                // instead of allowing a copied runtime to render the application.
                window.romsSession.revokeDuplicate(form.id, "session-revoked");
                return;
            }
            if (status !== 204) {
                // A login may complete through enhanced navigation while the new
                // authentication cookie is still settling. Fail closed by leaving
                // the application unusable until a short retry establishes the
                // lease; do not mislabel a transient 401/400 as replay.
                setTimeout(bootstrapAuthenticatedSession, 500);
                return;
            }
            ownsWindowLease = true;
            await window.romsSession.start(
                form.id,
                Number.parseInt(form.dataset.idleMinutes || "15", 10),
                instanceId);
        } finally {
            sessionBootstrapInProgress = false;
        }
    };
    // Login can complete through Blazor enhanced navigation, which preserves
    // this JavaScript runtime instead of re-executing the script. Observe the
    // authenticated layout so the lease is also established after that path.
    const sessionLayoutObserver = new MutationObserver(() => bootstrapAuthenticatedSession());
    const startSessionBootstrap = () => {
        bootstrapAuthenticatedSession();
        sessionLayoutObserver.observe(document.body, { childList: true, subtree: true });
    };
    if (document.readyState === "loading")
        document.addEventListener("DOMContentLoaded", startSessionBootstrap, { once: true });
    else
        startSessionBootstrap();
    document.addEventListener("enhancedload", bootstrapAuthenticatedSession);
})();
