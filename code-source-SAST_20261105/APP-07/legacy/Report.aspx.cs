// =============================================================================
// APP-07 "PayGate XML" - Page WebForms heritee Report.aspx (code-behind).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// __VIEWSTATE poste est deserialise sans controle d'integrite (MAC absent),
// ce qui autorise la soumission d'un graphe d'objets serialise force.
// =============================================================================
using System;
using System.Web.UI;

namespace PayGate.Legacy
{
    public partial class Report : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Le ViewState est traite au postback ; l'integrite n'est pas verifiee
            // (MAC desactive) -> deserialisation du __VIEWSTATE forge au postback.
            if (IsPostBack)
                lblInfo.Text = "Rapport regenere (postback heritage).";
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            lblInfo.Text = "Lots recharges a " + DateTime.UtcNow.ToString("u");
        }
    }
}
