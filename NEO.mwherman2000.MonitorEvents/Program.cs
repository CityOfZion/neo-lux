using Neo.Lux.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neo.Lux.Utils;
using Neo.Lux.Cryptography;

namespace NEO.mwherman2000.MonitorEvents
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = NeoRPC.ForPrivateNet();

            uint currentBlock = api.GetBlockHeight();
            Console.WriteLine($"currentBlock: {currentBlock}");

            uint deployTxBlock = 31163;
            string deployScriptHashHex = "0xf036ad1b59f21c09e3247fd5acb3bc73669547d2";
            Console.WriteLine($"deployScriptHash: {deployScriptHashHex}");
            string deployTxHashHex = "0xf523b9c3a1bb3f599c9732020036bafbb4c2bd6f6d1a72edc880d793706bd95d";
            Console.WriteLine($"deployTxHash: {deployTxHashHex}");
            Transaction deployTx = api.GetTransaction(deployTxHashHex.Replace("0x", ""));
            Console.WriteLine($"deployTx: {JsonConvert.SerializeObject(deployTx)}");
            byte[] deployTxScript = deployTx.script;
            int deployTxScriptLength = deployTxScript.Length;
            Console.WriteLine($"deployTxScriptLength: {deployTxScriptLength}");

            UInt160 deployTxScriptHash = deployTxScript.ToScriptHash();
            string deployTxSciptToScriptHashString = deployTxScriptHash.ToString();
            Console.WriteLine($"deployTxSciptToScriptHashString: {deployTxSciptToScriptHashString}");

            {
                string invokeTxHashHex = "0xe08ce09b03420fcf472896eb0063d103897ae19359f8fe796032bf890263df1a";
                Console.WriteLine($"invokeTxHashHex: {invokeTxHashHex}");
                Transaction invokeTx = api.GetTransaction(invokeTxHashHex.Replace("0x", ""));
                Console.WriteLine($"invokeTx: {JsonConvert.SerializeObject(invokeTx)}");
                byte[] invokeTxScript = invokeTx.script;
                int invokeTxScriptLength = invokeTxScript.Length;
                Console.WriteLine($"invokeTxScriptLength: {invokeTxScriptLength}");

                UInt160 invokeTxScriptHash = invokeTxScript.ToScriptHash();
                string invokeTxSciptToScriptHashString = invokeTxScriptHash.ToString();
                Console.WriteLine($"invokeTxSciptToScriptHashString: {invokeTxSciptToScriptHashString}");
            }

            Console.WriteLine("Monitoring: press Enter to start monitoring...");
            Console.ReadLine();

            //var monitor = new NeoEventMonitor0(api);
            //monitor.Run(500);

            var monitor = new NeoEventMonitor1(api, deployTx, deployTxBlock-5);
            monitor.Run();

            Console.WriteLine("Monitoring: press Enter to stop monitoring...");
            Console.ReadLine();

            monitor.Stop();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
