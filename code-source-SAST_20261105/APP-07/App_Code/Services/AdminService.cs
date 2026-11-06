// =============================================================================
// APP-07 "PayGate XML" - Implementation du service d'ADMINISTRATION INTERNE (WCF).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// NC: APP07-N004 / NC: APP07-N005
// Heberge sur un endpoint INTERNE distinct (AdminService.svc) NON publie dans le
// WSDL public et refuse depuis l'exterieur (cf. web.config : location/authorization).
// Chaque operation applique un controle d'autorisation par role (service-interne)
// via un jeton SOAP VERIFIE cryptographiquement.
// =============================================================================
using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.Web;
using System.Xml.Linq;
using PayGate.Contracts;
using PayGate.Core;

namespace PayGate.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                     ConcurrencyMode = ConcurrencyMode.Multiple)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AdminService : IAdminService
    {
        private static Stream RawBody(Message request)
        {
            // Le corps HTTP brut n'est plus lisible via HttpContext apres que WCF a
            // consomme GetBufferlessInputStream : on rebufferise le Message WCF en
            // enveloppe SOAP complete (en-tetes wsse + corps) pour le parsing cible.
            var ms = new MemoryStream();
            var buffer = request.CreateBufferedCopy(int.MaxValue);
            using (var msg = buffer.CreateMessage())
            using (var xw = System.Xml.XmlWriter.Create(ms))
                msg.WriteMessage(xw);
            ms.Position = 0;
            return ms;
        }

        private static Message Reply(string action, object body)
        {
            return Message.CreateMessage(MessageVersion.Soap11, action, body);
        }

        // NC: APP07-N005 - PurgeArchivedBatches soumise au controle d'autorisation.
        public Message PurgeArchivedBatches(Message request)
        {
            var doc = XmlParsing.LoadSecure(RawBody(request), XmlParsing.BuildStrictSchemas());
            var id = SoapAuth.ResolveIdentityVerified(XmlParsing.Header(doc));

            // Autorisation par role : reservee a service-interne.
            if (!id.Authenticated ||
                !string.Equals(id.Role, "interne", StringComparison.OrdinalIgnoreCase))
                throw new FaultException("Autorisation refusee (role service-interne requis).");

            var archived = Store.Batches.Values.Where(b => b.Status == "ARCHIVED").ToList();
            Batch removed;
            foreach (var b in archived) Store.Batches.TryRemove(b.BatchId, out removed);

            return Reply("PurgeArchivedBatchesResponse", new AdminActionResult
            {
                Success = true, Affected = archived.Count,
                Message = archived.Count + " lot(s) archive(s) purge(s)."
            });
        }
    }
}
