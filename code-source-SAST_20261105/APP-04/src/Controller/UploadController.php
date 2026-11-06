<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

/**
 * Ressources de depot. ECART volontaire APP04-V014 : le serveur accepte les verbes
 * d'ecriture (PUT/DELETE) et permet le depot/suppression de ressources recuperables.
 * (En configuration durcie, seuls GET/POST seraient autorises au niveau serveur.)
 */
final class UploadController
{
    private function dir(Context $ctx): string
    {
        $dir = $ctx->params['uploads_dir'];
        if (!is_dir($dir)) {
            @mkdir($dir, 0770, true);
        }
        return rtrim($dir, '/');
    }

    private function safeName(string $path): string
    {
        // On conserve un simple nom de fichier (la vulnerabilite ciblee est l'ACTIVATION
        // des verbes d'ecriture, pas une traversee de chemin a l'ecriture).
        return basename($path);
    }

    public function put(Context $ctx): Response
    {
        $name = $this->safeName($ctx->routeParams['path'] ?? 'file.bin');
        @file_put_contents($this->dir($ctx) . '/' . $name, $ctx->request->rawBody);
        return new Response('', 201, ['Location' => '/uploads/' . $name]);
    }

    public function get(Context $ctx): Response
    {
        $name = $this->safeName($ctx->routeParams['path'] ?? '');
        $file = $this->dir($ctx) . '/' . $name;
        if (!is_file($file)) {
            return Response::text('Not found', 404);
        }
        return Response::text((string) file_get_contents($file));
    }

    public function delete(Context $ctx): Response
    {
        $name = $this->safeName($ctx->routeParams['path'] ?? '');
        @unlink($this->dir($ctx) . '/' . $name);
        return new Response('', 204);
    }
}
