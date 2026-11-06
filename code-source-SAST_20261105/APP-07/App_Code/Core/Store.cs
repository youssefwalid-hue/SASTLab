// =============================================================================
// APP-07 "PayGate XML" - Etat de demonstration en memoire + seed + reset.
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// Profil par defaut : depot MEMOIRE (demarrage immediat, aucune base externe).
// Profil `localdb`   : voir OrderRepository (SQL Server LocalDB), active par la
//                      variable d'environnement DB_PROFILE=localdb.
// =============================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PayGate.Core
{
    public static class Store
    {
        public static readonly ConcurrentDictionary<string, Order> Orders =
            new ConcurrentDictionary<string, Order>(StringComparer.OrdinalIgnoreCase);

        public static readonly ConcurrentDictionary<string, Batch> Batches =
            new ConcurrentDictionary<string, Batch>(StringComparer.OrdinalIgnoreCase);

        // Nonces consommes (anti-rejeu) - utilise UNIQUEMENT par SubmitDirectDebit (NC N006).
        public static readonly ConcurrentDictionary<string, DateTime> ConsumedNonces =
            new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);

        private static readonly object _gate = new object();

        public static void Reset()
        {
            lock (_gate)
            {
                Orders.Clear();
                Batches.Clear();
                ConsumedNonces.Clear();
                Seed();
                SeedFixtures();
            }
        }

        private static void Seed()
        {
            // Ordres de demonstration fictifs.
            AddOrder("ORD-LAB-100", 1500.00m, "EUR", "FR7630001007941234567890185",
                     "EXECUTED", Lab.OperatorAccount);
            AddOrder("ORD-LAB-101", 250.50m, "EUR", "FR7630004000031234567890143",
                     "EXECUTED", Lab.OperatorAccount);
            // Ordre reserve a service-interne (cible de APP07-V003).
            AddOrder("ORD-INT-900", 99000.00m, "EUR", "FR7610011000201234567890123",
                     "EXECUTED", Lab.InternalAccount);

            // Lots de paiement en attente (cibles de APP07-V005 CancelAllBatches).
            Batches["BATCH-2026-10-01"] = new Batch
            {
                BatchId = "BATCH-2026-10-01",
                Status = "PENDING",
                OrderRefs = new List<string> { "ORD-LAB-100", "ORD-LAB-101" }
            };
            Batches["BATCH-2026-10-02"] = new Batch
            {
                BatchId = "BATCH-2026-10-02",
                Status = "PENDING",
                OrderRefs = new List<string> { "ORD-INT-900" }
            };
            // Lot archive (cible de la purge NC N005).
            Batches["BATCH-2026-09-15"] = new Batch
            {
                BatchId = "BATCH-2026-09-15",
                Status = "ARCHIVED",
                OrderRefs = new List<string>()
            };
        }

        private static void AddOrder(string refId, decimal amount, string currency,
                                     string iban, string status, string owner)
        {
            Orders[refId] = new Order
            {
                Reference = refId,
                Amount = amount,
                Currency = currency,
                CreditorIban = iban,
                Status = status,
                Owner = owner,
                ReceiptName = "ACK-" + refId + ".xml"
            };
        }

        // Depose les fichiers temoins + fichiers de lot fictifs servis par /batches/ (V010)
        // et les accuses (V014). Tout est 100% fictif.
        private static void SeedFixtures()
        {
            try
            {
                string appRoot = AppDomain.CurrentDomain.BaseDirectory;

                // /batches/ : fichiers de lot fictifs (directory listing volontaire V010).
                string batchesDir = Path.Combine(appRoot, "batches");
                Directory.CreateDirectory(batchesDir);
                File.WriteAllText(Path.Combine(batchesDir, "lot-2026-10-01.xml"),
                    "<batch id=\"BATCH-2026-10-01\"><!-- DONNEES FICTIVES LAB --></batch>");
                File.WriteAllText(Path.Combine(batchesDir, "lot-2026-10-02.xml"),
                    "<batch id=\"BATCH-2026-10-02\"><!-- DONNEES FICTIVES LAB --></batch>");

                // receipts/ : accuses legitimes (base de la traversee V014).
                string receiptsDir = Path.Combine(appRoot, "receipts");
                Directory.CreateDirectory(receiptsDir);
                File.WriteAllText(Path.Combine(receiptsDir, "ACK-ORD-LAB-100.xml"),
                    "<receipt ref=\"ORD-LAB-100\"><!-- FICTIF --></receipt>");

                // statements/ : releves (base du telechargement confine NC N007).
                string statementsDir = Path.Combine(appRoot, "statements");
                Directory.CreateDirectory(statementsDir);
                File.WriteAllText(Path.Combine(statementsDir, "STMT-2026-10.xml"),
                    "<statement month=\"2026-10\"><!-- FICTIF --></statement>");

                // Fichiers temoins hors des dossiers servis (cibles XXE / traversee).
                string labDir = @"C:\lab\fixtures";
                Directory.CreateDirectory(labDir);
                if (!File.Exists(Lab.XxeCanaryPath))
                    File.WriteAllText(Lab.XxeCanaryPath, "LAB-XXE-CANARY-APP07");
                if (!File.Exists(Lab.SecretLabPath))
                    File.WriteAllText(Lab.SecretLabPath, "LAB-SECRET-APP07-FICTIF");
                // Temoin SSRF (cible interne recuperee cote serveur, V016).
                if (!File.Exists(Lab.SsrfCanaryPath))
                    File.WriteAllText(Lab.SsrfCanaryPath, "LAB-SSRF-CANARY-APP07");
            }
            catch
            {
                // Hors Windows / droits insuffisants : le seed fichiers est best-effort.
            }
        }
    }
}
