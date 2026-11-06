package main

// =============================================================================
// introspect.go — Introspection GraphQL (GT: APP06-V003).
// Modèle de schéma statique suffisant pour qu'une requête d'introspection
// standard (y compris celle d'un scanner) résolve types, champs et mutations.
// Les objets d'introspection sont « lenient » : un champ non modélisé -> null,
// pour ne pas faire échouer une requête d'introspection complète.
// =============================================================================

type fieldDesc struct {
	Name     string
	TypeName string
	TypeKind string
}

type typeDesc struct {
	Name     string
	Kind     string // OBJECT | SCALAR | ENUM | INPUT_OBJECT | INTERFACE
	Fields   []fieldDesc
	EnumVals []string
}

// schemaTypes : description exposée par l'introspection.
var schemaTypes = []typeDesc{
	{Name: "Query", Kind: "OBJECT", Fields: []fieldDesc{
		{"me", "User", "OBJECT"},
		{"node", "Node", "INTERFACE"},
		{"account", "Account", "OBJECT"},
		{"notifications", "Notification", "OBJECT"},
		{"secureNotifications", "Notification", "OBJECT"},
		{"transactions", "Transaction", "OBJECT"},
		{"transactionsByCategory", "Transaction", "OBJECT"},
		{"wallet", "Wallet", "OBJECT"},
		{"session", "Session", "OBJECT"},
		{"trustedProfile", "User", "OBJECT"},
		{"diagnosticPing", "DiagnosticResult", "OBJECT"},
		{"diagnosticPingSafe", "DiagnosticResult", "OBJECT"},
		{"userDirectory", "DirectoryEntry", "OBJECT"},
		{"userDirectorySecure", "DirectoryEntry", "OBJECT"},
		{"search", "Transaction", "OBJECT"},
		{"exportTransactions", "ExportResult", "OBJECT"},
		{"activityHistory", "Notification", "OBJECT"},
		{"appInfo", "AppInfo", "OBJECT"},
	}},
	{Name: "Mutation", Kind: "OBJECT", Fields: []fieldDesc{
		{"login", "AuthPayload", "OBJECT"},
		{"updateProfile", "User", "OBJECT"},
		{"updateContact", "User", "OBJECT"},
		{"blockAllCards", "AdminResult", "OBJECT"},
		{"freezeAccount", "OkResult", "OBJECT"},
		{"verifyOtp", "OtpResult", "OBJECT"},
		{"verifyPin", "OtpResult", "OBJECT"},
		{"setAvatar", "AvatarResult", "OBJECT"},
		{"setAvatarSafe", "AvatarResult", "OBJECT"},
		{"requestPasswordReset", "ResetResult", "OBJECT"},
		{"requestPasswordResetSafe", "ResetResult", "OBJECT"},
		{"transfer", "TransferResult", "OBJECT"},
		{"transferSafe", "TransferResult", "OBJECT"},
	}},
	{Name: "User", Kind: "OBJECT", Fields: []fieldDesc{
		{"id", "ID", "SCALAR"}, {"username", "String", "SCALAR"},
		{"displayName", "String", "SCALAR"}, {"email", "String", "SCALAR"},
		{"phone", "String", "SCALAR"},
		{"role", "String", "SCALAR"}, {"avatarUrl", "String", "SCALAR"},
		{"accounts", "Account", "OBJECT"}, {"cards", "Card", "OBJECT"},
	}},
	{Name: "Account", Kind: "OBJECT", Fields: []fieldDesc{
		{"id", "ID", "SCALAR"}, {"iban", "String", "SCALAR"},
		{"balance", "Float", "SCALAR"}, {"status", "String", "SCALAR"},
		{"owner", "User", "OBJECT"},
	}},
	{Name: "Card", Kind: "OBJECT", Fields: []fieldDesc{
		{"id", "ID", "SCALAR"}, {"panMasked", "String", "SCALAR"},
		{"pan", "String", "SCALAR"}, {"cvv", "String", "SCALAR"},
		{"expiry", "String", "SCALAR"},
	}},
	{Name: "WalletCard", Kind: "OBJECT", Fields: []fieldDesc{
		{"id", "ID", "SCALAR"}, {"panMasked", "String", "SCALAR"},
		{"expiry", "String", "SCALAR"},
	}},
	{Name: "Wallet", Kind: "OBJECT", Fields: []fieldDesc{
		{"cards", "WalletCard", "OBJECT"},
	}},
	{Name: "Transaction", Kind: "OBJECT", Fields: []fieldDesc{
		{"id", "ID", "SCALAR"}, {"amount", "Float", "SCALAR"},
		{"label", "String", "SCALAR"}, {"category", "String", "SCALAR"},
	}},
	{Name: "Notification", Kind: "OBJECT", Fields: []fieldDesc{
		{"id", "ID", "SCALAR"}, {"userId", "ID", "SCALAR"},
		{"message", "String", "SCALAR"}, {"createdAt", "String", "SCALAR"},
	}},
	{Name: "Session", Kind: "OBJECT", Fields: []fieldDesc{
		{"userId", "ID", "SCALAR"}, {"deviceId", "String", "SCALAR"},
		{"enrolledDeviceId", "String", "SCALAR"}, {"deviceVerified", "Boolean", "SCALAR"},
	}},
	{Name: "AuthPayload", Kind: "OBJECT", Fields: []fieldDesc{
		{"token", "String", "SCALAR"}, {"user", "User", "OBJECT"},
	}},
	{Name: "AdminResult", Kind: "OBJECT", Fields: []fieldDesc{
		{"count", "Int", "SCALAR"}, {"ok", "Boolean", "SCALAR"},
	}},
	{Name: "OtpResult", Kind: "OBJECT", Fields: []fieldDesc{{"ok", "Boolean", "SCALAR"}}},
	{Name: "OkResult", Kind: "OBJECT", Fields: []fieldDesc{{"ok", "Boolean", "SCALAR"}}},
	{Name: "AvatarResult", Kind: "OBJECT", Fields: []fieldDesc{
		{"ok", "Boolean", "SCALAR"}, {"attempted", "Boolean", "SCALAR"},
		{"contentType", "String", "SCALAR"}, {"size", "Int", "SCALAR"},
		{"error", "String", "SCALAR"},
	}},
	{Name: "ResetResult", Kind: "OBJECT", Fields: []fieldDesc{
		{"sent", "Boolean", "SCALAR"}, {"exists", "Boolean", "SCALAR"},
		{"message", "String", "SCALAR"},
	}},
	{Name: "TransferResult", Kind: "OBJECT", Fields: []fieldDesc{
		{"ok", "Boolean", "SCALAR"}, {"fromBalance", "Float", "SCALAR"},
		{"toBalance", "Float", "SCALAR"}, {"message", "String", "SCALAR"},
	}},
	{Name: "DiagnosticResult", Kind: "OBJECT", Fields: []fieldDesc{
		{"ok", "Boolean", "SCALAR"}, {"output", "String", "SCALAR"},
		{"command", "String", "SCALAR"}, {"error", "String", "SCALAR"},
	}},
	{Name: "DirectoryEntry", Kind: "OBJECT", Fields: []fieldDesc{
		{"username", "String", "SCALAR"}, {"displayName", "String", "SCALAR"},
		{"email", "String", "SCALAR"}, {"phone", "String", "SCALAR"},
	}},
	{Name: "ExportResult", Kind: "OBJECT", Fields: []fieldDesc{
		{"format", "String", "SCALAR"}, {"content", "String", "SCALAR"},
		{"count", "Int", "SCALAR"},
	}},
	{Name: "AppInfo", Kind: "OBJECT", Fields: []fieldDesc{
		{"name", "String", "SCALAR"}, {"description", "String", "SCALAR"},
		{"supportEmail", "String", "SCALAR"}, {"helpUrl", "String", "SCALAR"},
	}},
	{Name: "UpdateProfileInput", Kind: "INPUT_OBJECT", Fields: []fieldDesc{
		{"displayName", "String", "SCALAR"}, {"email", "String", "SCALAR"},
		{"role", "String", "SCALAR"}, // exposé dans l'input -> base de V006
	}},
	{Name: "UpdateContactInput", Kind: "INPUT_OBJECT", Fields: []fieldDesc{
		{"displayName", "String", "SCALAR"}, {"email", "String", "SCALAR"},
	}},
	{Name: "Category", Kind: "ENUM", EnumVals: []string{"FOOD", "TRANSPORT", "SALARY", "OTHER"}},
	{Name: "String", Kind: "SCALAR"},
	{Name: "ID", Kind: "SCALAR"},
	{Name: "Int", Kind: "SCALAR"},
	{Name: "Float", Kind: "SCALAR"},
	{Name: "Boolean", Kind: "SCALAR"},
}

func findType(name string) *typeDesc {
	for i := range schemaTypes {
		if schemaTypes[i].Name == name {
			return &schemaTypes[i]
		}
	}
	return nil
}

// --- Objets d'introspection (lenient) ---------------------------------------

type introSchema struct{}

func (s *introSchema) lenient()         {}
func (s *introSchema) TypeName() string { return "__Schema" }
func (s *introSchema) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "types":
		out := []interface{}{}
		for i := range schemaTypes {
			out = append(out, &introType{desc: &schemaTypes[i]})
		}
		return out, nil
	case "queryType":
		return &introType{desc: findType("Query")}, nil
	case "mutationType":
		return &introType{desc: findType("Mutation")}, nil
	case "subscriptionType":
		return nil, nil
	case "directives":
		return []interface{}{}, nil
	}
	return nil, nil
}

type introType struct {
	desc *typeDesc // type nommé complet
	ref  *fieldDesc
}

func (t *introType) lenient()         {}
func (t *introType) TypeName() string { return "__Type" }
func (t *introType) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	// Référence de type (ofType d'un champ) : seuls name/kind sont pertinents.
	if t.ref != nil {
		switch name {
		case "name":
			return t.ref.TypeName, nil
		case "kind":
			return t.ref.TypeKind, nil
		case "ofType":
			return nil, nil
		}
		return nil, nil
	}
	d := t.desc
	if d == nil {
		return nil, nil
	}
	switch name {
	case "name":
		return d.Name, nil
	case "kind":
		return d.Kind, nil
	case "description":
		return nil, nil
	case "fields":
		if d.Kind != "OBJECT" && d.Kind != "INTERFACE" {
			return nil, nil
		}
		out := []interface{}{}
		for i := range d.Fields {
			out = append(out, &introField{f: &d.Fields[i]})
		}
		return out, nil
	case "inputFields":
		if d.Kind != "INPUT_OBJECT" {
			return nil, nil
		}
		out := []interface{}{}
		for i := range d.Fields {
			out = append(out, &introInputValue{f: &d.Fields[i]})
		}
		return out, nil
	case "enumValues":
		if d.Kind != "ENUM" {
			return nil, nil
		}
		out := []interface{}{}
		for _, ev := range d.EnumVals {
			out = append(out, &introEnumValue{name: ev})
		}
		return out, nil
	case "interfaces":
		if d.Kind == "OBJECT" {
			return []interface{}{}, nil
		}
		return nil, nil
	case "possibleTypes":
		return nil, nil
	case "ofType":
		return nil, nil
	}
	return nil, nil
}

type introField struct{ f *fieldDesc }

func (o *introField) lenient()         {}
func (o *introField) TypeName() string { return "__Field" }
func (o *introField) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "name":
		return o.f.Name, nil
	case "description":
		return nil, nil
	case "args":
		return []interface{}{}, nil
	case "type":
		return &introType{ref: o.f}, nil
	case "isDeprecated":
		return false, nil
	case "deprecationReason":
		return nil, nil
	}
	return nil, nil
}

type introInputValue struct{ f *fieldDesc }

func (o *introInputValue) lenient()         {}
func (o *introInputValue) TypeName() string { return "__InputValue" }
func (o *introInputValue) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "name":
		return o.f.Name, nil
	case "type":
		return &introType{ref: o.f}, nil
	case "defaultValue":
		return nil, nil
	case "description":
		return nil, nil
	}
	return nil, nil
}

type introEnumValue struct{ name string }

func (o *introEnumValue) lenient()         {}
func (o *introEnumValue) TypeName() string { return "__EnumValue" }
func (o *introEnumValue) Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError) {
	switch name {
	case "name":
		return o.name, nil
	case "isDeprecated":
		return false, nil
	case "deprecationReason", "description":
		return nil, nil
	}
	return nil, nil
}
