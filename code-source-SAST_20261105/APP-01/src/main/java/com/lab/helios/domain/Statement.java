package com.lab.helios.domain;

import jakarta.persistence.*;

/** Releve mensuel fictif (fichier PDF de laboratoire). */
@Entity
@Table(name = "statement")
public class Statement {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private String ownerUsername;

    @Column(nullable = false)
    private String accountId;

    /** Nom de fichier logique du releve (ex. releve-2026-01.pdf). */
    @Column(nullable = false)
    private String fileName;

    @Column(nullable = false)
    private String period;

    public Statement() {
    }

    public Statement(String ownerUsername, String accountId, String fileName, String period) {
        this.ownerUsername = ownerUsername;
        this.accountId = accountId;
        this.fileName = fileName;
        this.period = period;
    }

    public Long getId() {
        return id;
    }

    public String getOwnerUsername() {
        return ownerUsername;
    }

    public String getAccountId() {
        return accountId;
    }

    public String getFileName() {
        return fileName;
    }

    public String getPeriod() {
        return period;
    }
}
