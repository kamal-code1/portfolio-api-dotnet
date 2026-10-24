"use strict";

const form = document.getElementById("contact-form");
const statusEl = document.getElementById("form-status");
const submitBtn = document.getElementById("submit-btn");

function setStatus(text, kind) {
  statusEl.textContent = text;
  statusEl.className = "form-status" + (kind ? " " + kind : "");
}

function firstValidationError(body) {
  // Réponse ProblemDetails d'ASP.NET Core : { errors: { Champ: ["message"] } }
  if (body && body.errors) {
    const messages = Object.values(body.errors).flat();
    if (messages.length) return messages[0];
  }
  return body && body.message ? body.message : null;
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();

  if (!form.checkValidity()) {
    form.reportValidity();
    setStatus("Veuillez corriger les champs signalés.", "err");
    return;
  }

  const data = Object.fromEntries(new FormData(form).entries());
  submitBtn.disabled = true;
  setStatus("Envoi en cours…");

  try {
    const response = await fetch("/api/portfolio/messages", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data)
    });
    const body = await response.json().catch(() => null);

    if (response.ok && body && body.success) {
      form.reset();
      setStatus(body.message, "ok");
    } else {
      setStatus(firstValidationError(body) || "Votre message n'a pas pu être envoyé. Réessayez plus tard.", "err");
    }
  } catch {
    setStatus("Connexion impossible. Vérifiez votre réseau et réessayez.", "err");
  } finally {
    submitBtn.disabled = false;
  }
});
