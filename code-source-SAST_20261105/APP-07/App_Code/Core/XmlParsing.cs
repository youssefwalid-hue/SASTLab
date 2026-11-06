// =============================================================================
// APP-07 "PayGate XML" - Lecture / parsing des enveloppes SOAP.
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// Deux parseurs coexistent :
//  - LoadInsecure()  : DtdProcessing=Parse + XmlResolver actif + expansion
//    d'entites NON bornee, SANS validation XSD -> ecarts volontaires
//    APP07-V001 (XXE), APP07-V002 (injection XML / amount), APP07-V006
//    (billion laughs). Utilise par SubmitOrder.
//  - LoadSecure()    : DtdProcessing=Prohibit + XmlResolver=null +
//    MaxCharactersFromEntities borne + validation XSD stricte -> comportement
//    SECURISE, NC APP07-N001 (SubmitOrderV2) et NC APP07-N002 (QuoteOrder).
// =============================================================================
using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace PayGate.Core
{
    public static class XmlParsing
    {
        public static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
        public static readonly XNamespace Pay = "https://paygate.lab.exemple/payment";

        // --- Parseur NON durci (ecarts V001 / V002 / V006) -----------------------
        //                  resolution d'entites externes (file://...).
        //                  (MaxCharactersFromEntities=0 => illimite).
        public static XDocument LoadInsecure(Stream xml)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,           // DTD autorisee (ecart)
                XmlResolver = new XmlUrlResolver(),            // entites externes resolues (ecart)
                MaxCharactersFromEntities = 0,                 // expansion non bornee (ecart)
                MaxCharactersInDocument = 0
            };
            using (var reader = XmlReader.Create(xml, settings))
                return XDocument.Load(reader);
        }

        // --- Parseur DURCI (NC N001 / N002) --------------------------------------
        // NC: APP07-N001 / NC: APP07-N002 - DTD interdite, resolver neutralise,
        // expansion plafonnee, validation XSD stricte.
        public static XDocument LoadSecure(Stream xml, XmlSchemaSet schemas)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,        // DTD refusee
                XmlResolver = null,                            // pas de resolution externe
                MaxCharactersFromEntities = 1024,              // expansion bornee
                MaxCharactersInDocument = 256 * 1024,          // taille de message bornee
                ValidationType = ValidationType.Schema,
                Schemas = schemas
            };
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += (s, e) =>
            {
                throw new XmlSchemaValidationException(e.Message);
            };

            using (var reader = XmlReader.Create(xml, settings))
                return XDocument.Load(reader);
        }

        // --- Parseur DURCI sans XSD dedie (NC N008..N013) -----------------------
        // DTD interdite + resolver neutralise + expansion/taille bornees, mais SANS
        // validation contre le schema SubmitOrder : utilise par les operations soeurs
        // sures qui portent leur propre logique de validation (allowlist URL, format
        // de nom, liaison allowlistee de champs...). Neutralise XXE/DTD a la lecture.
        public static XDocument LoadHardened(Stream xml)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,        // DTD refusee
                XmlResolver = null,                            // pas de resolution externe
                MaxCharactersFromEntities = 1024,              // expansion bornee
                MaxCharactersInDocument = 256 * 1024           // taille de message bornee
            };
            using (var reader = XmlReader.Create(xml, settings))
                return XDocument.Load(reader);
        }

        public static XElement Header(XDocument doc)
        {
            return doc.Root == null ? null : doc.Root.Element(Soap + "Header");
        }

        public static XElement BodyChild(XDocument doc, string localName)
        {
            if (doc.Root == null) return null;
            var body = doc.Root.Element(Soap + "Body");
            if (body == null) return null;
            return body.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
        }

        public static string Field(XElement op, string localName)
        {
            if (op == null) return null;
            var el = op.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
            return el == null ? null : el.Value;
        }

        // amount dupliques (via commentaires XML injectes), on retient le DERNIER
        // noeud texte, donc une valeur differente de celle affichee en tete.
        public static decimal AmountInsecure(XElement op)
        {
            var amounts = op == null ? null : op.Elements().Where(e => e.Name.LocalName == "amount").ToList();
            if (amounts == null || amounts.Count == 0) return 0m;

            // Dernier element amount, dernier noeud texte de cet element.
            var last = amounts.Last();
            var textNodes = last.Nodes().OfType<XText>().ToList();
            string raw = textNodes.Count > 0
                ? textNodes.Last().Value.Trim()
                : last.Value.Trim();

            decimal v;
            decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                             System.Globalization.CultureInfo.InvariantCulture, out v);
            return v;
        }

        // Schema XSD strict partage par les operations durcies (NC N001 / N002).
        public static XmlSchemaSet BuildStrictSchemas()
        {
            const string xsd = @"<?xml version='1.0'?>
<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'
           targetNamespace='https://paygate.lab.exemple/payment'
           xmlns:pay='https://paygate.lab.exemple/payment'
           elementFormDefault='qualified'>
  <xs:element name='SubmitOrderV2' type='pay:OrderType'/>
  <xs:element name='QuoteOrder'    type='pay:OrderType'/>
  <xs:complexType name='OrderType'>
    <xs:sequence>
      <xs:element name='reference'    type='pay:RefType'     minOccurs='1' maxOccurs='1'/>
      <xs:element name='amount'       type='xs:decimal'      minOccurs='1' maxOccurs='1'/>
      <xs:element name='currency'     type='pay:CurrencyType' minOccurs='1' maxOccurs='1'/>
      <xs:element name='creditorIban' type='pay:IbanType'    minOccurs='0' maxOccurs='1'/>
      <xs:element name='nonce'        type='xs:string'       minOccurs='0' maxOccurs='1'/>
    </xs:sequence>
  </xs:complexType>
  <xs:simpleType name='RefType'>
    <xs:restriction base='xs:string'><xs:pattern value='[A-Za-z0-9\-]{1,32}'/></xs:restriction>
  </xs:simpleType>
  <xs:simpleType name='CurrencyType'>
    <xs:restriction base='xs:string'><xs:pattern value='[A-Z]{3}'/></xs:restriction>
  </xs:simpleType>
  <xs:simpleType name='IbanType'>
    <xs:restriction base='xs:string'><xs:pattern value='[A-Z0-9]{15,34}'/></xs:restriction>
  </xs:simpleType>
</xs:schema>";
            var set = new XmlSchemaSet { XmlResolver = null };
            using (var sr = new StringReader(xsd))
            using (var xr = XmlReader.Create(sr))
                set.Add(null, xr);
            return set;
        }
    }
}
