// =============================================================================
// APP-07 "PayGate XML" - Hebergement WCF custom (GT: APP07-V001 / V006).
// Environnement de laboratoire - DONNEES 100% FICTIVES - usage defensif.
//
// Probleme : le lecteur XML par defaut de WCF refuse les DTD (DtdProcessing.Prohibit,
// verrouille) -> toute enveloppe contenant une DTD est rejetee (HTTP 400) AVANT que le
// service ne s'execute, ce qui empeche le flux XXE (V001) et billion laughs (V006).
//
// Solution : un encodeur de message qui CAPTURE les octets HTTP bruts (DTD incluse) dans
// HttpContext.Items, puis remet a WCF une enveloppe SOAP 1.1 MINIMALE valide. Le routage
// basicHttp se fait par l'en-tete SOAPAction (transport), pas par le corps, donc un corps
// vide suffit a dispatcher. Les operations relisent les octets bruts via RawBody() et les
// parsent avec LEUR propre parseur (LoadInsecure = resolveur actif ; LoadSecure = durci),
// ce qui preserve a la fois les ecarts (V001/V006) et les controles negatifs (N001/N002).
// =============================================================================
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Web;
using PayGate.Contracts;

namespace PayGate.Hosting
{
    internal static class RawBodyStash
    {
        public const string Key = "__PayGateRawBody";
    }

    // Encodeur : capture les octets bruts, remet a WCF une enveloppe SOAP 1.1 minimale.
    internal sealed class DtdCapturingEncoder : MessageEncoder
    {
        private readonly MessageEncoder _inner;
        private static readonly byte[] Stub = Encoding.UTF8.GetBytes(
            "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body/></s:Envelope>");

        public DtdCapturingEncoder(MessageEncoder inner) { _inner = inner; }

        public override string ContentType { get { return _inner.ContentType; } }
        public override string MediaType { get { return _inner.MediaType; } }
        public override MessageVersion MessageVersion { get { return _inner.MessageVersion; } }
        public override bool IsContentTypeSupported(string contentType) { return true; }

        private static void Stash(byte[] raw)
        {
            HttpContext ctx = HttpContext.Current;
            if (ctx != null) ctx.Items[RawBodyStash.Key] = raw;
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            byte[] raw = new byte[buffer.Count];
            Buffer.BlockCopy(buffer.Array, buffer.Offset, raw, 0, buffer.Count);
            Stash(raw);
            bufferManager.ReturnBuffer(buffer.Array);

            byte[] stub = bufferManager.TakeBuffer(Stub.Length);
            Buffer.BlockCopy(Stub, 0, stub, 0, Stub.Length);
            Message m = _inner.ReadMessage(new ArraySegment<byte>(stub, 0, Stub.Length), bufferManager, contentType);
            m.Properties[RawBodyStash.Key] = raw;   // porte fiable encodeur -> operation
            return m;
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            byte[] raw;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                raw = ms.ToArray();
            }
            Stash(raw);
            Message m = _inner.ReadMessage(new MemoryStream(Stub, false), maxSizeOfHeaders, contentType);
            m.Properties[RawBodyStash.Key] = raw;
            return m;
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            _inner.WriteMessage(message, stream);
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            return _inner.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
        }
    }

    internal sealed class DtdCapturingEncoderFactory : MessageEncoderFactory
    {
        private readonly MessageEncoderFactory _inner;
        private readonly MessageEncoder _encoder;

        public DtdCapturingEncoderFactory(MessageEncoderFactory inner)
        {
            _inner = inner;
            _encoder = new DtdCapturingEncoder(inner.Encoder);
        }

        public override MessageEncoder Encoder { get { return _encoder; } }
        public override MessageVersion MessageVersion { get { return _inner.MessageVersion; } }
        public override MessageEncoder CreateSessionEncoder() { return new DtdCapturingEncoder(_inner.CreateSessionEncoder()); }
    }

    internal sealed class DtdCapturingEncodingBindingElement : MessageEncodingBindingElement
    {
        private readonly TextMessageEncodingBindingElement _inner;

        public DtdCapturingEncodingBindingElement()
        {
            _inner = new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8);
            _inner.ReaderQuotas.MaxDepth = 64;
            _inner.ReaderQuotas.MaxStringContentLength = 2097152;
            _inner.ReaderQuotas.MaxArrayLength = 2097152;
            _inner.ReaderQuotas.MaxBytesPerRead = 2097152;
            _inner.ReaderQuotas.MaxNameTableCharCount = 2097152;
        }

        private DtdCapturingEncodingBindingElement(DtdCapturingEncodingBindingElement other)
        {
            _inner = (TextMessageEncodingBindingElement)other._inner.Clone();
        }

        public override MessageVersion MessageVersion
        {
            get { return _inner.MessageVersion; }
            set { _inner.MessageVersion = value; }
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new DtdCapturingEncoderFactory(_inner.CreateMessageEncoderFactory());
        }

        public override BindingElement Clone() { return new DtdCapturingEncodingBindingElement(this); }
        public override T GetProperty<T>(BindingContext context) { return _inner.GetProperty<T>(context); }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelListener<TChannel>();
        }
    }

    // Factory referencee par PaymentService.svc : monte un endpoint IPaymentService par
    // schema (http/https) avec le customBinding (encodeur capturant + transport HTTP[S]).
    public class PaymentServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            ServiceHost host = new ServiceHost(serviceType, baseAddresses);

            for (int i = 0; i < baseAddresses.Length; i++)
            {
                Uri baseAddr = baseAddresses[i];
                BindingElement transport;
                if (baseAddr.Scheme == Uri.UriSchemeHttps)
                    transport = new HttpsTransportBindingElement();
                else if (baseAddr.Scheme == Uri.UriSchemeHttp)
                    transport = new HttpTransportBindingElement();
                else
                    continue;

                ((TransportBindingElement)transport).MaxReceivedMessageSize = 2097152;

                CustomBinding binding = new CustomBinding(new DtdCapturingEncodingBindingElement(), transport);
                // Adresse RELATIVE obligatoire quand multipleSiteBindingsEnabled=true : WCF relie
                // l'endpoint au base address du bon schema (http/https) selon le transport du binding.
                host.AddServiceEndpoint(typeof(IPaymentService), binding, "");
            }

            // Metadonnees (?wsdl) sur http et https.
            ServiceMetadataBehavior smb = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null) { smb = new ServiceMetadataBehavior(); host.Description.Behaviors.Add(smb); }
            smb.HttpGetEnabled = true;
            smb.HttpsGetEnabled = true;

            return host;
        }
    }
}
