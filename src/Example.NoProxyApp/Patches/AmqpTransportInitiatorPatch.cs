using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using HarmonyLib;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Example.NoProxyApp.Patches
{
    [HarmonyPatch(typeof(AmqpTransportInitiator), MethodType.Constructor, new[] { typeof(AmqpSettings), typeof(TransportSettings) })]
    internal static class AmqpTransportInitiatorPatch
    {
        public static string? RootCertificatePath { get; internal set; }

        static void Prefix(ref AmqpSettings settings, ref TransportSettings transportSettings)
        {
            if (transportSettings is TlsTransportSettings tlsTransportSettings)
            {
                tlsTransportSettings.CertificateValidationCallback = (message, cert, chain, errors) =>
                {
                    if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0) return false;
                    if (chain == null || cert == null || RootCertificatePath == null) return false;
                    var rootCertPath = Path.GetFullPath(RootCertificatePath);
                    var rootCertificate = new X509Certificate2(rootCertPath);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(rootCertificate);
                    var cert2 = new X509Certificate2(cert.Export(X509ContentType.Cert));
                    return chain.Build(cert2);
                };
            }
        }
    }
}
