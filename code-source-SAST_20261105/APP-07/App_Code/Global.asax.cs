// =============================================================================
// APP-07 "PayGate XML" - Cycle de vie applicatif (seed + en-tete de laboratoire).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
// =============================================================================
using System;
using System.Web;
using PayGate.Core;

namespace PayGate
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Seed de l'etat de demonstration + fichiers temoins fictifs.
            Store.Reset();
        }

        protected void Application_PreSendRequestHeaders(object sender, EventArgs e)
        {
            var resp = HttpContext.Current == null ? null : HttpContext.Current.Response;
            if (resp == null) return;

            // En-tete de marquage du laboratoire sur toutes les reponses.
            resp.Headers[Lab.EnvHeaderName] = Lab.EnvHeaderValue;

            // NOTE SECURITE (GT: APP07-V013) : les en-tetes de durcissement
            // (Strict-Transport-Security, X-Content-Type-Options, X-Frame-Options,
            // CSP) ne sont VOLONTAIREMENT pas emis ici - ecart de configuration TLS/
            // headers faible. Les en-tetes de version (GT: APP07-V009) ne sont pas
            // supprimes (voir web.config).
        }
    }
}
