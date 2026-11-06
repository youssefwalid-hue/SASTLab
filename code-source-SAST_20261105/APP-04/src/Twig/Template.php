<?php
namespace App\Twig;

/**
 * Moteur de gabarits Twig (sous-ensemble) pour rendu serveur.
 *
 * NOTE LABORATOIRE : dans un deploiement Symfony complet, c'est twig/twig qui
 * assure ce rendu. Le registre de paquets (Packagist) etant inaccessible dans
 * l'environnement de construction, ce moteur embarque reproduit fidelement les
 * regles pertinentes pour le corpus :
 *   - ECHAPPEMENT AUTOMATIQUE par defaut sur {{ var }} (comme Twig).
 *   - Filtre |raw qui DESACTIVE l'echappement (ecart volontaire APP04-V005).
 *   - Structures {% if %}, {% for %}, {% include %}.
 * Les gabarits .html.twig sont donc authentiques et interchangeables avec Twig.
 */
final class Template
{
    private string $dir;

    public function __construct(string $templatesDir)
    {
        $this->dir = rtrim($templatesDir, '/');
    }

    public function render(string $name, array $ctx = []): string
    {
        $src = $this->load($name);
        $compiled = $this->compile($src);
        return $this->evaluate($compiled, $ctx);
    }

    /**
     * Rend une CHAINE de gabarit fournie a l'execution (et non un fichier charge).
     *
     * ATTENTION LABORATOIRE : exposer cette methode a une entree utilisateur revient a
     * evaluer un gabarit controle par l'attaquant cote serveur -> SSTI (ECART volontaire
     * APP04-V018, utilise par NoteController::preview). La variante securisee ne doit PAS
     * appeler cette methode mais traiter l'entree comme une donnee (voir previewSecure).
     */
    public function renderString(string $source, array $ctx = []): string
    {
        $compiled = $this->compile($source);
        return $this->evaluate($compiled, $ctx);
    }

    private function load(string $name): string
    {
        $path = $this->dir . '/' . ltrim($name, '/');
        if (!is_file($path)) {
            return "[template introuvable: " . htmlspecialchars($name) . "]";
        }
        return (string) file_get_contents($path);
    }

    /** Compile la syntaxe Twig-subset en PHP. */
    private function compile(string $src): string
    {
        // {# commentaires #} - supprimes.
        $src = preg_replace('/\{#.*?#\}/s', '', $src);

        // {% ... %}
        $src = preg_replace_callback('/\{%\s*(.*?)\s*%\}/s', function ($m) {
            $tag = trim($m[1]);
            if (preg_match('/^if\s+(.+)$/', $tag, $mm)) {
                return '<?php if ($this->truthy($this->v($ctx, ' . var_export(trim($mm[1]), true) . '))): ?>';
            }
            if ($tag === 'else') {
                return '<?php else: ?>';
            }
            if ($tag === 'endif') {
                return '<?php endif; ?>';
            }
            if (preg_match('/^for\s+(\w+)\s+in\s+(.+)$/', $tag, $mm)) {
                $item = $mm[1];
                $list = trim($mm[2]);
                return '<?php foreach (($this->v($ctx, ' . var_export($list, true) . ') ?: []) as $__it): '
                    . '$ctx[' . var_export($item, true) . '] = $__it; ?>';
            }
            if ($tag === 'endfor') {
                return '<?php endforeach; ?>';
            }
            if (preg_match("/^include\s+'([^']+)'$/", $tag, $mm)) {
                return '<?= $this->render(' . var_export($mm[1], true) . ', $ctx) ?>';
            }
            return '';
        }, $src);

        // {{ expr|raw }}  -> non echappe (ECART volontaire la ou utilise)
        $src = preg_replace_callback('/\{\{\s*([^{}|]+?)\s*\|\s*raw\s*\}\}/s', function ($m) {
            return '<?= (string) $this->v($ctx, ' . var_export(trim($m[1]), true) . ') ?>';
        }, $src);

        // {{ expr }} -> echappe (defaut securise)
        $src = preg_replace_callback('/\{\{\s*([^{}]+?)\s*\}\}/s', function ($m) {
            return '<?= $this->esc($this->v($ctx, ' . var_export(trim($m[1]), true) . ')) ?>';
        }, $src);

        return $src;
    }

    private function evaluate(string $compiled, array $ctx): string
    {
        ob_start();
        try {
            eval('?>' . $compiled);
        } catch (\Throwable $e) {
            ob_end_clean();
            return '[erreur de rendu: ' . htmlspecialchars($e->getMessage()) . ']';
        }
        return ob_get_clean();
    }

    /** Resolution d'un chemin pointe (a.b.c) sur tableaux/objets. */
    public function v(array $ctx, string $expr)
    {
        $expr = trim($expr);
        if ($expr === '') {
            return '';
        }
        $parts = explode('.', $expr);
        $cur = $ctx;
        foreach ($parts as $p) {
            if (is_array($cur) && array_key_exists($p, $cur)) {
                $cur = $cur[$p];
            } elseif (is_object($cur) && isset($cur->$p)) {
                $cur = $cur->$p;
            } else {
                return null;
            }
        }
        return $cur;
    }

    public function esc($value): string
    {
        return htmlspecialchars((string) ($value ?? ''), ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
    }

    public function truthy($value): bool
    {
        if (is_array($value)) {
            return count($value) > 0;
        }
        return !empty($value);
    }
}
