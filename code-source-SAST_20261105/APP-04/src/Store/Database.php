<?php
namespace App\Store;

/**
 * Depot SQLite (profil standalone/local). Dans un deploiement `prod` reel,
 * le DSN pointe vers RDS MySQL (meme schema, meme logique metier et memes ecarts).
 * DONNEES 100% FICTIVES - aucun secret reel.
 */
final class Database
{
    private \PDO $pdo;
    private string $path;

    public function __construct(string $path)
    {
        $this->path = $path;
        $dir = dirname($path);
        if (!is_dir($dir)) {
            @mkdir($dir, 0770, true);
        }
        $this->pdo = new \PDO('sqlite:' . $path);
        $this->pdo->setAttribute(\PDO::ATTR_ERRMODE, \PDO::ERRMODE_EXCEPTION);
        $this->pdo->setAttribute(\PDO::ATTR_DEFAULT_FETCH_MODE, \PDO::FETCH_ASSOC);
        if (!$this->isSeeded()) {
            $this->seed();
        }
    }

    public function pdo(): \PDO
    {
        return $this->pdo;
    }

    private function isSeeded(): bool
    {
        try {
            $this->pdo->query('SELECT 1 FROM users LIMIT 1');
            return true;
        } catch (\Throwable $e) {
            return false;
        }
    }

    /** Reinitialise integralement l'etat de demonstration (POST /admin/reset). */
    public function reset(): void
    {
        foreach (['notes', 'files', 'clients', 'users', 'audit'] as $t) {
            $this->pdo->exec('DROP TABLE IF EXISTS ' . $t);
        }
        $this->seed();
    }

    public function seed(): void
    {
        // Colonnes email / phone / bio ajoutees pour le volume de donnees fictives et la
        // surface de profil (mass assignment APP04-V019). Emails fictifs @conseildesk.lab.exemple.
        $this->pdo->exec('
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL,
                nom_complet TEXT NOT NULL,
                email TEXT NOT NULL DEFAULT "",
                phone TEXT NOT NULL DEFAULT "",
                bio TEXT NOT NULL DEFAULT ""
            );
            CREATE TABLE clients (
                id INTEGER PRIMARY KEY,
                nom TEXT NOT NULL,
                segment TEXT NOT NULL,
                conseiller TEXT NOT NULL,
                ville TEXT NOT NULL,
                nir TEXT NOT NULL,
                revenus TEXT NOT NULL,
                note_risque TEXT NOT NULL,
                email TEXT NOT NULL DEFAULT "",
                phone TEXT NOT NULL DEFAULT "",
                statut TEXT NOT NULL DEFAULT "ACTIF"
            );
            CREATE TABLE files (
                id INTEGER PRIMARY KEY,
                client_id INTEGER NOT NULL,
                intitule TEXT NOT NULL,
                etat TEXT NOT NULL
            );
            CREATE TABLE notes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                client_id INTEGER NOT NULL,
                auteur TEXT NOT NULL,
                body TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE TABLE audit (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                ts TEXT NOT NULL,
                source_ip TEXT NOT NULL,
                username TEXT,
                action TEXT NOT NULL
            );
        ');

        $pw = password_hash('Password1!', PASSWORD_BCRYPT);
        $u = $this->pdo->prepare('INSERT INTO users (id, username, password_hash, role, nom_complet, email, phone, bio) VALUES (?,?,?,?,?,?,?,?)');
        // --- Comptes deterministes attendus par le validateur (NE PAS MODIFIER id/role) ---
        $u->execute([41, 'conseiller1', $pw, 'CONSEILLER', 'Camille Conseiller', 'conseiller1@conseildesk.lab.exemple', '+33 1 23 45 67 41', 'Conseiller particuliers, agence Paris-Centre (fictif).']);
        $u->execute([42, 'conseiller2', $pw, 'CONSEILLER', 'Claude Conseiller', 'conseiller2@conseildesk.lab.exemple', '+33 1 23 45 67 42', 'Conseiller professionnels, agence Lille (fictif).']);
        $u->execute([43, 'directeur1', $pw, 'DIRECTEUR', 'Dominique Directeur', 'directeur1@conseildesk.lab.exemple', '+33 1 23 45 67 43', 'Directeur d agence (fictif).']);
        // --- Comptes supplementaires (volume) : roles limites a CONSEILLER / DIRECTEUR ---
        $u->execute([44, 'conseiller3', $pw, 'CONSEILLER', 'Camille Charpentier', 'conseiller3@conseildesk.lab.exemple', '+33 1 23 45 67 44', 'Conseiller patrimonial, agence Lyon (fictif).']);
        $u->execute([45, 'conseiller4', $pw, 'CONSEILLER', 'Noa Nogaret', 'conseiller4@conseildesk.lab.exemple', '+33 1 23 45 67 45', 'Conseiller particuliers, agence Bordeaux (fictif).']);
        $u->execute([46, 'conseiller5', $pw, 'CONSEILLER', 'Sacha Sorel', 'conseiller5@conseildesk.lab.exemple', '+33 1 23 45 67 46', 'Conseiller professionnels, agence Nantes (fictif).']);
        $u->execute([47, 'directeur2', $pw, 'DIRECTEUR', 'Dani Delmas', 'directeur2@conseildesk.lab.exemple', '+33 1 23 45 67 47', 'Directeur region Sud (fictif).']);

        $c = $this->pdo->prepare('INSERT INTO clients (id, nom, segment, conseiller, ville, nir, revenus, note_risque, email, phone, statut) VALUES (?,?,?,?,?,?,?,?,?,?,?)');
        // --- Clients deterministes attendus par le validateur (NE PAS MODIFIER) ---
        // 1500 "Durand" doit rester l'UNIQUE client dont le nom contient "Durand" (sonde SQLi V002).
        $c->execute([1500, 'Durand', 'Particulier', 'conseiller1', 'Paris', '1 80 06 75 001 234', '42000', 'Profil prudent (fictif)', 'durand@client.lab.exemple', '+33 6 00 00 15 00', 'ACTIF']);
        $c->execute([1501, 'Dubois', 'Particulier', 'conseiller1', 'Lyon', '2 85 11 69 005 678', '38000', 'Profil equilibre (fictif)', 'dubois@client.lab.exemple', '+33 6 00 00 15 01', 'ACTIF']);
        $c->execute([2050, 'Martin', 'Professionnel', 'conseiller2', 'Lille', '1 77 59 59 009 012', '75000', 'Encours surveille (fictif)', 'martin@client.lab.exemple', '+33 6 00 00 20 50', 'SURVEILLE']);
        // --- Clients supplementaires (volume) : aucun nom ne contient "Durand" ---
        $c->execute([1502, 'Lefevre', 'Particulier', 'conseiller1', 'Paris', '2 83 04 75 010 101', '29000', 'Profil prudent (fictif)', 'lefevre@client.lab.exemple', '+33 6 00 00 15 02', 'ACTIF']);
        $c->execute([1503, 'Moreau', 'Patrimonial', 'conseiller1', 'Versailles', '1 79 09 78 010 202', '120000', 'Patrimoine diversifie (fictif)', 'moreau@client.lab.exemple', '+33 6 00 00 15 03', 'ACTIF']);
        $c->execute([1504, 'Girard', 'Particulier', 'conseiller1', 'Paris', '2 88 02 75 010 303', '33000', 'Profil equilibre (fictif)', 'girard@client.lab.exemple', '+33 6 00 00 15 04', 'INACTIF']);
        $c->execute([2051, 'Bernard', 'Professionnel', 'conseiller2', 'Lille', '1 75 12 59 020 404', '88000', 'Encours surveille (fictif)', 'bernard@client.lab.exemple', '+33 6 00 00 20 51', 'ACTIF']);
        $c->execute([2052, 'Petit', 'Professionnel', 'conseiller2', 'Roubaix', '2 81 06 59 020 505', '64000', 'Profil dynamique (fictif)', 'petit@client.lab.exemple', '+33 6 00 00 20 52', 'ACTIF']);
        $c->execute([3100, 'Robert', 'Patrimonial', 'conseiller3', 'Lyon', '1 82 03 69 030 606', '150000', 'Patrimoine diversifie (fictif)', 'robert@client.lab.exemple', '+33 6 00 00 31 00', 'ACTIF']);
        $c->execute([3101, 'Richard', 'Particulier', 'conseiller3', 'Villeurbanne', '2 90 10 69 030 707', '27000', 'Profil prudent (fictif)', 'richard@client.lab.exemple', '+33 6 00 00 31 01', 'ACTIF']);
        $c->execute([3200, 'Fontaine', 'Particulier', 'conseiller4', 'Bordeaux', '1 86 07 33 040 808', '41000', 'Profil equilibre (fictif)', 'fontaine@client.lab.exemple', '+33 6 00 00 32 00', 'ACTIF']);
        $c->execute([3201, 'Rousseau', 'Professionnel', 'conseiller4', 'Merignac', '2 84 01 33 040 909', '97000', 'Encours surveille (fictif)', 'rousseau@client.lab.exemple', '+33 6 00 00 32 01', 'SURVEILLE']);
        $c->execute([3300, 'Garnier', 'Professionnel', 'conseiller5', 'Nantes', '1 78 05 44 050 010', '72000', 'Profil dynamique (fictif)', 'garnier@client.lab.exemple', '+33 6 00 00 33 00', 'ACTIF']);
        $c->execute([3301, 'Chevalier', 'Patrimonial', 'conseiller5', 'Saint-Nazaire', '2 87 11 44 050 111', '135000', 'Patrimoine diversifie (fictif)', 'chevalier@client.lab.exemple', '+33 6 00 00 33 01', 'ACTIF']);

        $f = $this->pdo->prepare('INSERT INTO files (id, client_id, intitule, etat) VALUES (?,?,?,?)');
        // --- Dossiers deterministes attendus par le validateur (NE PAS MODIFIER etat) ---
        $f->execute([1001, 1500, 'Credit immobilier Durand', 'EN_ATTENTE_VALIDATION']);
        $f->execute([1002, 1500, 'Assurance-vie Durand', 'VALIDE']);
        $f->execute([2001, 2050, 'Financement pro Martin', 'VALIDE']);
        // --- Dossiers supplementaires (volume) : variete de statuts ---
        $f->execute([1003, 1501, 'Credit consommation Dubois', 'BROUILLON']);
        $f->execute([1004, 1502, 'Credit auto Lefevre', 'EN_ATTENTE_VALIDATION']);
        $f->execute([1005, 1503, 'Mandat de gestion Moreau', 'VALIDE']);
        $f->execute([1006, 1504, 'Cloture de compte Girard', 'CLOTURE']);
        $f->execute([2002, 2051, 'Pret tresorerie Bernard', 'REJETE']);
        $f->execute([2003, 2052, 'Leasing materiel Petit', 'VALIDE']);
        $f->execute([3102, 3100, 'Assurance-vie Robert', 'EN_ATTENTE_VALIDATION']);
        $f->execute([3202, 3201, 'Financement pro Rousseau', 'VALIDE']);
        $f->execute([3302, 3300, 'Pret pro Garnier', 'BROUILLON']);

        $n = $this->pdo->prepare('INSERT INTO notes (client_id, auteur, body, created_at) VALUES (?,?,?,?)');
        // --- Notes deterministes attendues par le validateur (NE PAS MODIFIER) ---
        $n->execute([1500, 'conseiller1', 'Relance prevue lundi pour le dossier immobilier.', '2026-09-01T09:00:00Z']);
        $n->execute([2050, 'conseiller2', 'Rendez-vous de revue annuelle a planifier.', '2026-09-02T10:30:00Z']);
        // --- Notes supplementaires (volume) ---
        $n->execute([1501, 'conseiller1', 'Demande de simulation credit consommation envoyee.', '2026-09-03T11:15:00Z']);
        $n->execute([1503, 'conseiller1', 'Point patrimonial trimestriel effectue, RAS.', '2026-09-04T14:00:00Z']);
        $n->execute([2051, 'conseiller2', 'Garanties complementaires a reclamer avant decision.', '2026-09-05T09:45:00Z']);
        $n->execute([3100, 'conseiller3', 'Arbitrage propose sur le contrat assurance-vie.', '2026-09-06T16:20:00Z']);
        $n->execute([3300, 'conseiller5', 'Rendez-vous de cadrage financement professionnel.', '2026-09-07T10:00:00Z']);
    }

    // --- Helpers de lecture --------------------------------------------------

    public function userByUsername(string $username): ?array
    {
        $s = $this->pdo->prepare('SELECT * FROM users WHERE username = ?');
        $s->execute([$username]);
        return $s->fetch() ?: null;
    }

    public function userById(int $id): ?array
    {
        $s = $this->pdo->prepare('SELECT * FROM users WHERE id = ?');
        $s->execute([$id]);
        return $s->fetch() ?: null;
    }

    public function allUsers(): array
    {
        return $this->pdo->query('SELECT * FROM users ORDER BY id')->fetchAll();
    }

    public function clientById(int $id): ?array
    {
        $s = $this->pdo->prepare('SELECT * FROM clients WHERE id = ?');
        $s->execute([$id]);
        return $s->fetch() ?: null;
    }

    public function fileById(int $id): ?array
    {
        $s = $this->pdo->prepare('SELECT * FROM files WHERE id = ?');
        $s->execute([$id]);
        return $s->fetch() ?: null;
    }

    public function notesByClient(int $clientId): array
    {
        $s = $this->pdo->prepare('SELECT * FROM notes WHERE client_id = ? ORDER BY id');
        $s->execute([$clientId]);
        return $s->fetchAll();
    }

    public function audit(string $sourceIp, ?string $username, string $action): void
    {
        $s = $this->pdo->prepare('INSERT INTO audit (ts, source_ip, username, action) VALUES (?,?,?,?)');
        $s->execute([gmdate('c'), $sourceIp, $username, $action]);
    }

    /** Historique d'activite d'un conseiller (requete parametree) - surface benigne. */
    public function auditByUser(string $username, int $limit = 50): array
    {
        $s = $this->pdo->prepare('SELECT ts, source_ip, action FROM audit WHERE username = ? ORDER BY id DESC LIMIT ?');
        $s->bindValue(1, $username, \PDO::PARAM_STR);
        $s->bindValue(2, $limit, \PDO::PARAM_INT);
        $s->execute();
        return $s->fetchAll();
    }

    /** Portefeuille d'un conseiller (requete parametree) - surface benigne (export CSV). */
    public function clientsByAdvisor(string $username): array
    {
        $s = $this->pdo->prepare(
            'SELECT id, nom, segment, ville, statut, email, phone FROM clients WHERE conseiller = ? ORDER BY id'
        );
        $s->execute([$username]);
        return $s->fetchAll();
    }
}
