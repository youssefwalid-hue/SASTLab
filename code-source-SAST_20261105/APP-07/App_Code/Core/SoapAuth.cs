// =============================================================================
// APP-07 "PayGate XML" - Lecture du jeton SOAP (WS-Security simule).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// Deux chemins de lecture coexistent :
//  - ResolveIdentityInsecure() : lit le wsse:UsernameToken comme une simple
//    assertion d'identite, SANS verification cryptographique -> ecart volontaire
//    APP07-V003 (en-tete SOAP d'authentification falsifiable). Utilise par le flux
//    SubmitOrder / GetOrder visee par la vulnerabilite.
//  - ResolveIdentityVerified() : verifie le condensat de mot de passe + nonce +
//    timestamp (PasswordDigest WS-Security) -> comportement SECURISE,
//    NC APP07-N003, utilise par GetAccountStatement.
// =============================================================================
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace PayGate.Core
{
    public static class SoapAuth
    {
        private static readonly XNamespace Wsse =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        private static readonly XNamespace Wsu =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private static readonly XNamespace Soap =
            "http://schemas.xmlsoap.org/soap/envelope/";

        private static string Role(string username)
        {
            if (string.Equals(username, Lab.InternalAccount, StringComparison.OrdinalIgnoreCase))
                return "interne";
            if (string.Equals(username, Lab.OperatorAccount, StringComparison.OrdinalIgnoreCase))
                return "operateur";
            return "inconnu";
        }

        // preuve cryptographique du secret : il suffit de substituer le Username
        // (p. ex. service-interne) pour prendre une autre identite.
        public static SoapIdentity ResolveIdentityInsecure(XElement header)
        {
            var token = header == null ? null : header
                .Descendants(Wsse + "UsernameToken")
                .FirstOrDefault();
            if (token == null)
                return new SoapIdentity { Authenticated = false };

            string username = (string)token.Element(Wsse + "Username");
            if (string.IsNullOrWhiteSpace(username))
                return new SoapIdentity { Authenticated = false };

            // Aucune verification du mot de passe / condensat / nonce : identite "sur parole".
            return new SoapIdentity
            {
                Username = username,
                Authenticated = true,
                Role = Role(username)
            };
        }

        // NC: APP07-N003 - verification cryptographique du PasswordDigest
        // (Base64(SHA1(nonce + created + password))) avec fenetre temporelle et
        // anti-rejeu du nonce. Un UsernameToken non prouve est rejete.
        public static SoapIdentity ResolveIdentityVerified(XElement header)
        {
            var token = header == null ? null : header.Descendants(Wsse + "UsernameToken").FirstOrDefault();
            if (token == null) return new SoapIdentity { Authenticated = false };

            string username = (string)token.Element(Wsse + "Username");
            string password = (string)token.Element(Wsse + "Password");
            string nonceB64 = (string)token.Element(Wsse + "Nonce");
            string created = (string)token.Element(Wsu + "Created");

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(nonceB64) ||
                string.IsNullOrWhiteSpace(created))
                return new SoapIdentity { Authenticated = false };

            // Fenetre temporelle (anti-rejeu temporel) : +/- 5 minutes.
            DateTime createdUtc;
            if (!DateTime.TryParse(created, out createdUtc) ||
                Math.Abs((DateTime.UtcNow - createdUtc.ToUniversalTime()).TotalMinutes) > 5)
                return new SoapIdentity { Authenticated = false };

            // Anti-rejeu du nonce (usage unique).
            if (!Store.ConsumedNonces.TryAdd("wsse:" + nonceB64, DateTime.UtcNow))
                return new SoapIdentity { Authenticated = false };

            // Verifie le PasswordDigest contre le secret attendu.
            string expected = ComputeDigest(nonceB64, created, Lab.Password);
            bool ok = FixedTimeEquals(password, expected);
            if (!ok) return new SoapIdentity { Authenticated = false };

            return new SoapIdentity
            {
                Username = username,
                Authenticated = true,
                Role = Role(username)
            };
        }

        private static string ComputeDigest(string nonceB64, string created, string password)
        {
            byte[] nonce;
            try { nonce = Convert.FromBase64String(nonceB64); }
            catch { nonce = new byte[0]; }
            byte[] cre = Encoding.UTF8.GetBytes(created);
            byte[] pwd = Encoding.UTF8.GetBytes(password);
            byte[] buf = new byte[nonce.Length + cre.Length + pwd.Length];
            Buffer.BlockCopy(nonce, 0, buf, 0, nonce.Length);
            Buffer.BlockCopy(cre, 0, buf, nonce.Length, cre.Length);
            Buffer.BlockCopy(pwd, 0, buf, nonce.Length + cre.Length, pwd.Length);
            using (var sha = SHA1.Create())
                return Convert.ToBase64String(sha.ComputeHash(buf));
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        public static XNamespace SoapNs { get { return Soap; } }
    }
}
