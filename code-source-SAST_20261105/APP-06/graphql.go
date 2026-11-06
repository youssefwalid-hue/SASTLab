package main

// =============================================================================
// graphql.go — Lexer + parser GraphQL (sous-ensemble suffisant pour le lab).
// Prend en charge : query/mutation, variables, alias, arguments (scalaires,
// enums, listes, objets/inputs), fragments nommés et inline (... on Type),
// introspection. Bibliothèque standard uniquement (pas de gqlgen disponible).
// =============================================================================

import (
	"fmt"
	"strconv"
	"strings"
)

// --- AST ---------------------------------------------------------------------

type Document struct {
	Operations []*Operation
	Fragments  map[string]*Fragment
}

type Operation struct {
	Type      string // "query" | "mutation"
	Name      string
	VarDefs   map[string]*VarDef
	Selection []*Selection
}

type VarDef struct {
	Name    string
	Type    string
	Default *Value
}

type Fragment struct {
	Name      string
	TypeCond  string
	Selection []*Selection
}

type Selection struct {
	// Champ
	Alias     string
	Name      string
	Arguments []*Argument
	Selection []*Selection
	// Fragment
	IsSpread     bool
	SpreadName   string
	IsInline     bool
	InlineOnType string
}

func (s *Selection) outputKey() string {
	if s.Alias != "" {
		return s.Alias
	}
	return s.Name
}

type Argument struct {
	Name  string
	Value *Value
}

type Value struct {
	Kind string // var|int|float|string|bool|null|enum|list|object
	Raw  string // texte brut pour scalaires/enum/var
	List []*Value
	Obj  map[string]*Value
}

// --- Lexer -------------------------------------------------------------------

type token struct {
	kind string // name|int|float|string|punct|eof
	val  string
}

type lexer struct {
	src string
	pos int
	tok token
}

func newLexer(src string) *lexer {
	l := &lexer{src: src}
	l.next()
	return l
}

func (l *lexer) errorf(format string, a ...interface{}) error {
	return fmt.Errorf("erreur de syntaxe GraphQL : "+format, a...)
}

func (l *lexer) skip() {
	for l.pos < len(l.src) {
		c := l.src[l.pos]
		if c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == ',' {
			l.pos++
			continue
		}
		if c == '#' {
			for l.pos < len(l.src) && l.src[l.pos] != '\n' {
				l.pos++
			}
			continue
		}
		break
	}
}

func (l *lexer) next() {
	l.skip()
	if l.pos >= len(l.src) {
		l.tok = token{kind: "eof"}
		return
	}
	c := l.src[l.pos]

	// Chaîne (y compris les chaînes de bloc """...""").
	if c == '"' {
		if strings.HasPrefix(l.src[l.pos:], `"""`) {
			end := strings.Index(l.src[l.pos+3:], `"""`)
			if end < 0 {
				l.tok = token{kind: "eof"}
				return
			}
			val := l.src[l.pos+3 : l.pos+3+end]
			l.pos = l.pos + 3 + end + 3
			l.tok = token{kind: "string", val: val}
			return
		}
		l.pos++
		var sb strings.Builder
		for l.pos < len(l.src) && l.src[l.pos] != '"' {
			if l.src[l.pos] == '\\' && l.pos+1 < len(l.src) {
				n := l.src[l.pos+1]
				switch n {
				case 'n':
					sb.WriteByte('\n')
				case 't':
					sb.WriteByte('\t')
				case 'r':
					sb.WriteByte('\r')
				case '"':
					sb.WriteByte('"')
				case '\\':
					sb.WriteByte('\\')
				case '/':
					sb.WriteByte('/')
				default:
					sb.WriteByte(n)
				}
				l.pos += 2
				continue
			}
			sb.WriteByte(l.src[l.pos])
			l.pos++
		}
		l.pos++ // guillemet fermant
		l.tok = token{kind: "string", val: sb.String()}
		return
	}

	// Nombre.
	if c == '-' || (c >= '0' && c <= '9') {
		start := l.pos
		l.pos++
		isFloat := false
		for l.pos < len(l.src) {
			d := l.src[l.pos]
			if d >= '0' && d <= '9' {
				l.pos++
			} else if d == '.' || d == 'e' || d == 'E' || d == '+' || d == '-' {
				isFloat = true
				l.pos++
			} else {
				break
			}
		}
		kind := "int"
		if isFloat {
			kind = "float"
		}
		l.tok = token{kind: kind, val: l.src[start:l.pos]}
		return
	}

	// Nom.
	if isNameStart(c) {
		start := l.pos
		l.pos++
		for l.pos < len(l.src) && isNameCont(l.src[l.pos]) {
			l.pos++
		}
		l.tok = token{kind: "name", val: l.src[start:l.pos]}
		return
	}

	// Ellipsis.
	if strings.HasPrefix(l.src[l.pos:], "...") {
		l.pos += 3
		l.tok = token{kind: "punct", val: "..."}
		return
	}

	// Ponctuateur simple.
	l.pos++
	l.tok = token{kind: "punct", val: string(c)}
}

func isNameStart(c byte) bool {
	return c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
}
func isNameCont(c byte) bool { return isNameStart(c) || (c >= '0' && c <= '9') }

// --- Parser ------------------------------------------------------------------

func ParseDocument(src string) (*Document, error) {
	l := newLexer(src)
	doc := &Document{Fragments: map[string]*Fragment{}}
	for l.tok.kind != "eof" {
		if l.tok.kind == "name" && l.tok.val == "fragment" {
			f, err := parseFragment(l)
			if err != nil {
				return nil, err
			}
			doc.Fragments[f.Name] = f
			continue
		}
		op, err := parseOperation(l)
		if err != nil {
			return nil, err
		}
		doc.Operations = append(doc.Operations, op)
	}
	if len(doc.Operations) == 0 {
		return nil, l.errorf("aucune opération")
	}
	return doc, nil
}

func parseOperation(l *lexer) (*Operation, error) {
	op := &Operation{Type: "query", VarDefs: map[string]*VarDef{}}
	if l.tok.kind == "name" && (l.tok.val == "query" || l.tok.val == "mutation" || l.tok.val == "subscription") {
		op.Type = l.tok.val
		l.next()
		if l.tok.kind == "name" {
			op.Name = l.tok.val
			l.next()
		}
		if l.tok.kind == "punct" && l.tok.val == "(" {
			if err := parseVarDefs(l, op); err != nil {
				return nil, err
			}
		}
	}
	if !(l.tok.kind == "punct" && l.tok.val == "{") {
		return nil, l.errorf("'{' attendu pour l'ensemble de sélection")
	}
	sel, err := parseSelectionSet(l)
	if err != nil {
		return nil, err
	}
	op.Selection = sel
	return op, nil
}

func parseVarDefs(l *lexer, op *Operation) error {
	l.next() // '('
	for !(l.tok.kind == "punct" && l.tok.val == ")") {
		if !(l.tok.kind == "punct" && l.tok.val == "$") {
			return l.errorf("'$' attendu dans la définition de variable")
		}
		l.next()
		if l.tok.kind != "name" {
			return l.errorf("nom de variable attendu")
		}
		vd := &VarDef{Name: l.tok.val}
		l.next()
		if !(l.tok.kind == "punct" && l.tok.val == ":") {
			return l.errorf("':' attendu après la variable")
		}
		l.next()
		t, err := parseTypeRef(l)
		if err != nil {
			return err
		}
		vd.Type = t
		if l.tok.kind == "punct" && l.tok.val == "=" {
			l.next()
			v, err := parseValue(l)
			if err != nil {
				return err
			}
			vd.Default = v
		}
		op.VarDefs[vd.Name] = vd
	}
	l.next() // ')'
	return nil
}

func parseTypeRef(l *lexer) (string, error) {
	var sb strings.Builder
	if l.tok.kind == "punct" && l.tok.val == "[" {
		sb.WriteString("[")
		l.next()
		inner, err := parseTypeRef(l)
		if err != nil {
			return "", err
		}
		sb.WriteString(inner)
		if !(l.tok.kind == "punct" && l.tok.val == "]") {
			return "", l.errorf("']' attendu")
		}
		sb.WriteString("]")
		l.next()
	} else if l.tok.kind == "name" {
		sb.WriteString(l.tok.val)
		l.next()
	} else {
		return "", l.errorf("type attendu")
	}
	if l.tok.kind == "punct" && l.tok.val == "!" {
		sb.WriteString("!")
		l.next()
	}
	return sb.String(), nil
}

func parseFragment(l *lexer) (*Fragment, error) {
	l.next() // 'fragment'
	if l.tok.kind != "name" {
		return nil, l.errorf("nom de fragment attendu")
	}
	f := &Fragment{Name: l.tok.val}
	l.next()
	if !(l.tok.kind == "name" && l.tok.val == "on") {
		return nil, l.errorf("'on' attendu dans le fragment")
	}
	l.next()
	if l.tok.kind != "name" {
		return nil, l.errorf("type de condition attendu")
	}
	f.TypeCond = l.tok.val
	l.next()
	sel, err := parseSelectionSet(l)
	if err != nil {
		return nil, err
	}
	f.Selection = sel
	return f, nil
}

func parseSelectionSet(l *lexer) ([]*Selection, error) {
	l.next() // '{'
	var sels []*Selection
	for !(l.tok.kind == "punct" && l.tok.val == "}") {
		if l.tok.kind == "eof" {
			return nil, l.errorf("'}' manquant")
		}
		s, err := parseSelection(l)
		if err != nil {
			return nil, err
		}
		sels = append(sels, s)
	}
	l.next() // '}'
	return sels, nil
}

func parseSelection(l *lexer) (*Selection, error) {
	if l.tok.kind == "punct" && l.tok.val == "..." {
		l.next()
		if l.tok.kind == "name" && l.tok.val == "on" {
			l.next()
			if l.tok.kind != "name" {
				return nil, l.errorf("type attendu après 'on'")
			}
			s := &Selection{IsInline: true, InlineOnType: l.tok.val}
			l.next()
			sel, err := parseSelectionSet(l)
			if err != nil {
				return nil, err
			}
			s.Selection = sel
			return s, nil
		}
		if l.tok.kind == "punct" && l.tok.val == "{" {
			s := &Selection{IsInline: true}
			sel, err := parseSelectionSet(l)
			if err != nil {
				return nil, err
			}
			s.Selection = sel
			return s, nil
		}
		if l.tok.kind != "name" {
			return nil, l.errorf("nom de fragment attendu")
		}
		s := &Selection{IsSpread: true, SpreadName: l.tok.val}
		l.next()
		return s, nil
	}

	if l.tok.kind != "name" {
		return nil, l.errorf("nom de champ attendu, obtenu %q", l.tok.val)
	}
	s := &Selection{Name: l.tok.val}
	l.next()
	if l.tok.kind == "punct" && l.tok.val == ":" {
		l.next()
		if l.tok.kind != "name" {
			return nil, l.errorf("nom de champ attendu après l'alias")
		}
		s.Alias = s.Name
		s.Name = l.tok.val
		l.next()
	}
	if l.tok.kind == "punct" && l.tok.val == "(" {
		args, err := parseArguments(l)
		if err != nil {
			return nil, err
		}
		s.Arguments = args
	}
	// Directives ignorées (@include, @skip) — on saute proprement.
	for l.tok.kind == "punct" && l.tok.val == "@" {
		l.next()
		if l.tok.kind == "name" {
			l.next()
		}
		if l.tok.kind == "punct" && l.tok.val == "(" {
			if _, err := parseArguments(l); err != nil {
				return nil, err
			}
		}
	}
	if l.tok.kind == "punct" && l.tok.val == "{" {
		sel, err := parseSelectionSet(l)
		if err != nil {
			return nil, err
		}
		s.Selection = sel
	}
	return s, nil
}

func parseArguments(l *lexer) ([]*Argument, error) {
	l.next() // '('
	var args []*Argument
	for !(l.tok.kind == "punct" && l.tok.val == ")") {
		if l.tok.kind != "name" {
			return nil, l.errorf("nom d'argument attendu")
		}
		a := &Argument{Name: l.tok.val}
		l.next()
		if !(l.tok.kind == "punct" && l.tok.val == ":") {
			return nil, l.errorf("':' attendu après l'argument")
		}
		l.next()
		v, err := parseValue(l)
		if err != nil {
			return nil, err
		}
		a.Value = v
		args = append(args, a)
	}
	l.next() // ')'
	return args, nil
}

func parseValue(l *lexer) (*Value, error) {
	switch {
	case l.tok.kind == "punct" && l.tok.val == "$":
		l.next()
		if l.tok.kind != "name" {
			return nil, l.errorf("nom de variable attendu")
		}
		v := &Value{Kind: "var", Raw: l.tok.val}
		l.next()
		return v, nil
	case l.tok.kind == "int":
		v := &Value{Kind: "int", Raw: l.tok.val}
		l.next()
		return v, nil
	case l.tok.kind == "float":
		v := &Value{Kind: "float", Raw: l.tok.val}
		l.next()
		return v, nil
	case l.tok.kind == "string":
		v := &Value{Kind: "string", Raw: l.tok.val}
		l.next()
		return v, nil
	case l.tok.kind == "name":
		switch l.tok.val {
		case "true", "false":
			v := &Value{Kind: "bool", Raw: l.tok.val}
			l.next()
			return v, nil
		case "null":
			l.next()
			return &Value{Kind: "null"}, nil
		default:
			v := &Value{Kind: "enum", Raw: l.tok.val}
			l.next()
			return v, nil
		}
	case l.tok.kind == "punct" && l.tok.val == "[":
		l.next()
		v := &Value{Kind: "list"}
		for !(l.tok.kind == "punct" && l.tok.val == "]") {
			item, err := parseValue(l)
			if err != nil {
				return nil, err
			}
			v.List = append(v.List, item)
		}
		l.next()
		return v, nil
	case l.tok.kind == "punct" && l.tok.val == "{":
		l.next()
		v := &Value{Kind: "object", Obj: map[string]*Value{}}
		for !(l.tok.kind == "punct" && l.tok.val == "}") {
			if l.tok.kind != "name" {
				return nil, l.errorf("clé d'objet attendue")
			}
			key := l.tok.val
			l.next()
			if !(l.tok.kind == "punct" && l.tok.val == ":") {
				return nil, l.errorf("':' attendu dans l'objet")
			}
			l.next()
			val, err := parseValue(l)
			if err != nil {
				return nil, err
			}
			v.Obj[key] = val
		}
		l.next()
		return v, nil
	}
	return nil, l.errorf("valeur attendue, obtenu %q", l.tok.val)
}

// resolveValue convertit une Value AST en valeur Go, variables substituées.
func resolveValue(v *Value, vars map[string]interface{}) interface{} {
	if v == nil {
		return nil
	}
	switch v.Kind {
	case "var":
		return vars[v.Raw]
	case "int":
		n, _ := strconv.ParseInt(v.Raw, 10, 64)
		return n
	case "float":
		f, _ := strconv.ParseFloat(v.Raw, 64)
		return f
	case "string":
		return v.Raw
	case "bool":
		return v.Raw == "true"
	case "enum":
		return v.Raw
	case "null":
		return nil
	case "list":
		out := make([]interface{}, 0, len(v.List))
		for _, it := range v.List {
			out = append(out, resolveValue(it, vars))
		}
		return out
	case "object":
		out := map[string]interface{}{}
		for k, val := range v.Obj {
			out[k] = resolveValue(val, vars)
		}
		return out
	}
	return nil
}
