using LunarParser;
using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Cryptography;
using Neo.VM;
using System.Numerics;
using Neo.Emulator;
using System.Diagnostics;

namespace NeoLux
{
    public class NeoException : Exception
    {
        public NeoException(string msg) : base (msg)
        {

        }
    }

    public struct InvokeResult
    {
        public string state;
        public decimal gasSpent;
        public object result;
    }

    public abstract class NeoAPI
    {
        private static Dictionary<string, string> _systemAssets = null;

        internal static Dictionary<string, string> GetAssetsInfo()
        {
            if (_systemAssets == null)
            {
                _systemAssets = new Dictionary<string, string>();
                AddAsset("NEO", "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b");
                AddAsset("GAS", "602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7");
            }

            return _systemAssets;
        }

        private static void AddAsset(string symbol, string hash)
        {
            _systemAssets[symbol] = hash;
        }

        public static IEnumerable<string> AssetSymbols
        {
            get
            {
                var info = GetAssetsInfo();
                return info.Keys;
            }
        }

        public static string SymbolFromAssetID(string assetID)
        {
            if (assetID == null)
            {
                return null;
            }

            if (assetID.StartsWith("0x"))
            {
                assetID = assetID.Substring(2);
            }

            var info = GetAssetsInfo();
            foreach (var entry in info)
            {
                if (entry.Value == assetID)
                {
                    return entry.Key;
                }
            }

            return null;
        }

        private static Dictionary<string, string> _tokenScripts = null;

        internal static Dictionary<string, string> GetTokenInfo()
        {
            if (_tokenScripts == null)
            {
                _tokenScripts = new Dictionary<string, string>();
                AddToken("RPX", "ecc6b20d3ccac1ee9ef109af5a7cdb85706b1df9");
                AddToken("DBC", "b951ecbbc5fe37a9c280a76cb0ce0014827294cf");
                AddToken("QLC", "0d821bd7b6d53f5c2b40e217c6defc8bbe896cf5");
                AddToken("APH", "a0777c3ce2b169d4a23bcba4565e3225a0122d95");
                AddToken("ZPT", "ac116d4b8d4ca55e6b6d4ecce2192039b51cccc5");
                AddToken("TKY", "132947096727c84c7f9e076c90f08fec3bc17f18");
                AddToken("TNC", "08e8c4400f1af2c20c28e0018f29535eb85d15b6");
                AddToken("CPX", "45d493a6f73fa5f404244a5fb8472fc014ca5885");
                AddToken("ACAT", "7f86d61ff377f1b12e589a5907152b57e2ad9a7a");
                AddToken("NRV", "2e25d2127e0240c6deaf35394702feb236d4d7fc");
            }

            return _tokenScripts;
        }

        private static void AddToken(string symbol, string hash)
        {
            _tokenScripts[symbol] = hash;
        }

        public static IEnumerable<string> TokenSymbols
        {
            get
            {
                var info = GetTokenInfo();
                return info.Keys;
            }
        }

        private static int level = 0;
        protected static object ParseStack(DataNode stack)
        {
            object results = null;
            object[] items = null;

            if (stack != null)
            {
                //Console.WriteLine("level0:\t" + level.ToString() + "\tName: " + stack.Name + "\tKind: " + stack.Kind.ToString() + "\tChildren: " + stack.ChildCount.ToString());
                level++;
                if (stack.Children.Count() > 0 && stack.Name == "stack")
                {
                    foreach (var stackChild in stack.Children)
                    {
                        if (stackChild.ChildCount >= 1)
                        {
                            items = ParseStackItems(stackChild.Children);
                        }
                    }
                }
                level--;
            }

            if (items.Length > 0)
            {
                results = items[0];
            }

            return results;
        }

        protected static object[] ParseStackItems(IEnumerable<DataNode> stackItems)
        {
            object[] results = null;

            level++;
            var items = new List<object>();
            foreach (DataNode stackChildItem in stackItems)
            {
                string Name = stackChildItem.Name;
                NodeKind Kind = stackChildItem.Kind;
                string Value = stackChildItem.Value;
                string type = stackChildItem.GetString("type");
                string value = stackChildItem.GetString("value");
                
                //Console.WriteLine("level1:\t" + level.ToString() + "\tName: " + (String.IsNullOrEmpty(Name) ? "<null>" : Name) + "\tKind: " + Kind.ToString() + "\tValue: " + (String.IsNullOrEmpty(Value) ? "<null>" : Value) + "\ttype: " + (String.IsNullOrEmpty(type) ? "<null>" : type) + "\tvalue: " + (String.IsNullOrEmpty(value) ? "<null>" : value) + "\tChildCount: " + stackChildItem.ChildCount.ToString());

                object temp = null;
                switch (Kind)
                {
                    case NodeKind.String:
                        {
                            // TODO
                            break;
                        }
                    case NodeKind.Object:
                        {
                            switch (type)
                            {
                                case "ByteArray":
                                    {
                                        temp = value.HexToBytes();
                                        break;
                                    }

                                case "Boolean":
                                    {
                                        temp = (value.ToLower() == "true");
                                        break;
                                    }

                                case "Integer":
                                    {
                                        BigInteger intVal;
                                        BigInteger.TryParse(value, out intVal);
                                        temp = intVal;
                                        break;
                                    }
                                case "Array": // Type
                                    {
                                        temp = ParseStackItems(stackChildItem.Children);
                                        break;
                                    }
                                default:
                                    {
                                        //Console.WriteLine("ParseStack:unknown DataNode stack type: '" + type + "'");
                                        break;
                                    }
                            }
                            break;
                        }
                    case NodeKind.Array:
                        {
                            temp = ParseStackItems(stackChildItem.Children);
                            break;
                        }
                    default:
                        {
                            //throw new ArgumentOutOfRangeException("ParseStack:unknown DataNode stack type: '" + type + "'");
                            //Console.WriteLine("ParseStack:unknown DataNode stack Kind: " + Kind.ToString());
                            break;
                        }
                }

                if (temp == null)
                {
                    // Do nothing
                }
                else
                {
                    items.Add(temp);
                }
            }
            results = items.ToArray();
            level--;

            return results;
        }

        public abstract InvokeResult TestInvokeScript(string scriptHash, object[] args);

        public InvokeResult TestInvokeScript(string scriptHash, string operation, object[] args)
        {
            return TestInvokeScript(scriptHash, new object[] { operation, args });
        }


        private static void EmitObject(ScriptBuilder sb, object item)
        {
            if (item is byte[])
            {
                var arr = (byte[])item;
                sb.EmitPush(arr);
            }
            else
            if (item is object[])
            {
                var list = (object[])item;

                for (int index = 0; index < list.Length; index++)
                {
                    EmitObject(sb, list[index]);
                }

                sb.EmitPush(list.Length);
                sb.Emit(OpCode.PACK);
            }
            else
            if (item == null)
            {
                sb.EmitPush("");
            }
            else
            if (item is string)
            {
                sb.EmitPush((string)item);
            }
            else
            if (item is bool)
            {
                sb.EmitPush((bool)item);
            }
            else
            if (item is BigInteger)
            {
                sb.EmitPush((BigInteger)item);
            }
            else
            {
                throw new Exception("Unsupported contract parameter: " + item.ToString());
            }
        }

        public static byte[] GenerateScript(string scriptHash, object[] args)
        {
            using (var sb = new ScriptBuilder())
            {
                var items = new Stack<object>();

                if (args != null)
                {
                    foreach (var item in args)
                    {
                        items.Push(item);
                    }
                }

                while (items.Count > 0)
                {
                    var item = items.Pop();
                    EmitObject(sb, item);
                }

                var temp = scriptHash.HexToBytes().Reverse().ToArray();
                sb.EmitAppCall(temp, false);

                var bytes = sb.ToArray();

                string hex = bytes.ByteToHex();
                return bytes;
            }
        }

        public bool CallContract(KeyPair key, string scriptHash, object[] args)
        {
            var bytes = GenerateScript(scriptHash, args);
            return CallContract(key, scriptHash, bytes);
        }

        public bool CallContract(KeyPair key, string scriptHash, string operation, object[] args)
        {
            return CallContract(key, scriptHash, new object[] { operation, args });
        }

        private void GenerateInputsOutputs(KeyPair key, string outputHash, Dictionary<string, decimal> ammounts, out List<Transaction.Input> inputs, out List<Transaction.Output> outputs)
        {
            if (ammounts == null || ammounts.Count == 0)
            {
                throw new NeoException("Invalid amount list");
            }

            //var outputHash = toAddress.GetScriptHashFromAddress().ToHexString();
            
            var unspent = GetUnspent(key.address);

            inputs = new List<Transaction.Input>();
            outputs = new List<Transaction.Output>();

            foreach (var entry in ammounts)
            {
                var symbol = entry.Key;

                if (!unspent.ContainsKey(symbol))
                {
                    throw new NeoException($"Not enough {symbol} in address {key.address}");
                }
            }

            foreach (var entry in ammounts)
            {
                var symbol = entry.Key;
                var cost = entry.Value;

                string assetID;

                var info = GetAssetsInfo();
                if (info.ContainsKey(symbol))
                {
                    assetID = info[symbol];
                }
                else
                {
                    throw new NeoException($"{symbol} is not a valid blockchain asset.");
                }

                var sources = unspent[symbol];

                decimal selected = 0;
                foreach (var src in sources)
                {
                    selected += src.value;

                    var input = new Transaction.Input()
                    {
                        prevHash = src.txid,
                        prevIndex = src.index,
                    };

                    inputs.Add(input);

                    if (selected >= cost)
                    {
                        break;
                    }
                }

                if (selected < cost)
                {
                    throw new NeoException($"Not enough {symbol}");
                }

                if(cost > 0)
                {
                    var output = new Transaction.Output()
                    {
                        assetID = assetID,
                        scriptHash = outputHash,
                        value = cost
                    };
                    outputs.Add(output);
                }

                if (selected > cost)
                {
                    var left = selected - cost;

                    var change = new Transaction.Output()
                    {
                        assetID = assetID,
                        scriptHash = LuxUtils.reverseHex(key.signatureHash.ToArray().ByteToHex()),
                        value = left
                    };
                    outputs.Add(change);
                }
            }
        }

        public bool CallContract(KeyPair key, string scriptHash, byte[] bytes)
        {
            /*var invoke = TestInvokeScript(net, bytes);
            if (invoke.state == null)
            {
                throw new Exception("Invalid script invoke");
            }

            decimal gasCost = invoke.gasSpent;*/


            List<Transaction.Input> inputs;
            List<Transaction.Output> outputs;
            var gasCost = 0;

            GenerateInputsOutputs(key, scriptHash, new Dictionary<string, decimal>() { { "GAS", gasCost } }, out inputs, out outputs);

            Transaction tx = new Transaction()
            {
                type = 0xd1,
                version = 0,
                script = bytes,
                gas = gasCost,
                inputs = inputs.ToArray(),
                outputs = outputs.ToArray()
            };

            tx = tx.Sign(key);

            var hexTx = tx.SerializeTransaction(true);

            return SendRawTransaction(hexTx);
        }

        public abstract bool SendRawTransaction(string hexTx);

        public abstract byte[] GetStorage(string scriptHash, byte[] key);

        public bool SendAsset(KeyPair fromKey, string toAddress, string symbol, decimal amount)
        {
            return SendAsset(fromKey, toAddress, new Dictionary<string, decimal>() { {symbol, amount }});
        }

        public bool SendAsset(KeyPair fromKey, string toAddress, Dictionary<string, decimal> amounts)
        {
            List<Transaction.Input> inputs;
            List<Transaction.Output> outputs;

            var scriptHash = LuxUtils.reverseHex(Neo.Emulator.Helper.ToHexString(toAddress.GetScriptHashFromAddress()));
            GenerateInputsOutputs(fromKey, scriptHash, amounts, out inputs, out outputs);

            Transaction tx = new Transaction()
            {
                type = 128,
                version = 0,
                script = null,
                gas = -1,
                inputs = inputs.ToArray(),
                outputs = outputs.ToArray()
            };
            
            tx = tx.Sign(fromKey);

            var hexTx = tx.SerializeTransaction(true);

            return SendRawTransaction(hexTx);

            /*
            var account = getAccountFromWIFKey(fromWif);
            var toScriptHash = getScriptHashFromAddress(toAddress);
            var balances = getBalance(net, account.address);
            // TODO: maybe have transactions handle this construction?
            var intents = _.map(assetAmounts, (v, k) =>
            {
                return { assetId: tx.ASSETS[k], value: v, scriptHash: toScriptHash }
            });
            var unsignedTx = tx.create.contract(account.publicKeyEncoded, balances, intents);
            var signedTx = tx.signTransaction(unsignedTx, account.privateKey);
            var hexTx = tx.serializeTransaction(signedTx);
            return queryRPC(net, "sendrawtransaction", new object[] { hexTx }, 4);*/
        }

        public Dictionary<string, decimal> GetBalancesOf(KeyPair key, bool getTokens = false)
        {
            return GetBalancesOf(key.address, getTokens);
        }

        public abstract Dictionary<string, decimal> GetBalancesOf(string address, bool getTokens = false);

        public bool IsAsset(string symbol)
        {
            var info = GetAssetsInfo();
            return info.ContainsKey(symbol);
        }

        public bool IsToken(string symbol)
        {
            var info = GetTokenInfo();
            return info.ContainsKey(symbol);
        }

        public NEP5 GetToken(string symbol)
        {
            var info = GetTokenInfo();
            if (info.ContainsKey(symbol))
            {
                var tokenScriptHash = info[symbol];
                return new NEP5(this, tokenScriptHash);
            }

            throw new NeoException("Invalid token symbol");
        }

        public struct UnspentEntry
        {
            public string txid;
            public uint index;
            public decimal value;
        }

        public abstract Dictionary<string, List<UnspentEntry>> GetUnspent(string address);

        /*public static decimal getClaimAmounts(Net net, string address)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/address/claims/" + address);
            return response.GetDecimal("total_claim");
            return (available: parseInt(res.data.total_claim), unavailable: parseInt(res.data.total_unspent_claim));
        }
        */

        /*public static DataNode getTransactionHistory(Net net, string address)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/address/history/" + address);
            return response["history"];
        }*/

        /*public static int getWalletDBHeight(Net net)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/block/height");
            return response.GetInt32("block_height");
        }*/


    }

}
