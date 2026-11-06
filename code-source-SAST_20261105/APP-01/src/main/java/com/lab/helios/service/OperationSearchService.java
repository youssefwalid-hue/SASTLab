package com.lab.helios.service;

import com.lab.helios.domain.Operation;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import jakarta.persistence.Query;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;

/**
 * Recherche d'operations.
 *
 * Deux filtres comparables :
 *  - par "label"       : ecart vulnerable APP01-V001 (concatenation SQL).
 *  - par "counterparty": negative control APP01-N001 (requete parametree sure).
 */
@Service
@Transactional(readOnly = true)
public class OperationSearchService {

    @PersistenceContext
    private EntityManager em;

    /**
     * Filtre les operations d'un compte par libelle.
     *
     * 
     * dans la clause WHERE d'une requete native, sans requete parametree ni validation.
     * Une charge comme x' OR '1'='1 neutralise le filtre.
     */
    @SuppressWarnings("unchecked")
    public List<Operation> searchByLabel(String accountId, String label) {
        String sql = "SELECT * FROM operation WHERE account_id = '" + accountId
                + "' AND label = '" + label + "'";
        Query query = em.createNativeQuery(sql, Operation.class);
        return (List<Operation>) query.getResultList();
    }

    /**
     * Filtre les operations d'un compte par contrepartie.
     *
     * // NC: APP01-N001 — requete parametree (binding) : la valeur est traitee comme un
     * litteral, aucune injection possible. Route sure comparable a APP01-V001.
     */
    @SuppressWarnings("unchecked")
    public List<Operation> searchByCounterparty(String accountId, String counterparty) {
        // NC: APP01-N001 — PreparedStatement / binding de parametres positionnels
        Query query = em.createNativeQuery(
                "SELECT * FROM operation WHERE account_id = ?1 AND counterparty = ?2", Operation.class);
        query.setParameter(1, accountId);
        query.setParameter(2, counterparty);
        return (List<Operation>) query.getResultList();
    }

    /** Lecture non filtree (parametree) pour l'affichage par defaut. */
    @SuppressWarnings("unchecked")
    public List<Operation> allForAccount(String accountId) {
        Query query = em.createNativeQuery(
                "SELECT * FROM operation WHERE account_id = ?1", Operation.class);
        query.setParameter(1, accountId);
        List<Operation> raw = query.getResultList();
        return new ArrayList<>(raw);
    }

    /** Petit utilitaire interne (non expose) conserve pour lisibilite des tests. */
    static BigDecimal zero() {
        return BigDecimal.ZERO;
    }

    static LocalDate today() {
        return LocalDate.now();
    }
}
