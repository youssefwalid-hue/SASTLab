package com.lab.helios.web;

import com.lab.helios.seed.DataSeeder;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

/** Reinitialisation de l'etat de demonstration du laboratoire. */
@RestController
@RequestMapping("/admin")
public class AdminController {

    private final DataSeeder dataSeeder;

    public AdminController(DataSeeder dataSeeder) {
        this.dataSeeder = dataSeeder;
    }

    /** POST /admin/reset — re-initialise toutes les donnees fictives. */
    @PostMapping(value = "/reset", produces = MediaType.APPLICATION_JSON_VALUE)
    @ResponseBody
    public Map<String, Object> reset() {
        dataSeeder.reseed();
        return Map.of("status", "ok", "message", "Donnees de laboratoire reinitialisees");
    }
}
