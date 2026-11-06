package com.lab.helios.web;

import com.lab.helios.domain.User;
import com.lab.helios.service.UserService;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;

import java.security.Principal;

/** Profil de l'utilisateur connecte : consultation et mise a jour. */
@Controller
public class ProfileController {

    private final UserService userService;

    public ProfileController(UserService userService) {
        this.userService = userService;
    }

    /** Page de profil (route benigne, rendu echappe via th:text). */
    @GetMapping("/profile")
    public String profile(Principal principal, Model model) {
        User u = userService.getByUsername(principal.getName());
        model.addAttribute("profile", u);
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "profile");
        return "profile";
    }

    /**
     * Mise a jour du profil.
     *
     * 
     * affiche et l'email, mais le controleur lie aussi un eventuel parametre "role" surajoute a la
     * requete et le transmet au service, ce qui permet d'elever le compte en ADMIN (voir
     * UserService.updateProfileInsecure).
     */
    @PostMapping("/profile/update")
    public String update(@RequestParam(required = false) String displayName,
                         @RequestParam(required = false) String email,
                         @RequestParam(required = false) String role,
                         Principal principal) {
        userService.updateProfileInsecure(principal.getName(), displayName, email, role);
        return "redirect:/profile";
    }

    /**
     * Mise a jour du profil par liaison restreinte.
     *
     * // NC: APP01-N008 — seuls displayName et email sont lies ; un parametre role surajoute est
     * ignore (voir UserService.updateProfileSecure).
     */
    @PostMapping("/profile/update-secure")
    public String updateSecure(@RequestParam(required = false) String displayName,
                               @RequestParam(required = false) String email,
                               Principal principal) {
        // NC: APP01-N008 — "role" volontairement absent de la signature (non liable)
        userService.updateProfileSecure(principal.getName(), displayName, email);
        return "redirect:/profile";
    }
}
