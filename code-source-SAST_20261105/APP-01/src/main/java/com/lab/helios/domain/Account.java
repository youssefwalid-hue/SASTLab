package com.lab.helios.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;

/** Compte bancaire fictif (identifiant metier ACC-xxxx). */
@Entity
@Table(name = "account")
public class Account {

    @Id
    @Column(name = "account_id")
    private String accountId; // ex. ACC-1001

    @Column(nullable = false)
    private String ownerUsername;

    @Column(nullable = false)
    private String label;

    @Column(nullable = false, precision = 15, scale = 2)
    private BigDecimal balance;

    @Column(nullable = false)
    private String currency;

    public Account() {
    }

    public Account(String accountId, String ownerUsername, String label, BigDecimal balance, String currency) {
        this.accountId = accountId;
        this.ownerUsername = ownerUsername;
        this.label = label;
        this.balance = balance;
        this.currency = currency;
    }

    public String getAccountId() {
        return accountId;
    }

    public void setAccountId(String accountId) {
        this.accountId = accountId;
    }

    public String getOwnerUsername() {
        return ownerUsername;
    }

    public void setOwnerUsername(String ownerUsername) {
        this.ownerUsername = ownerUsername;
    }

    public String getLabel() {
        return label;
    }

    public void setLabel(String label) {
        this.label = label;
    }

    public BigDecimal getBalance() {
        return balance;
    }

    public void setBalance(BigDecimal balance) {
        this.balance = balance;
    }

    public String getCurrency() {
        return currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency;
    }
}
