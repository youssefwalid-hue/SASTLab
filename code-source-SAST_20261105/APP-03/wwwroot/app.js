// SPA minimale Tresoria (laboratoire, donnees fictives).
// Securisee par defaut SAUF le rendu du libelle de virement (GT: APP03-V009).
(function () {
  "use strict";
  let token = null;

  const $ = (id) => document.getElementById(id);
  const api = (path, opts = {}) => {
    opts.headers = Object.assign(
      { "Content-Type": "application/json" },
      token ? { Authorization: "Bearer " + token } : {},
      opts.headers || {}
    );
    return fetch("/api" + path, opts);
  };

  $("btn-login").onclick = async () => {
    const r = await api("/login", {
      method: "POST",
      body: JSON.stringify({ username: $("u").value, password: $("p").value }),
    });
    if (!r.ok) { $("login-msg").textContent = "Échec de connexion"; return; }
    const data = await r.json();
    token = data.access_token;
    $("who").textContent = $("u").value + " (" + data.role + "/" + data.company + ")";
    $("login-view").hidden = true;
    $("app-view").hidden = false;
    loadBatches();
  };

  $("btn-logout").onclick = async () => {
    await api("/logout", { method: "POST" });
    token = null;
    $("app-view").hidden = true;
    $("login-view").hidden = false;
  };

  async function loadBatches() {
    const r = await api("/batches");
    if (!r.ok) return;
    const items = await r.json();
    const ul = $("batches");
    ul.innerHTML = "";
    items.forEach((b) => {
      const li = document.createElement("li");
      // Rendu SUR : texte (textContent) pour les champs de confiance / enumeres.
      li.textContent = `#${b.id} — ${b.label} [${b.status}] (${b.count} virements)`;
      ul.appendChild(li);
    });
  }
  $("btn-batches").onclick = loadBatches;

  $("btn-transfers").onclick = async () => {
    const r = await api("/transfers?batchId=" + encodeURIComponent($("bid").value));
    if (!r.ok) return;
    const items = await r.json();
    const tb = $("transfers");
    tb.innerHTML = "";
    items.forEach((t) => {
      const tr = document.createElement("tr");
      const ibanTd = document.createElement("td");
      ibanTd.textContent = t.iban;
      const amtTd = document.createElement("td");
      amtTd.textContent = t.amount;
      const lblTd = document.createElement("td");
      //   Le code sur ferait : lblTd.textContent = t.label; (interpolation / encodage).
      lblTd.innerHTML = t.label;
      tr.appendChild(ibanTd); tr.appendChild(amtTd); tr.appendChild(lblTd);
      tb.appendChild(tr);
    });
  };
})();
