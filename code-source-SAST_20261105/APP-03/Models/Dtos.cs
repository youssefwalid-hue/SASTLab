// APP-03 Tresoria - DTOs d'entree/sortie (listes blanches sauf ecarts vulnerables).
namespace Tresoria.Models;

// ---- Auth ----
public sealed class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Otp { get; set; }
}

public sealed class RefreshRequest
{
    public string Refresh_token { get; set; } = "";
}

public sealed class PasswordResetRequest
{
    public string Username { get; set; } = "";
    public string Reset_token { get; set; } = "";
    public string New_password { get; set; } = "";
}

// ---- Batches ----
public sealed class CreateBatchRequest
{
    public string Label { get; set; } = "";
    public string? Currency { get; set; }
    public List<CreateTransferLine> Transfers { get; set; } = new();
}

public sealed class CreateTransferLine
{
    public string Iban { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Label { get; set; }
    public string? Currency { get; set; }
}

/// <summary>
/// DTO d'edition de lot en LISTE BLANCHE (chemin SUR, utilise par le NC implicite).
/// Le chemin VULNERABLE V005 lie, lui, le corps brut (y compris "status").
/// </summary>
public sealed class UpdateBatchDto
{
    public string? Label { get; set; }
    public string? Currency { get; set; }
}

// ---- Transfers ----
public sealed class CreateTransferRequest
{
    public string AccountId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Iban { get; set; } = "";
    public string? Label { get; set; }
    public string? Currency { get; set; }           // valide en liste blanche (NC N003)
    public long? BatchId { get; set; }
}

// ---- Beneficiaries ----
/// <summary>DTO d'edition de beneficiaire en LISTE BLANCHE : "verified" est volontairement absent (NC N005).</summary>
public sealed class UpdateBeneficiaryDto
{
    public string? Name { get; set; }
    public string? Iban { get; set; }
}

/// <summary>DTO de sortie beneficiaire : SANS champ interne (NC N005).</summary>
public sealed class BeneficiaryView
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Iban { get; set; } = "";
    public bool Verified { get; set; }
}

// ---- Enrichissement v2 : DTOs des nouveaux volets (V017..V022 / N008..N013 / surfaces benignes) ----

/// <summary>Demande de lien de reinitialisation (V018 / N009).</summary>
public sealed class ForgotPasswordRequest
{
    public string Username { get; set; } = "";
}

/// <summary>Import d'un rapport distant par URL (V022 / N013).</summary>
public sealed class ImportReportRequest
{
    public string Url { get; set; } = "";
}

/// <summary>
/// DTO de preferences en LISTE BLANCHE (chemin SUR, NC N011) : lie uniquement Theme/Language.
/// Le chemin VULNERABLE V020 deserialise, lui, le corps brut avec resolution de type ($type).
/// </summary>
public sealed class PreferencesDto
{
    public string? Theme { get; set; }
    public string? Language { get; set; }
}
