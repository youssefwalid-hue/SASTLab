// =============================================================================
// APP-07 "PayGate XML" - Page WebForms DURCIE ReportSecure.aspx (code-behind).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// NC: APP07-N011 - pendant securise de GT: APP07-V019 (clickjacking).
// Emet les en-tetes anti-cadrage (X-Frame-Options: DENY + CSP frame-ancestors
// 'none') et conserve enableViewStateMac actif (.aspx + sans ecart web.config).
// =============================================================================
using System;
using System.Web.UI;

namespace PayGate.Legacy
{
    public partial class ReportSecure : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // En-tetes anti-clickjacking (absents sur la page vulnerable Report.aspx).
            Response.Headers["X-Frame-Options"] = "DENY";
            Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";

            if (IsPostBack)
                lblInfo.Text = "Rapport regenere (postback durci).";
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            lblInfo.Text = "Lots recharges a " + DateTime.UtcNow.ToString("u");
        }
    }
}
