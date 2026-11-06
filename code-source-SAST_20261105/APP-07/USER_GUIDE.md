> 🇫🇷 Version française ci-dessous — 🇬🇧 English version below (same content).

# PayGate XML — Guide utilisateur

> **Environnement de test – données fictives.** Application de laboratoire ; toutes les données sont fictives.

## Présentation
PayGate XML est une passerelle de paiements inter-entreprises exposée sous forme de service web SOAP/XML. Elle permet à un opérateur d'entreprise de remettre des ordres de paiement, de les consulter et de récupérer leurs accusés de réception. Les utilisateurs sont des systèmes et opérateurs techniques qui dialoguent avec le service via des enveloppes SOAP, pas via une interface graphique.

## Accès
- URL : `https://paygate.lab.exemple` (HTTPS)
- Service SOAP : `POST /PaymentService.svc`
- Contrat (WSDL) : `GET /PaymentService.svc?wsdl`
- Comptes de démonstration (mot de passe `Password1!`) :
  - `operateur-ent` — opérateur d'entreprise, remise et consultation d'ordres (jeton SOAP standard).
  - `service-interne` — compte interne, opérations d'administration et traitement de lots.
- Authentification : jeton d'identité porté dans l'en-tête SOAP `wsse:Security` / `wsse:UsernameToken` (WS-Security simulé), en complément de la couche HTTP. Le code OTP de laboratoire, si demandé, est `123456`.

## Rôles et droits

| Rôle | Peut faire |
| --- | --- |
| operateur-ent | Soumettre un ordre (`SubmitOrder`), consulter un ordre (`GetOrder`), télécharger l'accusé de réception correspondant. |
| service-interne | Tout ce que fait l'opérateur, plus les opérations d'administration (gestion et annulation de lots, ex. `CancelAllBatches`, `ListBatches`). |

## Parcours principaux

### 1. Récupérer le contrat du service
1. Appeler `GET /PaymentService.svc?wsdl`.
2. Générer un proxy client à partir du WSDL ou construire les enveloppes SOAP manuellement.

### 2. S'authentifier (en-tête SOAP)
Chaque appel métier inclut un en-tête de sécurité :
```xml
<soap:Header>
  <wsse:Security xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
    <wsse:UsernameToken>
      <wsse:Username>operateur-ent</wsse:Username>
      <wsse:Password>Password1!</wsse:Password>
    </wsse:UsernameToken>
  </wsse:Security>
</soap:Header>
```

### 3. Remettre un ordre de paiement (`SubmitOrder`)
1. Construire l'enveloppe SOAP avec l'en-tête d'authentification et le corps de l'ordre.
2. Envoyer `POST /PaymentService.svc` (SOAPAction `SubmitOrder`).

Exemple de requête (fictive) :
```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:pay="https://paygate.lab.exemple/payment">
  <soap:Header>
    <wsse:Security xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
      <wsse:UsernameToken>
        <wsse:Username>operateur-ent</wsse:Username>
        <wsse:Password>Password1!</wsse:Password>
      </wsse:UsernameToken>
    </wsse:Security>
  </soap:Header>
  <soap:Body>
    <pay:SubmitOrder>
      <pay:reference>ORD-LAB-100</pay:reference>
      <pay:amount>100.00</pay:amount>
      <pay:currency>EUR</pay:currency>
      <pay:creditorIban>FR7630001007941234567890185</pay:creditorIban>
    </pay:SubmitOrder>
  </soap:Body>
</soap:Envelope>
```

Exemple de réponse (fictive) :
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:pay="https://paygate.lab.exemple/payment">
  <soap:Body>
    <pay:SubmitOrderResponse>
      <pay:reference>ORD-LAB-100</pay:reference>
      <pay:status>ACCEPTED</pay:status>
      <pay:receiptId>ACK-ORD-LAB-100</pay:receiptId>
    </pay:SubmitOrderResponse>
  </soap:Body>
</soap:Envelope>
```

### 4. Consulter un ordre (`GetOrder`)
1. Envoyer `POST /PaymentService.svc` (SOAPAction `GetOrder`) avec la référence de l'ordre.
```xml
<soap:Body>
  <pay:GetOrder>
    <pay:ref>ORD-LAB-100</pay:ref>
  </pay:GetOrder>
</soap:Body>
```
2. La réponse renvoie l'état, le montant et les informations de l'ordre.

### 5. Télécharger l'accusé de réception
1. Appeler `GET /receipts/download?name=ACK-ORD-LAB-100.xml`.
2. Le fichier d'accusé XML correspondant à l'ordre est retourné.

### 6. Administration des lots (service-interne)
1. S'authentifier avec le compte `service-interne`.
2. Lister les lots : `POST /PaymentService.svc` (SOAPAction `ListBatches`).
3. Gérer un lot selon le besoin métier (traitement, annulation) via les opérations d'administration prévues au contrat.

## Notes
- Bandeau « Environnement de test – données fictives » présent sur toutes les pages.
- Pour réinitialiser les données de démonstration : `POST /admin/reset`.

---

# English version

# PayGate XML — User guide

> **Test environment – fictitious data.** Laboratory application; all data is fictitious.

## Overview
PayGate XML is a business-to-business payment gateway exposed as a SOAP/XML web service. It lets a corporate operator submit payment orders, look them up and retrieve their acknowledgements. Users are technical systems and operators that talk to the service through SOAP envelopes, not through a graphical interface.

## Access
- URL: `https://paygate.lab.exemple` (HTTPS)
- SOAP service: `POST /PaymentService.svc`
- Contract (WSDL): `GET /PaymentService.svc?wsdl`
- Demonstration accounts (password `Password1!`):
  - `operateur-ent` — corporate operator, order submission and lookup (standard SOAP token).
  - `service-interne` — internal account, administration operations and batch processing.
- Authentication: identity token carried in the SOAP header `wsse:Security` / `wsse:UsernameToken` (simulated WS-Security), in addition to the HTTP layer. The laboratory OTP code, if requested, is `123456`.

## Roles and rights

| Role | Can do |
| --- | --- |
| operateur-ent | Submit an order (`SubmitOrder`), look up an order (`GetOrder`), download the corresponding acknowledgement. |
| service-interne | Everything the operator does, plus administration operations (batch management and cancellation, e.g. `CancelAllBatches`, `ListBatches`). |

## Main journeys

### 1. Retrieve the service contract
1. Call `GET /PaymentService.svc?wsdl`.
2. Generate a client proxy from the WSDL or build the SOAP envelopes manually.

### 2. Authenticate (SOAP header)
Every business call includes a security header:
```xml
<soap:Header>
  <wsse:Security xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
    <wsse:UsernameToken>
      <wsse:Username>operateur-ent</wsse:Username>
      <wsse:Password>Password1!</wsse:Password>
    </wsse:UsernameToken>
  </wsse:Security>
</soap:Header>
```

### 3. Submit a payment order (`SubmitOrder`)
1. Build the SOAP envelope with the authentication header and the order body.
2. Send `POST /PaymentService.svc` (SOAPAction `SubmitOrder`).

Example request (fictitious):
```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:pay="https://paygate.lab.exemple/payment">
  <soap:Header>
    <wsse:Security xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
      <wsse:UsernameToken>
        <wsse:Username>operateur-ent</wsse:Username>
        <wsse:Password>Password1!</wsse:Password>
      </wsse:UsernameToken>
    </wsse:Security>
  </soap:Header>
  <soap:Body>
    <pay:SubmitOrder>
      <pay:reference>ORD-LAB-100</pay:reference>
      <pay:amount>100.00</pay:amount>
      <pay:currency>EUR</pay:currency>
      <pay:creditorIban>FR7630001007941234567890185</pay:creditorIban>
    </pay:SubmitOrder>
  </soap:Body>
</soap:Envelope>
```

Example response (fictitious):
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:pay="https://paygate.lab.exemple/payment">
  <soap:Body>
    <pay:SubmitOrderResponse>
      <pay:reference>ORD-LAB-100</pay:reference>
      <pay:status>ACCEPTED</pay:status>
      <pay:receiptId>ACK-ORD-LAB-100</pay:receiptId>
    </pay:SubmitOrderResponse>
  </soap:Body>
</soap:Envelope>
```

### 4. Look up an order (`GetOrder`)
1. Send `POST /PaymentService.svc` (SOAPAction `GetOrder`) with the order reference.
```xml
<soap:Body>
  <pay:GetOrder>
    <pay:ref>ORD-LAB-100</pay:ref>
  </pay:GetOrder>
</soap:Body>
```
2. The response returns the status, amount and order details.

### 5. Download the acknowledgement
1. Call `GET /receipts/download?name=ACK-ORD-LAB-100.xml`.
2. The XML acknowledgement file corresponding to the order is returned.

### 6. Batch administration (service-interne)
1. Authenticate with the `service-interne` account.
2. List batches: `POST /PaymentService.svc` (SOAPAction `ListBatches`).
3. Manage a batch according to business need (processing, cancellation) via the administration operations defined in the contract.

## Notes
- "Test environment – fictitious data" banner present on all pages.
- To reset the demonstration data: `POST /admin/reset`.
