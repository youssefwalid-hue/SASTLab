package main

// =============================================================================
// jwt.go — JWT HS256 minimal (bibliothèque standard uniquement).
// Jeton émis au login ; porte le sujet, le rôle et l'appareil enrôlé.
// =============================================================================

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"errors"
	"strings"
	"time"
)

// Secret de LABORATOIRE (données fictives, aucune valeur de production).
var jwtSecret = []byte("mobipay-lab-secret-fictif-ne-pas-reutiliser")

type claims struct {
	Sub      string `json:"sub"`      // username
	Role     string `json:"role"`     // USER | ADMIN
	DeviceID string `json:"deviceId"` // appareil enrôlé au login
	Exp      int64  `json:"exp"`
	algNone  bool   // non sérialisé : vrai si le jeton a été accepté avec alg:none (GT: APP06-V018)
}

// jwtHeader minimal pour lire l'algorithme déclaré par le client.
type jwtHeader struct {
	Alg string `json:"alg"`
	Typ string `json:"typ"`
}

func b64url(b []byte) string { return base64.RawURLEncoding.EncodeToString(b) }

func signJWT(c claims) string {
	header := `{"alg":"HS256","typ":"JWT"}`
	payload, _ := json.Marshal(c)
	signingInput := b64url([]byte(header)) + "." + b64url(payload)
	mac := hmac.New(sha256.New, jwtSecret)
	mac.Write([]byte(signingInput))
	sig := b64url(mac.Sum(nil))
	return signingInput + "." + sig
}

func issueToken(u *User) string {
	return signJWT(claims{
		Sub:      u.Username,
		Role:     u.Role,
		DeviceID: u.DeviceID,
		Exp:      time.Now().Add(24 * time.Hour).Unix(),
	})
}

var errBadToken = errors.New("jeton invalide")

func verifyJWT(token string) (*claims, error) {
	parts := strings.Split(token, ".")
	if len(parts) != 3 {
		return nil, errBadToken
	}

	// Lecture de l'en-tête pour déterminer l'algorithme déclaré par le client.
	hdrRaw, err := base64.RawURLEncoding.DecodeString(parts[0])
	if err != nil {
		return nil, errBadToken
	}
	var hdr jwtHeader
	_ = json.Unmarshal(hdrRaw, &hdr)

	payload, err := base64.RawURLEncoding.DecodeString(parts[1])
	if err != nil {
		return nil, errBadToken
	}
	var c claims
	if err := json.Unmarshal(payload, &c); err != nil {
		return nil, errBadToken
	}

	// accepté SANS vérification de signature. Le client choisit l'algorithme,
	// ce qui permet de forger un jeton arbitraire (ex. sub=mobile-admin).
	if strings.EqualFold(hdr.Alg, "none") {
		if c.Exp != 0 && time.Now().Unix() > c.Exp {
			return nil, errBadToken
		}
		c.algNone = true
		return &c, nil
	}

	// Chemin nominal HS256 : la signature est vérifiée.
	signingInput := parts[0] + "." + parts[1]
	mac := hmac.New(sha256.New, jwtSecret)
	mac.Write([]byte(signingInput))
	expected := b64url(mac.Sum(nil))
	if !hmac.Equal([]byte(expected), []byte(parts[2])) {
		return nil, errBadToken
	}
	if c.Exp != 0 && time.Now().Unix() > c.Exp {
		return nil, errBadToken
	}
	return &c, nil
}
