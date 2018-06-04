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

// https://github.com/bluzelle/neo/blob/master/BluzelleBridge/Core/BridgeManager.cs

namespace NEO.mwherman2000.MonitorEvents
{
    public class NeoEventMonitor0 : IBlockchainProvider
    {
        private NeoAPI _api;
        private bool _running;
        private uint _previousBlocks = 1;

        private SnapshotVM _listenerVM;
        private Dictionary<UInt256, Transaction> _transactions = new Dictionary<UInt256, Transaction>();

        public NeoEventMonitor0(NeoAPI api)
        {
            this._api = api;

            this._listenerVM = new SnapshotVM(this);
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
            Run(1);
        }

        public void Run(uint previousBlocks)
        {
            if (_running)
            {
                return;
            }
            this._running = true;

            _previousBlocks = previousBlocks;

            // TODO: The last block should persistent between multiple sessions, in order to not miss any block
            var blockHeight = _api.GetBlockHeight();
            var currentBlock = blockHeight - _previousBlocks;
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

                List<AVMInstruction> ops;

                try
                {
                    ops = NeoTools.Disassemble(tx.script);
                    Console.WriteLine($"{height} Processing tx {tx.Hash} has {tx.script.Length} byte codes");
                }
                catch
                {
                    continue;
                }

                //ProcessNotifications(height, tx);

                for (int i = 0; i < ops.Count; i++)
                {
                    var op = ops[i];
                    if (op.opcode == OpCode.APPCALL && op.data != null && op.data.Length == 20)
                    {
                        var scriptHash = new UInt160(op.data);
                        var engine = new ExecutionEngine(tx, _listenerVM, _listenerVM);
                        engine.LoadScript(tx.script);

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

            foreach (var entry in notifications)
            {
                Console.WriteLine($"{entry.Name}\t{JsonConvert.SerializeObject(entry)}");          
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
