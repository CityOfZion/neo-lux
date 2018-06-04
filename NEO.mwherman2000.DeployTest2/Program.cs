using Neo.Lux.Core;
using Neo.Lux.Cryptography;
using Neo.Lux.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

using Neo.SmartContract;

namespace NEO.mwherman2000.DeployTest2
{
    class Program
    {
        const string demoPrivateKeyHex ="a9e2b5436cab6ff74be2d5c91b8a67053494ab5b454ac2851f872fb0fd30ba5e";

        const string my0Address = "AR56pm3Ka5vtYGyxK7b4J4NzBQz1xkxjg4";
        const string my0PrivateKeyHex = "eab8bcb92f67d45e1d7f899f9e14db5aa7bd7d7a9c6245d25f26def073893bb7";
        const string my0PrivateKeyWIF = "L55yjoVA4m65GtcZ14aa241J1sfVhbskC2jDYJV6fTxfCzkxaCYs";
        const string my0PublicKey = "02c4367ec810d50f9a5783a3f9ecaab085d6565cffa51c03b431af3d55832126e6";

        const string my1Address =       "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y";
        const string my1PrivateKeyHex = "1dd37fba80fec4e6a6f13fd708d8dcb3b29def768017052f6c930fa1c5d90bbb";
        const string my1PrivateKeyWIF = "KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr";
        const string my1PublicKey =     "031a6c6fbbdf02ca351745fa86b9ba5a9452d785ac4f7fc2b7548ca2a46c4fcf4a";
                                        // 0xb7743f023b2cbb367f21b26d8da837929f1309f3
        const string avmFilename =      @"D:\repos\neo-lux\NEO.mwherman2000.DeployTestSC1\bin\Debug\NEO.mwherman2000.DeployTestSC1.avm";


        static void Main(string[] args)
        {
            var api = NeoRPC.ForPrivateNet();

            api.SetLogger(LocalLogger);

            var demoKeys = new KeyPair(demoPrivateKeyHex.HexToBytes());
            Console.WriteLine($"demoPrivateKeyHex.keys:\t{JsonConvert.SerializeObject(demoKeys)}");
            var balances = api.GetAssetBalancesOf(demoKeys.address);
            foreach (var entry in balances)
            {
                Console.WriteLine(entry.Key + "\t" + entry.Value);
            }

            var my0Keys = new KeyPair(my0PrivateKeyHex.HexToBytes());
            Console.WriteLine($"my0PrivateKeyHex.keys:\t{JsonConvert.SerializeObject(my0Keys)}");
            var my1Keys = new KeyPair(my1PrivateKeyHex.HexToBytes());
            Console.WriteLine($"my1PrivateKeyHex.keys:\t{JsonConvert.SerializeObject(my1Keys)}");

            Console.WriteLine();
            Console.WriteLine("Balances:");
            balances = api.GetAssetBalancesOf(my0Keys.address);
            foreach (var entry in balances)
            {
                Console.WriteLine(entry.Key + "\t" + entry.Value);
            }
            balances = api.GetAssetBalancesOf(my1Keys.address);
            foreach (var entry in balances)
            {
                Console.WriteLine(entry.Key + "\t" + entry.Value);
            }

            Console.WriteLine();
            Console.WriteLine("Unspent Balances:");
            var unspentBalances = api.GetUnspent(my0Keys.address);
            foreach (var entry in unspentBalances)
            {
                Console.WriteLine(entry.Key + "\t" + JsonConvert.SerializeObject(entry.Value));
            }
            unspentBalances = api.GetUnspent(my1Keys.address);
            foreach (var entry in unspentBalances)
            {
                Console.WriteLine(entry.Key + "\t" + JsonConvert.SerializeObject(entry.Value));
            }

            Console.WriteLine();
            Console.WriteLine("GetClaimable Balances:");
            decimal amount = 0;
            var claimableGas = api.GetClaimable(my0Keys.PublicKeyHash, out amount);
            Console.WriteLine($"GetClaimable:\t{amount}");
            foreach (var entry in claimableGas)
            {
                Console.WriteLine(JsonConvert.SerializeObject(entry));
            }
            claimableGas = api.GetClaimable(my1Keys.PublicKeyHash, out amount);
            Console.WriteLine($"GetClaimable:\t{amount}");
            foreach (var entry in claimableGas)
            {
                Console.WriteLine(JsonConvert.SerializeObject(entry));
            }

            Console.WriteLine();
            Console.WriteLine("AVM:");
            byte[] avmBytes = File.ReadAllBytes(avmFilename);
            Console.WriteLine($"Bytes:\t{avmFilename}\t{avmBytes.Length} bytes");
            Console.WriteLine($"Bytes:\t{avmFilename}\t'{avmBytes.ToHexString()}'");

            Console.WriteLine();
            Console.WriteLine("AVM Script Hash:");
            UInt160 avmScriptHash = avmBytes.ToScriptHash();
            string avmScriptHashString = avmBytes.ToScriptHash().ToString();
            string avmAddress = avmScriptHash.ToAddress();
            byte[] avmScriptHashBytes = avmScriptHash.ToArray();
            string avmScriptHashBytesHex = avmScriptHashBytes.ToHexString();
            Console.WriteLine($"avmScriptHash:\t{JsonConvert.SerializeObject(avmScriptHash)}");
            Console.WriteLine($"avmScriptHashString:\t{avmScriptHashString}");
            Console.WriteLine($"avmAddress:\t{avmAddress}");
            Console.WriteLine($"avmScriptHashBytes:\t{JsonConvert.SerializeObject(avmScriptHashBytes)}");
            Console.WriteLine($"avmScriptHashBytesHex:\t{avmScriptHashBytesHex}");

            Console.WriteLine();
            Console.WriteLine("Press EXIT to exit...");
            Console.ReadLine();

            //byte[] parameterTypes = { (byte)ContractParameterType.String, (byte)ContractParameterType.Array };
            //byte returnType = (byte)ContractParameterType.Void;
            //ContractPropertyState properties = ContractPropertyState.NoProperty;
            //Transaction tDeploy = api.DeployContract(my0Keys, avmBytes, parameterTypes, returnType, properties, "Demo AVM", "1.0.0", "Able Baker", "able@baker.com", "Demonstration AVM");
            //Console.WriteLine($"tDeploy:\t{JsonConvert.SerializeObject(tDeploy)}");
            //File.WriteAllText("tDeploy.json", JsonConvert.SerializeObject(tDeploy));

            //object[] testargs = { "a", "b", "c" };
            //InvokeResult response = api.InvokeScript(avmScriptHash, "testoperation", testargs);
            //Console.WriteLine($"response:\t{JsonConvert.SerializeObject(response)}");
            //Console.WriteLine($"state:\t{response.state}");
            //Console.WriteLine($"stack:\t{JsonConvert.SerializeObject(response.stack)}");
            //Console.WriteLine($"stack[0]:\t{response.stack[0]}");
            ////object obj = response.stack[1];
            ////byte[] stack1Bytes = (byte[])obj;
            ////string stack1String1 = stack1Bytes.ToHexString();
            ////Console.WriteLine($"stack1String1:\t{stack1String1}");
            ////string stack1String2 = stack1Bytes.ByteToHex();
            ////Console.WriteLine($"stack1String2:\t{stack1String2}");

            string testoperation = "testoperation2";
            Console.WriteLine($"{testoperation}\t{ToHex(testoperation)}");
            object[] testargs = new object[] { "a", "b", "c", "d" };
            Console.WriteLine($"{testargs[0]}\t{ToHex((string)testargs[0])}");
            Console.WriteLine($"{testargs[1]}\t{ToHex((string)testargs[1])}");
            Console.WriteLine($"{testargs[2]}\t{ToHex((string)testargs[2])}");
            Console.WriteLine($"{testargs[3]}\t{ToHex((string)testargs[3])}");
            Transaction tResult = api.CallContract(my0Keys, avmScriptHash, testoperation, testargs );
            Console.WriteLine($"tResult:\t{JsonConvert.SerializeObject(tResult)}");
            File.WriteAllText("tResult.json", JsonConvert.SerializeObject(tResult));

            Console.WriteLine();
            Console.WriteLine("Press EXIT to exit...");
            Console.ReadLine();
        }

        private static void LocalLogger(string s)
        {
            Console.WriteLine($"LOGGER: '{s}'");
        }

        private static string ToHex(string buffer)
        {
            string result = "0x";

            if (buffer == null) return "";

            Int16 length = (Int16)buffer.Length;

            if (length == 0) return result;

            int chArrayLength = length * 2;

            char[] chArray = new char[chArrayLength];
            int i = 0;
            int index = 0;
            for (i = 0; i < chArrayLength; i += 2)
            {
                byte b = (byte)buffer[index++];
                chArray[i] = ToHexChar(b / 16);
                chArray[i + 1] = ToHexChar(b % 16);
            }

            return (result + new String(chArray, 0, chArray.Length));
        }

        private static string ToHex(byte[] buffer)
        {
            string result = "0x";

            if (buffer == null) return "";

            Int16 length = (Int16)buffer.Length;

            if (length == 0) return result;

            int chArrayLength = length * 2;

            char[] chArray = new char[chArrayLength];
            int i = 0;
            int index = 0;
            for (i = 0; i < chArrayLength; i += 2)
            {
                byte b = buffer[index++];
                chArray[i] = ToHexChar(b / 16);
                chArray[i + 1] = ToHexChar(b % 16);
            }

            return (result + new String(chArray, 0, chArray.Length));
        }

        private static char ToHexChar(int i)
        {
            if (i >= 0 && i < 16)
            {
                if (i < 10)
                {
                    return (char)(i + '0');
                }
                else
                {
                    return (char)(i - 10 + 'A');
                }
            }
            else
            {
                return '?';
            }
        }
    }
}
