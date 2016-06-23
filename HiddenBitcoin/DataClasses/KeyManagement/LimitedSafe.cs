using System;
using System.Collections.Generic;

namespace HiddenBitcoin.DataClasses.KeyManagement
{
    public class LimitedSafe : Safe
    {
        public LimitedSafe(Safe safe, int addressCount) : base(safe)
        {
            AddressCount = addressCount;
        }

        public int AddressCount { get; }

        public List<string> Addresses
        {
            get
            {
                var monitoredAddresses = new List<string>();
                for (var i = 0; i < AddressCount; i++)
                {
                    monitoredAddresses.Add(GetAddress(i));
                }
                return monitoredAddresses;
            }
        }

        private void AssertAddressCount(int index)
        {
            if (index >= AddressCount)
                throw new IndexOutOfRangeException(
                    $"Value of index is {index}, it cannot be higher than {AddressCount - 1}");
        }

        public override string GetAddress(int index)
        {
            AssertAddressCount(index);
            return base.GetAddress(index);
        }

        public override string GetPrivateKey(int index)
        {
            AssertAddressCount(index);
            return base.GetPrivateKey(index);
        }

        public override PrivateKeyAddressPair GetPrivateKeyAddressPair(int index)
        {
            AssertAddressCount(index);
            return base.GetPrivateKeyAddressPair(index);
        }

        public string GetPrivateKey(string address)
        {
            if (!Addresses.Contains(address))
                throw new Exception("No private key of address in Safe");
            return GetPrivateKey(Addresses.IndexOf(address));
        }
    }
}