package com.lab.helios.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.Instant;

/** Virement fictif soumis a validation OTP. */
@Entity
@Table(name = "transfer")
public class Transfer {

    @Id
    @Column(name = "transfer_id")
    private String id; // ex. TRF-77

    @Column(nullable = false)
    private String ownerUsername;

    @Column(nullable = false)
    private String sourceAccount;

    @Column(nullable = false)
    private String beneficiaryId;

    @Column(nullable = false, precision = 15, scale = 2)
    private BigDecimal amount;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private TransferStatus status;

    /** Drapeau metier : l'OTP du challenge courant a-t-il ete valide. */
    @Column(nullable = false)
    private boolean otpValidated;

    @Column(nullable = false)
    private Instant createdAt;

    public Transfer() {
    }

    public Transfer(String id, String ownerUsername, String sourceAccount, String beneficiaryId,
                    BigDecimal amount, TransferStatus status, boolean otpValidated, Instant createdAt) {
        this.id = id;
        this.ownerUsername = ownerUsername;
        this.sourceAccount = sourceAccount;
        this.beneficiaryId = beneficiaryId;
        this.amount = amount;
        this.status = status;
        this.otpValidated = otpValidated;
        this.createdAt = createdAt;
    }

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getOwnerUsername() {
        return ownerUsername;
    }

    public void setOwnerUsername(String ownerUsername) {
        this.ownerUsername = ownerUsername;
    }

    public String getSourceAccount() {
        return sourceAccount;
    }

    public void setSourceAccount(String sourceAccount) {
        this.sourceAccount = sourceAccount;
    }

    public String getBeneficiaryId() {
        return beneficiaryId;
    }

    public void setBeneficiaryId(String beneficiaryId) {
        this.beneficiaryId = beneficiaryId;
    }

    public BigDecimal getAmount() {
        return amount;
    }

    public void setAmount(BigDecimal amount) {
        this.amount = amount;
    }

    public TransferStatus getStatus() {
        return status;
    }

    public void setStatus(TransferStatus status) {
        this.status = status;
    }

    public boolean isOtpValidated() {
        return otpValidated;
    }

    public void setOtpValidated(boolean otpValidated) {
        this.otpValidated = otpValidated;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(Instant createdAt) {
        this.createdAt = createdAt;
    }
}
