// APP-03 Tresoria - Entites du domaine (laboratoire, donnees 100% fictives).
namespace Tresoria.Models;

/// <summary>Statuts de workflow d'un lot de virements (quatre yeux).</summary>
public static class BatchStatus
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Validated = "VALIDATED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

/// <summary>Roles applicatifs.</summary>
public static class Roles
{
    public const string Saisie = "saisie";
    public const string Validation = "validation";
    public const string Admin = "admin";
}

public sealed class Company
{
    public string Id { get; set; } = "";           // "A" / "B"
    public string Name { get; set; } = "";
    public decimal DailyCap { get; set; }           // plafond quotidien fictif
}

public sealed class User
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";      // laboratoire : mot de passe fictif en clair
    public string Role { get; set; } = "";          // saisie / validation / admin
    public string Company { get; set; } = "";       // "A" / "B"
    public string Email { get; set; } = "";         // contact fictif @<app>.lab.exemple (surface benigne profil)
    public string Phone { get; set; } = "";         // telephone fictif (surface benigne profil)
}

public sealed class Account
{
    public string Id { get; set; } = "";            // ex. A-001
    public string Company { get; set; } = "";
    public decimal Balance { get; set; }
}

/// <summary>Virement unitaire. Contient aussi des champs INTERNES (ne pas exposer, sauf V010).</summary>
public sealed class Transfer
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public string Company { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string Iban { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Label { get; set; } = "";         // libelle (rendu SPA - V009)
    public string Status { get; set; } = BatchStatus.PendingApproval;
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Champs internes : usage back-office, JAMAIS destines au client. ---
    public decimal InternalCost { get; set; }       // cout bancaire interne
    public double FraudScore { get; set; }          // score anti-fraude
    public string InternalNote { get; set; } = "";  // commentaire back-office
}

public sealed class Batch
{
    public long Id { get; set; }
    public string Company { get; set; } = "";
    public string Label { get; set; } = "";
    public string Status { get; set; } = BatchStatus.Draft;
    public string Currency { get; set; } = "EUR";
    public string CreatedBy { get; set; } = "";
    public string? SubmittedBy { get; set; }
    public string? ValidatedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Transfer> Transfers { get; set; } = new();

    // --- Champs internes (exposes seulement par la faille V010). ---
    public decimal InternalCost { get; set; }
    public double FraudScore { get; set; }
    public string InternalNote { get; set; } = "";
}

public sealed class Beneficiary
{
    public long Id { get; set; }
    public string Company { get; set; } = "";
    public string Name { get; set; } = "";
    public string Iban { get; set; } = "";
    public bool Verified { get; set; }              // n'evolue que par le workflow (NC N005)
    public string CreatedBy { get; set; } = "";
    public string? ApprovedBy { get; set; }
}

public sealed class RefreshTokenRecord
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public bool Revoked { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed class ResetTokenRecord
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public bool Used { get; set; }                  // usage unique (NC N006)
    public DateTime ExpiresAt { get; set; }
}
