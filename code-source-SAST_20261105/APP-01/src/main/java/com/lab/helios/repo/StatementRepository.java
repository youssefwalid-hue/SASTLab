package com.lab.helios.repo;

import com.lab.helios.domain.Statement;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface StatementRepository extends JpaRepository<Statement, Long> {
    List<Statement> findByOwnerUsername(String ownerUsername);
}
