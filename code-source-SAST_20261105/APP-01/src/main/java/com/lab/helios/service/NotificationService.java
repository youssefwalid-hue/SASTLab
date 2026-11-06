package com.lab.helios.service;

import com.lab.helios.domain.Notification;
import com.lab.helios.repo.NotificationRepository;
import org.springframework.stereotype.Service;

import java.util.List;

/** Acces aux notifications fictives de l'utilisateur (route benigne /notifications). */
@Service
public class NotificationService {

    private final NotificationRepository repository;

    public NotificationService(NotificationRepository repository) {
        this.repository = repository;
    }

    /** Notifications de l'utilisateur, triees de la plus recente a la plus ancienne. */
    public List<Notification> listOf(String username) {
        return repository.findByOwnerUsernameOrderByCreatedAtDesc(username);
    }
}
