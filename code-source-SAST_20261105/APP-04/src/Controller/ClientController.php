<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class ClientController
{
    /**
     * Recherche client LEGACY. Deux ecarts volontaires :
     *  - APP04-V002 : requete SQL construite par concatenation de chaine (injection SQL).
     *  - APP04-V015 : l'IP source retenue (audit) provient de X-Forwarded-For fourni par
     *                 le client, sans restriction aux proxys de confiance (usurpation).
     */
    public function search(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }

        $search = (string) $ctx->request->get('search', '');

        $xff = $ctx->request->header('x-forwarded-for');
        $sourceIp = $xff !== null ? trim(explode(',', (string) $xff)[0]) : $ctx->request->remoteAddr();
        $ctx->db->audit($sourceIp, $ctx->security->username(), 'GET /clients?search=' . $search);

        $sql = "SELECT id, nom, segment, conseiller, ville FROM clients WHERE nom LIKE '%" . $search . "%' ORDER BY id";
        $rows = $ctx->db->pdo()->query($sql)->fetchAll();

        $resp = Response::html($ctx->render('clients/list.html.twig', [
            'search' => $search,
            'clients' => $rows,
            'secure' => false,
        ]));
        // Expose l'IP source retenue (demonstration de l'influence de l'en-tete falsifie).
        $resp->setHeader('X-Client-Ip', $sourceIp); 
        return $resp;
    }

    /**
     * Recherche client SECURISEE (NC APP04-N002) : requete preparee parametree (placeholder lie),
     * aucune concatenation ; X-Forwarded-For ignore (IP de connexion reelle utilisee).
     */
    public function searchSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }

        $search = (string) $ctx->request->get('search', '');

        // NC: APP04-N004/N002 - IP source = connexion reelle, X-Forwarded-* non pris en compte.
        $sourceIp = $ctx->request->remoteAddr();
        $ctx->db->audit($sourceIp, $ctx->security->username(), 'GET /clients/v2');

        // NC: APP04-N002 - requete preparee parametree (aucune concatenation).
        $stmt = $ctx->db->pdo()->prepare(
            'SELECT id, nom, segment, conseiller, ville FROM clients WHERE nom LIKE ? ORDER BY id'
        );
        $stmt->execute(['%' . $search . '%']);
        $rows = $stmt->fetchAll();

        $resp = Response::html($ctx->render('clients/list.html.twig', [
            'search' => $search,
            'clients' => $rows,
            'secure' => true,
        ]));
        $resp->setHeader('X-Client-Ip', $sourceIp);
        return $resp;
    }

    /**
     * Rapport de recherche LEGACY. ECART volontaire APP04-V017 : le terme `q` est restitue
     * dans le gabarit via le filtre |raw (XSS REFLECHI, aucun echappement). La charge n'est
     * pas persistee : elle est reflechie directement depuis la requete GET.
     */
    public function report(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $q = (string) $ctx->request->get('q', '');
        return Response::html($ctx->render('clients/report.html.twig', [
            'q' => $q,
            'secure' => false,
        ]));
    }

    /**
     * Rapport de recherche SECURISE (NC APP04-N007) : le terme `q` est restitue avec
     * l'echappement Twig par defaut ({{ q }}, sans |raw) ; la charge est neutralisee.
     */
    public function reportSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $q = (string) $ctx->request->get('q', '');
        // NC: APP04-N007 - echappement par defaut (pas de |raw).
        return Response::html($ctx->render('clients/report.html.twig', [
            'q' => $q,
            'secure' => true,
        ]));
    }

    /**
     * Export CSV du portefeuille - USE CASE BENIN (surface de crawl). Authentification exigee,
     * requete parametree (clientsByAdvisor), aucune donnee d'un autre conseiller, aucun champ
     * restreint (NIR/revenus) : comportement sur, sans ecart ni negative control associe.
     */
    public function exportCsv(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }
        $rows = $ctx->db->clientsByAdvisor((string) $ctx->security->username());
        $out = "id;nom;segment;ville;statut;email;telephone\n";
        foreach ($rows as $c) {
            // Neutralisation CSV : prefixe les valeurs a risque d'interpretation tableur.
            $cells = array_map(function ($v) {
                $v = str_replace(["\r", "\n", '"'], [' ', ' ', "'"], (string) $v);
                if ($v !== '' && str_contains('=+-@', $v[0])) {
                    $v = "'" . $v;
                }
                return $v;
            }, [$c['id'], $c['nom'], $c['segment'], $c['ville'], $c['statut'], $c['email'], $c['phone']]);
            $out .= implode(';', $cells) . "\n";
        }
        $resp = Response::text($out);
        $resp->setHeader('Content-Type', 'text/csv; charset=UTF-8');
        $resp->setHeader('Content-Disposition', 'attachment; filename="portefeuille.csv"');
        return $resp;
    }
}
