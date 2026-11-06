package com.lab.helios.web;

import org.springframework.expression.Expression;
import org.springframework.expression.ExpressionParser;
import org.springframework.expression.spel.standard.SpelExpressionParser;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * Petit utilitaire de "formules" de laboratoire (calcul d'un montant a partir d'une expression).
 * Comparaison directe entre evaluation d'expression (ecart) et echo litteral (controle sur).
 */
@RestController
@RequestMapping("/tools")
public class ToolsController {

    /**
     * Evalue une formule fournie par l'utilisateur.
     *
     * 
     * evaluee par SpelExpressionParser. Une charge SpEL (acces de type, appel de methode) est
     * donc executee cote serveur (CWE-917 / expression language injection). Les charges ne ciblent
     * que l'hote de laboratoire.
     */
    @GetMapping("/evaluate")
    public Map<String, Object> evaluate(@RequestParam(required = false, defaultValue = "") String expr) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("expr", expr);
        try {
            ExpressionParser parser = new SpelExpressionParser();
            Expression expression = parser.parseExpression(expr);
            Object value = expression.getValue();
            body.put("result", String.valueOf(value));
        } catch (Exception e) {
            body.put("result", "");
            body.put("error", e.getClass().getSimpleName());
        }
        return body;
    }

    /**
     * Variante sure : la formule n'est jamais evaluee comme expression.
     *
     * // NC: APP01-N012 — la valeur est traitee comme une chaine litterale inerte (aucune
     * evaluation d'expression) ; seule une valeur strictement numerique est reconnue comme nombre.
     */
    @GetMapping("/evaluate-safe")
    public Map<String, Object> evaluateSafe(@RequestParam(required = false, defaultValue = "") String expr) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("expr", expr);
        // NC: APP01-N012 — pas de moteur d'expression ; la valeur reste un litteral
        if (expr.matches("-?\\d{1,15}")) {
            body.put("result", expr);
        } else {
            body.put("result", expr);
            body.put("note", "valeur traitee comme litteral (aucune evaluation)");
        }
        return body;
    }
}
