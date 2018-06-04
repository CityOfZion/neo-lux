using Neo.Lux.Cryptography;
using Neo.Lux.Debugger;
using Neo.Lux.Utils;
using Neo.Lux.VM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Neo.Lux.Core
{
    public interface IBlockchainProvider
    {
        Transaction GetTransaction(UInt256 hash);
    }

    public class SnapshotVM : InteropService, IScriptTable
    {
        private Dictionary<UInt160, byte[]> scripts = new Dictionary<UInt160, byte[]>();
        private Dictionary<UInt160, Storage> storage = new Dictionary<UInt160, Storage>();
        public Block currentBlock { get; private set; }

        private Dictionary<UInt256, List<Notification>> notifications = new Dictionary<UInt256, List<Notification>>();

        private IBlockchainProvider provider;

        public SnapshotVM(IBlockchainProvider provider)
        {
            this.provider = provider;

            Register("Neo.Transaction.GetReferences", Transaction_GetReferences, 0.2m);
            Register("Neo.Transaction.GetOutputs", Transaction_GetOutputs, defaultGasCost);
            Register("Neo.Transaction.GetInputs", Transaction_GetInputs, defaultGasCost);
            Register("Neo.Transaction.GetHash", engine => { var tx = GetInteropFromStack<Transaction>(engine); if (tx == null) return false; engine.EvaluationStack.Push(tx.Hash.ToArray()); return true; }, defaultGasCost);

            Register("Neo.Output.GetScriptHash", engine => { var output = GetInteropFromStack<Transaction.Output>(engine); if (output == null) return false; engine.EvaluationStack.Push(output.scriptHash.ToArray()); return true; }, defaultGasCost);
            Register("Neo.Output.GetValue", engine => { var output = GetInteropFromStack<Transaction.Output>(engine); if (output == null) return false; engine.EvaluationStack.Push(output.value.ToBigInteger()); return true; }, defaultGasCost);
            Register("Neo.Output.GetAssetId", engine => { var output = GetInteropFromStack<Transaction.Output>(engine); if (output == null) return false; engine.EvaluationStack.Push(output.assetID); return true; }, defaultGasCost);

            Register("Neo.Storage.GetContext", engine => { var hash = engine.CurrentContext.ScriptHash; engine.EvaluationStack.Push((new VM.Types.InteropInterface(storage[hash]))); return true; }, defaultGasCost);
            Register("Neo.Storage.Get", Storage_Get, 0.1m);
            Register("Neo.Storage.Put", Storage_Put, 0.1m);
            Register("Neo.Storage.Delete", Storage_Delete, 0.1m);

            Register("Neo.Runtime.GetTime", engine => { engine.EvaluationStack.Push(currentBlock.Date.ToTimestamp()); return true; }, defaultGasCost);
            Register("Neo.Runtime.GetTrigger", engine => { engine.EvaluationStack.Push((int)TriggerType.Application); return true; }, defaultGasCost);
            Register("Neo.Runtime.CheckWitness", Runtime_CheckWitness, 0.2m);
            Register("Neo.Runtime.Log", Runtime_Log, defaultGasCost);
            Register("Neo.Runtime.Notify", Runtime_Notify, defaultGasCost);
        }

        public bool Runtime_Log(ExecutionEngine engine)
        {
            Console.WriteLine("Runtime_Log...");

            var msg = engine.EvaluationStack.Pop();

            if (msg != null)
            {
                string sMsg = msg.GetString();

                var eventArgs = new List<object>();
                eventArgs.Add(sMsg);

                List<Notification> list;
                var tx = (Transaction)engine.ScriptContainer;

                if (notifications.ContainsKey(tx.Hash))
                {
                    list = notifications[tx.Hash];
                }
                else
                {
                    list = new List<Notification>();
                    notifications[tx.Hash] = list;
                }

                list.Add(new Notification(tx.Hash, "Runtime_Log", eventArgs.ToArray()));

                return true;
            }
            else
            {
                return false;
            }
        }

        private static T GetInteropFromStack<T>(ExecutionEngine engine) where T : class, IInteropInterface
        {
            if (engine.EvaluationStack.Count == 0)
            {
                return default(T);
            }

            var obj = engine.EvaluationStack.Pop() as VM.Types.InteropInterface;
            if (obj == null)
            {
                return default(T);

            }

            return obj.GetInterface<T>();
        }

        private bool Storage_Get(ExecutionEngine engine)
        {
            var storage = GetInteropFromStack<Storage>(engine);
            var key = engine.EvaluationStack.Pop().GetByteArray();

            var key_name = FormattingUtils.OutputData(key, false);

            if (storage.entries.ContainsKey(key))
            {
                engine.EvaluationStack.Push(storage.entries[key]);
            }
            else
            {
                engine.EvaluationStack.Push(new byte[0] { });
            }

            return true;
        }

        private bool Storage_Put(ExecutionEngine engine)
        {
            var storage = GetInteropFromStack<Storage>(engine);

            var key = engine.EvaluationStack.Pop().GetByteArray();
            var val = engine.EvaluationStack.Pop().GetByteArray();

            var key_name = FormattingUtils.OutputData(key, false);
            var val_name = FormattingUtils.OutputData(val, false);

            storage.entries[key] = val;
            return true;
        }

        private bool Storage_Delete(ExecutionEngine engine)
        {
            var storage = GetInteropFromStack<Storage>(engine);
            var key = engine.EvaluationStack.Pop().GetByteArray();

            var key_name = FormattingUtils.OutputData(key, false);

            if (storage.entries.ContainsKey(key))
            {
                storage.entries.Remove(key);
            }

            return true;
        }

        public bool Transaction_GetReferences(ExecutionEngine engine)
        {
            var tx = GetInteropFromStack<Transaction>(engine);

            if (tx == null)
            {
                return false;
            }

            var items = new List<StackItem>();

            var references = new List<Transaction.Output>();
            foreach (var input in tx.inputs)
            {
                var other_tx = provider.GetTransaction(input.prevHash);
                references.Add(other_tx.outputs[input.prevIndex]);
            }

            foreach (var reference in references)
            {
                items.Add(new VM.Types.InteropInterface(reference));
            }

            var result = new VM.Types.Array(items.ToArray<StackItem>());
            engine.EvaluationStack.Push(result);

            return true;
        }

        public bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            var tx = GetInteropFromStack<Transaction>(engine);

            if (tx == null)
            {
                return false;
            }

            var items = new List<StackItem>();

            foreach (var output in tx.outputs)
            {
                items.Add(new VM.Types.InteropInterface(output));
            }

            var result = new VM.Types.Array(items.ToArray<StackItem>());
            engine.EvaluationStack.Push(result);

            return true;
        }

        public bool Transaction_GetInputs(ExecutionEngine engine)
        {
            var tx = GetInteropFromStack<Transaction>(engine);

            if (tx == null)
            {
                return false;
            }

            var items = new List<StackItem>();

            foreach (var input in tx.inputs)
            {
                items.Add(new VM.Types.InteropInterface(input));
            }

            var result = new VM.Types.Array(items.ToArray<StackItem>());
            engine.EvaluationStack.Push(result);

            return true;
        }


        public bool Runtime_CheckWitness(ExecutionEngine engine)
        {
            byte[] hashOrPubkey = engine.EvaluationStack.Pop().GetByteArray();

            bool result;

            var tx = (Transaction)engine.ScriptContainer;

            if (hashOrPubkey.Length == 20) // script hash
            {
                var hash = new UInt160(hashOrPubkey);

                var address = hash.ToAddress();

                result = false;

                foreach (var input in tx.inputs)
                {
                    var reference = provider.GetTransaction(input.prevHash);
                    var output = reference.outputs[input.prevIndex];
                    var other_address = output.scriptHash.ToAddress();

                    if (output.scriptHash.Equals(hash))
                    {
                        result = true;
                        break;
                    }
                }
            }
            else if (hashOrPubkey.Length == 33) // public key
            {
                //hash = ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1);
                //result = CheckWitness(engine, Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
                throw new Exception("ECPoint witness");
            }
            else
            {
                result = false;
            }

            //DoLog($"Checking Witness [{matchType}]: {FormattingUtils.OutputData(hashOrPubkey, false)} => {result}");

            engine.EvaluationStack.Push(new VM.Types.Boolean(result));
            return true;
        }

        private bool Runtime_Notify(ExecutionEngine engine)
        {
            Console.WriteLine("Runtime_Notify...");

            var something = engine.EvaluationStack.Pop();

            if (something is ICollection)
            {
                var items = (ICollection)something;

                string eventName = null;
                var eventArgs = new List<object>();

                int index = 0;

                foreach (StackItem item in items)
                {
                    if (index > 0)
                    {
                        object o = Chain.StackItemToObject(item);
                        eventArgs.Add(o);
                    }
                    else
                    {
                        eventName = item.GetString();
                    }

                    index++;
                }

                List<Notification> list;
                var tx = (Transaction)engine.ScriptContainer;

                if (notifications.ContainsKey(tx.Hash))
                {
                    list = notifications[tx.Hash];
                }
                else
                {
                    list = new List<Notification>();
                    notifications[tx.Hash] = list;
                }

                list.Add(new Notification(tx.Hash, eventName, eventArgs.ToArray()));

                return true;
            }
            else
            {
                return false;
            }
        }

        public void AddScript(byte[] script)
        {
            UInt160 scriptHash = script.ToScriptHash();
            scripts[scriptHash] = script;
            Console.WriteLine($"AddScript: scriptHash {scriptHash.ToString()} has {script.Length} bytes: {script.ToHexString()}"); ;
            storage[scriptHash] = new Storage();
        }

        public byte[] GetScript(byte[] script_hash)
        {
            var hash = new UInt160(script_hash);
            Console.WriteLine($"GetScript: scriptHash {script_hash.ToHexString()}");
            return scripts[hash];
        }

        public Storage GetStorage(UInt160 scriptHash)
        {
            return storage.ContainsKey(scriptHash) ? storage[scriptHash] : null;
        }

        public Storage GetStorage(NEP5 token)
        {
            return GetStorage(token.ScriptHash);
        }

        public IEnumerable<Notification> GetNotifications(UInt256 hash)
        {
            return notifications.ContainsKey(hash) ? notifications[hash] : null;
        }

        public IEnumerable<Notification> GetNotifications(Transaction tx)
        {
            return GetNotifications(tx.Hash);
        }

        public void SetCurrentBlock(Block block)
        {
            this.currentBlock = block;
        }
    }

    public class Snapshot: IBlockchainProvider
    {
        private Dictionary<UInt256, Transaction> transactions = new Dictionary<UInt256, Transaction>();
        private Dictionary<UInt256, Block> blocks = new Dictionary<UInt256, Block>();
        private HashSet<UInt256> external_txs = new HashSet<UInt256>();

        internal Snapshot(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var temp = line.Split(',');
                switch (temp[0])
                {
                    case "B":
                        {
                            var data = temp[1].HexToBytes();
                            var block = Block.Unserialize(data);
                            blocks[block.Hash] = block;

                            foreach (var tx in block.transactions)
                            {
                                transactions[tx.Hash] = tx;
                            }

                            break;
                        }

                    case "T":
                        {
                            var data = temp[1].HexToBytes();
                            var tx = Transaction.Unserialize(data);
                            transactions[tx.Hash] = tx;
                            external_txs.Add(tx.Hash);
                            break;
                        }
                }
            }
        }

        public Snapshot(NEP5 token, uint startBlock, uint endBlock = 0)
        {
            var api = token.api;

            if (endBlock == 0)
            {
                endBlock = api.GetBlockHeight();
            }

            if (endBlock < startBlock)
            {
                throw new ArgumentException("End block cannot be smaller than start block");
            }

            for (uint height = startBlock; height <= endBlock; height++)
            {
                var block = api.GetBlock(height);

                var snapCount = 0;

                foreach (var tx in block.transactions)
                {
                    switch (tx.type)
                    {
                        case TransactionType.ContractTransaction:
                            {

                                foreach (var output in tx.outputs)
                                {
                                    if (output.scriptHash == token.ScriptHash)
                                    {
                                        MergeTransaction(api, tx);
                                        snapCount++;
                                        break;
                                    }
                                }

                                break;
                            }

                        case TransactionType.InvocationTransaction:
                            {
                                List<AVMInstruction> ops;
                                try
                                {
                                    ops = NeoTools.Disassemble(tx.script);
                                }
                                catch
                                {
                                    continue;
                                }

                                for (int i = 0; i < ops.Count; i++)
                                {
                                    var op = ops[i];

                                    if (op.opcode == OpCode.APPCALL && op.data != null && op.data.Length == 20)
                                    {
                                        var scriptHash = new UInt160(op.data);

                                        if (scriptHash == token.ScriptHash)
                                        {
                                            MergeTransaction(api, tx);
                                            snapCount++;
                                            break;
                                        }
                                    }
                                }

                                break;
                            }
                    }
                }

                if (snapCount > 0)
                {
                    blocks[block.Hash] = block;
                }
            }
        }

        private void MergeTransaction(NeoAPI api, Transaction tx)
        {
            transactions[tx.Hash] = tx;

            foreach (var input in tx.inputs)
            {
                if (!transactions.ContainsKey(input.prevHash))
                {
                    var other = api.GetTransaction(input.prevHash);
                    transactions[other.Hash] = other;
                    external_txs.Add(other.Hash);
                }
            }
        }

        public IEnumerable<string> Export()
        {
            var result = new List<string>();

            foreach (var block in blocks.Values)
            {
                var data = block.Serialize().ByteToHex();
                result.Add("B," + data);
            }

            foreach (var hash in external_txs)
            {
                var tx = transactions[hash];
                var data = tx.Serialize().ByteToHex();
                result.Add("T," + data);
            }

            return result;
        }

        public static Snapshot Import(IEnumerable<string> lines)
        {
            return new Snapshot(lines);
        }

        private static uint FindBlock(NeoAPI api, uint timestamp, uint min, uint max)
        {
            var mid = (1 + max - min) / 2;
            do
            {
                var block = api.GetBlock(mid);
                var blockTime = block.Date.ToTimestamp();

                if (blockTime == timestamp)
                {
                    return block.Height;
                }
                else 
                if (blockTime < timestamp)
                {
                    var next = api.GetBlock(mid + 1);
                    var nextTime = next.Date.ToTimestamp();
                    if (nextTime == timestamp)
                    {
                        return next.Height;
                    }
                    else 
                    if  (nextTime > timestamp)
                    {
                        return block.Height;
                    }
                    else
                    {
                        return FindBlock(api, timestamp, mid + 1, max);
                    }
                }
                else
                {
                    return FindBlock(api, timestamp, min, mid - 1);
                }

            } while (true);
        }

        public static uint FindBlock(NeoAPI api, DateTime date)
        {
            uint min = 0;
            var max = api.GetBlockHeight();

            var timestamp = date.ToTimestamp();
            return FindBlock(api, timestamp, min, max);
        }

        public void Execute(NEP5 token, byte[] token_script, Action<SnapshotVM> visitor)
        {
            if (token_script == null)
            {
                throw new Exception("Could not find token script");
            }

            var script_hash = token_script.ToScriptHash();
            if (script_hash != token.ScriptHash)
            {
                throw new Exception("Invalid script code, does not match the token scripthash");
            }

            var api = token.api;
            var balances = new Dictionary<UInt160, decimal>();

            var vm = new SnapshotVM(this);
            vm.AddScript(token_script);

            var debugger = new DebugClient();
            debugger.SendScript(token_script);

            IEnumerable<Block> sorted_blocks = blocks.Values.OrderBy(x => x.Date);

            foreach (var block in sorted_blocks)
            {
                vm.SetCurrentBlock(block);

                bool executed = false;

                foreach (var tx in block.transactions)
                {
                    switch (tx.type)
                    {
                        case TransactionType.InvocationTransaction:
                            {
                                List<AVMInstruction> ops;
                                try
                                {
                                    ops = NeoTools.Disassemble(tx.script);
                                }
                                catch
                                {
                                    continue;
                                }

                                for (int i = 0; i < ops.Count; i++)
                                {
                                    var op = ops[i];

                                    if (op.opcode == OpCode.APPCALL && op.data != null && op.data.Length == 20)
                                    {
                                        var scriptHash = new UInt160(op.data);

                                        if (scriptHash != token.ScriptHash)
                                        {
                                            continue;
                                        }

                                        var engine = new ExecutionEngine(tx, vm, vm);
                                        engine.LoadScript(tx.script);

                                        engine.Execute(
                                            x =>
                                            {
                                                debugger.Step(x);
                                            }
                                            );

                                        executed = true;
                                    }
                                }

                                break;
                            }
                    }
                }

                if (executed)
                {
                    visitor(vm);
                }
            }
        }

        public Transaction GetTransaction(UInt256 hash)
        {
            return transactions.ContainsKey(hash) ? transactions[hash] : null;
        }
    }
}