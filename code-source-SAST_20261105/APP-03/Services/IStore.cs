using Tresoria.Models;

namespace Tresoria.Services;

/// <summary>
/// Abstraction de persistance. L'implementation par defaut est EN MEMOIRE (profils local / standalone).
/// En profil `prod`, une implementation SQL Server (lots, virements, beneficiaires) + Redis
/// (refresh tokens, cumuls quotidiens) brancherait la meme interface via DB_URL / REDIS_URL.
/// Seule la couche d'acces aux donnees change : la logique metier (et ses ecarts volontaires) reste identique.
/// </summary>
public interface IStore
{
    void Reset();

    // Users
    User? FindUser(string username);
    void SetPassword(string username, string password);

    // Companies / accounts
    Company? FindCompany(string id);
    Account? FindAccount(string id);
    IEnumerable<Account> Accounts();

    // Batches
    long NextBatchId(string company);
    void AddBatch(Batch b);
    Batch? GetBatch(long id);
    IEnumerable<Batch> Batches();
    void RemoveBatch(long id);

    // Transfers
    long NextTransferId();
    void AddTransfer(Transfer t);
    Transfer? GetTransfer(long id);
    IEnumerable<Transfer> Transfers();
    decimal DailyTotal(string accountId);           // cumul du jour pour un compte

    // Beneficiaries
    Beneficiary? GetBeneficiary(long id);
    IEnumerable<Beneficiary> Beneficiaries();        // listing cloisonne (surface benigne)

    // Refresh tokens
    void AddRefreshToken(RefreshTokenRecord r);
    RefreshTokenRecord? GetRefreshToken(string token);
    void RevokeRefreshTokensForUser(string username);

    // Reset tokens
    ResetTokenRecord? GetResetToken(string token);
}
