using Example.NoProxyApp;
using Example.NoProxyApp.Patches;
using HarmonyLib;

namespace ServiceBusEmulator.IntegrationTests.Patches
{
    static class Patch
    {
        private static PatchSetup patchSetup = new PatchSetup();

        public static void Run()
        {
            Harmony.DEBUG = true;
            AmqpTransportInitiatorPatch.RootCertificatePath = "./cacert.cer";
            patchSetup.PatchCertOnly();
        }
    }

}
