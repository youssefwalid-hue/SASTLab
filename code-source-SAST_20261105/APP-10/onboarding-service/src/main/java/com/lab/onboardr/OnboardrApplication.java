package com.lab.onboardr;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * APP-10 OnboardR — service d'onboarding KYC du Banking DAST Benchmark Lab.
 * Laboratoire defensif, donnees 100% fictives. Securise par defaut ; seuls les
 * ecarts listes (GT: APP10-Vxxx) sont vulnerables.
 */
@SpringBootApplication
public class OnboardrApplication {
    public static void main(String[] args) {
        SpringApplication.run(OnboardrApplication.class, args);
    }
}
