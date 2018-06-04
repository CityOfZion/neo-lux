using Neo.Lux.Core;
using Neo.Lux.Cryptography;
using Neo.Lux.Debugger;
using Neo.Lux.Utils;
using Neo.Lux.VM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// https://github.com/PhantasmaProtocol/PhantasmaBridge/blob/master/PhantasmaBridge/Core/ChainListener.cs#L151

namespace NEO.mwherman2000.MonitorEvents
{
    public class NeoEventMonitor1 : IBlockchainProvider
    {
        private NeoAPI _api;
        private bool _running;
        private uint _startBlock = 0;

        private byte[] _contractBytecode;
        private UInt160 _contractScriptHash;

        private SnapshotVM _listenerVM;
        private Dictionary<UInt256, Transaction> _transactions = new Dictionary<UInt256, Transaction>();

        public NeoEventMonitor1(NeoAPI api, Transaction deployTx, uint startBlock)
        {
            this._api = api;
            this._startBlock = startBlock;

            this._listenerVM = new SnapshotVM(this);

            List<AVMInstruction> deployTxScript = NeoTools.Disassemble(deployTx.script);
                
            for (int i= 1; i < deployTxScript.Count; i++)
            {
                var instruction = deployTxScript[i];
                if (instruction.opcode == OpCode.SYSCALL && instruction.data != null)
                {
                    var method = Encoding.ASCII.GetString(instruction.data);

                    if (method== "Neo.Contract.Create")
                    {
                        Console.WriteLine($"method == Neo.Contract.Create");
                        var prevInstruction = deployTxScript[i - 1];
                        this._contractBytecode = prevInstruction.data;
                        this._contractScriptHash = _contractBytecode.ToScriptHash();
                        Console.WriteLine($"_contractBytecode: {_contractBytecode.Length}\tcontractScriptHash: {this._contractScriptHash.ToString()}");
                        break;
                    }
                }
            }
            return;
        }

        public void Stop()
        {
            if (_running)
            {
                _running = false;
            }
        }

        public void Run()
        {
            if (_running)
            {
                return;
            }
            this._running = true;

            // TODO: The last block should persistent between multiple sessions, in order to not miss any block
            var blockHeight = _api.GetBlockHeight();
            var currentBlock = _startBlock;
            int retries = 0;

            do // a batch of blocks: from currentBlock to blockHeight
            {
                while (currentBlock <= blockHeight) // for each block in this batch
                {
                    if (ProcessIncomingBlock(currentBlock))
                    {
                        currentBlock++;
                        retries = 0;
                    }
                    else
                    {
                        retries++;
                        if (retries > 10)
                        {
                            Console.WriteLine($"Too many retries: skipping block {currentBlock}...");
                            currentBlock++;
                            retries = 0;
                        }
                        else
                        {
                            Console.WriteLine($"Waiting for block... retry {retries}");
                            Thread.Sleep(10 * 1000);
                        }
                    }
                }
                Console.WriteLine($"Waiting for next batch...");
                Thread.Sleep(30 * 1000);
                blockHeight = _api.GetBlockHeight();
            } while (_running);
        }

        private bool ProcessIncomingBlock(uint height)
        {
            //Console.WriteLine($"Processing block {height}...");
            var block = _api.GetBlock(height);

            if (block == null)
            {
                Console.WriteLine($"WARNING: could not fetch block #{height}");
                return false;
            }

            foreach (var tx in block.transactions)
            {
                Console.WriteLine($"{height} Processing tx {tx.Hash}...");

                //if (tx.type != TransactionType.InvocationTransaction)
                //{
                //    continue;
                //}

                List<AVMInstruction> txScript;

                try
                {
                    txScript = NeoTools.Disassemble(tx.script);
                    Console.WriteLine($"{height} Processing tx {tx.Hash} has {txScript.Count} instructions ({tx.script.Length} bytes. hash {tx.script.ToScriptHash()})");
                }
                catch
                {
                    continue;
                }

                var engine = new ExecutionEngine(tx, _listenerVM, _listenerVM);
                engine.LoadScript(tx.script);

                for (int i = 0; i < txScript.Count; i++)
                {
                    AVMInstruction instruction = txScript[i];
                    Console.WriteLine($"Opcode {i}\t{instruction.opcode} ({(int)instruction.opcode}, {Helpers.ToHex(new byte[] { (byte)instruction.opcode })})\t{Helpers.ToHex(instruction.data)}\t{(instruction.data == null ? "(null)" : Encoding.ASCII.GetString(instruction.data))}");

                    if (instruction.opcode == OpCode.APPCALL && instruction.data != null && instruction.data.Length == 20)
                    {
                        var scriptHash = new UInt160(instruction.data);
                        string scriptHashString = scriptHash.ToString();
                        Console.WriteLine($"scriptHashString: {scriptHashString}");

                        if (scriptHash != _contractScriptHash)
                        {
                            Console.WriteLine($"scriptHashString: {scriptHashString} != _contractScriptHash: {_contractScriptHash}: skipping");
                            continue;
                        }

                        var prevInstruction = txScript[i - 1];
                        if (prevInstruction.data == null)
                        {
                            Console.WriteLine($"scriptHashString: {scriptHashString}: no method - skipping");
                            continue;
                        }

                        _listenerVM.AddScript(_contractBytecode);

                        var method = Encoding.ASCII.GetString(prevInstruction.data);
                        int index = i - 3;
                        int argCount = (byte)txScript[index].opcode - 0x50; // (byte)OpCode.PUSH0;
                        var args = new List<byte[]>();
                        Console.WriteLine($"method: {method}: argCount {argCount}");
                        while (argCount > 0)
                        {
                            index--;
                            if (txScript[index].data != null)
                            {
                                index--;
                                args.Add(txScript[index].data);
                                Console.WriteLine($"arg: index {index} value {txScript[index].data.ToHexString()}");
                            }
                            else if ((byte)txScript[index].opcode >= (byte)OpCode.PUSH1 && (byte)txScript[index].opcode <= (byte)OpCode.PUSH16)
                            {
                                int value = (byte)txScript[index].opcode - 0x50;
                                Console.WriteLine($"arg: index {index} value {value}");
                            }
                            argCount--;
                        }

                        engine.Execute(null
                            /*x =>
                            {
                                debugger.Step(x);
                            }*/
                            );

                        ProcessNotifications(height, tx);
                    }
                }
            }

            //Console.WriteLine($"Processing block {height} success");
            return true;
        }

        /// <summary>
        /// Catches and processes all notifications triggered in a Neo transaction
        /// </summary>
        /// <param name="tx"></param>
        private void ProcessNotifications(uint height, Transaction tx)
        {
            Console.WriteLine($"{height} Processing tx {tx.Hash} notifications...");
            // add the transaction to the cache
            _transactions[tx.Hash] = tx;

            var notifications = _listenerVM.GetNotifications(tx);
            if (notifications == null)
            {
                return;
            }

            int index = 0;
            foreach (Notification entry in notifications)
            {
                foreach(object o in entry.Args)
                {   
                    if (o is System.String)
                        {
                            Console.WriteLine($"{index}\t'{entry.Name}'\t'{(string)o}'");
                            break;
                        }
                    else if (o is System.Byte[])
                        {
                            Console.WriteLine($"{index}\t'{entry.Name}'\t'{Helpers.ToHex((byte[])o)}'\t{Encoding.ASCII.GetString((byte[])o)}");
                            break;
                        }
                    else
                        {
                            Console.WriteLine($"{index}\t'{entry.Name}'\t{JsonConvert.SerializeObject(o)}");
                            break;
                        }
                }
                index++;
            }
        }

        /// <summary>
        /// Fetches a transaction from local catch. If not found, will try fetching it from a NEO blockchain node
        /// </summary>
        /// <param name="hash">Hash of the transaction</param>
        /// <returns></returns>
        public Transaction GetTransaction(UInt256 hash)
        {
            return _transactions.ContainsKey(hash) ? _transactions[hash] : _api.GetTransaction(hash);
        }
    }
}
