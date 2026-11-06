using System.Collections.Concurrent;
using Tresoria.Models;

namespace Tresoria.Services;

/// <summary>
/// Depot EN MEMOIRE (profils local / standalone). Thread-safe pour la plupart des acces.
/// NB : l'ecriture des virements est volontairement NON ATOMIQUE sur le cumul quotidien
/// (voir TransfersController - GT: APP03-V003) ; ce depot expose donc une lecture/ecriture
/// separee du cumul, comme le ferait un back-end non serialise.
/// </summary>
public sealed class MemoryStore : IStore
{
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, User> _users = new();
    private readonly ConcurrentDictionary<string, Company> _companies = new();
    private readonly ConcurrentDictionary<string, Account> _accounts = new();
    private readonly ConcurrentDictionary<long, Batch> _batches = new();
    private readonly ConcurrentDictionary<long, Transfer> _transfers = new();
    private readonly ConcurrentDictionary<long, Beneficiary> _beneficiaries = new();
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _refresh = new();
    private readonly ConcurrentDictionary<string, ResetTokenRecord> _reset = new();

    private readonly ConcurrentDictionary<string, long> _batchSeq = new();
    private long _transferSeq;

    public MemoryStore() => Reset();

    public void Reset()
    {
        lock (_lock)
        {
            _users.Clear(); _companies.Clear(); _accounts.Clear();
            _batches.Clear(); _transfers.Clear(); _beneficiaries.Clear();
            _refresh.Clear(); _reset.Clear(); _batchSeq.Clear();
            _transferSeq = 7000;
            Seed.Load(this, _users, _companies, _accounts, _batches, _transfers,
                      _beneficiaries, _reset, _batchSeq, ref _transferSeq);
        }
    }

    public User? FindUser(string username) => _users.TryGetValue(username, out var u) ? u : null;
    public void SetPassword(string username, string password)
    {
        if (_users.TryGetValue(username, out var u)) u.Password = password;
    }

    public Company? FindCompany(string id) => _companies.TryGetValue(id, out var c) ? c : null;
    public Account? FindAccount(string id) => _accounts.TryGetValue(id, out var a) ? a : null;
    public IEnumerable<Account> Accounts() => _accounts.Values;

    public long NextBatchId(string company)
    {
        lock (_lock)
        {
            // A : 9001.. ; B : 9005..  (identifiants sequentiels fictifs)
            long start = company == "B" ? 9005 : 9001;
            long cur = _batchSeq.AddOrUpdate(company, start, (_, v) => v + 1);
            return cur;
        }
    }
    public void AddBatch(Batch b) => _batches[b.Id] = b;
    public Batch? GetBatch(long id) => _batches.TryGetValue(id, out var b) ? b : null;
    public IEnumerable<Batch> Batches() => _batches.Values;
    public void RemoveBatch(long id) => _batches.TryRemove(id, out _);

    public long NextTransferId() { lock (_lock) { return ++_transferSeq; } }
    public void AddTransfer(Transfer t) => _transfers[t.Id] = t;
    public Transfer? GetTransfer(long id) => _transfers.TryGetValue(id, out var t) ? t : null;
    public IEnumerable<Transfer> Transfers() => _transfers.Values;

    public decimal DailyTotal(string accountId)
    {
        var today = DateTime.UtcNow.Date;
        return _transfers.Values
            .Where(t => t.AccountId == accountId && t.CreatedAt.Date == today)
            .Sum(t => t.Amount);
    }

    public Beneficiary? GetBeneficiary(long id) => _beneficiaries.TryGetValue(id, out var b) ? b : null;
    public IEnumerable<Beneficiary> Beneficiaries() => _beneficiaries.Values;

    public void AddRefreshToken(RefreshTokenRecord r) => _refresh[r.Token] = r;
    public RefreshTokenRecord? GetRefreshToken(string token) => _refresh.TryGetValue(token, out var r) ? r : null;
    public void RevokeRefreshTokensForUser(string username)
    {
        foreach (var r in _refresh.Values.Where(x => x.Username == username)) r.Revoked = true;
    }

    public ResetTokenRecord? GetResetToken(string token) => _reset.TryGetValue(token, out var r) ? r : null;
}
