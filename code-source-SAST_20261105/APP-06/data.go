package main

// =============================================================================
// data.go — Dépôt mémoire (standalone) et données 100 % FICTIVES.
// MobiPay (APP-06) — Banking DAST Benchmark Lab. Usage défensif.
// SÉCURISÉ PAR DÉFAUT ; les seuls écarts sont les 16 vulnérabilités taguées.
// =============================================================================

import (
	"encoding/base64"
	"sort"
	"strings"
	"sync"
)

const (
	labPassword = "Password1!"
	labOTP      = "123456"
	pinLockMax  = 3 // seuil de verrouillage du PIN sûr (NC APP06-N007)
)

// --- Entités domaine (données fictives) --------------------------------------

type User struct {
	ID          string // clé interne, ex. "2001"
	Username    string
	Password    string
	Role        string // USER | ADMIN
	DisplayName string
	Email       string
	Phone       string // téléphone fictif
	DeviceID    string // appareil enrôlé (empreinte légitime)
	AvatarURL   string
}

type Account struct {
	ID      string // "1001"
	IBAN    string
	Balance float64
	Status  string // ACTIVE | FROZEN | CLOSED
	OwnerID string
}

type Card struct {
	ID        string // "5001"
	OwnerID   string
	PAN       string // DONNÉE FICTIVE — jamais une vraie carte
	PANMasked string
	CVV       string // DONNÉE FICTIVE
	Expiry    string
}

type Transaction struct {
	ID       string
	OwnerID  string
	Amount   float64
	Label    string
	Category string // FOOD | TRANSPORT | SALARY | OTHER
}

type Notification struct {
	ID        string
	UserID    string
	Message   string
	CreatedAt string
}

// --- Dépôt mémoire -----------------------------------------------------------

type Store struct {
	mu            sync.Mutex
	users         map[string]*User         // id -> user
	usersByName   map[string]*User         // username -> user
	accounts      map[string]*Account      // id -> account
	cards         map[string]*Card         // id -> card
	transactions  map[string]*Transaction  // id -> tx
	notifications map[string]*Notification // id -> notif
	pinFailures   map[string]int           // username -> échecs cumulés (PIN sûr)
}

var store = &Store{}

func init() { store.Reset() }

// Reset réinitialise entièrement l'état (POST /admin/reset).
func (s *Store) Reset() {
	s.mu.Lock()
	defer s.mu.Unlock()

	s.users = map[string]*User{}
	s.usersByName = map[string]*User{}
	s.accounts = map[string]*Account{}
	s.cards = map[string]*Card{}
	s.transactions = map[string]*Transaction{}
	s.notifications = map[string]*Notification{}
	s.pinFailures = map[string]int{}

	addUser := func(u *User) {
		s.users[u.ID] = u
		s.usersByName[u.Username] = u
	}
	// --- Comptes historiques (identifiants déterministes attendus par le validateur) ---
	addUser(&User{ID: "2001", Username: "mobile-alice", Password: labPassword, Role: "USER",
		DisplayName: "Alice Lab", Email: "alice@mobipay.lab.exemple", Phone: "+33600000001", DeviceID: "device-aaa"})
	addUser(&User{ID: "2002", Username: "mobile-bruno", Password: labPassword, Role: "USER",
		DisplayName: "Bruno Lab", Email: "bruno@mobipay.lab.exemple", Phone: "+33600000002", DeviceID: "device-bbb"})
	addUser(&User{ID: "2003", Username: "mobile-admin", Password: labPassword, Role: "ADMIN",
		DisplayName: "Admin Lab", Email: "admin@mobipay.lab.exemple", Phone: "+33600000003", DeviceID: "device-adm"})
	// --- Volume supplémentaire (données 100 % fictives, emails @mobipay.lab.exemple) ---
	addUser(&User{ID: "2004", Username: "mobile-chloe", Password: labPassword, Role: "USER",
		DisplayName: "Chloe Lab", Email: "chloe@mobipay.lab.exemple", Phone: "+33600000004", DeviceID: "device-ccc"})
	addUser(&User{ID: "2005", Username: "mobile-david", Password: labPassword, Role: "USER",
		DisplayName: "David Lab", Email: "david@mobipay.lab.exemple", Phone: "+33600000005", DeviceID: "device-ddd"})
	addUser(&User{ID: "2006", Username: "mobile-emma", Password: labPassword, Role: "USER",
		DisplayName: "Emma Lab", Email: "emma@mobipay.lab.exemple", Phone: "+33600000006", DeviceID: "device-eee"})
	addUser(&User{ID: "2007", Username: "mobile-farid", Password: labPassword, Role: "USER",
		DisplayName: "Farid Lab", Email: "farid@mobipay.lab.exemple", Phone: "+33600000007", DeviceID: "device-fff"})
	addUser(&User{ID: "2008", Username: "mobile-gina", Password: labPassword, Role: "USER",
		DisplayName: "Gina Lab", Email: "gina@mobipay.lab.exemple", Phone: "+33600000008", DeviceID: "device-ggg"})
	addUser(&User{ID: "2009", Username: "mobile-hugo", Password: labPassword, Role: "USER",
		DisplayName: "Hugo Lab", Email: "hugo@mobipay.lab.exemple", Phone: "+33600000009", DeviceID: "device-hhh"})
	addUser(&User{ID: "2010", Username: "mobile-support", Password: labPassword, Role: "ADMIN",
		DisplayName: "Support Lab", Email: "support@mobipay.lab.exemple", Phone: "+33600000010", DeviceID: "device-sup"})

	// Comptes : Account:1001 -> alice, Account:1002 -> bruno, Account:1003 -> admin (historiques).
	s.accounts["1001"] = &Account{ID: "1001", IBAN: "FR7630000000000000000001001", Balance: 1240.55, Status: "ACTIVE", OwnerID: "2001"}
	s.accounts["1002"] = &Account{ID: "1002", IBAN: "FR7630000000000000000001002", Balance: 8820.00, Status: "ACTIVE", OwnerID: "2002"}
	s.accounts["1003"] = &Account{ID: "1003", IBAN: "FR7630000000000000000001003", Balance: 50.00, Status: "ACTIVE", OwnerID: "2003"}
	// Comptes supplémentaires (statuts variés).
	s.accounts["1004"] = &Account{ID: "1004", IBAN: "FR7630000000000000000001004", Balance: 320.10, Status: "ACTIVE", OwnerID: "2004"}
	s.accounts["1005"] = &Account{ID: "1005", IBAN: "FR7630000000000000000001005", Balance: 15420.75, Status: "ACTIVE", OwnerID: "2005"}
	s.accounts["1006"] = &Account{ID: "1006", IBAN: "FR7630000000000000000001006", Balance: 0.00, Status: "CLOSED", OwnerID: "2006"}
	s.accounts["1007"] = &Account{ID: "1007", IBAN: "FR7630000000000000000001007", Balance: 742.30, Status: "FROZEN", OwnerID: "2007"}
	s.accounts["1008"] = &Account{ID: "1008", IBAN: "FR7630000000000000000001008", Balance: 2890.00, Status: "ACTIVE", OwnerID: "2008"}
	s.accounts["1009"] = &Account{ID: "1009", IBAN: "FR7630000000000000000001009", Balance: 61.49, Status: "ACTIVE", OwnerID: "2009"}

	// Cartes (PAN/CVV 100 % FICTIFS).
	s.cards["5001"] = &Card{ID: "5001", OwnerID: "2001", PAN: "4000123412341234", PANMasked: "**** **** **** 1234", CVV: "123", Expiry: "12/29"}
	s.cards["5002"] = &Card{ID: "5002", OwnerID: "2002", PAN: "4000999988887777", PANMasked: "**** **** **** 7777", CVV: "456", Expiry: "06/28"}
	s.cards["5003"] = &Card{ID: "5003", OwnerID: "2004", PAN: "4000111122223333", PANMasked: "**** **** **** 3333", CVV: "789", Expiry: "03/30"}
	s.cards["5004"] = &Card{ID: "5004", OwnerID: "2005", PAN: "4000444455556666", PANMasked: "**** **** **** 6666", CVV: "321", Expiry: "09/27"}
	s.cards["5005"] = &Card{ID: "5005", OwnerID: "2008", PAN: "4000777788889999", PANMasked: "**** **** **** 9999", CVV: "654", Expiry: "11/28"}

	// Transactions d'alice et de bruno (la "table" ciblée par la SQLi V010) — historiques.
	s.transactions["7001"] = &Transaction{ID: "7001", OwnerID: "2001", Amount: 12.90, Label: "Boulangerie", Category: "FOOD"}
	s.transactions["7002"] = &Transaction{ID: "7002", OwnerID: "2001", Amount: 48.00, Label: "Supermarche", Category: "FOOD"}
	s.transactions["7003"] = &Transaction{ID: "7003", OwnerID: "2001", Amount: 2.10, Label: "Bus", Category: "TRANSPORT"}
	s.transactions["7004"] = &Transaction{ID: "7004", OwnerID: "2002", Amount: 2500.00, Label: "Salaire", Category: "SALARY"}
	s.transactions["7005"] = &Transaction{ID: "7005", OwnerID: "2002", Amount: 15.40, Label: "Restaurant", Category: "FOOD"}
	// Transactions supplémentaires (autres comptes, variété de catégories).
	s.transactions["7006"] = &Transaction{ID: "7006", OwnerID: "2004", Amount: 9.99, Label: "Cafe", Category: "FOOD"}
	s.transactions["7007"] = &Transaction{ID: "7007", OwnerID: "2004", Amount: 1800.00, Label: "Salaire", Category: "SALARY"}
	s.transactions["7008"] = &Transaction{ID: "7008", OwnerID: "2005", Amount: 54.20, Label: "Essence", Category: "TRANSPORT"}
	s.transactions["7009"] = &Transaction{ID: "7009", OwnerID: "2005", Amount: 120.00, Label: "Pharmacie", Category: "OTHER"}
	s.transactions["7010"] = &Transaction{ID: "7010", OwnerID: "2008", Amount: 33.00, Label: "Cinema", Category: "OTHER"}
	s.transactions["7011"] = &Transaction{ID: "7011", OwnerID: "2008", Amount: 2100.00, Label: "Salaire", Category: "SALARY"}
	s.transactions["7012"] = &Transaction{ID: "7012", OwnerID: "2009", Amount: 7.50, Label: "Metro", Category: "TRANSPORT"}

	// Notifications privées.
	s.notifications["9001"] = &Notification{ID: "9001", UserID: "2001", Message: "Bienvenue sur MobiPay", CreatedAt: "2026-09-01T09:00:00Z"}
	s.notifications["9002"] = &Notification{ID: "9002", UserID: "2002", Message: "Virement recu de 2500,00 EUR", CreatedAt: "2026-09-02T10:15:00Z"}
	s.notifications["9003"] = &Notification{ID: "9003", UserID: "2002", Message: "Nouveau beneficiaire enregistre", CreatedAt: "2026-09-02T11:00:00Z"}
	// Notifications supplémentaires.
	s.notifications["9004"] = &Notification{ID: "9004", UserID: "2001", Message: "Paiement carte de 48,00 EUR", CreatedAt: "2026-09-03T08:30:00Z"}
	s.notifications["9005"] = &Notification{ID: "9005", UserID: "2004", Message: "Bienvenue sur MobiPay", CreatedAt: "2026-09-04T12:00:00Z"}
	s.notifications["9006"] = &Notification{ID: "9006", UserID: "2005", Message: "Plafond de carte mis a jour", CreatedAt: "2026-09-05T14:45:00Z"}
	s.notifications["9007"] = &Notification{ID: "9007", UserID: "2008", Message: "Virement recu de 2100,00 EUR", CreatedAt: "2026-09-06T09:10:00Z"}
}

// --- Accès (toutes les opérations prennent le verrou) ------------------------

func (s *Store) userByName(name string) *User {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.usersByName[name]
}

func (s *Store) userByID(id string) *User {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.users[id]
}

func (s *Store) accountByID(id string) *Account {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.accounts[id]
}

func (s *Store) cardByID(id string) *Card {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.cards[id]
}

func (s *Store) accountsOf(ownerID string) []*Account {
	s.mu.Lock()
	defer s.mu.Unlock()
	var out []*Account
	for _, a := range s.accounts {
		if a.OwnerID == ownerID {
			out = append(out, a)
		}
	}
	sort.Slice(out, func(i, j int) bool { return out[i].ID < out[j].ID })
	return out
}

func (s *Store) cardsOf(ownerID string) []*Card {
	s.mu.Lock()
	defer s.mu.Unlock()
	var out []*Card
	for _, c := range s.cards {
		if c.OwnerID == ownerID {
			out = append(out, c)
		}
	}
	sort.Slice(out, func(i, j int) bool { return out[i].ID < out[j].ID })
	return out
}

func (s *Store) notificationsOf(userID string) []*Notification {
	s.mu.Lock()
	defer s.mu.Unlock()
	var out []*Notification
	for _, n := range s.notifications {
		if n.UserID == userID {
			out = append(out, n)
		}
	}
	sort.Slice(out, func(i, j int) bool { return out[i].ID < out[j].ID })
	return out
}

// setRole / setProfile / setAvatar sont utilisés par les mutations.
func (s *Store) setProfile(id, displayName, email, role string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	u := s.users[id]
	if u == nil {
		return
	}
	if displayName != "" {
		u.DisplayName = displayName
	}
	if email != "" {
		u.Email = email
	}
	if role != "" { 
		u.Role = role
	}
}

func (s *Store) setAvatar(id, url string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	if u := s.users[id]; u != nil {
		u.AvatarURL = url
	}
}

// blockAllCards simule une opération d'administration (compte les cartes).
func (s *Store) blockAllCards() int {
	s.mu.Lock()
	defer s.mu.Unlock()
	return len(s.cards)
}

// PIN sûr (NC APP06-N007) : compteur d'échecs par utilisateur.
func (s *Store) pinFailure(username string) int {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.pinFailures[username]++
	return s.pinFailures[username]
}

func (s *Store) pinLocked(username string) bool {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.pinFailures[username] >= pinLockMax
}

// userExists indique si un identifiant correspond a un compte (enumeration V017).
func (s *Store) userExists(username string) bool {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.usersByName[username] != nil
}

// directory renvoie l'annuaire des utilisateurs (username, displayName, email,
// phone). Donnees fictives ; sert aux resolveurs userDirectory (V022) et
// userDirectorySecure (N013).
func (s *Store) directory() []*User {
	s.mu.Lock()
	defer s.mu.Unlock()
	out := make([]*User, 0, len(s.users))
	for _, u := range s.users {
		out = append(out, u)
	}
	sort.Slice(out, func(i, j int) bool { return out[i].ID < out[j].ID })
	return out
}

// searchTransactions : recherche SURE par sous-chaine de libelle, cloisonnee au
// proprietaire (use case benin, aucun ecart).
func (s *Store) searchTransactions(ownerID, term string) []*Transaction {
	s.mu.Lock()
	defer s.mu.Unlock()
	term = strings.ToLower(strings.TrimSpace(term))
	out := []*Transaction{}
	for _, t := range s.transactions {
		if t.OwnerID != ownerID {
			continue
		}
		if term == "" || strings.Contains(strings.ToLower(t.Label), term) {
			out = append(out, t)
		}
	}
	sort.Slice(out, func(i, j int) bool { return out[i].ID < out[j].ID })
	return out
}

// transferVulnerable : GT: APP06-V021 — aucun controle de signe ni de solde.
// Un montant negatif inverse le flux (le compte source est credite, la cible
// debitee), et le decouvert n'est pas verifie. La propriete du compte source
// est verifiee en amont par le resolveur (ce n'est donc pas une BOLA).
func (s *Store) transferVulnerable(fromID, toID string, amount float64) (bool, float64, float64, string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	from := s.accounts[fromID]
	to := s.accounts[toID]
	if from == nil || to == nil {
		return false, 0, 0, "compte introuvable"
	}
	from.Balance -= amount 
	to.Balance += amount
	return true, from.Balance, to.Balance, ""
}

// transferSafe : NC: APP06-N012 — montant strictement positif et solde suffisant
// exiges avant toute ecriture.
func (s *Store) transferSafe(fromID, toID string, amount float64) (bool, float64, float64, string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	if amount <= 0 {
		return false, 0, 0, "montant invalide : doit etre strictement positif"
	}
	from := s.accounts[fromID]
	to := s.accounts[toID]
	if from == nil || to == nil {
		return false, 0, 0, "compte introuvable"
	}
	if from.Balance < amount {
		return false, 0, 0, "solde insuffisant"
	}
	from.Balance -= amount
	to.Balance += amount
	return true, from.Balance, to.Balance, ""
}

// --- SQL-ish : évaluateur de filtre VULNÉRABLE (V010) ------------------------
//
// Reproduit fidèlement une concaténation de chaîne dans un WHERE :
//   SELECT ... FROM transactions WHERE owner_id='<sub>' AND <filter>
// AND lie plus fort que OR, donc `OR '1'='1'` rend la clause toujours vraie et
// renvoie les lignes de TOUS les utilisateurs (franchissement d'autorisation).
// Prend aussi en charge un `UNION SELECT ... FROM users` exfiltrant une autre table.

func (s *Store) queryTransactionsVulnerable(ownerID, filter string) []*Transaction {
	s.mu.Lock()
	all := make([]*Transaction, 0, len(s.transactions))
	for _, t := range s.transactions {
		all = append(all, t)
	}
	users := make([]*User, 0, len(s.users))
	for _, u := range s.users {
		users = append(users, u)
	}
	s.mu.Unlock()
	sort.Slice(all, func(i, j int) bool { return all[i].ID < all[j].ID })
	sort.Slice(users, func(i, j int) bool { return users[i].ID < users[j].ID })

	lower := strings.ToLower(filter)

	// Branche UNION : exfiltration de la table users (password_hash fictif).
	if strings.Contains(lower, "union") && strings.Contains(lower, "select") && strings.Contains(lower, "users") {
		out := []*Transaction{}
		for _, t := range all { // owner_id='<sub>' AND category='x' -> aucune ligne, puis UNION
			if t.OwnerID == ownerID && false {
				out = append(out, t)
			}
		}
		for _, u := range users {
			out = append(out, &Transaction{
				ID:       "u" + u.ID,
				OwnerID:  u.ID,
				Amount:   0,
				Label:    u.Username + ":" + fakeHash(u.Password),
				Category: "users",
			})
		}
		return out
	}

	// Sinon : évaluation booléenne de `owner_id='<sub>' AND (<filter>)`.
	expr := "owner_id='" + ownerID + "' AND " + filter
	out := []*Transaction{}
	for _, t := range all {
		if evalWhere(expr, t) {
			out = append(out, t)
		}
	}
	return out
}

// Filtre SÛR (NC APP06-N005) : arguments typés, requête paramétrée simulée.
func (s *Store) queryTransactionsSafe(ownerID, category string) []*Transaction {
	s.mu.Lock()
	defer s.mu.Unlock()
	out := []*Transaction{}
	for _, t := range s.transactions {
		if t.OwnerID != ownerID { // cloisonnement strict par propriétaire
			continue
		}
		if category == "" || t.Category == category {
			out = append(out, t)
		}
	}
	sort.Slice(out, func(i, j int) bool { return out[i].ID < out[j].ID })
	return out
}

// evalWhere : OR de termes, chaque terme = AND d'atomes `A=B`. AND > OR.
func evalWhere(expr string, t *Transaction) bool {
	for _, term := range splitKeyword(expr, "or") {
		all := true
		for _, atom := range splitKeyword(term, "and") {
			if !evalAtom(strings.TrimSpace(atom), t) {
				all = false
				break
			}
		}
		if all {
			return true
		}
	}
	return false
}

func evalAtom(atom string, t *Transaction) bool {
	atom = strings.TrimSuffix(strings.TrimSpace(atom), ";")
	eq := indexTopEq(atom)
	if eq < 0 {
		return false
	}
	l := operandValue(strings.TrimSpace(atom[:eq]), t)
	r := operandValue(strings.TrimSpace(atom[eq+1:]), t)
	return l == r
}

func operandValue(op string, t *Transaction) string {
	op = strings.TrimSpace(op)
	// Si l'opérande porte une quote simple (même non fermée, comme dans une
	// charge d'injection où la quote de fermeture vient du gabarit SQL), on le
	// traite comme un littéral.
	if strings.Contains(op, "'") {
		return strings.Trim(op, "'")
	}
	switch strings.ToLower(op) {
	case "owner_id":
		return t.OwnerID
	case "category":
		return t.Category
	case "label":
		return t.Label
	}
	return op
}

// splitKeyword découpe sur un mot-clé SQL (and/or) en dehors des quotes simples.
func splitKeyword(s, kw string) []string {
	var parts []string
	lower := strings.ToLower(s)
	inQ := false
	last := 0
	i := 0
	for i < len(s) {
		if s[i] == '\'' {
			inQ = !inQ
			i++
			continue
		}
		if !inQ && matchWord(lower, i, kw) {
			parts = append(parts, s[last:i])
			i += len(kw)
			last = i
			continue
		}
		i++
	}
	parts = append(parts, s[last:])
	return parts
}

func matchWord(lower string, i int, kw string) bool {
	if i+len(kw) > len(lower) || lower[i:i+len(kw)] != kw {
		return false
	}
	before := i == 0 || isSep(lower[i-1])
	after := i+len(kw) == len(lower) || isSep(lower[i+len(kw)])
	return before && after
}

func isSep(b byte) bool { return b == ' ' || b == '\t' || b == '(' || b == ')' }

func indexTopEq(s string) int {
	inQ := false
	for i := 0; i < len(s); i++ {
		if s[i] == '\'' {
			inQ = !inQ
		} else if s[i] == '=' && !inQ {
			return i
		}
	}
	return -1
}

func fakeHash(pw string) string {
	// Hash FICTIF (non cryptographique) pour la démonstration d'exfiltration.
	var h uint32 = 2166136261
	for i := 0; i < len(pw); i++ {
		h ^= uint32(pw[i])
		h *= 16777619
	}
	const hexd = "0123456789abcdef"
	b := make([]byte, 8)
	for i := 0; i < 8; i++ {
		b[7-i] = hexd[h&0xF]
		h >>= 4
	}
	return "x7f" + string(b)
}

// --- Identifiants globaux (Relay) --------------------------------------------

func encodeGlobalID(typ, key string) string {
	return base64.StdEncoding.EncodeToString([]byte(typ + ":" + key))
}

// decodeGlobalID renvoie (type, clé, ok). Une entrée invalide produit ok=false.
func decodeGlobalID(id string) (string, string, bool) {
	raw, err := base64.StdEncoding.DecodeString(id)
	if err != nil {
		return "", "", false
	}
	parts := strings.SplitN(string(raw), ":", 2)
	if len(parts) != 2 || parts[0] == "" || parts[1] == "" {
		return "", "", false
	}
	return parts[0], parts[1], true
}
