package com.lab.helios.web;

import com.lab.helios.service.AccountService;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

import java.security.Principal;

/** Tableau de bord : liste des comptes de l'utilisateur connecte. */
@Controller
public class DashboardController {

    private final AccountService accountService;

    public DashboardController(AccountService accountService) {
        this.accountService = accountService;
    }

    @GetMapping("/")
    public String dashboard(Principal principal, Model model) {
        model.addAttribute("username", principal.getName());
        model.addAttribute("accounts", accountService.accountsOf(principal.getName()));
        model.addAttribute("activeNav", "dashboard");
        return "dashboard";
    }
}
