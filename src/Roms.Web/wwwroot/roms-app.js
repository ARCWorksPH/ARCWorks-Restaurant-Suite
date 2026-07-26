(() => {
    let installPrompt, idleTimer, warningTimer, countdownTimer;
    let listeners = [];

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
                showInstallRequirement("This public address uses HTTP. Browsers only install ROMS from a secure HTTPS address or from localhost.");
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
            showInstallRequirement("The browser has not made installation available yet. Use Chrome or Edge, interact with ROMS for a moment, then try again or use the browser menu.");
        }
    }};

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
        start: (formId, idleMinutes) => {
            window.romsSession.stop();
            const onActivity = () => reset(formId, idleMinutes);
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
            hideWarning();
        }
    };
})();
