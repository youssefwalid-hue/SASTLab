package main

// =============================================================================
// execute.go — Exécution d'un document GraphQL contre les résolveurs.
// Les objets implémentent l'interface Obj ; l'exécuteur façonne la sortie selon
// l'ensemble de sélection (champs, alias, fragments inline/nommés).
// =============================================================================

type gqlError struct {
	Message    string                 `json:"message"`
	Path       []interface{}          `json:"path,omitempty"`
	Extensions map[string]interface{} `json:"extensions,omitempty"`
}

// Obj est tout objet GraphQL résoluble.
type Obj interface {
	TypeName() string
	Field(ex *Executor, name string, args map[string]interface{}) (interface{}, *gqlError)
}

// lenientObj : les objets d'introspection renvoient nil pour un champ inconnu
// (plutôt qu'une erreur), afin qu'une requête d'introspection complète de
// scanner n'échoue pas.
type lenientObj interface{ lenient() }

type Executor struct {
	doc         *Document
	vars        map[string]interface{}
	currentUser *User  // sujet authentifié (nil si anonyme)
	deviceHdr   string // en-tête X-Device-Id fourni par le client
	authAlgNone bool   // vrai si le jeton a été accepté via alg:none (GT: APP06-V018)
	errors      []*gqlError
}

func (ex *Executor) addError(e *gqlError) { ex.errors = append(ex.errors, e) }

// implementsType indique si obj satisfait une condition de type/fragment.
func implementsType(obj Obj, cond string) bool {
	if cond == "" || obj.TypeName() == cond {
		return true
	}
	if cond == "Node" {
		switch obj.TypeName() {
		case "Account", "User", "Card", "Transaction", "Notification":
			return true
		}
	}
	return false
}

// resolveArgs transforme les arguments AST en map Go (variables substituées).
func (ex *Executor) resolveArgs(args []*Argument) map[string]interface{} {
	out := map[string]interface{}{}
	for _, a := range args {
		out[a.Name] = resolveValue(a.Value, ex.vars)
	}
	return out
}

// executeSelectionSet exécute un ensemble de sélection sur un objet.
func (ex *Executor) executeSelectionSet(obj Obj, sels []*Selection, path []interface{}) map[string]interface{} {
	out := map[string]interface{}{}
	_, isLenient := obj.(lenientObj)

	for _, s := range sels {
		switch {
		case s.IsSpread:
			frag := ex.doc.Fragments[s.SpreadName]
			if frag == nil || !implementsType(obj, frag.TypeCond) {
				continue
			}
			sub := ex.executeSelectionSet(obj, frag.Selection, path)
			for k, v := range sub {
				out[k] = v
			}
		case s.IsInline:
			if !implementsType(obj, s.InlineOnType) {
				continue
			}
			sub := ex.executeSelectionSet(obj, s.Selection, path)
			for k, v := range sub {
				out[k] = v
			}
		default:
			key := s.outputKey()
			fieldPath := append(append([]interface{}{}, path...), key)

			if s.Name == "__typename" {
				out[key] = obj.TypeName()
				continue
			}

			args := ex.resolveArgs(s.Arguments)
			val, ferr := obj.Field(ex, s.Name, args)
			if ferr != nil {
				if ferr.Path == nil {
					ferr.Path = fieldPath
				}
				ex.addError(ferr)
				out[key] = nil
				continue
			}
			if val == nil && !isLenient {
				// Champ connu mais valeur absente -> null (comportement normal).
			}
			out[key] = ex.shape(val, s.Selection, fieldPath)
		}
	}
	return out
}

// shape façonne une valeur résolue selon la sous-sélection.
func (ex *Executor) shape(val interface{}, sels []*Selection, path []interface{}) interface{} {
	if val == nil {
		return nil
	}
	switch v := val.(type) {
	case Obj:
		if len(sels) == 0 {
			return v.TypeName()
		}
		return ex.executeSelectionSet(v, sels, path)
	case []interface{}:
		out := make([]interface{}, 0, len(v))
		for i, item := range v {
			itemPath := append(append([]interface{}{}, path...), i)
			out = append(out, ex.shape(item, sels, itemPath))
		}
		return out
	default:
		return v
	}
}

// Execute lance l'opération choisie et renvoie (data, errors).
func (ex *Executor) Execute(opName string) (map[string]interface{}, []*gqlError) {
	var op *Operation
	if opName != "" {
		for _, o := range ex.doc.Operations {
			if o.Name == opName {
				op = o
				break
			}
		}
	}
	if op == nil {
		op = ex.doc.Operations[0]
	}

	var root Obj
	if op.Type == "mutation" {
		root = &MutationRoot{}
	} else {
		root = &QueryRoot{}
	}

	// complexity) n'est appliquée avant l'exécution : une requête fortement
	// imbriquée (relations cycliques) est traitée sans rejet.
	data := ex.executeSelectionSet(root, op.Selection, nil)
	return data, ex.errors
}
