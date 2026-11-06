// =============================================================================
// APP-07 "PayGate XML" - Contrat de service d'ADMINISTRATION (WCF).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// NC: APP07-N004 / NC: APP07-N005
// Ce contrat INTERNE n'est PAS publie sur l'endpoint public /PaymentService.svc :
//  - il est heberge sur un endpoint interne distinct (AdminService.svc, non
//    expose via le WSDL public et refuse depuis l'exterieur) ;
//  - chaque operation applique un controle d'autorisation par role (service-interne).
// C'est le pendant SECURISE de APP07-V004 (fuite WSDL) et APP07-V005 (BFLA).
// =============================================================================
using System.ServiceModel;
using PayGate.Contracts;

namespace PayGate.Contracts
{
    [ServiceContract(Namespace = "https://paygate.lab.exemple/admin")]
    public interface IAdminService
    {
        // NC: APP07-N005 - operation d'administration soumise a autorisation par role.
        [OperationContract(Action = "PurgeArchivedBatches", ReplyAction = "PurgeArchivedBatchesResponse")]
        System.ServiceModel.Channels.Message PurgeArchivedBatches(System.ServiceModel.Channels.Message request);
    }
}
