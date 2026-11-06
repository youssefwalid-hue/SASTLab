package com.lab.helios.web;

import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

import java.security.Principal;

/** Page d'aide / a propos (route benigne, contenu statique). */
@Controller
public class HelpController {

    @GetMapping("/help")
    public String help(Principal principal, Model model) {
        model.addAttribute("username", principal == null ? null : principal.getName());
        model.addAttribute("activeNav", "help");
        return "help";
    }
}
