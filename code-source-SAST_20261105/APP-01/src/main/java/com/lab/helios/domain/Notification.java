package com.lab.helios.domain;

import jakarta.persistence.*;
import java.time.Instant;

/** Notification applicative fictive affichee a l'utilisateur (volume de donnees de laboratoire). */
@Entity
@Table(name = "notification")
public class Notification {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private String ownerUsername;

    @Column(nullable = false, length = 500)
    private String message;

    /** Categorie libre (ex. securite, virement, information). */
    @Column(nullable = false)
    private String category;

    @Column(name = "created_at", nullable = false)
    private Instant createdAt;

    @Column(nullable = false)
    private boolean unread;

    public Notification() {
    }

    public Notification(String ownerUsername, String message, String category, Instant createdAt, boolean unread) {
        this.ownerUsername = ownerUsername;
        this.message = message;
        this.category = category;
        this.createdAt = createdAt;
        this.unread = unread;
    }

    public Long getId() {
        return id;
    }

    public String getOwnerUsername() {
        return ownerUsername;
    }

    public String getMessage() {
        return message;
    }

    public String getCategory() {
        return category;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public boolean isUnread() {
        return unread;
    }
}
