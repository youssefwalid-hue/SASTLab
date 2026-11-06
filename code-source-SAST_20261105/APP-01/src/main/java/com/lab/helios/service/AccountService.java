package com.lab.helios.service;

import com.lab.helios.domain.Account;
import com.lab.helios.repo.AccountRepository;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

/** Acces aux comptes. */
@Service
public class AccountService {

    private final AccountRepository accountRepository;

    public AccountService(AccountRepository accountRepository) {
        this.accountRepository = accountRepository;
    }

    public List<Account> accountsOf(String username) {
        return accountRepository.findByOwnerUsername(username);
    }

    /**
     * Lecture d'un compte par son identifiant SANS controle de propriete.
     *
     * 
     * authentifie ; tout utilisateur connecte peut lire le compte d'un tiers.
     */
    public Optional<Account> getByIdInsecure(String accountId) {
        return accountRepository.findById(accountId);
    }

    /**
     * Lecture d'un compte AVEC controle de propriete (comportement sur de reference).
     * Utilise par le resume API securise.
     *
     * // NC: APP01-N004 — verification de propriete : un compte non rattache a
     * l'utilisateur renvoie un acces refuse (403), aucune donnee tierce exposee.
     */
    public Account getByIdChecked(String accountId, String username) {
        // NC: APP01-N004 — controle d'autorisation au niveau objet
        Account account = accountRepository.findById(accountId)
                .orElseThrow(() -> new AccessDeniedException("Compte introuvable"));
        if (!account.getOwnerUsername().equals(username)) {
            throw new AccessDeniedException("Acces refuse au compte " + accountId);
        }
        return account;
    }

    public boolean isOwner(String accountId, String username) {
        return accountRepository.findById(accountId)
                .map(a -> a.getOwnerUsername().equals(username))
                .orElse(false);
    }
}
