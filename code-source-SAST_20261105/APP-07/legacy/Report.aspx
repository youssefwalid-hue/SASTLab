<%@ Page Language="C#" AutoEventWireup="true"
    CodeFile="Report.aspx.cs" Inherits="PayGate.Legacy.Report"
    EnableViewStateMac="false" %>
<%-- GT: APP07-V015 - page WebForms heritee avec enableViewStateMac DESACTIVE :
     le __VIEWSTATE est deserialise sans verification d'integrite (MAC absent). --%>
<!DOCTYPE html>
<html lang="fr">
<head runat="server"><title>PayGate - Rapport (heritage)</title></head>
<body>
  <p style="background:#ffd;padding:6px">Environnement de test - donnees fictives</p>
  <form id="form1" runat="server">
    <asp:Label ID="lblInfo" runat="server" Text="Rapport de lots (heritage)" />
    <asp:Button ID="btnRefresh" runat="server" Text="Rafraichir"
                OnClick="btnRefresh_Click" />
  </form>
</body>
</html>
