<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class NoteController
{
    /**
     * Notes d'un client. ECART volontaire APP04-V004 : aucune verification du rattachement
     * du client au conseiller connecte (IDOR). Le rendu des notes utilise le filtre |raw
     * cote gabarit -> ECART volontaire APP04-V005 (XSS stocke).
     */
    public function list(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $clientId = (int) $ctx->routeParams['id'];
        $client = $ctx->db->clientById($clientId);
        if ($client === null) {
            return Response::html($ctx->render('error.html.twig', ['code' => 404, 'message' => 'Client inconnu']), 404);
        }

        $notes = $ctx->db->notesByClient($clientId);

        return Response::html($ctx->render('clients/notes.html.twig', [
            'client' => $client,
            'notes' => $notes,
            'csrf' => $ctx->security->csrfToken('note'),
        ]));
    }

    /**
     * Ajout d'une note. Le corps est stocke tel quel puis restitue sans echappement
     * (gabarit clients/notes.html.twig, filtre |raw) -> ECART volontaire APP04-V005 (XSS stocke).
     */
    public function add(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $clientId = (int) $ctx->routeParams['id'];
        $client = $ctx->db->clientById($clientId);
        if ($client === null) {
            return Response::html($ctx->render('error.html.twig', ['code' => 404, 'message' => 'Client inconnu']), 404);
        }

        $body = (string) $ctx->request->get('body', '');
        $stmt = $ctx->db->pdo()->prepare(
            'INSERT INTO notes (client_id, auteur, body, created_at) VALUES (?,?,?,?)'
        );
        $stmt->execute([$clientId, $ctx->security->username(), $body, gmdate('c')]);

        return Response::redirect('/clients/' . $clientId . '/notes');
    }

    // Jeton temoin injecte dans le contexte de rendu de l'apercu : prouve l'evaluation
    // cote serveur d'une expression de gabarit (oracle SSTI deterministe, 100% fictif).
    private const PREVIEW_MARKER = 'SSTI-APP04-EVAL-OK';

    /**
     * Apercu LEGACY d'un modele de note. ECART volontaire APP04-V018 : le modele fourni par
     * l'utilisateur est COMPILE PUIS EVALUE comme gabarit cote serveur (Template::renderString)
     * -> injection de gabarit (SSTI). Une expression {{ ... }} injectee est evaluee avec le
     * contexte serveur (le temoin marker en atteste).
     */
    public function preview(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $template = (string) $ctx->request->get('template', '');
        $rendered = $ctx->twig->renderString($template, [
            'auteur' => (string) $ctx->security->username(),
            'marker' => self::PREVIEW_MARKER,
        ]);
        return Response::json(['rendered' => $rendered]);
    }

    /**
     * Apercu SECURISE (NC APP04-N008) : le modele fourni est traite comme une DONNEE (jamais
     * compile) ; seules des variables sur liste blanche sont substituees litteralement, et la
     * sortie est echappee. Une expression {{ ... }} injectee n'est pas evaluee.
     */
    public function previewSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $template = (string) $ctx->request->get('template', '');
        // NC: APP04-N008 - substitution litterale sur liste blanche, aucune evaluation de gabarit.
        $allowed = [
            '{auteur}' => (string) $ctx->security->username(),
            '{date}' => gmdate('Y-m-d'),
        ];
        $rendered = strtr($template, $allowed);
        $rendered = htmlspecialchars($rendered, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
        return Response::json(['rendered' => $rendered]);
    }
}
