package com.lab.helios.repo;

import com.lab.helios.domain.PasswordReset;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface PasswordResetRepository extends JpaRepository<PasswordReset, Long> {
    Optional<PasswordReset> findTopByEmailOrderByCreatedAtDesc(String email);
}
