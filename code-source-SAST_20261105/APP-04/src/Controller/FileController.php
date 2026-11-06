<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class FileController
{
    public function show(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $file = $ctx->db->fileById((int) $ctx->routeParams['id']);
        if ($file === null) {
            return Response::html($ctx->render('error.html.twig', ['code' => 404, 'message' => 'Dossier inconnu']), 404);
        }
        return Response::html($ctx->render('files/show.html.twig', [
            'file' => $file,
            'csrf' => $ctx->security->csrfToken('file'),
        ]));
    }

    /**
     * Cloture LEGACY. ECART volontaire APP04-V007 : aucune verification de l'etat courant
     * du dossier (pas de machine a etats) ; la cloture est acceptee depuis n'importe quel etat.
     */
    public function close(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $id = (int) $ctx->routeParams['id'];
        $file = $ctx->db->fileById($id);
        if ($file === null) {
            return Response::json(['error' => 'not_found'], 404);
        }

        $stmt = $ctx->db->pdo()->prepare('UPDATE files SET etat = ? WHERE id = ?');
        $stmt->execute(['CLOTURE', $id]);

        return Response::json(['id' => $id, 'etat' => 'CLOTURE']);
    }

    /**
     * Cloture SECURISEE (NC APP04-N006) : machine a etats appliquee cote serveur ;
     * la transition `close` n'est autorisee que depuis l'etat VALIDE, sinon 409.
     */
    public function closeSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $id = (int) $ctx->routeParams['id'];
        $file = $ctx->db->fileById($id);
        if ($file === null) {
            return Response::json(['error' => 'not_found'], 404);
        }

        // NC: APP04-N006 - seule la transition VALIDE -> CLOTURE est autorisee.
        if ($file['etat'] !== 'VALIDE') {
            return Response::json([
                'error' => 'transition_interdite',
                'etat_courant' => $file['etat'],
                'transition' => 'close',
            ], 409);
        }
        $stmt = $ctx->db->pdo()->prepare('UPDATE files SET etat = ? WHERE id = ?');
        $stmt->execute(['CLOTURE', $id]);

        return Response::json(['id' => $id, 'etat' => 'CLOTURE']);
    }

    /**
     * Vue imprimable d'un dossier LEGACY. ECART volontaire APP04-V021 : la reponse renonce a la
     * protection anti-framing (aucun X-Frame-Options, CSP sans frame-ancestors) -> clickjacking
     * possible (la page peut etre encadree par un site tiers). Le marqueur __frameable__ est
     * interprete par le Kernel pour NE PAS ajouter l'en-tete anti-framing par defaut.
     */
    public function printView(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $file = $ctx->db->fileById((int) $ctx->routeParams['id']);
        if ($file === null) {
            return Response::html($ctx->render('error.html.twig', ['code' => 404, 'message' => 'Dossier inconnu']), 404);
        }
        $resp = Response::html($ctx->render('files/print.html.twig', ['file' => $file]));
        $resp->setHeader('__frameable__', '1');
        return $resp;
    }

    /**
     * Vue imprimable SECURISEE (NC APP04-N011) : conserve la protection anti-framing par defaut
     * du Kernel (X-Frame-Options: DENY), la page ne peut pas etre encadree par un site tiers.
     */
    public function printSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $file = $ctx->db->fileById((int) $ctx->routeParams['id']);
        if ($file === null) {
            return Response::html($ctx->render('error.html.twig', ['code' => 404, 'message' => 'Dossier inconnu']), 404);
        }
        // NC: APP04-N011 - aucun marqueur __frameable__ : X-Frame-Options: DENY applique par le Kernel.
        return Response::html($ctx->render('files/print.html.twig', ['file' => $file]));
    }
}
