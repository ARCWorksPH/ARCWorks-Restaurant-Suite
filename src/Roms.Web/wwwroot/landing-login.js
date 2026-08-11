(() => {
    document.addEventListener("click", event => {
        const toggle = event.target.closest("[data-password-toggle]");
        if (!toggle) return;

        const targetId = toggle.getAttribute("data-target");
        const input = targetId ? document.getElementById(targetId) : null;
        if (!input) return;

        const reveal = input.type === "password";
        input.type = reveal ? "text" : "password";
        toggle.setAttribute("aria-label", reveal ? "Conceal secret" : "Reveal secret");
        toggle.setAttribute("title", reveal ? "Hide password" : "Show password");
    });
})();
