<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class ProfileController
{
    /** Champs reellement destines au conseiller (intention metier de l'edition de profil). */
    private const PROFILE_FIELDS = ['nom_complet', 'email', 'phone', 'bio'];

    /**
     * Page profil du conseiller connecte - USE CASE BENIN (surface de crawl). Authentification
     * exigee, rendu Twig echappe par defaut, aucune donnee d'un autre utilisateur : sans ecart.
     */
    public function index(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        return Response::html($ctx->render('profile.html.twig', [
            'user' => $ctx->security->user(),
            'csrf' => $ctx->security->csrfToken('profile'),
        ]));
    }

    /**
     * Mise a jour de profil LEGACY. ECART volontaire APP04-V019 : la liaison du formulaire
     * (over-binding / mass assignment) recopie SANS liste blanche tous les champs fournis vers
     * les colonnes correspondantes, y compris `role`. Un conseiller peut donc s'auto-elever en
     * ajoutant `role=DIRECTEUR` a la requete de mise a jour du profil (CWE-915 -> elevation).
     */
    public function update(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $user = $ctx->security->user();
        $input = $ctx->request->request; // corps form-urlencoded ou JSON

        $bindable = array_merge(self::PROFILE_FIELDS, ['role']);
        $sets = [];
        $vals = [];
        foreach ($input as $k => $v) {
            if (in_array($k, $bindable, true)) {
                $sets[] = "$k = ?";
                $vals[] = (string) $v;
            }
        }
        if ($sets !== []) {
            $vals[] = (int) $user['id'];
            $stmt = $ctx->db->pdo()->prepare('UPDATE users SET ' . implode(', ', $sets) . ' WHERE id = ?');
            $stmt->execute($vals);
        }

        $updated = $ctx->db->userById((int) $user['id']);
        return Response::json(['status' => 'ok', 'profile' => [
            'username' => $updated['username'],
            'role' => $updated['role'],
            'nom_complet' => $updated['nom_complet'],
            'email' => $updated['email'] ?? '',
            'phone' => $updated['phone'] ?? '',
        ]]);
    }

    /**
     * Mise a jour de profil SECURISEE (NC APP04-N009) : liaison sur LISTE BLANCHE stricte
     * (nom_complet, email, phone, bio) ; tout champ hors liste - dont `role` - est ignore.
     */
    public function updateSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $user = $ctx->security->user();
        $input = $ctx->request->request;

        // NC: APP04-N009 - seules les colonnes explicitement autorisees sont liees.
        $sets = [];
        $vals = [];
        foreach (self::PROFILE_FIELDS as $field) {
            if (array_key_exists($field, $input)) {
                $sets[] = "$field = ?";
                $vals[] = (string) $input[$field];
            }
        }
        if ($sets !== []) {
            $vals[] = (int) $user['id'];
            $stmt = $ctx->db->pdo()->prepare('UPDATE users SET ' . implode(', ', $sets) . ' WHERE id = ?');
            $stmt->execute($vals);
        }

        $updated = $ctx->db->userById((int) $user['id']);
        return Response::json(['status' => 'ok', 'profile' => [
            'username' => $updated['username'],
            'role' => $updated['role'],
            'nom_complet' => $updated['nom_complet'],
            'email' => $updated['email'] ?? '',
            'phone' => $updated['phone'] ?? '',
        ]]);
    }
}
