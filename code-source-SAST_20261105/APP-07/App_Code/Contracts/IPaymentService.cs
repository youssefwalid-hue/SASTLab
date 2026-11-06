// =============================================================================
// APP-07 "PayGate XML" - Contrat de service SOAP PUBLIC (WCF).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// NOTE SECURITE : ce contrat PUBLIC expose, en plus des operations metier, des
// operations d'administration (CancelAllBatches) -> ecart volontaire APP07-V004
// (WSDL exposant des operations internes). Le contrat INTERNE correctement
// cloisonne se trouve dans IAdminService.cs (endpoint non publie) -> NC APP07-N004.
// =============================================================================
using System.ServiceModel;
using PayGate.Contracts;

namespace PayGate.Contracts
{
    [ServiceContract(Namespace = "https://paygate.lab.exemple/payment")]
    public interface IPaymentService
    {
        // ---- Operations metier (legitimement publiques) -------------------------

        [OperationContract(Action = "SubmitOrder", ReplyAction = "SubmitOrderResponse")]
        System.ServiceModel.Channels.Message SubmitOrder(System.ServiceModel.Channels.Message request);

        // NC: APP07-N001 - operation soeur au parseur XML durci (XXE neutralise).
        [OperationContract(Action = "SubmitOrderV2", ReplyAction = "SubmitOrderV2Response")]
        System.ServiceModel.Channels.Message SubmitOrderV2(System.ServiceModel.Channels.Message request);

        // NC: APP07-N002 - operation soeur a validation XSD stricte
        //                  (injection XML + billion laughs bloques).
        [OperationContract(Action = "QuoteOrder", ReplyAction = "QuoteOrderResponse")]
        System.ServiceModel.Channels.Message QuoteOrder(System.ServiceModel.Channels.Message request);

        // NC: APP07-N006 - operation soeur state-changing avec anti-rejeu (nonce).
        [OperationContract(Action = "SubmitDirectDebit", ReplyAction = "SubmitDirectDebitResponse")]
        System.ServiceModel.Channels.Message SubmitDirectDebit(System.ServiceModel.Channels.Message request);

        [OperationContract(Action = "GetOrder", ReplyAction = "GetOrderResponse")]
        System.ServiceModel.Channels.Message GetOrder(System.ServiceModel.Channels.Message request);

        // NC: APP07-N007 - consultation parametree (SQLi bloquee).
        [OperationContract(Action = "GetOrderStatus", ReplyAction = "GetOrderStatusResponse")]
        System.ServiceModel.Channels.Message GetOrderStatus(System.ServiceModel.Channels.Message request);

        // NC: APP07-N003 - operation soeur a jeton SOAP verifie cryptographiquement.
        [OperationContract(Action = "GetAccountStatement", ReplyAction = "GetAccountStatementResponse")]
        System.ServiceModel.Channels.Message GetAccountStatement(System.ServiceModel.Channels.Message request);

        [OperationContract(Action = "ListBatches", ReplyAction = "ListBatchesResponse")]
        System.ServiceModel.Channels.Message ListBatches(System.ServiceModel.Channels.Message request);

        // dans le contrat PUBLIC (fuite de surface) et sans controle de role.
        [OperationContract(Action = "CancelAllBatches", ReplyAction = "CancelAllBatchesResponse")]
        System.ServiceModel.Channels.Message CancelAllBatches(System.ServiceModel.Channels.Message request);

        // =====================================================================
        // Enrichissement v23 - nouvelles couvertures (V016..V021) + NC (N008..N013).
        // =====================================================================

        // restriction de schema ni d'hote (file://, loopback, metadata...).
        [OperationContract(Action = "ImportOrderFromUrl", ReplyAction = "ImportOrderFromUrlResponse")]
        System.ServiceModel.Channels.Message ImportOrderFromUrl(System.ServiceModel.Channels.Message request);

        // NC: APP07-N008 - operation soeur avec allowlist stricte de l'URL (SSRF bloquee).
        [OperationContract(Action = "ImportOrderFromUrlV2", ReplyAction = "ImportOrderFromUrlV2Response")]
        System.ServiceModel.Channels.Message ImportOrderFromUrlV2(System.ServiceModel.Channels.Message request);

        [OperationContract(Action = "FindBeneficiary", ReplyAction = "FindBeneficiaryResponse")]
        System.ServiceModel.Channels.Message FindBeneficiary(System.ServiceModel.Channels.Message request);

        // NC: APP07-N009 - recherche a format valide + XPath parametre (injection bloquee).
        [OperationContract(Action = "FindBeneficiarySecure", ReplyAction = "FindBeneficiarySecureResponse")]
        System.ServiceModel.Channels.Message FindBeneficiarySecure(System.ServiceModel.Channels.Message request);

        [OperationContract(Action = "ExportBatchJournal", ReplyAction = "ExportBatchJournalResponse")]
        System.ServiceModel.Channels.Message ExportBatchJournal(System.ServiceModel.Channels.Message request);

        // NC: APP07-N010 - operation soeur a jeton SOAP VERIFIE (anonyme refuse).
        [OperationContract(Action = "ExportBatchJournalSecure", ReplyAction = "ExportBatchJournalSecureResponse")]
        System.ServiceModel.Channels.Message ExportBatchJournalSecure(System.ServiceModel.Channels.Message request);

        // (chaine de connexion, chemin interne) via includeExceptionDetailInFaults.
        [OperationContract(Action = "GetOrderDiag", ReplyAction = "GetOrderDiagResponse")]
        System.ServiceModel.Channels.Message GetOrderDiag(System.ServiceModel.Channels.Message request);

        // NC: APP07-N012 - operation soeur a Fault generique assaini (aucune donnee systeme).
        [OperationContract(Action = "GetOrderDiagSecure", ReplyAction = "GetOrderDiagSecureResponse")]
        System.ServiceModel.Channels.Message GetOrderDiagSecure(System.ServiceModel.Channels.Message request);

        // fournis (dont owner/status) -> elevation via champ privilegie.
        [OperationContract(Action = "UpdateOrder", ReplyAction = "UpdateOrderResponse")]
        System.ServiceModel.Channels.Message UpdateOrder(System.ServiceModel.Channels.Message request);

        // NC: APP07-N013 - mise a jour a liaison ALLOWLISTEE (owner/status ignores).
        [OperationContract(Action = "UpdateOrderSecure", ReplyAction = "UpdateOrderSecureResponse")]
        System.ServiceModel.Channels.Message UpdateOrderSecure(System.ServiceModel.Channels.Message request);
    }
}
