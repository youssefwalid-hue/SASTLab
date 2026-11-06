using System.Collections.Concurrent;
using Tresoria.Models;

namespace Tresoria.Services;

/// <summary>
/// Jeu de donnees de laboratoire 100% FICTIF (aucun secret reel, IBAN de test).
/// Entreprise A : saisie1, valideur1, admin-ent. Entreprise B : saisie2.
/// Mot de passe fictif : Password1!  -  OTP fictif : 123456.
/// </summary>
internal static class Seed
{
    public const string LabPassword = "Password1!";

    public static void Load(
        IStore store,
        ConcurrentDictionary<string, User> users,
        ConcurrentDictionary<string, Company> companies,
        ConcurrentDictionary<string, Account> accounts,
        ConcurrentDictionary<long, Batch> batches,
        ConcurrentDictionary<long, Transfer> transfers,
        ConcurrentDictionary<long, Beneficiary> beneficiaries,
        ConcurrentDictionary<string, ResetTokenRecord> resetTokens,
        ConcurrentDictionary<string, long> batchSeq,
        ref long transferSeq)
    {
        companies["A"] = new Company { Id = "A", Name = "Entreprise A (fictive)", DailyCap = 10000.00m };
        companies["B"] = new Company { Id = "B", Name = "Entreprise B (fictive)", DailyCap = 10000.00m };

        // Comptes utilisateurs. Emails / telephones 100% FICTIFS (domaine *.lab.exemple),
        // exploites par les surfaces benignes (profil / notifications). Ne pas changer les
        // identifiants (username) ni les roles/entreprises attendus par le validateur.
        users["saisie1"]   = new User { Username = "saisie1",   Password = LabPassword, Role = Roles.Saisie,     Company = "A", Email = "saisie1@tresoria.lab.exemple",   Phone = "+33 1 99 00 00 01" };
        users["valideur1"] = new User { Username = "valideur1", Password = LabPassword, Role = Roles.Validation, Company = "A", Email = "valideur1@tresoria.lab.exemple", Phone = "+33 1 99 00 00 02" };
        users["admin-ent"] = new User { Username = "admin-ent", Password = LabPassword, Role = Roles.Admin,      Company = "A", Email = "admin-ent@tresoria.lab.exemple", Phone = "+33 1 99 00 00 03" };
        users["saisie2"]   = new User { Username = "saisie2",   Password = LabPassword, Role = Roles.Saisie,     Company = "B", Email = "saisie2@tresoria.lab.exemple",   Phone = "+33 1 99 00 00 04" };
        // Comptes supplementaires (volume de donnees fictives ; non requis par les cas existants).
        users["saisie3"]   = new User { Username = "saisie3",   Password = LabPassword, Role = Roles.Saisie,     Company = "B", Email = "saisie3@tresoria.lab.exemple",   Phone = "+33 1 99 00 00 05" };
        users["valideur2"] = new User { Username = "valideur2", Password = LabPassword, Role = Roles.Validation, Company = "B", Email = "valideur2@tresoria.lab.exemple", Phone = "+33 1 99 00 00 06" };

        accounts["A-001"] = new Account { Id = "A-001", Company = "A", Balance = 250000.00m };
        accounts["B-001"] = new Account { Id = "B-001", Company = "B", Balance = 180000.00m };
        // Comptes supplementaires fictifs (variete pour le crawl ; A-001/B-001 inchanges).
        accounts["A-002"] = new Account { Id = "A-002", Company = "A", Balance = 75000.00m };
        accounts["B-002"] = new Account { Id = "B-002", Company = "B", Balance = 42000.00m };

        // --- Lot 9001 (entreprise A) : DRAFT, cree par saisie1 (sert a V001/V010/V013). ---
        var b9001 = new Batch
        {
            Id = 9001, Company = "A", Label = "Loyers octobre", Status = BatchStatus.Draft,
            Currency = "EUR", CreatedBy = "saisie1",
            InternalCost = 0.42m, FraudScore = 0.07, InternalNote = "revue manuelle backoffice"
        };
        b9001.Transfers.Add(new Transfer
        {
            Id = ++transferSeq, BatchId = 9001, Company = "A", AccountId = "A-001",
            Iban = "FR7630000000000000000000001", Amount = 1500.00m, Currency = "EUR",
            Label = "Loyer siege", Status = BatchStatus.Draft, CreatedBy = "saisie1",
            InternalCost = 0.42m, FraudScore = 0.07, InternalNote = "revue manuelle backoffice"
        });
        batches[9001] = b9001;

        // --- Lot 9002 (entreprise A) : PENDING_APPROVAL (sert a V002 : BFLA validate). ---
        var b9002 = new Batch
        {
            Id = 9002, Company = "A", Label = "Fournisseurs S40", Status = BatchStatus.PendingApproval,
            Currency = "EUR", CreatedBy = "saisie1", SubmittedBy = "saisie1",
            InternalCost = 1.10m, FraudScore = 0.03, InternalNote = "lot soumis"
        };
        b9002.Transfers.Add(new Transfer
        {
            Id = ++transferSeq, BatchId = 9002, Company = "A", AccountId = "A-001",
            Iban = "FR7630000000000000000000010", Amount = 800.00m, Currency = "EUR",
            Label = "Prestataire", Status = BatchStatus.PendingApproval, CreatedBy = "saisie1"
        });
        batches[9002] = b9002;

        // --- Lot 9003 (entreprise A) : DRAFT (sert a V005 : mass assignment status). ---
        var b9003 = new Batch
        {
            Id = 9003, Company = "A", Label = "Loyers octobre (brouillon)", Status = BatchStatus.Draft,
            Currency = "EUR", CreatedBy = "saisie1"
        };
        batches[9003] = b9003;

        // --- Lot 9005 (entreprise B) : DRAFT, cree par saisie2 (sert a V004 IDOR, V009 XSS liste). ---
        var b9005 = new Batch
        {
            Id = 9005, Company = "B", Label = "Salaires B", Status = BatchStatus.Draft,
            Currency = "EUR", CreatedBy = "saisie2"
        };
        batches[9005] = b9005;

        // seq des lots : prochain A -> 9004, prochain B -> 9006
        batchSeq["A"] = 9003;
        batchSeq["B"] = 9005;

        // --- Beneficiaires (NC N001 / N005). ---
        beneficiaries[4201] = new Beneficiary
        {
            Id = 4201, Company = "A", Name = "Fournisseur Alpha (fictif)",
            Iban = "FR7630000000000000000004201", Verified = false, CreatedBy = "saisie1"
        };
        beneficiaries[4202] = new Beneficiary
        {
            Id = 4202, Company = "A", Name = "Fournisseur Beta (fictif)",
            Iban = "FR7630000000000000000004202", Verified = false, CreatedBy = "saisie1"
        };

        // --- Virement 7001 (entreprise A) pour NC N004 (consultation cloisonnee). ---
        var t7001 = new Transfer
        {
            Id = 7001, BatchId = 9001, Company = "A", AccountId = "A-001",
            Iban = "FR7630000000000000000007001", Amount = 320.00m, Currency = "EUR",
            Label = "Frais generaux", Status = BatchStatus.Draft, CreatedBy = "saisie1"
        };
        transfers[7001] = t7001;
        if (transferSeq < 7001) transferSeq = 7001;

        // --- Volume de donnees fictives supplementaires (surface de crawl ; aucun ecart). ---
        // Beneficiaires additionnels (entreprises A et B). IDs > 4202 pour ne pas heurter N001/N005.
        beneficiaries[4203] = new Beneficiary { Id = 4203, Company = "A", Name = "Fournisseur Gamma (fictif)",  Iban = "FR7630000000000000000004203", Verified = true,  CreatedBy = "saisie1", ApprovedBy = "valideur1" };
        beneficiaries[4204] = new Beneficiary { Id = 4204, Company = "A", Name = "Prestataire Delta (fictif)",  Iban = "FR7630000000000000000004204", Verified = false, CreatedBy = "saisie1" };
        beneficiaries[4205] = new Beneficiary { Id = 4205, Company = "B", Name = "Fournisseur Epsilon (fictif)", Iban = "FR7630000000000000000004205", Verified = true,  CreatedBy = "saisie2", ApprovedBy = "valideur2" };
        beneficiaries[4206] = new Beneficiary { Id = 4206, Company = "B", Name = "Prestataire Zeta (fictif)",    Iban = "FR7630000000000000000004206", Verified = false, CreatedBy = "saisie2" };

        // Virements historiques fictifs (cloisonnes ; consultes par les surfaces benignes). IDs 7101+.
        // NB : sur le compte A-002 (pas A-001) pour laisser le cumul du jour de A-001 (V003) inchange.
        transfers[7101] = new Transfer { Id = 7101, BatchId = 9001, Company = "A", AccountId = "A-002", Iban = "FR7630000000000000000007101", Amount = 540.00m,  Currency = "EUR", Label = "Fournitures bureau", Status = BatchStatus.Approved,         CreatedBy = "saisie1" };
        transfers[7102] = new Transfer { Id = 7102, BatchId = 9001, Company = "A", AccountId = "A-002", Iban = "FR7630000000000000000007102", Amount = 1280.00m, Currency = "EUR", Label = "Maintenance ascenseur", Status = BatchStatus.Validated,       CreatedBy = "saisie1" };
        transfers[7103] = new Transfer { Id = 7103, BatchId = 9002, Company = "A", AccountId = "A-002", Iban = "FR7630000000000000000007103", Amount = 4300.00m, Currency = "USD", Label = "Licence logicielle", Status = BatchStatus.PendingApproval,     CreatedBy = "saisie1" };
        transfers[7104] = new Transfer { Id = 7104, BatchId = 9005, Company = "B", AccountId = "B-001", Iban = "FR7630000000000000000007104", Amount = 2600.00m, Currency = "EUR", Label = "Salaire octobre", Status = BatchStatus.Draft,               CreatedBy = "saisie2" };
        transfers[7105] = new Transfer { Id = 7105, BatchId = 9005, Company = "B", AccountId = "B-002", Iban = "FR7630000000000000000007105", Amount = 980.00m,  Currency = "GBP", Label = "Prestation audit", Status = BatchStatus.Rejected,            CreatedBy = "saisie2" };
        if (transferSeq < 7105) transferSeq = 7105;

        // Lots additionnels (variete de statuts). IDs hors sequence active (batchSeq inchange : A->9004, B->9006).
        batches[9007] = new Batch { Id = 9007, Company = "A", Label = "Abonnements SaaS", Status = BatchStatus.Validated, Currency = "EUR", CreatedBy = "saisie1", SubmittedBy = "saisie1", ValidatedBy = "valideur1" };
        batches[9008] = new Batch { Id = 9008, Company = "B", Label = "Frais deplacement", Status = BatchStatus.Rejected, Currency = "EUR", CreatedBy = "saisie2", SubmittedBy = "saisie2" };

        // --- Jeton de reinitialisation fictif a usage unique (NC N006). ---
        resetTokens["reset-fixture-001"] = new ResetTokenRecord
        {
            Token = "reset-fixture-001", Username = "saisie1",
            Used = false, ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }
}
