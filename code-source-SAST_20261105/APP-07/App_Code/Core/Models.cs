// =============================================================================
// APP-07 "PayGate XML" - Modeles internes et constantes de laboratoire.
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
// =============================================================================
using System;
using System.Collections.Generic;

namespace PayGate.Core
{
    public static class Lab
    {
        // En-tete present sur toutes les reponses HTTP du laboratoire.
        public const string EnvHeaderName = "X-Lab-Environment";
        public const string EnvHeaderValue = "test-data-fictives";

        public const string Password = "Password1!";   // mot de passe fictif commun
        public const string Otp = "123456";            // OTP fictif

        // Comptes de laboratoire (fictifs).
        public const string OperatorAccount = "operateur-ent";
        public const string InternalAccount = "service-interne";

        // Fichiers temoins de laboratoire (chemins Windows cibles de la mise en evidence).
        public const string XxeCanaryPath = @"C:\lab\fixtures\xxe-canary.txt";
        public const string SecretLabPath = @"C:\lab\fixtures\secret-lab.txt";
        public const string SsrfCanaryPath = @"C:\lab\fixtures\ssrf-canary.txt";  // V016
    }

    public class Order
    {
        public string Reference { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CreditorIban { get; set; }
        public string Status { get; set; }
        public string Owner { get; set; }     // operateur-ent / service-interne
        public string ReceiptName { get; set; }
    }

    public class Batch
    {
        public string BatchId { get; set; }
        public string Status { get; set; }    // PENDING / CANCELLED / ARCHIVED
        private List<string> _orderRefs = new List<string>();
        public List<string> OrderRefs { get { return _orderRefs; } set { _orderRefs = value; } }
    }

    // Identite resolue a partir de l'en-tete SOAP wsse:UsernameToken.
    public class SoapIdentity
    {
        public string Username { get; set; }
        public bool Authenticated { get; set; }
        public string Role { get; set; }      // "operateur" / "interne"
    }
}
