<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class ApiController
{
    /**
     * API legere clients LEGACY. ECART volontaire APP04-V009 : l'objet client est charge par
     * id sans controle d'autorisation au niveau objet (BOLA / IDOR).
     */
    public function client(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $id = (int) $ctx->routeParams['id'];
        $client = $ctx->db->clientById($id);
        if ($client === null) {
            return Response::json(['error' => 'not_found'], 404);
        }

        return Response::json([
            'id' => (int) $client['id'],
            'nom' => $client['nom'],
            'segment' => $client['segment'],
            'ville' => $client['ville'],
            'conseiller' => $client['conseiller'],
        ]);
    }

    /**
     * API clients SECURISEE (NC APP04-N004) : voter de propriete (403 inter-conseiller),
     * le directeur conserve un acces transverse. X-Forwarded-* non pris en compte.
     */
    public function clientSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $id = (int) $ctx->routeParams['id'];
        $client = $ctx->db->clientById($id);
        if ($client === null) {
            return Response::json(['error' => 'not_found'], 404);
        }

        // NC: APP04-N004 - controle de propriete : seul le conseiller rattache (ou un directeur) accede.
        $isOwner = $client['conseiller'] === $ctx->security->username();
        if (!$isOwner && !$ctx->security->isGranted('ROLE_DIRECTEUR')) {
            return Response::json(['error' => 'forbidden'], 403);
        }

        return Response::json([
            'id' => (int) $client['id'],
            'nom' => $client['nom'],
            'segment' => $client['segment'],
            'ville' => $client['ville'],
            'conseiller' => $client['conseiller'],
        ]);
    }

    /**
     * Profil JSON du conseiller connecte LEGACY. ECART volontaire APP04-V020 : la politique
     * CORS REFLECHIT l'en-tete Origin dans Access-Control-Allow-Origin et autorise les
     * credentials (Access-Control-Allow-Credentials: true). N'importe quel site tiers peut
     * donc lire la reponse authentifiee via XHR avec cookies.
     */
    public function profile(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $u = $ctx->security->user();
        $resp = Response::json([
            'username' => $u['username'],
            'role' => $u['role'],
            'nom_complet' => $u['nom_complet'],
            'email' => $u['email'] ?? '',
        ]);
        $origin = (string) $ctx->request->header('origin', '');
        if ($origin !== '') {
            $resp->setHeader('Access-Control-Allow-Origin', $origin);
            $resp->setHeader('Access-Control-Allow-Credentials', 'true');
            $resp->setHeader('Vary', 'Origin');
        }
        return $resp;
    }

    /**
     * Profil JSON SECURISE (NC APP04-N010) : politique CORS stricte sur liste blanche. L'origine
     * tierce n'est jamais reflechie ; les credentials ne sont pas autorises pour une origine
     * non approuvee.
     */
    public function profileSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::json(['error' => 'unauthenticated'], 401);
        }
        $u = $ctx->security->user();
        $resp = Response::json([
            'username' => $u['username'],
            'role' => $u['role'],
            'nom_complet' => $u['nom_complet'],
            'email' => $u['email'] ?? '',
        ]);
        // NC: APP04-N010 - CORS strict : seules des origines explicitement approuvees sont reflechies.
        $allowlist = ['https://conseildesk.lab.exemple'];
        $origin = (string) $ctx->request->header('origin', '');
        if ($origin !== '' && in_array($origin, $allowlist, true)) {
            $resp->setHeader('Access-Control-Allow-Origin', $origin);
            $resp->setHeader('Access-Control-Allow-Credentials', 'true');
            $resp->setHeader('Vary', 'Origin');
        }
        return $resp;
    }
}
