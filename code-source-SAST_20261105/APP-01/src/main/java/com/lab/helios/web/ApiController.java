package com.lab.helios.web;

import com.lab.helios.domain.Account;
import com.lab.helios.service.AccountService;
import com.lab.helios.service.UserService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.http.ResponseEntity;
import org.springframework.util.StringUtils;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.server.ResponseStatusException;
import org.springframework.http.HttpStatus;

import java.security.Principal;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

/** API REST legere (session). */
@RestController
@RequestMapping("/api")
public class ApiController {

    /** Origines autorisees cote serveur pour la route CORS sure (NC APP01-N009). */
    private static final List<String> CORS_ALLOWLIST = List.of("https://helios.lab.exemple");

    private final AccountService accountService;
    private final UserService userService;

    public ApiController(AccountService accountService, UserService userService) {
        this.accountService = accountService;
        this.userService = userService;
    }

    /**
     * Solde d'un compte.
     *
     * 
     * verifier qu'il appartient a l'appelant (voir AccountService.getByIdInsecure).
     */
    @GetMapping("/accounts/{accountId}/balance")
    public Map<String, Object> balance(@PathVariable String accountId) {
        Optional<Account> account = accountService.getByIdInsecure(accountId);
        if (account.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Compte introuvable");
        }
        Account a = account.get();
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("accountId", a.getAccountId());
        body.put("balance", a.getBalance());
        body.put("currency", a.getCurrency());
        return body;
    }

    /**
     * Resume d'un compte.
     *
     * // NC: APP01-N004 — l'accountId est lie a l'utilisateur de la session ; un compte tiers
     * renvoie 403 (voir AccountService.getByIdChecked).
     */
    @GetMapping("/accounts/{accountId}/summary")
    public ResponseEntity<Map<String, Object>> summary(@PathVariable String accountId, Principal principal) {
        // NC: APP01-N004 — controle de propriete au niveau objet
        Account a = accountService.getByIdChecked(accountId, principal.getName());
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("accountId", a.getAccountId());
        body.put("label", a.getLabel());
        body.put("balance", a.getBalance());
        body.put("currency", a.getCurrency());
        return ResponseEntity.ok(body);
    }

    /**
     * Profil de l'utilisateur de la session, consommable par un widget tiers.
     *
     * 
     * quel dans Access-Control-Allow-Origin ET Access-Control-Allow-Credentials est positionne a
     * true. N'importe quelle origine peut donc lire la reponse authentifiee de la victime depuis
     * un contexte cross-site (CWE-942).
     */
    @GetMapping("/profile")
    public Map<String, Object> profile(Principal principal, HttpServletRequest request, HttpServletResponse response) {
        String origin = request.getHeader("Origin");
        if (StringUtils.hasText(origin)) {
            response.setHeader("Access-Control-Allow-Origin", origin);
            response.setHeader("Access-Control-Allow-Credentials", "true");
            response.setHeader("Vary", "Origin");
        }
        return userService.exposeMinimalSecure(principal.getName());
    }

    /**
     * Variante sure du profil cross-site.
     *
     * // NC: APP01-N009 — CORS sur allowlist : seule une origine explicitement autorisee cote
     * serveur est renvoyee dans Access-Control-Allow-Origin ; une origine inconnue n'obtient
     * aucun en-tete CORS et ne peut lire la reponse.
     */
    @GetMapping("/profile-cors-secure")
    public Map<String, Object> profileCorsSecure(Principal principal, HttpServletRequest request, HttpServletResponse response) {
        String origin = request.getHeader("Origin");
        // NC: APP01-N009 — reflexion uniquement si l'origine figure dans l'allowlist serveur
        if (StringUtils.hasText(origin) && CORS_ALLOWLIST.contains(origin)) {
            response.setHeader("Access-Control-Allow-Origin", origin);
            response.setHeader("Access-Control-Allow-Credentials", "true");
            response.setHeader("Vary", "Origin");
        }
        return userService.exposeMinimalSecure(principal.getName());
    }

    /**
     * Fiche utilisateur renvoyee a l'appelant.
     *
     * 
     * y compris des champs internes et sensibles (hash de mot de passe, role, identifiant interne)
     * qui ne devraient jamais sortir de l'API (CWE-213).
     */
    @GetMapping("/users/{username}")
    public Map<String, Object> userDetail(@PathVariable String username) {
        return userService.exposeFullInsecure(username);
    }

    /**
     * Fiche utilisateur par projection minimale.
     *
     * // NC: APP01-N013 — DTO d'affichage restreint : aucun champ interne ni sensible n'est expose.
     */
    @GetMapping("/users-secure/{username}")
    public Map<String, Object> userDetailSecure(@PathVariable String username) {
        // NC: APP01-N013 — projection explicite vers un DTO (pas de hash, pas de role, pas d'id interne)
        return userService.exposeMinimalSecure(username);
    }
}
