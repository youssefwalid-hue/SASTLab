package com.lab.helios.domain;

import jakarta.persistence.*;
import java.time.Instant;

/** Beneficiaire de virement fictif. */
@Entity
@Table(name = "beneficiary")
public class Beneficiary {

    @Id
    @Column(name = "ben_id")
    private String id; // ex. BEN-01

    @Column(nullable = false)
    private String ownerUsername;

    @Column(nullable = false)
    private String name;

    @Column(nullable = false)
    private String iban;

    @Column(length = 500)
    private String note;

    /**
     * Date d'activation : un beneficiaire n'est utilisable pour un virement qu'apres ce delai.
     * Champ gere cote serveur dans le flux legitime. Voir ecart APP01-V008 / NC APP01-N007.
     */
    @Column(name = "activated_at")
    private Instant activatedAt;

    public Beneficiary() {
    }

    public Beneficiary(String id, String ownerUsername, String name, String iban, String note, Instant activatedAt) {
        this.id = id;
        this.ownerUsername = ownerUsername;
        this.name = name;
        this.iban = iban;
        this.note = note;
        this.activatedAt = activatedAt;
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

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getIban() {
        return iban;
    }

    public void setIban(String iban) {
        this.iban = iban;
    }

    public String getNote() {
        return note;
    }

    public void setNote(String note) {
        this.note = note;
    }

    public Instant getActivatedAt() {
        return activatedAt;
    }

    public void setActivatedAt(Instant activatedAt) {
        this.activatedAt = activatedAt;
    }

    @Transient
    public boolean isActive() {
        return activatedAt != null && activatedAt.isBefore(Instant.now());
    }
}
