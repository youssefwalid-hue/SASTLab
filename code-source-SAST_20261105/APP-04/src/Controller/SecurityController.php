<?php
namespace App\Controller;

use App\Context;
use App\Http\Response;

final class SecurityController
{
    public function loginForm(Context $ctx): Response
    {
        return Response::html($ctx->render('login.html.twig', [
            'error' => null,
            'csrf' => $ctx->security->csrfToken('login'),
        ]));
    }

    /**
     * Connexion LEGACY (parcours principal). Contient deux ecarts volontaires :
     *  - APP04-V010 : l'identifiant de session n'est PAS regenere a l'authentification
     *                 (fixation de session possible).
     *  - APP04-V011 : le cookie de session est emis SANS l'attribut HttpOnly.
     * Le reste (verification du mot de passe) est correct.
     */
    public function loginLegacy(Context $ctx): Response
    {
        $username = (string) $ctx->request->get('username', '');
        $password = (string) $ctx->request->get('password', '');
        $user = $ctx->db->userByUsername($username);

        if ($user === null || !password_verify($password, $user['password_hash'])) {
            return Response::html($ctx->render('login.html.twig', [
                'error' => 'Identifiants invalides.',
                'csrf' => $ctx->security->csrfToken('login'),
            ]), 401);
        }

        $provided = $ctx->request->cookies['PHPSESSID'] ?? null;
        $ctx->session->adoptProvidedId($provided);
        $ctx->session->set('user_id', (int) $user['id']);

        $resp = Response::redirect('/dashboard');
        $resp->addCookie($ctx->session->cookieHeaderInsecure());
        return $resp;
    }

    /**
     * Connexion SECURISEE (NC APP04-N005) : rotation de l'identifiant de session a
     * l'authentification (anti-fixation) et cookie durci HttpOnly + Secure + SameSite=Lax.
     */
    public function loginSecure(Context $ctx): Response
    {
        $username = (string) $ctx->request->get('username', '');
        $password = (string) $ctx->request->get('password', '');
        $user = $ctx->db->userByUsername($username);

        if ($user === null || !password_verify($password, $user['password_hash'])) {
            return Response::json(['error' => 'invalid_credentials'], 401);
        }

        // NC: APP04-N005 - regeneration de l'identifiant (ancien invalide).
        $ctx->session->regenerate();
        $ctx->session->set('user_id', (int) $user['id']);

        $resp = Response::json(['status' => 'ok', 'user' => $user['username']]);
        // NC: APP04-N005 - cookie durci.
        $resp->addCookie($ctx->session->cookieHeaderSecure());
        return $resp;
    }

    public function logout(Context $ctx): Response
    {
        $ctx->session->clear();
        $resp = Response::redirect('/login');
        $resp->addCookie('PHPSESSID=deleted; Path=/; Max-Age=0; HttpOnly; Secure; SameSite=Lax');
        return $resp;
    }

    /**
     * Demande de reinitialisation de mot de passe LEGACY. ECART volontaire APP04-V022 :
     * la reponse DIFFERE selon l'existence du compte (message + code HTTP distincts), ce qui
     * permet l'ENUMERATION de comptes (CWE-204). Aucun email reel n'est envoye (laboratoire).
     */
    public function passwordReset(Context $ctx): Response
    {
        $username = (string) $ctx->request->get('username', '');
        $user = $ctx->db->userByUsername($username);
        if ($user === null) {
            return Response::json(['status' => 'unknown_account',
                'message' => 'Aucun compte ne correspond a cet identifiant.'], 404);
        }
        return Response::json(['status' => 'reset_sent',
            'message' => 'Un lien de reinitialisation a ete envoye au compte.'], 200);
    }

    /**
     * Demande de reinitialisation SECURISEE (NC APP04-N012) : reponse UNIFORME quel que soit
     * l'etat du compte (meme code HTTP, meme corps) -> aucune enumeration possible.
     */
    public function passwordResetSecure(Context $ctx): Response
    {
        $username = (string) $ctx->request->get('username', '');
        // Le traitement reel (envoi conditionnel) reste cote serveur sans fuiter l'existence.
        $ctx->db->userByUsername($username);
        // NC: APP04-N012 - message et code HTTP identiques pour tout identifiant.
        return Response::json(['status' => 'ok',
            'message' => 'Si un compte correspond, un lien de reinitialisation a ete envoye.'], 200);
    }
}
