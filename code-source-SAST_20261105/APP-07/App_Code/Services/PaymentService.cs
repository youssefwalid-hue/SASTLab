// =============================================================================
// APP-07 "PayGate XML" - Implementation du service SOAP PUBLIC (WCF).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
// SECURISE PAR DEFAUT ; les ecarts volontaires sont tagues // GT: APP07-Vxxx et
// les contre-controles securises // NC: APP07-Nxxx.
//
// NOTE D'HEBERGEMENT : le service tourne en mode ASP.NET compatibility
// (AspNetCompatibilityRequirementsMode.Required). Les operations de traitement
// XML relisent le CORPS HTTP BRUT via HttpContext afin de controler finement le
// parsing (DTD/entites) - condition necessaire pour que la DTD atteigne le
// parseur dans le flux cible par APP07-V001/V002/V006. Le contrat WCF reste
// utilise pour le routage par SOAPAction et la generation du WSDL (?wsdl).
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
    public class PaymentService : IPaymentService
    {
        // Relit l'enveloppe SOAP brute depuis la requete HTTP (controle du parsing).
        private static Stream RawBody(Message request)
        {
            // Octets HTTP BRUTS (DTD incluse) captures par l'encodeur custom
            // (PayGate.Hosting.DtdCapturingEncoder) dans HttpContext.Items : indispensable
            // pour que LoadInsecure voie la DTD (XXE V001 / billion laughs V006) et que
            // LoadSecure la refuse (N001/N002) sur le MEME flux brut.
            object stashed = null;
            if (request != null && request.Properties.ContainsKey("__PayGateRawBody"))
                stashed = request.Properties["__PayGateRawBody"];
            if (stashed == null)
            {
                HttpContext ctx = HttpContext.Current;
                if (ctx != null) stashed = ctx.Items["__PayGateRawBody"];
            }
            byte[] raw = stashed as byte[];
            if (raw != null && raw.Length > 0)
                return new MemoryStream(raw, false);

            // Repli : rebufferiser le Message WCF (si l'encodeur custom n'a pas tourne).
            var ms = new MemoryStream();
            var buffer = request.CreateBufferedCopy(int.MaxValue);
            using (var msg = buffer.CreateMessage())
            using (var xw = System.Xml.XmlWriter.Create(ms))
                msg.WriteMessage(xw);
            ms.Position = 0;
            return ms;
        }

        private static Message Fault(string reason)
        {
            return Message.CreateMessage(MessageVersion.Soap11, "Fault",
                new FaultBodyWriter(reason));
        }

        // Enveloppe le resultat typesafe dans un Message SOAP (DataContractSerializer),
        // requis par WCF pour les operations a parametre Message brut (Message in -> Message out).
        private static Message Reply(string action, object body)
        {
            return Message.CreateMessage(MessageVersion.Soap11, action, body);
        }

        // ---------------------------------------------------------------------
        // SubmitOrder : flux cible par V001 (XXE), V002 (injection XML / amount),
        // V006 (billion laughs), V003 (jeton falsifiable) et V007 (rejeu).
        // ---------------------------------------------------------------------
        public Message SubmitOrder(Message request)
        {
            XDocument doc = XmlParsing.LoadInsecure(RawBody(request));

            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated)
                throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "SubmitOrder");
            if (op == null) throw new FaultException("Operation SubmitOrder invalide.");

            //                  (ex. contenu de C:\lab\fixtures\xxe-canary.txt).
            string reference = XmlParsing.Field(op, "reference");
            decimal amount = XmlParsing.AmountInsecure(op);
            string currency = XmlParsing.Field(op, "currency") ?? "EUR";
            string iban = XmlParsing.Field(op, "creditorIban") ?? "";

            //                  cree a chaque fois un nouvel ordre (double execution).
            string newRef = string.IsNullOrEmpty(reference)
                ? "ORD-" + Guid.NewGuid().ToString("N").Substring(0, 8)
                : reference;

            // Collision de reference -> suffixe (preuve du rejeu : 2 ordres distincts).
            string storedRef = newRef;
            int n = 1;
            while (Store.Orders.ContainsKey(storedRef))
                storedRef = newRef + "-R" + (n++);

            Store.Orders[storedRef] = new Order
            {
                Reference = storedRef,
                Amount = amount,
                Currency = currency,
                CreditorIban = iban,
                Status = "EXECUTED",
                Owner = id.Username,
                ReceiptName = "ACK-" + storedRef + ".xml"
            };

            return Reply("SubmitOrderResponse", new SubmitResult
            {
                Reference = storedRef,
                Status = "EXECUTED",
                ReceiptName = "ACK-" + storedRef + ".xml",
                Message = "Ordre enregistre (ref=" + reference + ")"
            });
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N001 - operation soeur au parseur DURCI (XXE neutralise).
        // ---------------------------------------------------------------------
        public Message SubmitOrderV2(Message request)
        {
            XDocument doc;
            try
            {
                // Parseur durci : DTD interdite, resolver=null, XSD strict.
                doc = XmlParsing.LoadSecure(RawBody(request), XmlParsing.BuildStrictSchemas());
            }
            catch (Exception ex)
            {
                throw new FaultException("Validation XML refusee (SubmitOrderV2) : " + ex.Message);
            }

            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "SubmitOrderV2");
            string reference = XmlParsing.Field(op, "reference");
            decimal amount = decimal.Parse(XmlParsing.Field(op, "amount") ?? "0",
                System.Globalization.CultureInfo.InvariantCulture);
            Store.Orders[reference] = new Order
            {
                Reference = reference, Amount = amount,
                Currency = XmlParsing.Field(op, "currency") ?? "EUR",
                CreditorIban = XmlParsing.Field(op, "creditorIban") ?? "",
                Status = "EXECUTED", Owner = id.Username,
                ReceiptName = "ACK-" + reference + ".xml"
            };
            return Reply("SubmitOrderV2Response", new SubmitResult { Reference = reference, Status = "EXECUTED",
                ReceiptName = "ACK-" + reference + ".xml", Message = "OK (durci)" });
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N002 - QuoteOrder : XSD strict (injection XML + billion laughs
        // bloques). Simulation de frais, sans effet d'etat.
        // ---------------------------------------------------------------------
        public Message QuoteOrder(Message request)
        {
            try
            {
                var doc = XmlParsing.LoadSecure(RawBody(request), XmlParsing.BuildStrictSchemas());
                var op = XmlParsing.BodyChild(doc, "QuoteOrder");
                decimal amount = decimal.Parse(XmlParsing.Field(op, "amount") ?? "0",
                    System.Globalization.CultureInfo.InvariantCulture);
                return Reply("QuoteOrderResponse", new SubmitResult { Status = "QUOTED",
                    Message = "Frais estimes : " + (amount * 0.001m).ToString("0.00") });
            }
            catch (Exception ex)
            {
                throw new FaultException("Validation XSD refusee (QuoteOrder) : " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N006 - SubmitDirectDebit avec anti-rejeu (nonce + timestamp).
        // ---------------------------------------------------------------------
        public Message SubmitDirectDebit(Message request)
        {
            var doc = XmlParsing.LoadSecure(RawBody(request), XmlParsing.BuildStrictSchemas());
            var id = SoapAuth.ResolveIdentityVerified(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton non prouve.");

            var op = XmlParsing.BodyChild(doc, "SubmitDirectDebit");
            string nonce = XmlParsing.Field(op, "nonce");
            if (string.IsNullOrEmpty(nonce)) throw new FaultException("Nonce requis.");

            // Anti-rejeu : un nonce deja consomme est refuse (pas de double execution).
            if (!Store.ConsumedNonces.TryAdd("dd:" + nonce, DateTime.UtcNow))
                throw new FaultException("Rejeu detecte : nonce deja utilise.");

            string reference = XmlParsing.Field(op, "reference");
            Store.Orders["DD-" + reference] = new Order
            {
                Reference = "DD-" + reference,
                Amount = decimal.Parse(XmlParsing.Field(op, "amount") ?? "0",
                    System.Globalization.CultureInfo.InvariantCulture),
                Currency = "EUR", CreditorIban = XmlParsing.Field(op, "creditorIban") ?? "",
                Status = "EXECUTED", Owner = id.Username, ReceiptName = ""
            };
            return Reply("SubmitDirectDebitResponse", new SubmitResult { Reference = "DD-" + reference, Status = "EXECUTED",
                Message = "Prelevement enregistre." });
        }

        // ---------------------------------------------------------------------
        // GetOrder : flux cible par V011 (injection SQL) et V003 (acces sous une
        // identite falsifiee a un ordre de service-interne).
        // ---------------------------------------------------------------------
        public Message GetOrder(Message request)
        {
            XDocument doc = XmlParsing.LoadInsecure(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "GetOrder");
            string reference = XmlParsing.Field(op, "ref") ?? XmlParsing.Field(op, "reference");

            var rows = OrderRepository.GetOrderByRefInsecure(reference);

            // falsifiee en service-interne) peut relire un ordre d'un autre proprietaire.
            var first = rows.FirstOrDefault();
            if (first == null) throw new FaultException("Ordre introuvable.");
            return Reply("GetOrderResponse", first);
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N007 - GetOrderStatus : requete parametree + format valide.
        // ---------------------------------------------------------------------
        public Message GetOrderStatus(Message request)
        {
            var raw = RawBody(request);
            XDocument doc;
            try { doc = XmlParsing.LoadSecure(raw, XmlParsing.BuildStrictSchemas()); }
            catch { raw.Position = 0; doc = XDocument.Load(raw); } // GetOrderStatus n'a pas d'XSD dedie

            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "GetOrderStatus");
            string reference = XmlParsing.Field(op, "ref") ?? XmlParsing.Field(op, "reference");
            try
            {
                var o = OrderRepository.GetOrderStatusSecure(reference);
                if (o == null) throw new FaultException("Ordre introuvable.");
                return Reply("GetOrderStatusResponse", o);
            }
            catch (ArgumentException)
            {
                throw new FaultException("Reference invalide (format refuse).");
            }
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N003 - GetAccountStatement : jeton SOAP VERIFIE.
        // ---------------------------------------------------------------------
        public Message GetAccountStatement(Message request)
        {
            var doc = XmlParsing.LoadSecure(RawBody(request), XmlParsing.BuildStrictSchemas());
            var id = SoapAuth.ResolveIdentityVerified(XmlParsing.Header(doc));
            if (!id.Authenticated)
                throw new FaultException("Authentification refusee (jeton non prouve).");

            return Reply("GetAccountStatementResponse", new AccountStatement
            {
                Account = id.Username,
                Balance = string.Equals(id.Username, Lab.InternalAccount,
                                        StringComparison.OrdinalIgnoreCase) ? 99000.00m : 4250.00m,
                Currency = "EUR"
            });
        }

        // ---------------------------------------------------------------------
        // ListBatches : consultation des lots (operation metier legitime).
        // ---------------------------------------------------------------------
        public Message ListBatches(Message request)
        {
            var doc = XmlParsing.LoadInsecure(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            return Reply("ListBatchesResponse", Store.Batches.Values.Select(b => new BatchInfo
            {
                BatchId = b.BatchId, Status = b.Status, OrderCount = b.OrderRefs.Count
            }).ToArray());
        }

        // ---------------------------------------------------------------------
        // controle d'autorisation par fonction (BFLA). Un jeton standard l'execute.
        // (La fuite de cette operation dans le WSDL public = GT: APP07-V004.)
        // ---------------------------------------------------------------------
        public Message CancelAllBatches(Message request)
        {
            var doc = XmlParsing.LoadInsecure(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            // ECART : aucune verification de role/claim (operateur standard accepte).
            int affected = 0;
            foreach (var b in Store.Batches.Values)
                if (b.Status == "PENDING") { b.Status = "CANCELLED"; affected++; }

            return Reply("CancelAllBatchesResponse", new AdminActionResult
            {
                Success = true, Affected = affected,
                Message = affected + " lot(s) annule(s) par " + id.Username
            });
        }

        // =====================================================================
        // Enrichissement v23 - V016..V021 (ecarts) + N008..N013 (controles surs).
        // Idiomes identiques : RawBody + LoadInsecure (ecart) / LoadHardened (sur),
        // SoapAuth, Reply / FaultException.
        // =====================================================================

        // ---------------------------------------------------------------------
        // aucune restriction de schema/hote (file://, loopback, metadata cloud...).
        // ---------------------------------------------------------------------
        public Message ImportOrderFromUrl(Message request)
        {
            XDocument doc = XmlParsing.LoadInsecure(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "ImportOrderFromUrl");
            string sourceUrl = XmlParsing.Field(op, "sourceUrl");

            // ECART : requete serveur vers une URL entierement controlee par l'appelant.
            string fetched;
            try { fetched = RemoteFetch.FetchInsecure(sourceUrl); }
            catch (Exception ex) { throw new FaultException("Import impossible : " + ex.Message); }

            return Reply("ImportOrderFromUrlResponse", new ContentResult { Content = fetched });
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N008 - import avec allowlist stricte (https + hote de lab).
        // ---------------------------------------------------------------------
        public Message ImportOrderFromUrlV2(Message request)
        {
            XDocument doc = XmlParsing.LoadHardened(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "ImportOrderFromUrlV2");
            string sourceUrl = XmlParsing.Field(op, "sourceUrl");
            try
            {
                string fetched = RemoteFetch.FetchSecureAllowlist(sourceUrl);
                return Reply("ImportOrderFromUrlV2Response", new ContentResult { Content = fetched });
            }
            catch (UnauthorizedAccessException)
            {
                throw new FaultException("URL refusee (hors allowlist).");
            }
        }

        // ---------------------------------------------------------------------
        // ---------------------------------------------------------------------
        public Message FindBeneficiary(Message request)
        {
            XDocument doc = XmlParsing.LoadInsecure(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "FindBeneficiary");
            string name = XmlParsing.Field(op, "name");

            // ECART : nom concatene dans le XPath (' or '1'='1 renvoie tout).
            var rows = BeneficiaryDirectory.FindInsecure(name);
            return Reply("FindBeneficiaryResponse", rows.ToArray());
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N009 - recherche a format valide + correspondance exacte en code.
        // ---------------------------------------------------------------------
        public Message FindBeneficiarySecure(Message request)
        {
            XDocument doc = XmlParsing.LoadHardened(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "FindBeneficiarySecure");
            string name = XmlParsing.Field(op, "name");
            try
            {
                var rows = BeneficiaryDirectory.FindSecure(name);
                return Reply("FindBeneficiarySecureResponse", rows.ToArray());
            }
            catch (ArgumentException)
            {
                throw new FaultException("Nom de beneficiaire invalide (format refuse).");
            }
        }

        // ---------------------------------------------------------------------
        // absente) : un appelant anonyme recupere le journal (references, owner, IBAN).
        // ---------------------------------------------------------------------
        public Message ExportBatchJournal(Message request)
        {
            // ECART : aucun en-tete SOAP lu, aucun jeton verifie -> acces anonyme.
            XmlParsing.LoadInsecure(RawBody(request));
            return Reply("ExportBatchJournalResponse", new ContentResult { Content = BuildJournal() });
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N010 - meme journal, mais jeton SOAP VERIFIE requis (anonyme refuse).
        // ---------------------------------------------------------------------
        public Message ExportBatchJournalSecure(Message request)
        {
            XDocument doc = XmlParsing.LoadHardened(RawBody(request));
            var id = SoapAuth.ResolveIdentityVerified(XmlParsing.Header(doc));
            if (!id.Authenticated)
                throw new FaultException("Authentification refusee (jeton non prouve).");
            return Reply("ExportBatchJournalSecureResponse", new ContentResult { Content = BuildJournal() });
        }

        private static string BuildJournal()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var o in Store.Orders.Values)
                sb.Append(o.Reference).Append('|').Append(o.Owner).Append('|')
                  .Append(o.CreditorIban).Append('|').Append(o.Amount).Append(';');
            return sb.ToString();
        }

        // ---------------------------------------------------------------------
        // (chaine de connexion + chemin interne + hote) dans le faultstring.
        // ---------------------------------------------------------------------
        public Message GetOrderDiag(Message request)
        {
            XDocument doc = XmlParsing.LoadInsecure(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            // ECART : donnees systeme renvoyees telles quelles (includeExceptionDetailInFaults).
            string diag = "Echec acces base de donnees. ConnectionString=" +
                          OrderRepository.DiagConnString +
                          " ; AppRoot=" + AppDomain.CurrentDomain.BaseDirectory +
                          " ; Host=" + Environment.MachineName;
            throw new FaultException(diag);
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N012 - Fault generique assaini : aucune donnee systeme.
        // ---------------------------------------------------------------------
        public Message GetOrderDiagSecure(Message request)
        {
            XDocument doc = XmlParsing.LoadHardened(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            throw new FaultException("Erreur interne. Reference incident " +
                Guid.NewGuid().ToString("N").Substring(0, 8) + ". Contactez le support.");
        }

        // ---------------------------------------------------------------------
        // champs fournis, y compris owner et status (champs privilegies).
        // ---------------------------------------------------------------------
        public Message UpdateOrder(Message request)
        {
            XDocument doc = XmlParsing.LoadInsecure(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "UpdateOrder");
            string reference = XmlParsing.Field(op, "reference");
            Order o;
            if (string.IsNullOrEmpty(reference) || !Store.Orders.TryGetValue(reference, out o))
                throw new FaultException("Ordre introuvable.");

            // ECART : tout champ present est lie (pas d'allowlist).
            string amount = XmlParsing.Field(op, "amount");
            if (amount != null)
                o.Amount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
            string currency = XmlParsing.Field(op, "currency"); if (currency != null) o.Currency = currency;
            string owner = XmlParsing.Field(op, "owner"); if (owner != null) o.Owner = owner;        // privilegie
            string status = XmlParsing.Field(op, "status"); if (status != null) o.Status = status;   // privilegie

            return Reply("UpdateOrderResponse", ToResult(o));
        }

        // ---------------------------------------------------------------------
        // NC: APP07-N013 - mise a jour a liaison ALLOWLISTEE : seuls amount/currency
        // sont pris en compte ; owner et status (privilegies) sont IGNORES.
        // ---------------------------------------------------------------------
        public Message UpdateOrderSecure(Message request)
        {
            XDocument doc = XmlParsing.LoadHardened(RawBody(request));
            var id = SoapAuth.ResolveIdentityInsecure(XmlParsing.Header(doc));
            if (!id.Authenticated) throw new FaultException("Jeton SOAP absent.");

            var op = XmlParsing.BodyChild(doc, "UpdateOrderSecure");
            string reference = XmlParsing.Field(op, "reference");
            Order o;
            if (string.IsNullOrEmpty(reference) || !Store.Orders.TryGetValue(reference, out o))
                throw new FaultException("Ordre introuvable.");

            // Allowlist : amount et currency uniquement. owner/status NON lies (ignores).
            string amount = XmlParsing.Field(op, "amount");
            if (amount != null)
                o.Amount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
            string currency = XmlParsing.Field(op, "currency"); if (currency != null) o.Currency = currency;

            return Reply("UpdateOrderSecureResponse", ToResult(o));
        }

        private static OrderResult ToResult(Order o)
        {
            return new OrderResult
            {
                Reference = o.Reference, Amount = o.Amount, Currency = o.Currency,
                CreditorIban = o.CreditorIban, Status = o.Status, Owner = o.Owner,
                ReceiptName = o.ReceiptName
            };
        }
    }

    // Writer de Fault SOAP minimal pour les reponses d'erreur controlees.
    internal class FaultBodyWriter : BodyWriter
    {
        private readonly string _reason;
        public FaultBodyWriter(string reason) : base(true) { _reason = reason; }
        protected override void OnWriteBodyContents(System.Xml.XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Fault", "http://schemas.xmlsoap.org/soap/envelope/");
            writer.WriteElementString("faultstring", _reason);
            writer.WriteEndElement();
        }
    }
}
