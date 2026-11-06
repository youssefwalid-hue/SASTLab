package main

// =============================================================================
// main.go — Serveur HTTP MobiPay (APP-06).
// Endpoint unique POST /graphql, POST /admin/reset, GET /healthz.
// Banking DAST Benchmark Lab — DONNÉES 100 % FICTIVES — usage défensif.
// =============================================================================

import (
	"encoding/json"
	"log"
	"net/http"
	"net/url"
	"os"
	"strings"
)

const serverBanner = "Go-http (go1.22 net/http)" 

type graphQLRequest struct {
	Query         string                 `json:"query"`
	OperationName string                 `json:"operationName"`
	Variables     map[string]interface{} `json:"variables"`
	Extensions    map[string]interface{} `json:"extensions"`
}

type graphQLResponse struct {
	Data   map[string]interface{} `json:"data"`
	Errors []*gqlError            `json:"errors,omitempty"`
}

// commonHeaders positionne les en-têtes présents sur TOUTES les réponses.
// NB : les en-têtes de durcissement (HSTS, X-Content-Type-Options, CSP…) sont
// volontairement ABSENTS (GT: APP06-V014), et la bannière serveur est exposée
// (GT: APP06-V016).
func commonHeaders(w http.ResponseWriter) {
	w.Header().Set("X-Lab-Environment", "test-data-fictives")
	w.Header().Set("Server", serverBanner)           
	w.Header().Set("X-Powered-By", "gqlgen/Go 1.22") 
}

// applyCORS reflète l'Origin avec credentials (GT: APP06-V013).
func applyCORS(w http.ResponseWriter, r *http.Request) {
	origin := r.Header.Get("Origin")
	if origin != "" {
		w.Header().Set("Access-Control-Allow-Origin", origin) 
		w.Header().Set("Access-Control-Allow-Credentials", "true")
		w.Header().Set("Access-Control-Allow-Headers", "Authorization, Content-Type, X-Device-Id")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
	}
}

func graphqlHandler(w http.ResponseWriter, r *http.Request) {
	commonHeaders(w)
	applyCORS(w, r)
	w.Header().Set("Content-Type", "application/json; charset=utf-8")

	if r.Method == http.MethodOptions {
		w.WriteHeader(http.StatusNoContent)
		return
	}
	if r.Method != http.MethodPost {
		w.WriteHeader(http.StatusMethodNotAllowed)
		_ = json.NewEncoder(w).Encode(graphQLResponse{Errors: []*gqlError{{Message: "methode non autorisee : utiliser POST"}}})
		return
	}

	var req graphQLRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		w.WriteHeader(http.StatusBadRequest)
		_ = json.NewEncoder(w).Encode(graphQLResponse{Errors: []*gqlError{{Message: "corps JSON invalide"}}})
		return
	}

	// (champ `query`) est acceptée même hors de la liste persistée. Seule une
	// requête SANS `query` mais AVEC un hash persisté inconnu est refusée.
	if strings.TrimSpace(req.Query) == "" {
		if hasPersistedHash(req.Extensions) {
			_ = json.NewEncoder(w).Encode(graphQLResponse{Errors: []*gqlError{{
				Message:    "PersistedQueryNotFound",
				Extensions: map[string]interface{}{"code": "PERSISTED_QUERY_NOT_FOUND"}}}})
			return
		}
		w.WriteHeader(http.StatusBadRequest)
		_ = json.NewEncoder(w).Encode(graphQLResponse{Errors: []*gqlError{{Message: "requete vide"}}})
		return
	}

	doc, perr := ParseDocument(req.Query)
	if perr != nil {
		_ = json.NewEncoder(w).Encode(graphQLResponse{Errors: []*gqlError{{Message: perr.Error()}}})
		return
	}

	ex := &Executor{
		doc:       doc,
		vars:      req.Variables,
		deviceHdr: r.Header.Get("X-Device-Id"),
	}
	if ex.vars == nil {
		ex.vars = map[string]interface{}{}
	}

	// Authentification : jeton JWT porteur. L'empreinte d'appareil n'est PAS
	// revérifiée (GT: APP06-V007).
	if auth := r.Header.Get("Authorization"); strings.HasPrefix(auth, "Bearer ") {
		if c, err := verifyJWT(strings.TrimPrefix(auth, "Bearer ")); err == nil {
			ex.currentUser = store.userByName(c.Sub)
			ex.authAlgNone = c.algNone 
		}
	}

	data, errs := ex.Execute(req.OperationName)
	_ = json.NewEncoder(w).Encode(graphQLResponse{Data: data, Errors: errs})
}

func hasPersistedHash(ext map[string]interface{}) bool {
	if ext == nil {
		return false
	}
	pq, ok := ext["persistedQuery"].(map[string]interface{})
	if !ok {
		return false
	}
	_, has := pq["sha256Hash"]
	return has
}

func resetHandler(w http.ResponseWriter, r *http.Request) {
	commonHeaders(w)
	applyCORS(w, r)
	store.Reset()
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	_ = json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "message": "etat reinitialise (donnees fictives)"})
}

// redirectHandler : GT: APP06-V020 — redirection ouverte. Le paramètre `next`
// est réfléchi tel quel dans l'en-tête Location, sans validation ni allowlist :
// un lien /go?next=https://attaquant.exemple/ renvoie une redirection externe.
func redirectHandler(w http.ResponseWriter, r *http.Request) {
	commonHeaders(w)
	applyCORS(w, r)
	next := r.URL.Query().Get("next")
	if next == "" {
		next = "/healthz"
	}
	w.Header().Set("Location", next) 
	w.WriteHeader(http.StatusFound)  // 302
}

// redirectSafeHandler : NC: APP06-N011 — redirection validée. Seules les cibles
// relatives (commençant par "/", sans "//") ou un hôte explicitement allowlisté
// sont autorisées ; toute destination externe est rejetée (400).
func redirectSafeHandler(w http.ResponseWriter, r *http.Request) {
	commonHeaders(w)
	applyCORS(w, r)
	next := r.URL.Query().Get("next")
	if !safeRedirectTarget(next) {
		w.Header().Set("Content-Type", "application/json; charset=utf-8")
		w.WriteHeader(http.StatusBadRequest)
		_ = json.NewEncoder(w).Encode(map[string]interface{}{"error": "destination refusee : hors allowlist"})
		return
	}
	w.Header().Set("Location", next)
	w.WriteHeader(http.StatusFound)
}

// safeRedirectTarget n'autorise que les chemins relatifs internes ou l'hôte de
// laboratoire allowlisté (NC APP06-N011).
func safeRedirectTarget(next string) bool {
	if next == "" {
		return false
	}
	// Chemin relatif interne : commence par "/" mais pas "//" (protocol-relative).
	if strings.HasPrefix(next, "/") && !strings.HasPrefix(next, "//") {
		return true
	}
	u, err := url.Parse(next)
	if err != nil {
		return false
	}
	return u.Hostname() == "mobipay.lab.exemple"
}

func healthHandler(w http.ResponseWriter, r *http.Request) {
	// Volontairement SANS en-têtes de durcissement (GT: APP06-V014), avec
	// bannière serveur exposée (GT: APP06-V016).
	commonHeaders(w)
	applyCORS(w, r)
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	_ = json.NewEncoder(w).Encode(map[string]interface{}{"status": "ok", "app": "MobiPay", "env": "test-data-fictives"})
}

func main() {
	mux := http.NewServeMux()
	mux.HandleFunc("/graphql", graphqlHandler)
	mux.HandleFunc("/admin/reset", resetHandler)
	mux.HandleFunc("/healthz", healthHandler)
	mux.HandleFunc("/go", redirectHandler)          
	mux.HandleFunc("/go/safe", redirectSafeHandler) // NC: APP06-N011 — redirection validée
	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/" {
			healthHandler(w, r)
			return
		}
		commonHeaders(w)
		w.WriteHeader(http.StatusNotFound)
	})

	port := os.Getenv("PORT")
	if port == "" {
		port = "8080" // profil local (BUILD_SPEC) ; EB fournit PORT=5000
	}
	log.Printf("MobiPay (APP-06) — DAST Benchmark Lab — ecoute sur :%s (donnees 100%% fictives)", port)
	// SuppressServerHeader non disponible ; on positionne Server explicitement.
	srv := &http.Server{Addr: ":" + port, Handler: mux}
	if err := srv.ListenAndServe(); err != nil {
		log.Fatal(err)
	}
}
