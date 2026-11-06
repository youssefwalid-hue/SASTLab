package com.lab.helios;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * APP-01 Helios Banque — application bancaire de laboratoire du Banking DAST Benchmark Lab.
 *
 * Donnees 100% fictives, usage defensif (test de scanners DAST en laboratoire interne).
 * L'application est SECURISEE PAR DEFAUT ; les 22 vulnerabilites du corpus sont des ecarts
 * localises taggues "// GT: APP01-Vxxx". Les 13 negative controls sont taggues "// NC: APP01-Nxxx".
 */
@SpringBootApplication
public class HeliosApplication {
    public static void main(String[] args) {
        SpringApplication.run(HeliosApplication.class, args);
    }
}
