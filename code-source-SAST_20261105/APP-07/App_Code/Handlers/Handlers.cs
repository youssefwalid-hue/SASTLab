// =============================================================================
// APP-07 "PayGate XML" - Handlers HTTP hors SOAP.
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//   - AdminResetHandler       : POST /admin/reset  (remise a zero de l'etat)
//   - ReceiptDownloadHandler  : GET  /receipts/download?name=...   -> GT V014
//   - StatementDownloadHandler: GET  /statements/download?name=... -> NC N007
//   - ErrHandler              : GET  /PaymentService.svc/err       -> GT V009
// =============================================================================
using System;
using System.IO;
using System.Web;
using PayGate.Core;

namespace PayGate.Handlers
{
    // POST /admin/reset - reinitialise l'etat de demonstration.
    public class AdminResetHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }
        public void ProcessRequest(HttpContext ctx)
        {
            Store.Reset();
            ctx.Response.ContentType = "application/json";
            ctx.Response.Write("{\"status\":\"reset\",\"data\":\"fictives\"}");
        }
    }

    // Le nom est resolu SANS canonicalisation ni confinement : une sequence ..\
    // permet de telecharger un fichier hors du dossier receipts.
    public class ReceiptDownloadHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }
        public void ProcessRequest(HttpContext ctx)
        {
            string name = ctx.Request.QueryString["name"] ?? "";
            string path = PathUtil.ResolveReceiptInsecure(name);   // ECART (V014)
            if (!File.Exists(path))
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Write("not found");
                return;
            }
            ctx.Response.ContentType = "application/octet-stream";
            ctx.Response.AddHeader("Content-Disposition",
                "attachment; filename=\"" + Path.GetFileName(path) + "\"");
            ctx.Response.WriteFile(path);
        }
    }

    // NC: APP07-N007 - GET /statements/download?name=...
    // Canonicalisation + confinement strict au dossier autorise.
    public class StatementDownloadHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }
        public void ProcessRequest(HttpContext ctx)
        {
            string name = ctx.Request.QueryString["name"] ?? "";
            string path;
            try { path = PathUtil.ResolveStatementSecure(name); }   // SECURISE (N007)
            catch (UnauthorizedAccessException)
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.Write("acces refuse");
                return;
            }
            if (!File.Exists(path)) { ctx.Response.StatusCode = 404; ctx.Response.Write("not found"); return; }
            ctx.Response.ContentType = "application/octet-stream";
            ctx.Response.WriteFile(path);
        }
    }

    // page detaillee et les en-tetes (X-AspNet-Version, Server, X-Powered-By)
    // divulguent les versions (customErrors Off dans web.config).
    public class ErrHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }
        public void ProcessRequest(HttpContext ctx)
        {
            throw new InvalidOperationException(
                "Erreur de laboratoire volontaire - trace detaillee exposee (V009).");
        }
    }
}
