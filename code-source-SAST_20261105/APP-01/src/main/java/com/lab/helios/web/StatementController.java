package com.lab.helios.web;

import com.lab.helios.service.StatementService;
import org.springframework.core.io.ByteArrayResource;
import org.springframework.core.io.Resource;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.Principal;

/** Releves : page de liste, telechargement par nom (vuln) et par identifiant (NC). */
@Controller
public class StatementController {

    private final StatementService statementService;

    public StatementController(StatementService statementService) {
        this.statementService = statementService;
    }

    @GetMapping("/statements")
    public String list(Principal principal, Model model) {
        model.addAttribute("statements", statementService.listOf(principal.getName()));
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "statements");
        return "statements";
    }

    /**
     * Telechargement d'un releve par nom de fichier.
     *
     * 
     * au repertoire de base sans canonicalisation (voir StatementService.resolveByFileNameInsecure).
     */
    @GetMapping("/statements/download")
    @ResponseBody
    public ResponseEntity<Resource> download(@RequestParam String file) {
        Path path = statementService.resolveByFileNameInsecure(file);
        try {
            byte[] data = Files.readAllBytes(path);
            ByteArrayResource resource = new ByteArrayResource(data);
            return ResponseEntity.ok()
                    .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"" + path.getFileName() + "\"")
                    .contentType(MediaType.APPLICATION_OCTET_STREAM)
                    .contentLength(data.length)
                    .body(resource);
        } catch (IOException e) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Fichier introuvable");
        }
    }

    /**
     * Telechargement d'un releve par identifiant interne.
     *
     * // NC: APP01-N003 — resolution par id indexe + controle de propriete + chemin confine ;
     * aucune traversee possible (voir StatementService.resolveByIdSecure).
     */
    @GetMapping("/statements/by-id/{statementId}")
    @ResponseBody
    public ResponseEntity<Resource> downloadById(@PathVariable Long statementId, Principal principal) {
        try {
            // NC: APP01-N003 — resolution sure par identifiant et controle de propriete
            Path path = statementService.resolveByIdSecure(statementId, principal.getName());
            byte[] data = Files.readAllBytes(path);
            ByteArrayResource resource = new ByteArrayResource(data);
            return ResponseEntity.ok()
                    .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"" + path.getFileName() + "\"")
                    .contentType(MediaType.APPLICATION_PDF)
                    .contentLength(data.length)
                    .body(resource);
        } catch (IOException e) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Releve introuvable");
        }
    }
}
