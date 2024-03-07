﻿using Example.NoProxyApp.Patches;
using HarmonyLib;

namespace Example.NoProxyApp
{
    internal class PatchSetup
    {
        private Harmony _harmony = new Harmony("com.devopsifyme.servicebusemulator.noproxyexample");

        public void Patch()
        {
            _harmony.PatchAll();
        }

        public void PatchCertOnly()
        {
            _harmony.CreateClassProcessor(typeof(AmqpTransportInitiatorPatch)).Patch();
        }
    }
}
