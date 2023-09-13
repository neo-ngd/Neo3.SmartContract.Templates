using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract.Examples
{
    [DisplayName("Oracle")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is an oracle example")]
    [ContractPermission("*", "*")]
    public class OracleContract : Framework.SmartContract
    {
        [DisplayName("result")]
        public static event Action<byte[]> OnResult;

        [InitialValue("NRq8WQasfc5fiQwZHkiu6XLrXUvFdD9cfw", ContractParameterType.Hash160)]
        private static readonly UInt160 owner = default;
        // Prefix_TotalSupply = 0x00; Prefix_Balance = 0x01;
        private const byte Prefix_Contract = 0x02;
        public static readonly StorageMap ContractMap = new(Storage.CurrentContext, Prefix_Contract);
        private static readonly byte[] ownerKey = "owner".ToByteArray();
        private static readonly byte[] timekey = "time".ToByteArray();
        private static bool IsOwner() => Runtime.CheckWitness(GetOwner());
        private const byte Prefix_RequestCount = 0x00;
        private static readonly BigInteger RequestCount = 1;

        public static void _deploy(object data, bool update)
        {
            if (update) return;
            byte[] key = new byte[] { Prefix_RequestCount };
            Storage.Put(Storage.CurrentContext, key, RequestCount);
            ContractMap.Put(ownerKey, owner);
        }

        public static UInt160 GetOwner()
        {
            return (UInt160)ContractMap.Get(ownerKey);
        }

        public static void DoRequest(string url, string filter)
        {
            // JSONPath format https://github.com/atifaziz/JSONPath
            string callback = "callback"; // callback method
            object userdata = "userdata"; // arbitrary type
            long gasForResponse = Oracle.MinimumResponseFee;

            Oracle.Request(url, filter, callback, userdata, gasForResponse);

        }

        public static void Callback(string url, byte[] userdata, OracleResponseCode code, byte[] result)
        {
            byte[] key = new byte[] { Prefix_RequestCount };
            if (Runtime.CallingScriptHash != Oracle.Hash) throw new Exception("Unauthorized!");
            BigInteger count = (BigInteger)Storage.Get(Storage.CurrentContext, key);
            Storage.Put(Storage.CurrentContext, (ByteString)count, (ByteString)result);
            Storage.Put(Storage.CurrentContext, key, count + 1);
            ContractMap.Put(timekey, Runtime.Time);
            OnResult(result);
        }

        public static BigInteger GetTime()
        {
            return (BigInteger)ContractMap.Get(timekey);
        }

        public static BigInteger GetCount()
        {
            byte[] key = new byte[] { Prefix_RequestCount };
            return (BigInteger)Storage.Get(Storage.CurrentContext, key);
        }

        public static ByteString GetResult(BigInteger count)
        {
            return Storage.Get(Storage.CurrentContext, (ByteString)count);
        }
        public static bool Update(ByteString nefFile, ByteString manifest)
        {
            if (!IsOwner()) throw new InvalidOperationException("No Authorization!!!");
            ContractManagement.Update(nefFile, manifest, null);
            return true;
        }
    }
}
