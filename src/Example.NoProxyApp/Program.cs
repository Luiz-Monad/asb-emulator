﻿// See https://aka.ms/new-console-template for more information
using Example.NoProxyApp;
using Example.NoProxyApp.Patches;

var patchSetup = new PatchSetup();
var testQueueName = "testQueue";
var client = new BusClient();

Console.WriteLine("Patching...");
AmqpTransportInitiatorPatch.RootCertificatePath = Settings.RootCertificatePath;
//patchSetup.Patch();
patchSetup.PatchCertOnly();

Console.WriteLine("Sending Message...");
await client.SendMessageAsync(testQueueName);

Console.WriteLine("Receiving Message...");
var response = await client.ReceiveMessageAsync(testQueueName);

Console.WriteLine("Received response: " + response.Body.ToString());