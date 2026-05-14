import { el } from "./dom";

const TOKEN_KEY = "smartconnect.token";

export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setToken(value: string | null): void {
  try {
    if (value && value.trim() !== "") {
      localStorage.setItem(TOKEN_KEY, value.trim());
    } else {
      localStorage.removeItem(TOKEN_KEY);
    }
  } catch {
    // localStorage disabled — bearer token is session-only via input value
  }
}

export function mountAuthBar(target: HTMLElement): void {
  const current = getToken() ?? "";
  const input = el("input", {
    type: "password",
    placeholder: "Paste JWT here (optional)",
    size: 40,
    value: current,
    autocomplete: "off",
    "aria-label": "Bearer token",
  }) as HTMLInputElement;
  const status = el("span", { class: "muted", id: "auth-status" }, current ? "token set" : "no token");
  const save = el("button", {
    type: "button",
    onclick: () => {
      setToken(input.value);
      status.textContent = input.value.trim() ? "token saved" : "token cleared";
    },
  }, "Save");
  const clearBtn = el("button", {
    type: "button",
    onclick: () => {
      input.value = "";
      setToken(null);
      status.textContent = "token cleared";
    },
  }, "Clear");
  target.appendChild(el("div", { class: "auth-bar" }, [
    el("label", { class: "muted" }, "Auth:"),
    input, save, clearBtn, status,
  ]));
}
