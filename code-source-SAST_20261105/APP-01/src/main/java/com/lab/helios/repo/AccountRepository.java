package com.lab.helios.repo;

import com.lab.helios.domain.Account;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface AccountRepository extends JpaRepository<Account, String> {
    List<Account> findByOwnerUsername(String ownerUsername);
}
