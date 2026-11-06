// =============================================================================
// APP-07 "PayGate XML" - Acces aux ordres (consultation).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// Deux chemins :
//  - GetOrderByRefInsecure() : reference CONCATENEE dans la requete SQL
//    -> ecart volontaire APP07-V011 (injection SQL).
//  - GetOrderStatusSecure()  : requete PARAMETREE + validation de format
//    -> comportement SECURISE, NC APP07-N007.
//
// Stockage : profil MEMOIRE par defaut (emulation fidele de la semantique SQL,
// y compris l'injection, pour un lab autonome) ; profil `localdb` (variable
// d'environnement DB_PROFILE=localdb) utilisant SQL Server LocalDB avec
// SqlConnection/SqlCommand (concatenation reelle pour V011).
// =============================================================================
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using PayGate.Contracts;

namespace PayGate.Core
{
    public static class OrderRepository
    {
        private static bool UseLocalDb
        {
            get
            {
                return string.Equals(Environment.GetEnvironmentVariable("DB_PROFILE"),
                              "localdb", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string ConnString
        {
            get
            {
                return Environment.GetEnvironmentVariable("DB_CONNSTRING") ??
                    @"Server=(localdb)\MSSQLLocalDB;Database=PayGateLab;Integrated Security=true;";
            }
        }

        // evidence du Fault verbeux (donnees systeme divulguees). A NE PAS exposer en prod.
        public static string DiagConnString { get { return ConnString; } }

        // Une charge comme  ORD-LAB-100' OR '1'='1  renvoie plusieurs lignes ;
        // une charge UNION renvoie des colonnes supplementaires.
        public static List<OrderResult> GetOrderByRefInsecure(string reference)
        {
            if (UseLocalDb)
            {
                var results = new List<OrderResult>();
                // ECART : concatenation directe (pas de SqlParameter).
                string sql = "SELECT Reference, Amount, Currency, CreditorIban, Status, Owner " +
                             "FROM Orders WHERE Reference = '" + reference + "'";
                using (var cn = new SqlConnection(ConnString))
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cn.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            results.Add(new OrderResult
                            {
                                Reference = r.GetString(0),
                                Amount = r.GetDecimal(1),
                                Currency = r.GetString(2),
                                CreditorIban = r.GetString(3),
                                Status = r.GetString(4),
                                Owner = r.GetString(5)
                            });
                }
                return results;
            }

            // Profil memoire : emulation de la semantique d'injection (lab autonome).
            return EmulateInjectableFilter(reference);
        }

        // NC: APP07-N007 - requete parametree + validation stricte du format de
        // reference : aucune injection possible, retour exact ou vide.
        public static OrderResult GetOrderStatusSecure(string reference)
        {
            if (string.IsNullOrEmpty(reference) ||
                !System.Text.RegularExpressions.Regex.IsMatch(reference, @"^[A-Za-z0-9\-]{1,32}$"))
                throw new ArgumentException("Format de reference invalide.");

            if (UseLocalDb)
            {
                const string sql = "SELECT Reference, Amount, Currency, CreditorIban, Status, Owner " +
                                   "FROM Orders WHERE Reference = @ref";
                using (var cn = new SqlConnection(ConnString))
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add(new SqlParameter("@ref", reference));
                    cn.Open();
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                            return new OrderResult
                            {
                                Reference = r.GetString(0),
                                Amount = r.GetDecimal(1),
                                Currency = r.GetString(2),
                                CreditorIban = r.GetString(3),
                                Status = r.GetString(4),
                                Owner = r.GetString(5)
                            };
                    return null;
                }
            }

            // Profil memoire : correspondance EXACTE uniquement (equivalent parametre).
            Order o;
            return Store.Orders.TryGetValue(reference, out o) ? ToResult(o) : null;
        }

        // Emulation (profil memoire) du comportement d'une requete concatenee.
        private static List<OrderResult> EmulateInjectableFilter(string reference)
        {
            string inj = (reference ?? string.Empty);
            string upper = inj.ToUpperInvariant();

            // Tautologie  ' OR '1'='1  -> renvoie tous les ordres.
            bool tautology = upper.Replace(" ", "").Contains("OR'1'='1") ||
                             upper.Contains("OR 1=1");
            // UNION SELECT -> renvoie egalement toutes les lignes (colonnes refletees).
            bool union = upper.Contains("UNION") && upper.Contains("SELECT");

            if (tautology || union)
                return Store.Orders.Values.Select(ToResult).ToList();

            // Correspondance exacte par defaut (comportement nominal).
            Order o;
            return Store.Orders.TryGetValue(inj, out o)
                ? new List<OrderResult> { ToResult(o) }
                : new List<OrderResult>();
        }

        private static OrderResult ToResult(Order o)
        {
            return new OrderResult
            {
                Reference = o.Reference,
                Amount = o.Amount,
                Currency = o.Currency,
                CreditorIban = o.CreditorIban,
                Status = o.Status,
                Owner = o.Owner,
                ReceiptName = o.ReceiptName
            };
        }
    }
}
