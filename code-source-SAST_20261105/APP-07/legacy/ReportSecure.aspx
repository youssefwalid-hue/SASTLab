<%@ Page Language="C#" AutoEventWireup="true"
    CodeFile="ReportSecure.aspx.cs" Inherits="PayGate.Legacy.ReportSecure"
    EnableViewStateMac="true" %>
<%-- NC: APP07-N011 - page soeur DURCIE : emet X-Frame-Options: DENY + CSP
     frame-ancestors 'none' (anti-clickjacking) et conserve le MAC ViewState actif.
     Pendant securise de GT: APP07-V019 (Report.aspx sans en-tete anti-cadrage). --%>
<!DOCTYPE html>
<html lang="fr">
<head runat="server"><title>PayGate - Rapport (durci)</title></head>
<body>
  <p style="background:#ffd;padding:6px">Environnement de test - donnees fictives</p>
  <form id="form1" runat="server">
    <asp:Label ID="lblInfo" runat="server" Text="Rapport de lots (durci)" />
    <asp:Button ID="btnRefresh" runat="server" Text="Rafraichir"
                OnClick="btnRefresh_Click" />
  </form>
</body>
</html>
