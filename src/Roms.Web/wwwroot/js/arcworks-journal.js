const VERSION = 1;
const KDF_ITERATIONS = 600000;
const encoder = new TextEncoder();
const decoder = new TextDecoder();
let state = freshState();

function freshState() {
    return {
        host: null, key: null, envelope: null, entries: [], deleted: [], current: null,
        plaintext: new Map(), timeZone: "Asia/Manila", locale: "en-PH"
    };
}

const b64 = bytes => {
    let binary = "";
    for (let index = 0; index < bytes.length; index += 0x8000)
        binary += String.fromCharCode(...bytes.subarray(index, index + 0x8000));
    return btoa(binary);
};
const bytes = encoded => Uint8Array.from(atob(encoded), value => value.charCodeAt(0));
const recoveryText = value => b64(value).replaceAll("+", "-").replaceAll("/", "_").replaceAll("=", "");
const recoveryBytes = value => {
    const normalized = value.trim().replaceAll("-", "+").replaceAll("_", "/");
    return bytes(normalized + "=".repeat((4 - normalized.length % 4) % 4));
};
const random = length => { const value = new Uint8Array(length); crypto.getRandomValues(value); return value; };

async function derivePassphraseKey(passphrase, salt, iterations) {
    const material = await crypto.subtle.importKey("raw", encoder.encode(passphrase), "PBKDF2", false, ["deriveKey"]);
    return crypto.subtle.deriveKey(
        { name: "PBKDF2", hash: "SHA-256", salt, iterations }, material,
        { name: "AES-GCM", length: 256 }, false, ["encrypt", "decrypt"]);
}

async function importRecoveryKey(raw) {
    if (raw.length !== 32) throw new Error("The recovery key is invalid.");
    return crypto.subtle.importKey("raw", raw, { name: "AES-GCM" }, false, ["encrypt", "decrypt"]);
}

async function wrapDataKey(dataKeyRaw, wrappingKey, nonce) {
    return new Uint8Array(await crypto.subtle.encrypt({ name: "AES-GCM", iv: nonce }, wrappingKey, dataKeyRaw));
}

async function unwrapDataKey(wrapped, wrappingKey, nonce) {
    const raw = await crypto.subtle.decrypt({ name: "AES-GCM", iv: nonce }, wrappingKey, wrapped);
    return crypto.subtle.importKey("raw", raw, { name: "AES-GCM" }, false, ["encrypt", "decrypt"]);
}

function antiforgery() {
    return document.querySelector("#session-timeout-form input[name='__RequestVerificationToken']")?.value ?? "";
}

async function api(path, options = {}) {
    const response = await fetch(`/api/private-journal${path}`, {
        credentials: "same-origin",
        ...options,
        headers: {
            ...(options.body ? { "Content-Type": "application/json" } : {}),
            ...(options.method && options.method !== "GET" ? { "RequestVerificationToken": antiforgery() } : {}),
            ...options.headers
        }
    });
    if (!response.ok) {
        let message = "The private journal request failed.";
        try { message = (await response.json()).detail ?? message; } catch { }
        throw new Error(message);
    }
    return response.status === 204 ? null : response.json();
}

function element(tag, props = {}, ...children) {
    const node = document.createElement(tag);
    for (const [name, value] of Object.entries(props)) {
        if (name === "className") node.className = value;
        else if (name.startsWith("on")) node.addEventListener(name.slice(2).toLowerCase(), value);
        else if (value !== null && value !== undefined) node.setAttribute(name, value);
    }
    for (const child of children.flat()) node.append(child instanceof Node ? child : document.createTextNode(String(child)));
    return node;
}

function status(message, failed = false) {
    const box = state.host?.querySelector(".journal-status");
    if (!box) return;
    box.textContent = message;
    box.classList.toggle("error", failed);
}

function validatePassphrase(passphrase) {
    if (passphrase.length < 12) throw new Error("Use a journal passphrase of at least 12 characters.");
}

export async function init(hostId) {
    dispose();
    state.host = document.getElementById(hostId);
    if (!state.host) return;
    state.timeZone = state.host.dataset.timeZone || state.timeZone;
    state.locale = state.host.dataset.locale || state.locale;
    try {
        state.envelope = await api("/key-envelope");
        renderLocked();
    } catch (error) { renderFatal(error); }
}

export function dispose() {
    if (state.host) state.host.replaceChildren();
    state.key = null;
    state.envelope = null;
    state.entries = [];
    state.deleted = [];
    state.current = null;
    state.plaintext.clear();
    state = freshState();
}

function renderFatal(error) {
    state.host?.replaceChildren(element("section", { className: "journal-panel" },
        element("h1", {}, "My Private Journal"), element("p", { className: "journal-status error" }, error.message)));
}

function renderLocked() {
    const setup = !state.envelope;
    const passphrase = element("input", { type: "password", autocomplete: "new-password", spellcheck: "false" });
    const confirm = element("input", { type: "password", autocomplete: "new-password", spellcheck: "false" });
    const recovery = element("input", { type: "password", autocomplete: "off", spellcheck: "false" });
    const newPassphrase = element("input", { type: "password", autocomplete: "new-password", spellcheck: "false" });
    const body = element("section", { className: "journal-panel" },
        element("h1", {}, "My Private Journal"),
        element("p", {}, setup
            ? "Create a separate journal passphrase. ARCWorks cannot read or recover your entries."
            : "Unlock locally in this browser. Your passphrase is never sent to ARCWorks."),
        element("label", {}, setup ? "New journal passphrase" : "Journal passphrase"), passphrase,
        ...(setup ? [element("label", {}, "Confirm passphrase"), confirm] : []),
        element("button", { type: "button", onclick: () => setup ? setupVault(passphrase.value, confirm.value) : unlock(passphrase.value) }, setup ? "Create private journal" : "Unlock"),
        ...(!setup ? [
            element("details", {}, element("summary", {}, "Recover with recovery key"),
                element("label", {}, "Recovery key"), recovery,
                element("label", {}, "New journal passphrase"), newPassphrase,
                element("button", { type: "button", onclick: () => recover(recovery.value, newPassphrase.value) }, "Recover and rotate keys"))
        ] : []),
        element("p", { className: "journal-status", role: "status" }));
    state.host.replaceChildren(body);
}

async function setupVault(passphrase, confirmation) {
    try {
        validatePassphrase(passphrase);
        if (passphrase !== confirmation) throw new Error("The journal passphrases do not match.");
        const dataKeyRaw = random(32);
        const salt = random(16), passNonce = random(12), recoveryNonce = random(12), recoveryRaw = random(32);
        const passKey = await derivePassphraseKey(passphrase, salt, KDF_ITERATIONS);
        const recoveryKey = await importRecoveryKey(recoveryRaw);
        const envelope = {
            passphraseSalt: b64(salt), passphraseNonce: b64(passNonce),
            passphraseWrappedKey: b64(await wrapDataKey(dataKeyRaw, passKey, passNonce)),
            recoveryNonce: b64(recoveryNonce),
            recoveryWrappedKey: b64(await wrapDataKey(dataKeyRaw, recoveryKey, recoveryNonce)),
            kdfIterations: KDF_ITERATIONS, cryptoVersion: VERSION, expectedVersion: null
        };
        await api("/key-envelope", { method: "PUT", body: JSON.stringify(envelope) });
        state.envelope = await api("/key-envelope");
        state.key = await crypto.subtle.importKey("raw", dataKeyRaw, { name: "AES-GCM" }, false, ["encrypt", "decrypt"]);
        renderWorkspace();
        showRecoveryKey(recoveryText(recoveryRaw));
    } catch (error) { status(error.message, true); }
}

async function unlock(passphrase) {
    try {
        const passKey = await derivePassphraseKey(passphrase, bytes(state.envelope.passphraseSalt), state.envelope.kdfIterations);
        state.key = await unwrapDataKey(bytes(state.envelope.passphraseWrappedKey), passKey, bytes(state.envelope.passphraseNonce));
        await loadEntries();
        renderWorkspace();
    } catch { status("The journal passphrase is incorrect or the encrypted key is damaged.", true); }
}

async function recover(recoveryValue, newPassphrase) {
    try {
        validatePassphrase(newPassphrase);
        const oldRecoveryKey = await importRecoveryKey(recoveryBytes(recoveryValue));
        const dataKeyRaw = new Uint8Array(await crypto.subtle.decrypt(
            { name: "AES-GCM", iv: bytes(state.envelope.recoveryNonce) }, oldRecoveryKey,
            bytes(state.envelope.recoveryWrappedKey)));
        const salt = random(16), passNonce = random(12), recoveryNonce = random(12), recoveryRaw = random(32);
        const passKey = await derivePassphraseKey(newPassphrase, salt, KDF_ITERATIONS);
        const recoveryKey = await importRecoveryKey(recoveryRaw);
        await api("/key-envelope", { method: "PUT", body: JSON.stringify({
            passphraseSalt: b64(salt), passphraseNonce: b64(passNonce),
            passphraseWrappedKey: b64(await wrapDataKey(dataKeyRaw, passKey, passNonce)),
            recoveryNonce: b64(recoveryNonce), recoveryWrappedKey: b64(await wrapDataKey(dataKeyRaw, recoveryKey, recoveryNonce)),
            kdfIterations: KDF_ITERATIONS, cryptoVersion: VERSION, expectedVersion: state.envelope.version
        }) });
        state.envelope = await api("/key-envelope");
        state.key = await crypto.subtle.importKey("raw", dataKeyRaw, { name: "AES-GCM" }, false, ["encrypt", "decrypt"]);
        await loadEntries();
        renderWorkspace();
        showRecoveryKey(recoveryText(recoveryRaw));
    } catch { status("Recovery failed. Check the recovery key and try again.", true); }
}

async function loadEntries() {
    const [active, deleted] = await Promise.all([api("/entries?deleted=false"), api("/entries?deleted=true")]);
    state.entries = active;
    state.deleted = deleted;
    state.plaintext.clear();
    for (const entry of [...active, ...deleted]) {
        try {
            const clear = await crypto.subtle.decrypt({ name: "AES-GCM", iv: bytes(entry.nonce) }, state.key, bytes(entry.ciphertext));
            state.plaintext.set(entry.id, JSON.parse(decoder.decode(clear)));
        } catch { state.plaintext.set(entry.id, { title: "Unreadable entry", body: "", tags: [] }); }
    }
}

function showRecoveryKey(value) {
    const panel = state.host.querySelector(".journal-panel");
    panel.prepend(element("div", { className: "journal-recovery", role: "alert" },
        element("strong", {}, "Save this recovery key now. It is shown once: "), value,
        element("br"), "If both this key and your passphrase are lost, no one—including ARCWorks administrators—can recover the journal."));
}

function renderWorkspace() {
    state.host.replaceChildren(element("section", { className: "journal-panel" },
        element("div", { className: "journal-header" },
            element("div", {}, element("h1", {}, "My Private Journal"), element("p", {}, "Encrypted locally. Private to you.")),
            element("button", { type: "button", onclick: lock }, "Lock journal")),
        element("div", { className: "journal-grid" }, renderList(), renderEditor()),
        element("p", { className: "journal-status", role: "status" })));
}

function lock() { state.key = null; state.plaintext.clear(); state.entries = []; state.deleted = []; state.current = null; renderLocked(); }

function renderList() {
    const search = element("input", { type: "search", placeholder: "Search decrypted entries", autocomplete: "off", spellcheck: "false" });
    const list = element("div");
    const draw = () => {
        const query = search.value.trim().toLocaleLowerCase();
        list.replaceChildren();
        for (const entry of state.entries) {
            const value = state.plaintext.get(entry.id);
            if (query && !`${value.title} ${value.body} ${(value.tags ?? []).join(" ")}`.toLocaleLowerCase().includes(query)) continue;
            list.append(element("button", { className: "journal-entry-button", type: "button", onclick: () => { state.current = entry; renderWorkspace(); } },
                element("strong", {}, value.title || "Untitled"),
                element("time", { datetime: entry.updatedUtc }, `Updated ${formatTimestamp(entry.updatedUtc)}`)));
        }
    };
    search.addEventListener("input", draw);
    draw();
    return element("aside", { className: "journal-list" },
        element("button", { type: "button", onclick: () => { state.current = null; renderWorkspace(); } }, "New entry"),
        search, list,
        element("details", {}, element("summary", {}, `Deleted (${state.deleted.length})`),
            ...state.deleted.map(entry => {
                const value = state.plaintext.get(entry.id);
                return element("div", {}, value.title || "Untitled",
                    element("button", { type: "button", onclick: () => restoreEntry(entry) }, "Restore"),
                    element("button", { type: "button", className: "danger", onclick: () => discardEntry(entry) }, "Discard forever"));
            })));
}

function renderEditor() {
    const value = state.current ? state.plaintext.get(state.current.id) : { title: "", body: "", tags: [] };
    const title = element("input", { maxlength: "200", autocomplete: "off", spellcheck: "false", value: value.title });
    const tags = element("input", { maxlength: "500", autocomplete: "off", spellcheck: "false", value: (value.tags ?? []).join(", ") });
    const body = element("textarea", { maxlength: "100000", autocomplete: "off", spellcheck: "false" }, value.body);
    const preview = element("div", { className: "journal-preview" });
    const words = element("span");
    const update = () => {
        renderSafeMarkdown(body.value, preview);
        const count = body.value.trim() ? body.value.trim().split(/\s+/u).length : 0;
        words.textContent = `${count} word${count === 1 ? "" : "s"}`;
    };
    body.addEventListener("input", update);
    const insert = (before, after = before) => {
        const start = body.selectionStart, end = body.selectionEnd;
        body.setRangeText(`${before}${body.value.slice(start, end)}${after}`, start, end, "select");
        body.dispatchEvent(new Event("input")); body.focus();
    };
    const toolbar = element("div", { className: "journal-toolbar", role: "toolbar", "aria-label": "Markdown formatting" },
        element("button", { type: "button", onclick: () => insert("**") }, "Bold"),
        element("button", { type: "button", onclick: () => insert("*") }, "Italic"),
        element("button", { type: "button", onclick: () => insert("## ", "") }, "Heading"),
        element("button", { type: "button", onclick: () => insert("- ", "") }, "List"),
        element("button", { type: "button", onclick: () => insert("> ", "") }, "Quote"),
        element("button", { type: "button", onclick: () => insert("\n---\n", "") }, "Rule"));
    update();
    return element("main", { className: "journal-editor" },
        ...(state.current ? [element("p", { className: "journal-entry-times" },
            `Created ${formatTimestamp(state.current.createdUtc)} · Updated ${formatTimestamp(state.current.updatedUtc)}`)] : []),
        element("label", {}, "Title"), title, element("label", {}, "Tags (comma separated)"), tags,
        toolbar, element("label", {}, "Journal entry (Markdown)"), body, words,
        element("h2", {}, "Safe preview"), preview,
        element("button", { type: "button", onclick: () => saveEntry(title.value, body.value, tags.value) }, "Save encrypted entry"),
        ...(state.current ? [element("button", { type: "button", className: "danger", onclick: () => deleteEntry(state.current) }, "Move to deleted")] : []));
}

function formatTimestamp(value) {
    return new Intl.DateTimeFormat(state.locale, {
        timeZone: state.timeZone, dateStyle: "medium", timeStyle: "short"
    }).format(new Date(value));
}

async function saveEntry(title, body, tags) {
    try {
        if (!title.trim() && !body.trim()) throw new Error("Add a title or journal text before saving.");
        validateJournalMarkdown(body);
        const payload = encoder.encode(JSON.stringify({ title: title.trim(), body, tags: tags.split(",").map(x => x.trim()).filter(Boolean) }));
        const nonce = random(12);
        const ciphertext = new Uint8Array(await crypto.subtle.encrypt({ name: "AES-GCM", iv: nonce }, state.key, payload));
        const write = { ciphertext: b64(ciphertext), nonce: b64(nonce), cryptoVersion: VERSION, expectedVersion: state.current?.version ?? null };
        if (state.current) await api(`/entries/${state.current.id}`, { method: "PUT", body: JSON.stringify(write) });
        else await api("/entries", { method: "POST", body: JSON.stringify(write) });
        await loadEntries(); state.current = null; renderWorkspace(); status("Encrypted journal entry saved.");
    } catch (error) { status(error.message, true); }
}

export function validateJournalMarkdown(markdown) {
    if (/<\/?[a-z][^>]*>/iu.test(markdown))
        throw new Error("Raw HTML is not supported in the private journal.");
    if (/!\[[^\]]*\]\([^)]*\)/u.test(markdown))
        throw new Error("Images are not supported in the private journal.");
    if (/(?:javascript|vbscript|data)\s*:/iu.test(markdown))
        throw new Error("Unsafe URLs are not supported in the private journal.");
}

async function deleteEntry(entry) {
    if (!confirm("Move this private journal entry to Deleted?")) return;
    try { await api(`/entries/${entry.id}/delete`, { method: "POST", body: JSON.stringify({ expectedVersion: entry.version }) }); await loadEntries(); state.current = null; renderWorkspace(); }
    catch (error) { status(error.message, true); }
}
async function restoreEntry(entry) {
    try { await api(`/entries/${entry.id}/restore`, { method: "POST", body: JSON.stringify({ expectedVersion: entry.version }) }); await loadEntries(); renderWorkspace(); }
    catch (error) { status(error.message, true); }
}
async function discardEntry(entry) {
    if (!confirm("Permanently discard this encrypted entry? This cannot be undone.")) return;
    try { await api(`/entries/${entry.id}?expectedVersion=${entry.version}`, { method: "DELETE" }); await loadEntries(); renderWorkspace(); }
    catch (error) { status(error.message, true); }
}

export function renderSafeMarkdown(markdown, target) {
    target.replaceChildren();
    const lines = markdown.replaceAll("\r\n", "\n").split("\n");
    let list = null;
    for (const line of lines) {
        if (/^\s*([-*_])\1\1+\s*$/.test(line)) { target.append(element("hr")); list = null; continue; }
        const heading = /^(#{1,3})\s+(.+)$/.exec(line);
        if (heading) { const h = element(`h${heading[1].length}`); appendInline(h, heading[2]); target.append(h); list = null; continue; }
        const item = /^\s*[-*]\s+(.+)$/.exec(line);
        if (item) { if (!list) { list = element("ul"); target.append(list); } const li = element("li"); appendInline(li, item[1]); list.append(li); continue; }
        const quote = /^>\s?(.*)$/.exec(line);
        if (quote) { const block = element("blockquote"); appendInline(block, quote[1]); target.append(block); list = null; continue; }
        if (!line.trim()) { target.append(element("br")); list = null; continue; }
        const paragraph = element("p"); appendInline(paragraph, line); target.append(paragraph); list = null;
    }
}

function appendInline(target, text) {
    // Text nodes are the security boundary: raw HTML, image syntax, URLs and
    // event attributes can never become markup. Only bold and italic delimiters
    // create elements.
    const pattern = /(\*\*[^*]+\*\*|\*[^*]+\*)/g;
    let cursor = 0;
    for (const match of text.matchAll(pattern)) {
        target.append(document.createTextNode(text.slice(cursor, match.index)));
        const strong = match[0].startsWith("**");
        target.append(element(strong ? "strong" : "em", {}, match[0].slice(strong ? 2 : 1, strong ? -2 : -1)));
        cursor = match.index + match[0].length;
    }
    target.append(document.createTextNode(text.slice(cursor)));
}
