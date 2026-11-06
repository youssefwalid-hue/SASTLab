// =============================================================================
// APP-07 "PayGate XML" - Passerelle de paiements SOAP/XML - DAST Benchmark Lab
// Contrats de donnees (DataContract) partages par le contrat de service WCF.
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
// SECURISE PAR DEFAUT : les ecarts volontaires sont tagues // GT: APP07-Vxxx.
// =============================================================================
using System;
using System.Runtime.Serialization;

namespace PayGate.Contracts
{
    [DataContract(Namespace = "https://paygate.lab.exemple/payment")]
    public class OrderResult
    {
        [DataMember] public string Reference { get; set; }
        [DataMember] public decimal Amount { get; set; }
        [DataMember] public string Currency { get; set; }
        [DataMember] public string CreditorIban { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string Owner { get; set; }
        [DataMember] public string ReceiptName { get; set; }
    }

    [DataContract(Namespace = "https://paygate.lab.exemple/payment")]
    public class BatchInfo
    {
        [DataMember] public string BatchId { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public int OrderCount { get; set; }
    }

    [DataContract(Namespace = "https://paygate.lab.exemple/payment")]
    public class SubmitResult
    {
        [DataMember] public string Reference { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string ReceiptName { get; set; }
        [DataMember] public string Message { get; set; }
    }

    [DataContract(Namespace = "https://paygate.lab.exemple/payment")]
    public class AccountStatement
    {
        [DataMember] public string Account { get; set; }
        [DataMember] public decimal Balance { get; set; }
        [DataMember] public string Currency { get; set; }
    }

    [DataContract(Namespace = "https://paygate.lab.exemple/payment")]
    public class AdminActionResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public int Affected { get; set; }
        [DataMember] public string Message { get; set; }
    }

    // Enrichissement v23 : resultats des nouvelles operations (V016..V021 / NC).

    // Beneficiaire renvoye par FindBeneficiary (V017) / FindBeneficiarySecure (N009).
    [DataContract(Namespace = "https://paygate.lab.exemple/payment")]
    public class BeneficiaryResult
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public string Iban { get; set; }
        [DataMember] public string Country { get; set; }
    }

    // Contenu generique : import SSRF (V016/N008), journal (V018/N010), diag (V020/N012).
    [DataContract(Namespace = "https://paygate.lab.exemple/payment")]
    public class ContentResult
    {
        [DataMember] public string Content { get; set; }
    }
}
