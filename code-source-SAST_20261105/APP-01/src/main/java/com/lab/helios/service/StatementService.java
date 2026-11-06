package com.lab.helios.service;

import com.lab.helios.domain.Statement;
import com.lab.helios.repo.StatementRepository;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;

/** Acces aux releves PDF fictifs. */
@Service
public class StatementService {

    private final StatementRepository repository;
    private final String statementsDir;

    public StatementService(StatementRepository repository,
                            @Value("${helios.statements-dir}") String statementsDir) {
        this.repository = repository;
        this.statementsDir = statementsDir;
    }

    public List<Statement> listOf(String username) {
        return repository.findByOwnerUsername(username);
    }

    public String statementsDir() {
        return statementsDir;
    }

    /**
     * Resout un releve par nom de fichier pour telechargement.
     *
     * 
     * repertoire de base sans canonicalisation ni verification que le chemin resolu reste
     * dans le dossier autorise. Une sequence ../ permet de sortir du repertoire des releves.
     */
    public Path resolveByFileNameInsecure(String fileName) {
        return Paths.get(statementsDir, fileName);
    }

    /**
     * Resout un releve par identifiant interne, avec controle de propriete.
     *
     * // NC: APP01-N003 — resolution par identifiant indexe + verification de propriete ;
     * le nom de fichier n'est jamais derive d'une entree utilisateur, aucune traversee possible.
     */
    public Path resolveByIdSecure(Long statementId, String username) throws IOException {
        // NC: APP01-N003 — resolution par id interne, controle de propriete, chemin canonicalise et confine
        Statement st = repository.findById(statementId)
                .orElseThrow(() -> new AccessDeniedException("Releve introuvable"));
        if (!st.getOwnerUsername().equals(username)) {
            throw new AccessDeniedException("Acces refuse au releve " + statementId);
        }
        Path base = Paths.get(statementsDir).toRealPath();
        Path resolved = base.resolve(st.getFileName()).normalize();
        if (!resolved.startsWith(base)) {
            throw new AccessDeniedException("Chemin hors du repertoire autorise");
        }
        return resolved;
    }

    public byte[] read(Path path) throws IOException {
        return Files.readAllBytes(path);
    }
}
