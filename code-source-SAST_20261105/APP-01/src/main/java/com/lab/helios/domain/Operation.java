package com.lab.helios.domain;

import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.LocalDate;

/** Operation (mouvement) fictive d'un compte. */
@Entity
@Table(name = "operation")
public class Operation {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "account_id", nullable = false)
    private String accountId;

    @Column(nullable = false)
    private String label;

    @Column(nullable = false)
    private String counterparty;

    @Column(nullable = false, precision = 15, scale = 2)
    private BigDecimal amount;

    @Column(name = "op_date", nullable = false)
    private LocalDate date;

    public Operation() {
    }

    public Operation(String accountId, String label, String counterparty, BigDecimal amount, LocalDate date) {
        this.accountId = accountId;
        this.label = label;
        this.counterparty = counterparty;
        this.amount = amount;
        this.date = date;
    }

    public Long getId() {
        return id;
    }

    public String getAccountId() {
        return accountId;
    }

    public String getLabel() {
        return label;
    }

    public String getCounterparty() {
        return counterparty;
    }

    public BigDecimal getAmount() {
        return amount;
    }

    public LocalDate getDate() {
        return date;
    }
}
