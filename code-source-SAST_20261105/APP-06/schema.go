package main

// =============================================================================
// schema.go — Résolveurs MobiPay.
// SÉCURISÉ PAR DÉFAUT partout, SAUF les 16 écarts localisés tagués `// GT:`.
// Les contre-mesures sûres (contrôles négatifs) sont taguées `// NC:`.
// =============================================================================

import (
	"encoding/json"
	"fmt"
	"io"
	"net"
	"net/http"
	"net/url"
	"os/exec"
	"strconv"
	"strings"
	"time"
)

func argString(args map[string]interface{}, name string) string {
	if v, ok := args[name]; ok {
		if s, ok := v.(string); ok {
			return s
		}
		if v != nil {
			return fmt.Sprintf("%v", v)
		}
	}
	return ""
}

func (ex *Executor) requireAuth() (*User, *gqlError) {
	if ex.currentUser == nil {
		return nil, &gqlError{Message: "non authentifie : jeton requis",
			Extensions: map[string]interface{}{"code": "UNAUTHENTICATED"}}
	}
	return ex.currentUser, nil
}

func unknownField(typ, name string) *gqlError {
	return &gqlError{Message: fmt.Sprintf("Cannot query field \"%s\" on type \"%s\".", name, typ),
		Extensions: map[string]interface{}{"code": "GRAPHQL_VALIDATION_FAILED"}}
}

// --- Racine Query ------------------------------------------------------------

type QueryRoot struct{}

func (q *QueryRoot) TypeName() string { return "Query" }

func (q *QueryRoot) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "__schema":
		return &introSchema{}, nil 
	case "__type":
		if td := findType(argString(args, "name")); td != nil {
			return &introType{desc: td}, nil
		}
		return nil, nil

	case "me":
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		return &userObj{u}, nil

	case "session":
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		// client et n'est jamais liée au jeton : l'accès est accordé quelle que
		// soit sa valeur. `deviceVerified` expose la non-concordance.
		enrolled := u.DeviceID
		return &sessionObj{userID: u.ID, deviceHdr: ex.deviceHdr, enrolled: enrolled}, nil

	case "node":
		if _, e := ex.requireAuth(); e != nil {
			return nil, e
		}
		id := argString(args, "id")
		typ, key, ok := decodeGlobalID(id)
		if !ok {
			return nil, &gqlError{
				Message: "node resolver failure at /app/internal/relay/node.go:88: pq: invalid input syntax for type bytea; raw global id=" + id,
				Extensions: map[string]interface{}{
					"code":       "INTERNAL_SERVER_ERROR",
					"stacktrace": "relay.decodeGlobalID(/app/internal/relay/node.go:88) <- gql.resolveNode(/app/internal/gql/resolver_node.go:42)",
					"driver":     "github.com/lib/pq",
				},
			}
		}
		switch typ {
		case "Account":
			if a := store.accountByID(key); a != nil {
				return &accountObj{a}, nil
			}
		case "User":
			if u := store.userByID(key); u != nil {
				return &userObj{u}, nil
			}
		case "Card":
			if c := store.cardByID(key); c != nil {
				return &cardObj{c}, nil
			}
		}
		return nil, nil

	case "account":
		// NC: APP06-N001 — accès à un compte AVEC contrôle de propriété.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		id := argString(args, "id")
		_, key, ok := decodeGlobalID(id)
		if !ok {
			key = id
		}
		a := store.accountByID(key)
		if a == nil || a.OwnerID != u.ID {
			return nil, &gqlError{Message: "acces refuse : objet hors perimetre",
				Extensions: map[string]interface{}{"code": "FORBIDDEN"}}
		}
		return &accountObj{a}, nil

	case "notifications":
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		target := u.ID
		if uid := argString(args, "userId"); uid != "" {
			target = uid
		}
		return notifList(target), nil

	case "secureNotifications":
		// NC: APP06-N001 — tout userId client est ignoré au profit du sujet du jeton.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		return notifList(u.ID), nil

	case "transactions":
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		filter := argString(args, "filter")
		if filter == "" {
			filter = "'1'='1'"
		}
		txs := store.queryTransactionsVulnerable(u.ID, filter)
		return txListFrom(txs), nil

	case "transactionsByCategory":
		// NC: APP06-N005 — filtre typé (enum) mappé en requête paramétrée.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		cat := argString(args, "category")
		if cat != "" && !validCategory(cat) {
			return nil, &gqlError{Message: "Value \"" + cat + "\" does not exist in \"Category\" enum.",
				Extensions: map[string]interface{}{"code": "GRAPHQL_VALIDATION_FAILED"}}
		}
		txs := store.queryTransactionsSafe(u.ID, cat)
		return txListFrom(txs), nil

	case "wallet":
		// NC: APP06-N002 — type sûr WalletCard SANS pan/cvv.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		return &walletObj{ownerID: u.ID}, nil

	case "trustedProfile":
		// NC: APP06-N009 — profil « de confiance » : rejette les jetons acceptés
		// via alg:none (non signés). Un jeton HS256 valide passe normalement.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		if ex.authAlgNone {
			return nil, &gqlError{Message: "acces refuse : jeton non signe (alg:none) rejete",
				Extensions: map[string]interface{}{"code": "FORBIDDEN"}}
		}
		return &userObj{u}, nil

	case "diagnosticPing":
		// dans une commande shell sans validation.
		if _, e := ex.requireAuth(); e != nil {
			return nil, e
		}
		host := argString(args, "host")
		out, ok := runDiagnosticVulnerable(host)
		return &diagResultObj{ok: ok, output: out, cmd: "/bin/sh -c echo pong-" + host}, nil

	case "diagnosticPingSafe":
		// NC: APP06-N010 — hôte validé (allowlist de caractères) + exécution SANS shell.
		if _, e := ex.requireAuth(); e != nil {
			return nil, e
		}
		host := argString(args, "host")
		out, ok, errMsg := runDiagnosticSafe(host)
		return &diagResultObj{ok: ok, output: out, cmd: "echo [arg]", errMsg: errMsg}, nil

	case "userDirectory":
		// l'annuaire des utilisateurs (username, email, téléphone) SANS exiger
		// de jeton (aucun appel à requireAuth).
		out := []interface{}{}
		for _, u := range store.directory() {
			out = append(out, &directoryEntryObj{u})
		}
		return out, nil

	case "userDirectorySecure":
		// NC: APP06-N013 — même annuaire mais protégé par authentification.
		if _, e := ex.requireAuth(); e != nil {
			return nil, e
		}
		out := []interface{}{}
		for _, u := range store.directory() {
			out = append(out, &directoryEntryObj{u})
		}
		return out, nil

	case "search":
		// Use case bénin : recherche cloisonnée au propriétaire (aucun écart).
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		return txListFrom(store.searchTransactions(u.ID, argString(args, "term"))), nil

	case "exportTransactions":
		// Use case bénin : export CSV/JSON des transactions de l'utilisateur.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		format := strings.ToUpper(argString(args, "format"))
		if format == "" {
			format = "JSON"
		}
		txs := store.searchTransactions(u.ID, "")
		content, count := buildExport(format, txs)
		return &exportResultObj{format: format, content: content, count: count}, nil

	case "activityHistory":
		// Use case bénin : historique paginé des notifications de l'utilisateur.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		limit := argInt(args, "limit", 10)
		offset := argInt(args, "offset", 0)
		all := store.notificationsOf(u.ID)
		out := []interface{}{}
		for i := offset; i < len(all) && i < offset+limit; i++ {
			out = append(out, &notifObj{all[i]})
		}
		return out, nil

	case "appInfo":
		// Use case bénin : page d'aide / à-propos (aucune donnée sensible).
		return &appInfoObj{}, nil
	}

	return nil, unknownField("Query", name)
}

// argInt lit un argument entier avec valeur par défaut.
func argInt(args map[string]interface{}, name string, def int) int {
	v, ok := args[name]
	if !ok || v == nil {
		return def
	}
	switch n := v.(type) {
	case int64:
		return int(n)
	case int:
		return n
	case float64:
		return int(n)
	}
	return def
}

// buildExport sérialise les transactions en CSV ou JSON (use case bénin).
func buildExport(format string, txs []*Transaction) (string, int) {
	if format == "CSV" {
		var sb strings.Builder
		sb.WriteString("id,amount,label,category\n")
		for _, t := range txs {
			sb.WriteString(fmt.Sprintf("%s,%.2f,%s,%s\n", t.ID, t.Amount, t.Label, t.Category))
		}
		return sb.String(), len(txs)
	}
	type row struct {
		ID       string  `json:"id"`
		Amount   float64 `json:"amount"`
		Label    string  `json:"label"`
		Category string  `json:"category"`
	}
	rows := make([]row, 0, len(txs))
	for _, t := range txs {
		rows = append(rows, row{t.ID, t.Amount, t.Label, t.Category})
	}
	b, _ := json.Marshal(rows)
	return string(b), len(txs)
}

// --- Diagnostic réseau : injection (V019) vs exécution sûre (N010) -----------

func runDiagnosticVulnerable(host string) (string, bool) {
	out, err := exec.Command("/bin/sh", "-c", "echo pong-"+host).CombinedOutput()
	return strings.TrimRight(string(out), "\n"), err == nil
}

func runDiagnosticSafe(host string) (string, bool, string) {
	// NC: APP06-N010 — validation de l'hôte + exécution sans interpréteur shell.
	if !validHost(host) {
		return "", false, "hote invalide : caracteres non autorises"
	}
	out, err := exec.Command("echo", "pong-"+host).CombinedOutput()
	return strings.TrimRight(string(out), "\n"), err == nil, ""
}

func validHost(h string) bool {
	if h == "" || len(h) > 253 {
		return false
	}
	for i := 0; i < len(h); i++ {
		c := h[i]
		ok := (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
			(c >= '0' && c <= '9') || c == '.' || c == '-'
		if !ok {
			return false
		}
	}
	return true
}

func notifList(userID string) []interface{} {
	out := []interface{}{}
	for _, n := range store.notificationsOf(userID) {
		out = append(out, &notifObj{n})
	}
	return out
}

func txListFrom(txs []*Transaction) []interface{} {
	out := []interface{}{}
	for _, t := range txs {
		out = append(out, &txObj{t})
	}
	return out
}

func validCategory(c string) bool {
	switch c {
	case "FOOD", "TRANSPORT", "SALARY", "OTHER":
		return true
	}
	return false
}

// --- Racine Mutation ---------------------------------------------------------

type MutationRoot struct{}

func (m *MutationRoot) TypeName() string { return "Mutation" }

func (m *MutationRoot) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "login":
		u := store.userByName(argString(args, "username"))
		if u == nil || u.Password != argString(args, "password") {
			return nil, &gqlError{Message: "identifiants invalides",
				Extensions: map[string]interface{}{"code": "UNAUTHENTICATED"}}
		}
		return &authPayloadObj{token: issueToken(u), user: u}, nil

	case "updateProfile":
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		in, _ := args["input"].(map[string]interface{})
		display := ""
		email := ""
		role := ""
		if in != nil {
			display, _ = in["displayName"].(string)
			email, _ = in["email"].(string)
			role, _ = in["role"].(string) 
		}
		store.setProfile(u.ID, display, email, role)
		return &userObj{store.userByID(u.ID)}, nil

	case "updateContact":
		// NC: APP06-N004 — input strict : le champ privilégié role est rejeté.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		in, _ := args["input"].(map[string]interface{})
		if in != nil {
			if _, bad := in["role"]; bad {
				return nil, &gqlError{Message: "Field \"role\" is not defined by type \"UpdateContactInput\".",
					Extensions: map[string]interface{}{"code": "GRAPHQL_VALIDATION_FAILED"}}
			}
			display, _ := in["displayName"].(string)
			email, _ := in["email"].(string)
			store.setProfile(u.ID, display, email, "") // role jamais modifié
		}
		return &userObj{store.userByID(u.ID)}, nil

	case "blockAllCards":
		if _, e := ex.requireAuth(); e != nil {
			return nil, e
		}
		return &countObj{n: store.blockAllCards()}, nil

	case "freezeAccount":
		// NC: APP06-N003 — mutation d'administration restreinte au rôle ADMIN.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		if u.Role != "ADMIN" {
			return nil, &gqlError{Message: "acces refuse : role ADMIN requis",
				Extensions: map[string]interface{}{"code": "FORBIDDEN"}}
		}
		id := argString(args, "id")
		_, key, ok := decodeGlobalID(id)
		if !ok {
			key = id
		}
		if store.accountByID(key) == nil {
			return nil, &gqlError{Message: "compte introuvable", Extensions: map[string]interface{}{"code": "NOT_FOUND"}}
		}
		return &okObj{ok: true}, nil

	case "verifyOtp":
		// tentatives par utilisateur ; chaque alias dans une même requête est un
		// essai réel non limité.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		_ = u
		code := argString(args, "code")
		return &okObj{ok: code == labOTP}, nil

	case "verifyPin":
		// NC: APP06-N007 — anti-batching : tentatives comptées par utilisateur,
		// verrouillage après N échecs quel que soit le nombre d'alias.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		if store.pinLocked(u.Username) {
			return nil, &gqlError{Message: "trop de tentatives : verification verrouillee",
				Extensions: map[string]interface{}{"code": "TOO_MANY_REQUESTS"}}
		}
		code := argString(args, "code")
		if code == labOTP {
			return &okObj{ok: true}, nil
		}
		store.pinFailure(u.Username)
		return &okObj{ok: false}, nil

	case "setAvatar":
		// allowlist ni blocage des adresses internes.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		raw := argString(args, "url")
		store.setAvatar(u.ID, raw)
		ct, size, ferr := fetchURL(raw)
		return &avatarObj{ok: ferr == "", attempted: true, contentType: ct, size: size, errMsg: ferr}, nil

	case "setAvatarSafe":
		// NC: APP06-N006 — allowlist CDN + rejet des adresses privées, AVANT toute requête.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		raw := argString(args, "url")
		if !avatarAllowed(raw) {
			return &avatarObj{ok: false, attempted: false, errMsg: "URL refusee : hors allowlist ou adresse interne"}, nil
		}
		store.setAvatar(u.ID, raw)
		return &avatarObj{ok: true, attempted: true, contentType: "image/png", size: 1024}, nil

	case "requestPasswordReset":
		// l'identifiant existe (message et champ `exists` distincts). Aucune
		// authentification requise (flux public de réinitialisation).
		username := argString(args, "username")
		exists := store.userExists(username)
		msg := "Aucun compte associe a cet identifiant"
		if exists {
			msg = "Un email de reinitialisation a ete envoye"
		}
		return &resetResultObj{sent: exists, exists: exists, message: msg}, nil

	case "requestPasswordResetSafe":
		// NC: APP06-N008 — réponse uniforme quel que soit l'identifiant (pas d'énumération).
		_ = store.userExists(argString(args, "username"))
		return &resetResultObj{sent: true, exists: false,
			message: "Si un compte existe, un email de reinitialisation a ete envoye"}, nil

	case "transfer":
		// accepté) et le solde n'est pas vérifié. La propriété du compte source
		// EST vérifiée (ce n'est pas une BOLA).
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		fromID := resolveAccountKey(argString(args, "fromAccountId"))
		toID := resolveAccountKey(argString(args, "toAccountId"))
		from := store.accountByID(fromID)
		if from == nil || from.OwnerID != u.ID {
			return nil, &gqlError{Message: "acces refuse : compte source hors perimetre",
				Extensions: map[string]interface{}{"code": "FORBIDDEN"}}
		}
		amount := argFloat(args, "amount")
		ok, fb, tb, errMsg := store.transferVulnerable(fromID, toID, amount)
		return &transferResultObj{ok: ok, fromBalance: fb, toBalance: tb, message: errMsg}, nil

	case "transferSafe":
		// NC: APP06-N012 — montant strictement positif et solde suffisant exigés.
		u, e := ex.requireAuth()
		if e != nil {
			return nil, e
		}
		fromID := resolveAccountKey(argString(args, "fromAccountId"))
		toID := resolveAccountKey(argString(args, "toAccountId"))
		from := store.accountByID(fromID)
		if from == nil || from.OwnerID != u.ID {
			return nil, &gqlError{Message: "acces refuse : compte source hors perimetre",
				Extensions: map[string]interface{}{"code": "FORBIDDEN"}}
		}
		amount := argFloat(args, "amount")
		ok, fb, tb, errMsg := store.transferSafe(fromID, toID, amount)
		return &transferResultObj{ok: ok, fromBalance: fb, toBalance: tb, message: errMsg}, nil
	}

	return nil, unknownField("Mutation", name)
}

// resolveAccountKey accepte un ID global Relay (base64 "Account:xxxx") ou une
// clé brute.
func resolveAccountKey(id string) string {
	if _, key, ok := decodeGlobalID(id); ok {
		return key
	}
	return id
}

// argFloat lit un argument numérique (int ou float).
func argFloat(args map[string]interface{}, name string) float64 {
	switch n := args[name].(type) {
	case float64:
		return n
	case int64:
		return float64(n)
	case int:
		return float64(n)
	case string:
		f, _ := strconv.ParseFloat(n, 64)
		return f
	}
	return 0
}

// --- SSRF : récupération réelle (V015) vs allowlist (N006) -------------------

func fetchURL(raw string) (contentType string, size int, errMsg string) {
	client := &http.Client{Timeout: 2 * time.Second}
	resp, err := client.Get(raw) 
	if err != nil {
		return "", 0, err.Error()
	}
	defer resp.Body.Close()
	b, _ := io.ReadAll(io.LimitReader(resp.Body, 4096))
	return resp.Header.Get("Content-Type"), len(b), ""
}

func avatarAllowed(raw string) bool {
	u, err := url.Parse(raw)
	if err != nil || (u.Scheme != "http" && u.Scheme != "https") {
		return false
	}
	host := u.Hostname()
	// Allowlist stricte du CDN de laboratoire.
	if host != "cdn.mobipay.lab.exemple" {
		return false
	}
	// Rejet des adresses privées/loopback/link-local résolues.
	if ips, _ := net.LookupIP(host); len(ips) > 0 {
		for _, ip := range ips {
			if ip.IsLoopback() || ip.IsPrivate() || ip.IsLinkLocalUnicast() {
				return false
			}
		}
	}
	return true
}

// --- Objets domaine ----------------------------------------------------------

type userObj struct{ u *User }

func (o *userObj) TypeName() string { return "User" }
func (o *userObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "id":
		return encodeGlobalID("User", o.u.ID), nil
	case "username":
		return o.u.Username, nil
	case "displayName":
		return o.u.DisplayName, nil
	case "email":
		return o.u.Email, nil
	case "phone":
		return o.u.Phone, nil
	case "role":
		return o.u.Role, nil
	case "avatarUrl":
		return o.u.AvatarURL, nil
	case "accounts":
		out := []interface{}{}
		for _, a := range store.accountsOf(o.u.ID) {
			out = append(out, &accountObj{a})
		}
		return out, nil
	case "cards":
		// Le type Card exposé porte pan/cvv (voir cardObj) — base de V002.
		out := []interface{}{}
		for _, c := range store.cardsOf(o.u.ID) {
			out = append(out, &cardObj{c})
		}
		return out, nil
	}
	return nil, unknownField("User", name)
}

type accountObj struct{ a *Account }

func (o *accountObj) TypeName() string { return "Account" }
func (o *accountObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "id":
		return encodeGlobalID("Account", o.a.ID), nil
	case "iban":
		return o.a.IBAN, nil
	case "balance":
		return o.a.Balance, nil
	case "status":
		return o.a.Status, nil
	case "owner":
		if u := store.userByID(o.a.OwnerID); u != nil {
			return &userObj{u}, nil
		}
		return nil, nil
	}
	return nil, unknownField("Account", name)
}

type cardObj struct{ c *Card }

func (o *cardObj) TypeName() string { return "Card" }
func (o *cardObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "id":
		return encodeGlobalID("Card", o.c.ID), nil
	case "panMasked":
		return o.c.PANMasked, nil
	case "pan":
		return o.c.PAN, nil 
	case "cvv":
		return o.c.CVV, nil 
	case "expiry":
		return o.c.Expiry, nil
	}
	return nil, unknownField("Card", name)
}

// walletObj + walletCardObj : type SÛR (NC N002), sans pan/cvv.
type walletObj struct{ ownerID string }

func (o *walletObj) TypeName() string { return "Wallet" }
func (o *walletObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	if name == "cards" {
		out := []interface{}{}
		for _, c := range store.cardsOf(o.ownerID) {
			out = append(out, &walletCardObj{c})
		}
		return out, nil
	}
	return nil, unknownField("Wallet", name)
}

type walletCardObj struct{ c *Card }

func (o *walletCardObj) TypeName() string { return "WalletCard" }
func (o *walletCardObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "id":
		return encodeGlobalID("Card", o.c.ID), nil
	case "panMasked":
		return o.c.PANMasked, nil
	case "expiry":
		return o.c.Expiry, nil
	}
	// pan / cvv n'existent pas sur WalletCard -> erreur de validation (N002).
	return nil, unknownField("WalletCard", name)
}

type txObj struct{ t *Transaction }

func (o *txObj) TypeName() string { return "Transaction" }
func (o *txObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "id":
		return o.t.ID, nil
	case "amount":
		return o.t.Amount, nil
	case "label":
		return o.t.Label, nil
	case "category":
		return o.t.Category, nil
	}
	return nil, unknownField("Transaction", name)
}

type notifObj struct{ n *Notification }

func (o *notifObj) TypeName() string { return "Notification" }
func (o *notifObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "id":
		return o.n.ID, nil
	case "userId":
		return o.n.UserID, nil
	case "message":
		return o.n.Message, nil
	case "createdAt":
		return o.n.CreatedAt, nil
	}
	return nil, unknownField("Notification", name)
}

type sessionObj struct {
	userID    string
	deviceHdr string
	enrolled  string
}

func (o *sessionObj) TypeName() string { return "Session" }
func (o *sessionObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "userId":
		return o.userID, nil
	case "deviceId":
		return o.deviceHdr, nil
	case "enrolledDeviceId":
		return o.enrolled, nil
	case "deviceVerified":
		return o.deviceHdr == o.enrolled, nil
	}
	return nil, unknownField("Session", name)
}

type authPayloadObj struct {
	token string
	user  *User
}

func (o *authPayloadObj) TypeName() string { return "AuthPayload" }
func (o *authPayloadObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "token":
		return o.token, nil
	case "user":
		return &userObj{o.user}, nil
	}
	return nil, unknownField("AuthPayload", name)
}

type countObj struct{ n int }

func (o *countObj) TypeName() string { return "AdminResult" }
func (o *countObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "count":
		return int64(o.n), nil
	case "ok":
		return true, nil
	}
	return nil, unknownField("AdminResult", name)
}

type okObj struct{ ok bool }

func (o *okObj) TypeName() string { return "OtpResult" }
func (o *okObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	if name == "ok" {
		return o.ok, nil
	}
	return nil, unknownField("OtpResult", name)
}

type avatarObj struct {
	ok          bool
	attempted   bool
	contentType string
	size        int
	errMsg      string
}

func (o *avatarObj) TypeName() string { return "AvatarResult" }
func (o *avatarObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "ok":
		return o.ok, nil
	case "attempted":
		return o.attempted, nil
	case "contentType":
		return o.contentType, nil
	case "size":
		return int64(o.size), nil
	case "error":
		return o.errMsg, nil
	}
	return nil, unknownField("AvatarResult", name)
}

// --- Objets des volets d'enrichissement -------------------------------------

type resetResultObj struct {
	sent    bool
	exists  bool
	message string
}

func (o *resetResultObj) TypeName() string { return "ResetResult" }
func (o *resetResultObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "sent":
		return o.sent, nil
	case "exists":
		return o.exists, nil 
	case "message":
		return o.message, nil
	}
	return nil, unknownField("ResetResult", name)
}

type transferResultObj struct {
	ok          bool
	fromBalance float64
	toBalance   float64
	message     string
}

func (o *transferResultObj) TypeName() string { return "TransferResult" }
func (o *transferResultObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "ok":
		return o.ok, nil
	case "fromBalance":
		return o.fromBalance, nil
	case "toBalance":
		return o.toBalance, nil
	case "message":
		return o.message, nil
	}
	return nil, unknownField("TransferResult", name)
}

type diagResultObj struct {
	ok     bool
	output string
	cmd    string
	errMsg string
}

func (o *diagResultObj) TypeName() string { return "DiagnosticResult" }
func (o *diagResultObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "ok":
		return o.ok, nil
	case "output":
		return o.output, nil
	case "command":
		return o.cmd, nil
	case "error":
		return o.errMsg, nil
	}
	return nil, unknownField("DiagnosticResult", name)
}

type directoryEntryObj struct{ u *User }

func (o *directoryEntryObj) TypeName() string { return "DirectoryEntry" }
func (o *directoryEntryObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "username":
		return o.u.Username, nil
	case "displayName":
		return o.u.DisplayName, nil
	case "email":
		return o.u.Email, nil
	case "phone":
		return o.u.Phone, nil
	}
	return nil, unknownField("DirectoryEntry", name)
}

type exportResultObj struct {
	format  string
	content string
	count   int
}

func (o *exportResultObj) TypeName() string { return "ExportResult" }
func (o *exportResultObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "format":
		return o.format, nil
	case "content":
		return o.content, nil
	case "count":
		return int64(o.count), nil
	}
	return nil, unknownField("ExportResult", name)
}

type appInfoObj struct{}

func (o *appInfoObj) TypeName() string { return "AppInfo" }
func (o *appInfoObj) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "name":
		return "MobiPay", nil
	case "description":
		return "API mobile de paiement (environnement de laboratoire, donnees fictives).", nil
	case "supportEmail":
		return "support@mobipay.lab.exemple", nil
	case "helpUrl":
		return "https://mobipay.lab.exemple/aide", nil
	}
	return nil, unknownField("AppInfo", name)
}

var _ = strings.TrimSpace
