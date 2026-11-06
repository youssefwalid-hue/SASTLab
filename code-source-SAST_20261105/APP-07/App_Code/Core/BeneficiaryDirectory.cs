// =============================================================================
// APP-07 "PayGate XML" - Annuaire de beneficiaires (recherche XPath).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// L'annuaire est un petit document XML en memoire (fictif). Deux chemins :
//  - FindInsecure() : le nom recherche est CONCATENE dans l'expression XPath
//    -> ecart volontaire APP07-V017 (injection XPath). Une charge du type
//    ' or '1'='1  renvoie TOUS les beneficiaires (y compris l'entree interne).
//  - FindSecure()   : validation stricte du format + correspondance exacte en
//    code (aucune expression XPath construite par concatenation)
//    -> comportement SECURISE, NC APP07-N009.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using PayGate.Contracts;

namespace PayGate.Core
{
    public static class BeneficiaryDirectory
    {
        // Annuaire fictif : une entree "interne" sensible sert de preuve a l'injection.
        public const string DirectoryXml =
            "<beneficiaries>" +
            "<beneficiary><name>ACME Fictif</name><iban>FR7630001007941234567890185</iban><country>FR</country></beneficiary>" +
            "<beneficiary><name>Beta Lab</name><iban>FR7630004000031234567890143</iban><country>FR</country></beneficiary>" +
            "<beneficiary><name>benef-interne</name><iban>FR7610011000201234567890123</iban><country>LU</country></beneficiary>" +
            "</beneficiaries>";

        private static XmlDocument Load()
        {
            var doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(DirectoryXml);
            return doc;
        }

        public static List<BeneficiaryResult> FindInsecure(string name)
        {
            var doc = Load();
            // ECART : expression XPath construite par concatenation (pas de variable).
            string xpath = "/beneficiaries/beneficiary[name='" + (name ?? string.Empty) + "']";
            var results = new List<BeneficiaryResult>();
            foreach (XmlNode n in doc.SelectNodes(xpath))
                results.Add(ToResult(n));
            return results;
        }

        // NC: APP07-N009 - format valide + correspondance EXACTE en code : la valeur
        // recherchee n'entre jamais dans une expression XPath (injection impossible).
        public static List<BeneficiaryResult> FindSecure(string name)
        {
            if (string.IsNullOrEmpty(name) || !Regex.IsMatch(name, @"^[A-Za-z0-9 \-]{1,40}$"))
                throw new ArgumentException("Nom de beneficiaire invalide.");

            var results = new List<BeneficiaryResult>();
            // Enumeration des noeuds fixes (XPath constant), comparaison exacte en code.
            foreach (XmlNode n in Load().SelectNodes("/beneficiaries/beneficiary"))
            {
                string candidate = n["name"] == null ? "" : n["name"].InnerText;
                if (string.Equals(candidate, name, StringComparison.Ordinal))
                    results.Add(ToResult(n));
            }
            return results;
        }

        private static BeneficiaryResult ToResult(XmlNode n)
        {
            return new BeneficiaryResult
            {
                Name = n["name"] == null ? "" : n["name"].InnerText,
                Iban = n["iban"] == null ? "" : n["iban"].InnerText,
                Country = n["country"] == null ? "" : n["country"].InnerText
            };
        }
    }
}
