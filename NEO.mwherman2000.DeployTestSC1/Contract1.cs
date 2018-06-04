using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace NEO.mwherman2000.DeployTestSC1
{
    public class Contract1 : SmartContract
    {
        public static event Action<byte[], byte[], BigInteger> Transfer;

        public static object Main(string operation, params object[] args)
        {
            Runtime.Log("Log test message");

            Runtime.Notify("operation", operation);
            Runtime.Notify("args", args);
            Runtime.Notify("args[0]", args[0]);
            Runtime.Notify("args[1]", args[1]);
            Runtime.Notify("args[2]", args[2]);

            Transfer("arg1".AsByteArray(), "arg2".AsByteArray(), new BigInteger(1234));

            return args;
        }
    }
}
