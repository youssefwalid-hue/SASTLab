<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class ExportController
{
    /**
     * Export de dossier LEGACY. Deux ecarts volontaires :
     *  - APP04-V003 : le nom de gabarit est concatene au chemin de base puis lu, sans liste
     *                 blanche ni canonicalisation (LFI / traversee de chemin).
     *  - APP04-V008 : le parametre `full` expose des champs PII restreints sans controle de role.
     */
    public function export(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }

        $template = (string) $ctx->request->get('template', 'dossier');
        $id = (int) $ctx->request->get('id', 0);
        $full = $ctx->request->get('full');

        $path = $ctx->params['export_dir'] . $template;
        $tpl = @file_get_contents($path);
        if ($tpl === false) {
            $tpl = "[gabarit introuvable: " . $template . "]";
        }

        $client = $ctx->db->clientById($id);
        $out = $tpl;
        if ($client !== null) {
            $out = str_replace(
                ['{{nom}}', '{{segment}}', '{{ville}}', '{{conseiller}}', '{{id}}'],
                [$client['nom'], $client['segment'], $client['ville'], $client['conseiller'], (string) $client['id']],
                $out
            );
        }

        if ($full !== null && $full !== '' && $full !== '0' && $client !== null) {
            $out .= "\n\n--- CHAMPS RESTREINTS (full) ---\n"
                . "NIR: " . $client['nir'] . "\n"
                . "REVENUS: " . $client['revenus'] . "\n"
                . "NOTE_RISQUE: " . $client['note_risque'] . "\n";
        }

        return Response::text($out);
    }

    /**
     * Export SECURISE (NC APP04-N003) : `template` mappe sur une liste blanche id->chemin fixe
     * (realpath verifie, aucun caractere de chemin accepte) ; champs restreints conditionnes
     * a ROLE_DIRECTEUR, `full` ignore sinon.
     */
    public function exportSecure(Context $ctx): Response
    {
        if (!$ctx->security->isAuthenticated()) {
            return Response::redirect('/login');
        }

        $template = (string) $ctx->request->get('template', 'dossier');
        $id = (int) $ctx->request->get('id', 0);
        $full = $ctx->request->get('full');

        // NC: APP04-N003 - liste blanche stricte id -> fichier fixe.
        $allowed = ['dossier' => 'dossier', 'synthese' => 'synthese'];
        if (!isset($allowed[$template])) {
            return Response::text("Gabarit non autorise.", 400);
        }
        $base = realpath($ctx->params['export_dir']);
        $path = realpath($ctx->params['export_dir'] . $allowed[$template]);
        if ($path === false || $base === false || !str_starts_with($path, $base)) {
            return Response::text("Gabarit non autorise.", 400);
        }
        $tpl = (string) file_get_contents($path);

        $client = $ctx->db->clientById($id);
        $out = $tpl;
        if ($client !== null) {
            $out = str_replace(
                ['{{nom}}', '{{segment}}', '{{ville}}', '{{conseiller}}', '{{id}}'],
                [$client['nom'], $client['segment'], $client['ville'], $client['conseiller'], (string) $client['id']],
                $out
            );
        }

        // NC: APP04-N003 - champs restreints uniquement pour un directeur ; `full` ignore sinon.
        if ($full !== null && $client !== null && $ctx->security->isGranted('ROLE_DIRECTEUR')) {
            $out .= "\n\n--- CHAMPS RESTREINTS ---\n"
                . "NIR: " . $client['nir'] . "\n"
                . "REVENUS: " . $client['revenus'] . "\n"
                . "NOTE_RISQUE: " . $client['note_risque'] . "\n";
        }

        return Response::text($out);
    }
}
