/*
 * Novabourse — SPA React minimale (laboratoire DAST, donnees fictives).
 * Flux OIDC PKCE c^ote client + consultation du portefeuille et des instruments.
 *
 * 
 * Le nom de portefeuille est rendu via dangerouslySetInnerHTML (rendu HTML non
 * echappe) au lieu du rendu textuel par defaut de React : une charge stockee via
 * POST /api/portfolios s'execute a l'affichage de la liste.
 */
const { useState, useEffect } = React;

// --- PKCE helpers (S256) -----------------------------------------------------
function b64url(bytes) {
  return btoa(String.fromCharCode.apply(null, new Uint8Array(bytes)))
    .replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
function randomVerifier() {
  const a = new Uint8Array(32); crypto.getRandomValues(a);
  return b64url(a.buffer);
}
async function challenge(verifier) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
  return b64url(digest);
}

async function beginLogin() {
  const verifier = randomVerifier();
  const cc = await challenge(verifier);
  const state = b64url(crypto.getRandomValues(new Uint8Array(12)).buffer);
  sessionStorage.setItem("pkce_verifier", verifier);
  sessionStorage.setItem("oidc_state", state);  // state lie a la session (SPA)
  const redirect = location.origin + "/callback";
  const url = "/authorize?response_type=code&client_id=novabourse-spa"
    + "&redirect_uri=" + encodeURIComponent(redirect)
    + "&code_challenge=" + encodeURIComponent(cc)
    + "&code_challenge_method=S256&state=" + encodeURIComponent(state)
    + "&login=invest-a";
  location.href = url;
}

async function completeLogin() {
  const p = new URLSearchParams(location.search);
  const code = p.get("code");
  if (!code) return null;
  const verifier = sessionStorage.getItem("pkce_verifier");
  const body = new URLSearchParams({
    grant_type: "authorization_code", code: code,
    redirect_uri: location.origin + "/callback",
    client_id: "novabourse-spa", code_verifier: verifier || "",
  });
  const r = await fetch("/token", { method: "POST", body });
  if (!r.ok) return null;
  const j = await r.json();
  sessionStorage.setItem("access_token", j.access_token);
  history.replaceState({}, "", "/");
  return j.access_token;
}

function auth() { return { Authorization: "Bearer " + (sessionStorage.getItem("access_token") || "") }; }

function App() {
  const [token, setToken] = useState(sessionStorage.getItem("access_token"));
  const [portfolios, setPortfolios] = useState([]);
  const [instruments, setInstruments] = useState([]);
  const [q, setQ] = useState("NOVA");

  useEffect(() => { completeLogin().then((t) => { if (t) setToken(t); }); }, []);
  useEffect(() => {
    if (!token) return;
    fetch("/api/portfolios", { headers: auth() }).then(r => r.json()).then(setPortfolios).catch(() => {});
  }, [token]);

  const search = () => fetch("/api/instruments?q=" + encodeURIComponent(q), { headers: auth() })
    .then(r => r.json()).then(d => setInstruments(Array.isArray(d) ? d : [])).catch(() => {});

  if (!token) {
    return (
      <div>
        <h1>Novabourse</h1>
        <p className="muted">Portail titres de laboratoire. Connexion OIDC PKCE.</p>
        <button onClick={beginLogin}>Se connecter (invest-a)</button>
      </div>
    );
  }

  return (
    <div>
      <h1>Mes portefeuilles</h1>
      {portfolios.map((pf) => (
        <div className="card" key={pf.id}>
          {/* 
          <div dangerouslySetInnerHTML={{ __html: pf.name }} />
          <div className="muted">
            <code>{pf.id}</code> — espèces : {pf.cash} € — {pf.positions.length} position(s)
          </div>
        </div>
      ))}
      <h1>Recherche d'instruments</h1>
      <input value={q} onChange={(e) => setQ(e.target.value)} />
      <button onClick={search}>Rechercher</button>
      <ul>
        {instruments.map((it) => (
          <li key={it.code}><code>{it.code}</code> — {it.name} — {it.lastPrice} €</li>
        ))}
      </ul>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);
