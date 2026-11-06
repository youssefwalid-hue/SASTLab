package com.lab.helios.repo;

import com.lab.helios.domain.Beneficiary;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface BeneficiaryRepository extends JpaRepository<Beneficiary, String> {
    List<Beneficiary> findByOwnerUsername(String ownerUsername);

    // Requete derivee (parametree par Spring Data) : utilisee par la recherche benigne /beneficiaries/search.
    List<Beneficiary> findByOwnerUsernameAndNameContainingIgnoreCase(String ownerUsername, String name);
}
