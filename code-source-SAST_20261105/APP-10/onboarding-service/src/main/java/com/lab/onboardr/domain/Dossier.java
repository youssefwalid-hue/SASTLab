package com.lab.onboardr.domain;

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * Dossier KYC fictif. Contient des champs publics (vus par le prospect) et des champs
 * de back-office (reviewerNotes, riskScore, internalStatus) reserves a la conformite.
 */
public class Dossier {
    public long id;
    public String owner;          // username du prospect proprietaire
    public String subject;        // identite fictive (nom affiche)
    public String email;          // email fictif @onboardr.lab.exemple
    public String phone;
    public String address;
    public String status;         // PENDING / APPROVED / REJECTED
    public boolean otpVerified;   // etape de verification OTP franchie
    public String decision;       // decision de conformite appliquee (null si aucune)
    public String decidedBy;      // qui a pris la decision

    // Champs de back-office (ne doivent jamais apparaitre dans la reponse publique)
    public String reviewerNotes;
    public int riskScore;
    public String internalStatus;

    public Dossier(long id, String owner, String subject, String phone, String address) {
        this.id = id;
        this.owner = owner;
        this.subject = subject;
        this.email = "";
        this.phone = phone;
        this.address = address;
        this.status = "PENDING";
        this.otpVerified = false;
        this.decision = null;
        this.decidedBy = null;
        this.reviewerNotes = "RAS — dossier fictif de laboratoire";
        this.riskScore = 10;
        this.internalStatus = "IN_REVIEW";
    }

    /** Vue publique : champs destines au prospect uniquement. */
    public Map<String, Object> publicView() {
        Map<String, Object> m = new LinkedHashMap<>();
        m.put("id", id);
        m.put("subject", subject);
        m.put("phone", phone);
        m.put("address", address);
        m.put("status", status);
        m.put("otpVerified", otpVerified);
        m.put("decision", decision);
        return m;
    }

    /** Vue de back-office : inclut les champs internes (route interne / conformite). */
    public Map<String, Object> internalView() {
        Map<String, Object> m = publicView();
        m.put("owner", owner);
        m.put("email", email);
        m.put("reviewerNotes", reviewerNotes);
        m.put("riskScore", riskScore);
        m.put("internalStatus", internalStatus);
        m.put("decidedBy", decidedBy);
        return m;
    }

    /** Resume de liste. */
    public Map<String, Object> summaryView() {
        Map<String, Object> m = new LinkedHashMap<>();
        m.put("id", id);
        m.put("subject", subject);
        m.put("status", status);
        return m;
    }
}
