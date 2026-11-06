package com.lab.helios.repo;

import com.lab.helios.domain.Transfer;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface TransferRepository extends JpaRepository<Transfer, String> {
    List<Transfer> findByOwnerUsername(String ownerUsername);
}
