<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

/**
 * Pages de portail BENIGNES (surface de crawl). Toutes exigent l'authentification, lisent
 * uniquement les donnees du conseiller connecte via des requetes parametrees, et s'appuient
 * sur l'echappement Twig par defaut. Aucun ecart volontaire, aucun negative control associe.
 */
final class PortalController
{
    public function notifications(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        // Notifications fictives, derivees de donnees du conseiller connecte.
        $me = (string) $ctx->security->username();
        $mines = $ctx->db->clientsByAdvisor($me);
        $items = [];
        foreach (array_slice($mines, 0, 3) as $c) {
            $items[] = [
                'titre' => 'Revue de dossier a planifier',
                'detail' => 'Client ' . $c['nom'] . ' (' . $c['ville'] . ') - statut ' . $c['statut'] . '.',
                'date' => '2026-09-10',
            ];
        }
        return Response::html($ctx->render('notifications.html.twig', ['items' => $items]));
    }

    public function history(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        // Historique d'activite du conseiller connecte (requete parametree, lecture seule).
        $events = $ctx->db->auditByUser((string) $ctx->security->username(), 50);
        return Response::html($ctx->render('history.html.twig', ['events' => $events]));
    }

    public function help(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        return Response::html($ctx->render('help.html.twig', []));
    }
}
