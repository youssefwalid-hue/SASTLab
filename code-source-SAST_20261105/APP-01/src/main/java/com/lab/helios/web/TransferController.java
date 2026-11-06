package com.lab.helios.web;

import com.lab.helios.service.AccountService;
import com.lab.helios.service.BeneficiaryService;
import com.lab.helios.service.TransferService;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;

import java.math.BigDecimal;
import java.security.Principal;

/** Virements : creation, validation OTP, confirmation (vuln) et finalisation (NC). */
@Controller
public class TransferController {

    private final TransferService transferService;
    private final BeneficiaryService beneficiaryService;
    private final AccountService accountService;

    public TransferController(TransferService transferService,
                             BeneficiaryService beneficiaryService,
                             AccountService accountService) {
        this.transferService = transferService;
        this.beneficiaryService = beneficiaryService;
        this.accountService = accountService;
    }

    @GetMapping("/transfers")
    public String list(Principal principal, Model model) {
        model.addAttribute("transfers", transferService.listOf(principal.getName()));
        model.addAttribute("beneficiaries", beneficiaryService.listOf(principal.getName()));
        model.addAttribute("accounts", accountService.accountsOf(principal.getName()));
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "transfers");
        return "transfer";
    }

    @PostMapping("/transfers")
    public String create(@RequestParam String sourceAccount,
                        @RequestParam String beneficiaryId,
                        @RequestParam BigDecimal amount,
                        Principal principal) {
        transferService.create(principal.getName(), sourceAccount, beneficiaryId, amount);
        return "redirect:/transfers";
    }

    /** Etape legitime de validation du challenge OTP. */
    @PostMapping("/transfers/{id}/validate-otp")
    public String validateOtp(@PathVariable String id,
                             @RequestParam String otp,
                             Principal principal) {
        transferService.validateOtp(id, principal.getName(), otp);
        return "redirect:/transfers";
    }

    /**
     * Confirmation de virement.
     *
     * 
     * ni l'OTP soumis (voir TransferService.confirmInsecure).
     */
    @PostMapping("/transfers/{id}/confirm")
    public String confirm(@PathVariable String id,
                         @RequestParam(required = false, defaultValue = "") String otp,
                         Principal principal) {
        transferService.confirmInsecure(id, principal.getName(), otp);
        return "redirect:/transfers";
    }

    /**
     * Finalisation de virement.
     *
     * // NC: APP01-N005 — l'OTP et le drapeau otpValidated sont revifies avant execution
     * (voir TransferService.finalizeSecure).
     */
    @PostMapping("/transfers/{id}/finalize")
    public String finalizeTransfer(@PathVariable String id,
                                  @RequestParam(required = false, defaultValue = "") String otp,
                                  Principal principal) {
        // NC: APP01-N005 — revification de l'OTP avant execution
        transferService.finalizeSecure(id, principal.getName(), otp);
        return "redirect:/transfers";
    }
}
