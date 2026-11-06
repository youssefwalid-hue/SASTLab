<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class AdminController
{
    /** Liste des utilisateurs - ecran reserve au directeur (controle d'acces vertical correct ici). */
    public function users(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        if (!$ctx->security->isGranted('ROLE_DIRECTEUR')) {
            return Response::html($ctx->render('error.html.twig', [
                'code' => 403, 'message' => 'Acces reserve aux directeurs',
            ]), 403);
        }
        return Response::html($ctx->render('admin/users.html.twig', [
            'users' => $ctx->db->allUsers(),
            'csrf' => $ctx->security->csrfToken('role'),
        ]));
    }

    /**
     * Changement de rôle LEGACY. Deux ecarts volontaires :
     *  - APP04-V001 : aucune verification que l'appelant detient ROLE_DIRECTEUR
     *                 (elevation de privilege, auto-elevation possible).
     *  - APP04-V006 : aucune validation de jeton anti-CSRF sur cette action a effet de bord.
     */
    public function changeRole(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $id = (int) $ctx->routeParams['id'];
        $role = (string) $ctx->request->get('role', '');
        if (!in_array($role, ['CONSEILLER', 'DIRECTEUR'], true)) {
            return Response::json(['error' => 'role_invalide'], 400);
        }
        $target = $ctx->db->userById($id);
        if ($target === null) {
            return Response::json(['error' => 'not_found'], 404);
        }

        $stmt = $ctx->db->pdo()->prepare('UPDATE users SET role = ? WHERE id = ?');
        $stmt->execute([$role, $id]);

        return Response::json(['id' => $id, 'username' => $target['username'], 'role' => $role]);
    }

    /**
     * Attribution de rôle SECURISEE (NC APP04-N001) : exige ROLE_DIRECTEUR (403 pour un
     * conseiller), valide un jeton anti-CSRF, et interdit l'auto-elevation.
     */
    public function assignRoleSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        // NC: APP04-N001 - controle d'acces vertical : reserve au directeur.
        if (!$ctx->security->isGranted('ROLE_DIRECTEUR')) {
            return Response::json(['error' => 'forbidden'], 403);
        }
        // NC: APP04-N001 - jeton anti-CSRF obligatoire.
        $token = $ctx->request->get('_token');
        if (!$ctx->security->isCsrfTokenValid('role', is_string($token) ? $token : null)) {
            return Response::json(['error' => 'csrf_invalide'], 403);
        }
        $id = (int) $ctx->routeParams['id'];
        $role = (string) $ctx->request->get('role', '');
        if (!in_array($role, ['CONSEILLER', 'DIRECTEUR'], true)) {
            return Response::json(['error' => 'role_invalide'], 400);
        }
        $target = $ctx->db->userById($id);
        if ($target === null) {
            return Response::json(['error' => 'not_found'], 404);
        }
        // NC: APP04-N001 - interdiction de l'auto-elevation.
        if ($target['username'] === $ctx->security->username() && $role === 'DIRECTEUR') {
            return Response::json(['error' => 'auto_elevation_interdite'], 403);
        }

        $stmt = $ctx->db->pdo()->prepare('UPDATE users SET role = ? WHERE id = ?');
        $stmt->execute([$role, $id]);
        return Response::json(['id' => $id, 'username' => $target['username'], 'role' => $role]);
    }

    /** Reinitialisation de l'etat de demonstration (parcours de laboratoire). */
    public function reset(Context $ctx): Response
    {
        $ctx->db->reset();
        // Nettoie aussi les sessions de laboratoire.
        $dir = $ctx->params['sessions_dir'];
        if (is_dir($dir)) {
            foreach (glob($dir . '/sess_*.json') ?: [] as $f) {
                @unlink($f);
            }
        }
        return Response::json(['status' => 'reset', 'environment' => 'test-data-fictives']);
    }
}
