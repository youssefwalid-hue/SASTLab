package com.lab.helios.seed;

import com.lab.helios.domain.*;
import com.lab.helios.repo.*;
import com.lab.helios.service.BeneficiaryService;
import com.lab.helios.service.TransferService;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.CommandLineRunner;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.io.IOException;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.Instant;
import java.time.LocalDate;
import java.time.temporal.ChronoUnit;

/**
 * Seed des donnees 100% fictives de laboratoire au demarrage.
 * Egalement invoque par POST /admin/reset pour re-initialiser l'etat.
 *
 * Comptes : alice/bruno (CLIENT), claire (ADVISOR), admin (ADMIN) — mot de passe Password1! (BCrypt).
 */
@Component
public class DataSeeder implements CommandLineRunner {

    private static final String LAB_PASSWORD = "Password1!";

    private final UserRepository userRepository;
    private final AccountRepository accountRepository;
    private final OperationRepository operationRepository;
    private final BeneficiaryRepository beneficiaryRepository;
    private final TransferRepository transferRepository;
    private final StatementRepository statementRepository;
    private final PasswordResetRepository passwordResetRepository;
    private final NotificationRepository notificationRepository;
    private final BeneficiaryService beneficiaryService;
    private final TransferService transferService;
    private final PasswordEncoder passwordEncoder;
    private final String statementsDir;

    public DataSeeder(UserRepository userRepository,
                      AccountRepository accountRepository,
                      OperationRepository operationRepository,
                      BeneficiaryRepository beneficiaryRepository,
                      TransferRepository transferRepository,
                      StatementRepository statementRepository,
                      PasswordResetRepository passwordResetRepository,
                      NotificationRepository notificationRepository,
                      BeneficiaryService beneficiaryService,
                      TransferService transferService,
                      PasswordEncoder passwordEncoder,
                      @Value("${helios.statements-dir}") String statementsDir) {
        this.userRepository = userRepository;
        this.accountRepository = accountRepository;
        this.operationRepository = operationRepository;
        this.beneficiaryRepository = beneficiaryRepository;
        this.transferRepository = transferRepository;
        this.statementRepository = statementRepository;
        this.passwordResetRepository = passwordResetRepository;
        this.notificationRepository = notificationRepository;
        this.beneficiaryService = beneficiaryService;
        this.transferService = transferService;
        this.passwordEncoder = passwordEncoder;
        this.statementsDir = statementsDir;
    }

    @Override
    public void run(String... args) {
        reseed();
    }

    @Transactional
    public void reseed() {
        // Purge
        notificationRepository.deleteAll();
        passwordResetRepository.deleteAll();
        transferRepository.deleteAll();
        operationRepository.deleteAll();
        beneficiaryRepository.deleteAll();
        statementRepository.deleteAll();
        accountRepository.deleteAll();
        userRepository.deleteAll();

        String hash = passwordEncoder.encode(LAB_PASSWORD);

        // --- Utilisateurs fictifs (emails/telephones 100% fictifs @helios.lab.exemple) -------------
        // Comptes deterministes attendus par le validateur : alice, bruno (CLIENT), claire (ADVISOR), admin (ADMIN).
        userRepository.save(new User("alice", hash, "alice@helios.lab.exemple", "+33 1 00 00 00 01", "Alice Martin", Role.CLIENT));
        userRepository.save(new User("bruno", hash, "bruno@helios.lab.exemple", "+33 1 00 00 00 02", "Bruno Lefevre", Role.CLIENT));
        userRepository.save(new User("claire", hash, "claire@helios.lab.exemple", "+33 1 00 00 00 03", "Claire Dubois", Role.ADVISOR));
        userRepository.save(new User("admin", hash, "admin@helios.lab.exemple", "+33 1 00 00 00 04", "Administrateur Lab", Role.ADMIN));
        // Volume additionnel : clients et conseiller supplementaires (donnees fictives)
        userRepository.save(new User("david", hash, "david@helios.lab.exemple", "+33 1 00 00 00 05", "David Petit", Role.CLIENT));
        userRepository.save(new User("emma", hash, "emma@helios.lab.exemple", "+33 1 00 00 00 06", "Emma Moreau", Role.CLIENT));
        userRepository.save(new User("farid", hash, "farid@helios.lab.exemple", "+33 1 00 00 00 07", "Farid Benali", Role.CLIENT));
        userRepository.save(new User("gabrielle", hash, "gabrielle@helios.lab.exemple", "+33 1 00 00 00 08", "Gabrielle Roux", Role.CLIENT));
        userRepository.save(new User("nadia", hash, "nadia@helios.lab.exemple", "+33 1 00 00 00 09", "Nadia Faure", Role.ADVISOR));

        // --- Comptes fictifs ----------------------------------------------------------------------
        // Deterministes : ACC-1001 (alice), ACC-2001 (bruno).
        accountRepository.save(new Account("ACC-1001", "alice", "Compte courant Alice",
                new BigDecimal("1240.50"), "EUR"));
        accountRepository.save(new Account("ACC-2001", "bruno", "Compte courant Bruno",
                new BigDecimal("8730.00"), "EUR"));
        // Volume additionnel : comptes epargne et comptes des nouveaux clients
        accountRepository.save(new Account("ACC-1002", "alice", "Livret epargne Alice",
                new BigDecimal("5600.00"), "EUR"));
        accountRepository.save(new Account("ACC-2002", "bruno", "Livret epargne Bruno",
                new BigDecimal("15200.75"), "EUR"));
        accountRepository.save(new Account("ACC-3001", "david", "Compte courant David",
                new BigDecimal("642.10"), "EUR"));
        accountRepository.save(new Account("ACC-4001", "emma", "Compte courant Emma",
                new BigDecimal("3310.00"), "EUR"));
        accountRepository.save(new Account("ACC-5001", "farid", "Compte courant Farid",
                new BigDecimal("980.40"), "EUR"));
        accountRepository.save(new Account("ACC-6001", "gabrielle", "Compte courant Gabrielle",
                new BigDecimal("12750.00"), "EUR"));

        // --- Operations fictives ------------------------------------------------------------------
        // Operations deterministes d'ACC-1001 / ACC-2001 (inchangees ; attendues par le validateur).
        operationRepository.save(new Operation("ACC-1001", "courses", "SARL Marche Bio",
                new BigDecimal("-54.20"), LocalDate.of(2026, 1, 8)));
        operationRepository.save(new Operation("ACC-1001", "salaire", "Employeur Fictif SA",
                new BigDecimal("2450.00"), LocalDate.of(2026, 1, 2)));
        operationRepository.save(new Operation("ACC-1001", "abonnement", "Transports Urbains",
                new BigDecimal("-75.00"), LocalDate.of(2026, 1, 5)));
        operationRepository.save(new Operation("ACC-1001", "restaurant", "Le Bistrot Fictif",
                new BigDecimal("-32.90"), LocalDate.of(2026, 1, 12)));
        operationRepository.save(new Operation("ACC-2001", "loyer", "Agence Immo Fictive",
                new BigDecimal("-1100.00"), LocalDate.of(2026, 1, 3)));
        operationRepository.save(new Operation("ACC-2001", "virement recu", "Client Fictif",
                new BigDecimal("3200.00"), LocalDate.of(2026, 1, 9)));
        // Volume additionnel (nouveaux mois / nouveaux comptes) — ne reutilise PAS les libelles/contreparties
        // deterministes des tests (ni "courses" sur ACC-1001, ni "Jean" en contrepartie).
        operationRepository.save(new Operation("ACC-1001", "assurance", "Mutuelle Fictive",
                new BigDecimal("-41.00"), LocalDate.of(2026, 2, 4)));
        operationRepository.save(new Operation("ACC-1001", "telephonie", "Operateur Fictif",
                new BigDecimal("-29.99"), LocalDate.of(2026, 2, 6)));
        operationRepository.save(new Operation("ACC-1002", "epargne", "Virement interne",
                new BigDecimal("300.00"), LocalDate.of(2026, 2, 1)));
        operationRepository.save(new Operation("ACC-2001", "salaire", "Employeur Fictif SA",
                new BigDecimal("3200.00"), LocalDate.of(2026, 2, 2)));
        operationRepository.save(new Operation("ACC-2002", "interets", "Helios Banque (lab)",
                new BigDecimal("12.40"), LocalDate.of(2026, 2, 28)));
        operationRepository.save(new Operation("ACC-3001", "courses", "Supermarche Fictif",
                new BigDecimal("-88.15"), LocalDate.of(2026, 2, 11)));
        operationRepository.save(new Operation("ACC-4001", "loyer", "Agence Immo Fictive",
                new BigDecimal("-890.00"), LocalDate.of(2026, 2, 3)));
        operationRepository.save(new Operation("ACC-5001", "restaurant", "Le Bistrot Fictif",
                new BigDecimal("-24.50"), LocalDate.of(2026, 2, 15)));
        operationRepository.save(new Operation("ACC-6001", "virement recu", "Notaire Fictif",
                new BigDecimal("9000.00"), LocalDate.of(2026, 2, 20)));

        // --- Beneficiaires fictifs (actifs : activatedAt dans le passe) ---------------------------
        // Deterministes : BEN-01, BEN-02 (alice), BEN-03 (bruno).
        Instant past = Instant.now().minus(30, ChronoUnit.DAYS);
        beneficiaryRepository.save(new Beneficiary("BEN-01", "alice", "Paul Durand",
                "FR7630001007941234567890185", "Ami", past));
        beneficiaryRepository.save(new Beneficiary("BEN-02", "alice", "Location Vacances Fictive",
                "FR7630004000031234567890143", "Caution", past));
        beneficiaryRepository.save(new Beneficiary("BEN-03", "bruno", "Sophie Bernard",
                "FR7612739000501234567890149", "Famille", past));
        // Volume additionnel
        beneficiaryRepository.save(new Beneficiary("BEN-04", "alice", "Marc Leroy",
                "FR7610096000301234567890120", "Collegue", past));
        beneficiaryRepository.save(new Beneficiary("BEN-05", "bruno", "Garage Fictif SARL",
                "FR7630002005001234567890167", "Entretien", past));
        beneficiaryRepository.save(new Beneficiary("BEN-06", "david", "Julie Martin",
                "FR7630003000401234567890188", "Soeur", past));
        beneficiaryRepository.save(new Beneficiary("BEN-07", "emma", "Ecole Fictive",
                "FR7612548029801234567890130", "Scolarite", past));
        beneficiaryRepository.save(new Beneficiary("BEN-08", "gabrielle", "Artisan Fictif",
                "FR7618206000201234567890145", "Travaux", past));
        beneficiaryService.resyncSequence();

        // --- Virements fictifs --------------------------------------------------------------------
        // Deterministe : TRF-01 (alice, EXECUTED).
        transferRepository.save(new Transfer("TRF-01", "alice", "ACC-1001", "BEN-01",
                new BigDecimal("120.00"), TransferStatus.EXECUTED, false,
                Instant.now().minus(10, ChronoUnit.DAYS)));
        // Volume additionnel : statuts varies (EXECUTED / PENDING_OTP)
        transferRepository.save(new Transfer("TRF-02", "alice", "ACC-1001", "BEN-02",
                new BigDecimal("450.00"), TransferStatus.EXECUTED, false,
                Instant.now().minus(7, ChronoUnit.DAYS)));
        transferRepository.save(new Transfer("TRF-03", "bruno", "ACC-2001", "BEN-03",
                new BigDecimal("75.00"), TransferStatus.PENDING_OTP, false,
                Instant.now().minus(2, ChronoUnit.DAYS)));
        transferRepository.save(new Transfer("TRF-04", "david", "ACC-3001", "BEN-06",
                new BigDecimal("200.00"), TransferStatus.EXECUTED, false,
                Instant.now().minus(5, ChronoUnit.DAYS)));
        transferRepository.save(new Transfer("TRF-05", "gabrielle", "ACC-6001", "BEN-08",
                new BigDecimal("1500.00"), TransferStatus.PENDING_OTP, false,
                Instant.now().minus(1, ChronoUnit.DAYS)));
        transferService.resyncSequence();

        // --- Releves fictifs (fichiers PDF de laboratoire) ----------------------------------------
        writeStatementsDir();
        statementRepository.save(new Statement("alice", "ACC-1001", "releve-2026-01.pdf", "2026-01"));
        statementRepository.save(new Statement("alice", "ACC-1001", "releve-2026-02.pdf", "2026-02"));
        statementRepository.save(new Statement("bruno", "ACC-2001", "releve-2026-01.pdf", "2026-01"));
        // Volume additionnel
        statementRepository.save(new Statement("bruno", "ACC-2001", "releve-2026-02.pdf", "2026-02"));
        statementRepository.save(new Statement("alice", "ACC-1002", "releve-2026-02.pdf", "2026-02"));
        statementRepository.save(new Statement("david", "ACC-3001", "releve-2026-02.pdf", "2026-02"));
        statementRepository.save(new Statement("emma", "ACC-4001", "releve-2026-02.pdf", "2026-02"));

        // --- Notifications fictives ---------------------------------------------------------------
        Instant now = Instant.now();
        notificationRepository.save(new Notification("alice", "Bienvenue sur votre espace Helios Banque (lab).",
                "information", now.minus(9, ChronoUnit.DAYS), false));
        notificationRepository.save(new Notification("alice", "Votre virement TRF-01 a ete execute.",
                "virement", now.minus(8, ChronoUnit.DAYS), false));
        notificationRepository.save(new Notification("alice", "Nouvelle connexion detectee sur votre compte.",
                "securite", now.minus(1, ChronoUnit.DAYS), true));
        notificationRepository.save(new Notification("bruno", "Votre releve de fevrier est disponible.",
                "information", now.minus(3, ChronoUnit.DAYS), true));
        notificationRepository.save(new Notification("bruno", "Un virement TRF-03 est en attente de validation OTP.",
                "virement", now.minus(2, ChronoUnit.DAYS), true));
        notificationRepository.save(new Notification("david", "Bienvenue sur votre espace Helios Banque (lab).",
                "information", now.minus(5, ChronoUnit.DAYS), false));
        notificationRepository.save(new Notification("gabrielle", "Un virement TRF-05 est en attente de validation OTP.",
                "virement", now.minus(1, ChronoUnit.DAYS), true));
    }

    /** Ecrit des PDF fictifs minimalistes dans le repertoire des releves. */
    private void writeStatementsDir() {
        try {
            Path dir = Paths.get(statementsDir);
            Files.createDirectories(dir);
            writeFakePdf(dir.resolve("releve-2026-01.pdf"), "Releve fictif - Janvier 2026 - Helios Banque (lab)");
            writeFakePdf(dir.resolve("releve-2026-02.pdf"), "Releve fictif - Fevrier 2026 - Helios Banque (lab)");
        } catch (IOException e) {
            throw new IllegalStateException("Impossible d'initialiser le repertoire des releves", e);
        }
    }

    private void writeFakePdf(Path path, String title) throws IOException {
        // PDF minimal valide (une page, un texte) — contenu 100% fictif.
        String pdf = "%PDF-1.4\n"
                + "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n"
                + "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n"
                + "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 595 842]"
                + "/Resources<</Font<</F1 5 0 R>>>>/Contents 4 0 R>>endobj\n"
                + "4 0 obj<</Length 70>>stream\n"
                + "BT /F1 14 Tf 60 760 Td (" + title + ") Tj ET\n"
                + "endstream endobj\n"
                + "5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n"
                + "trailer<</Root 1 0 R>>\n"
                + "%%EOF\n";
        Files.write(path, pdf.getBytes(StandardCharsets.ISO_8859_1));
    }
}
