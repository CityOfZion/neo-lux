using System;

using Neo.Cryptography;

namespace NeoLux.Demo
{
    class Demo
    {
        static void Main(string[] args)
        {
            // NOTE - You can also create an API instance for a specific private net
            var api = NeoDB.ForTestNet();

            // NOTE - Private keys should not be hardcoded in the code, this is just for demonstration purposes!
            var privateKey = "a9e2b5436cab6ff74be2d5c91b8a67053494ab5b454ac2851f872fb0fd30ba5e";

            Console.WriteLine("*Loading NEO address...");
            var keys = new KeyPair(privateKey.HexToBytes());
            Console.WriteLine("Got :"+keys.address);

            // it is possible to optionally obtain also token balances with this method
            Console.WriteLine("*Syncing balances...");
            var balances = api.GetBalancesOf(keys.address, false);
            foreach (var entry in balances)
            {
                Console.WriteLine(entry.Value + " " + entry.Key);
            }

            //const string NeoDraw_ContractHash = "694ebe0840d1952b09f5435152eebbbc1f8e4b8e";
            //var response = api.TestInvokeScript(NeoDraw_ContractHash, new object[] { "getx", "user", new object[] { "100" } });
            //TEST CASE 1: object[] results = { -1 }; return results; /* WORKS */
            //{ "jsonrpc":"2.0","id":1,
            //  "result":{ "script":"0331303051c104757365720467657478678e4b8e1fbcbbee525143f5092b95d14008be4e69",
            //      "state":"HALT, BREAK","gas_consumed":"0.47",
            //      "stack":[
            //          { "type":"ByteArray", { "value":"756e6b6e6f776e206f7065726174696f6e2027"},
            //          { "type":"Array","value":[
            //              { "type":"Integer","value":"-1"}]}]}}

            //const string NeoDraw_ContractHash = "694ebe0840d1952b09f5435152eebbbc1f8e4b8e";
            //var response = api.TestInvokeScript(NeoDraw_ContractHash, new object[] { "get", "user", new object[] { "100" } });
            //TEST CASE 2: UserCredentials uc = FindUser(AppVAU, encodedUsername); results = new object[] { uc }; return results; /* DOESN'T WORK */
            //{ "jsonrpc":"2.0","id":1,
            //  "result":{ "script":"0331303051c1047573657203676574678e4b8e1fbcbbee525143f5092b95d14008be4e69",
            //      "state":"HALT, BREAK","gas_consumed":"2.082",
            //      "stack":[
            //          { "type":"Array","value":[
            //              {"type":"Array","value":[
            //                  {"type":"ByteArray","value":"313030"},{"type":"ByteArray","value":"313030"},{"type":"Integer","value":"4"},
            //                  {"type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  {"type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  {"type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  {"type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false}]}]}]}}

            const string NeoDraw_ContractHash = "694ebe0840d1952b09f5435152eebbbc1f8e4b8e";
            var response = api.TestInvokeScript(NeoDraw_ContractHash, new object[] { "getall", "point", new object[] { "100" } });
            // TEST CASE 3: UserPoint[] points = new UserPoint[(int)nPoints]; results = points; return results;
            //{ "jsonrpc":"2.0","id":1,
            //  "result":{ "script":"0331303051c105706f696e7406676574616c6c678e4b8e1fbcbbee525143f5092b95d14008be4e69",
            //      "state":"HALT, BREAK","gas_consumed":"7.724",
            //      "stack":[
            //          { "type":"Array","value":[
            //              { "type":"Array","value":[
            //                  { "type":"ByteArray","value":"3130"},{"type":"ByteArray","value":"3230"},{"type":"Integer","value":"4"},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false}]},
            //              { "type":"Array","value":[
            //                  { "type":"ByteArray","value":"3430"},{"type":"ByteArray","value":"3630"},{"type":"Integer","value":"4"},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false}]},
            //              { "type":"Array","value":[
            //                  { "type":"ByteArray","value":"35"},{"type":"ByteArray","value":"35"},{"type":"Integer","value":"4"},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false},
            //                  { "type":"Boolean","value":false},{"type":"Boolean","value":false},{"type":"Boolean","value":false}]}]}]}}                                                                     //TEST CASE 3: UserPoint[] points = new UserPoint[(int)nPoints]; results = points; return results; // DOESN'T WORK

            object[] resultsArray = (object[])response.result;
            Console.WriteLine("resultsArray.length: " + resultsArray.Length);
            int raIndex = 0;
            foreach (object resultsElement in resultsArray)
            {
                Console.WriteLine("resultsElement:" + resultsElement.GetType().Name);
                if (resultsElement.GetType().Name != "Object[]")
                {
                    Console.WriteLine("resultsElement:\t" + resultsElement.ToString());
                }
                else
                {
                    int rIndex = 0;
                    object[] results = (object[])resultsElement;
                    if (results != null)
                    {
                        Console.WriteLine("results.length: " + results.Length);
                        foreach (object result in results)
                        {
                            Console.WriteLine("result:\t" + raIndex.ToString() + "\t" + rIndex.ToString() + "\t" + result.ToString() + "\t" + result.GetType().Name);
                            if (result.GetType().Name == "Object[]")
                            {
                                int oooooIndex = 0;
                                foreach (object ooooo in (object[])result)
                                {
                                    if (ooooo != null)
                                    {
                                        Console.WriteLine("ooooo:\t" + raIndex.ToString() + "\t" + rIndex.ToString() + "\t" + oooooIndex.ToString() + "\t" + ooooo.ToString() + "\t" + ooooo.GetType().Name);
                                        if (ooooo.GetType().Name == "Object[]")
                                        {
                                            int ooooIndex = 0;
                                            foreach (object oooo in (object[])ooooo)
                                            {
                                                if (oooo != null)
                                                {
                                                    Console.WriteLine("oooo:\t" + raIndex.ToString() + "\t" + rIndex.ToString() + "\t" + oooooIndex.ToString() + "\t" + ooooIndex.ToString() + "\t" + oooo.ToString() + "\t" + oooo.GetType().Name);
                                                    //if (oooo.GetType().Name == "Object[]")
                                                    //{
                                                    //    int oooIndex = 0;
                                                    //    foreach (object ooo in (object[])oooo)
                                                    //    {
                                                    //        if (ooo != null)
                                                    //        {
                                                    //            Console.WriteLine("ooo:\t" + raIndex.ToString() + "\t" + rIndex.ToString() + "\t" + oooooIndex.ToString() + "\t" + ooooIndex.ToString() + "\t" + oooIndex.ToString() + "\t" + ooo.ToString() + "\t" + ooo.GetType().Name);
                                                    //            if (ooo.GetType().Name == "Object[]")
                                                    //            {
                                                    //                int ooIndex = 0;
                                                    //                foreach (object oo in (object[])ooo)
                                                    //                {
                                                    //                    if (oo != null)
                                                    //                    {
                                                    //                        Console.WriteLine("oo:\t" + raIndex.ToString() + "\t" + rIndex.ToString() + "\t" + oooooIndex.ToString() + "\t" + ooooIndex.ToString() + "\t" + oooIndex.ToString() + "\t" + ooIndex.ToString() + "\t" + oo.ToString() + "\t" + oo.GetType().Name);
                                                    //                        if (oo.GetType().Name == "Object[]" && ((object[])oo).Length > 0)
                                                    //                        {
                                                    //                            int oIndex = 0;
                                                    //                            foreach (object o in (object[])oo)
                                                    //                            {
                                                    //                                if (o != null)
                                                    //                                {
                                                    //                                    Console.WriteLine("o:\t" + raIndex.ToString() + "\t" + rIndex.ToString() + "\t" + oooooIndex.ToString() + "\t" + ooooIndex.ToString() + "\t" + oooIndex.ToString() + "\t" + ooIndex.ToString() + "\t" + oIndex.ToString() + "\t" + o.ToString() + "\t" + o.GetType().Name);
                                                    //                                    oIndex++;
                                                    //                                }
                                                    //                            }
                                                    //                        }
                                                    //                        else
                                                    //                        {
                                                    //                            var oo0 = (string)oo;
                                                    //                            Console.WriteLine("oo0:\t" + raIndex.ToString() + "\t" + rIndex.ToString() + "\t" + oooooIndex.ToString() + "\t" + ooooIndex.ToString() + "\t" + oooIndex.ToString() + "\t" + ooIndex.ToString() + "\t" + oo0.ToString() + "\t" + oo0.GetType().Name);
                                                    //                        }
                                                    //                        ooIndex++;
                                                    //                    }
                                                    //                }
                                                    //            }
                                                    //            oooIndex++;
                                                    //        }
                                                    //    }
                                                    //}
                                                    ooooIndex++;
                                                }
                                            }
                                        }
                                        oooooIndex++;
                                    }
                                }
                            }
                            rIndex++;
                        }
                    }
                    raIndex++;
                }
            }

            Console.WriteLine("Press Enter to Exit...");
            Console.ReadLine();

            // TestInvokeScript let's us call a smart contract method and get back a result
            // NEP5 https://github.com/neo-project/proposals/issues/3

            api = NeoDB.ForMainNet();
            var redPulse = api.GetToken("RPX");

            // you could also create a NEP5 from a contract script hash
            //var redPulse_contractHash = "ecc6b20d3ccac1ee9ef109af5a7cdb85706b1df9";
            //var redPulse = new NEP5(api, redPulse_contractHash);

            Console.WriteLine("*Querying Symbol from RedPulse contract...");
            //var response = api.TestInvokeScript(redPulse_contractHash, "symbol", new object[] { "" });
            //var symbol = System.Text.Encoding.ASCII.GetString((byte[])response.result);
            var symbol = redPulse.Symbol;
            Console.WriteLine(symbol); // should print "RPX"

            // here we get the RedPulse token balance from an address
            Console.WriteLine("*Querying BalanceOf from RedPulse contract...");
            //var balance = api.GetTokenBalance("AVQ6jAQ3Prd32BXU5r2Vb3QL1gYzTpFhaf", "RPX");
            var balance = redPulse.BalanceOf("AVQ6jAQ3Prd32BXU5r2Vb3QL1gYzTpFhaf");
            Console.WriteLine(balance);

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
