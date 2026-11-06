// =============================================================================
// APP-07 "PayGate XML" - Recuperation d'une ressource distante (import d'ordre).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
//  - FetchInsecure()         : recupere l'URL fournie SANS aucune restriction de
//    schema ni d'hote (http/https/file/loopback/metadata...) -> ecart volontaire
//    APP07-V016 (SSRF). Le contenu recupere est renvoye au client (echo = preuve).
//  - FetchSecureAllowlist()  : n'autorise que https:// vers l'hote de laboratoire
//    explicitement allowliste -> comportement SECURISE, NC APP07-N008.
// =============================================================================
using System;
using System.IO;
using System.Net;

namespace PayGate.Core
{
    public static class RemoteFetch
    {
        // Hote de laboratoire autorise (seul hote legitime d'import d'ordres).
        public const string AllowedHost = "paygate.lab.exemple";

        // (file:///, http://127.0.0.1, http://169.254.169.254/..., etc.).
        public static string FetchInsecure(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL source requise.");

            // ECART : requete serveur vers une URL entierement controlee par l'appelant.
            var req = WebRequest.Create(url);
            req.Timeout = 5000;
            using (var resp = req.GetResponse())
            using (var rs = resp.GetResponseStream())
            using (var sr = new StreamReader(rs))
                return sr.ReadToEnd();
        }

        // NC: APP07-N008 - allowlist stricte : uniquement https:// vers l'hote de lab.
        public static string FetchSecureAllowlist(string url)
        {
            Uri uri;
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri))
                throw new UnauthorizedAccessException("URL invalide.");

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, AllowedHost, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Hote/schema non autorise (allowlist).");

            var req = WebRequest.Create(uri);
            req.Timeout = 5000;
            using (var resp = req.GetResponse())
            using (var rs = resp.GetResponseStream())
            using (var sr = new StreamReader(rs))
                return sr.ReadToEnd();
        }
    }
}
