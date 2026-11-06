// =============================================================================
// APP-07 "PayGate XML" - Resolution de chemin pour les telechargements.
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
//  - ResolveReceiptInsecure() : nom concatene au dossier de base SANS
//    canonicalisation ni confinement -> ecart volontaire APP07-V014
//    (traversee de chemin) sur /receipts/download.
//  - ResolveStatementSecure() : canonicalisation + confinement strict au dossier
//    autorise -> comportement SECURISE, NC APP07-N007 sur /statements/download.
// =============================================================================
using System;
using System.IO;

namespace PayGate.Core
{
    public static class PathUtil
    {
        public static string ReceiptsDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "receipts"); }
        }

        public static string StatementsDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "statements"); }
        }

        // Des sequences ..\ permettent de sortir du dossier receipts.
        public static string ResolveReceiptInsecure(string name)
        {
            // Aucune canonicalisation ni verification de confinement (ecart).
            return Path.Combine(ReceiptsDir, name);
        }

        // NC: APP07-N007 - canonicalisation + verification que le chemin resolu
        // reste strictement sous le dossier autorise ; sinon acces refuse.
        public static string ResolveStatementSecure(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new UnauthorizedAccessException("Nom de releve invalide.");

            string baseDir = Path.GetFullPath(StatementsDir + Path.DirectorySeparatorChar);
            string candidate = Path.GetFullPath(Path.Combine(baseDir, name));

            if (!candidate.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chemin hors du dossier autorise.");

            return candidate;
        }
    }
}
