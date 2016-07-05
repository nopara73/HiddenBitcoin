using System;
using System.Net.Http;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json.Linq;

namespace HiddenBitcoin.DataClasses
{
    public enum FeeType
    {
        Fastest,
        HalfHour,
        Hour
    }

    public static class FeesPerBytes
    {
        internal static bool Set;

        public static decimal Fastest;
        public static decimal HalfHour;
        public static decimal Hour;
    }

    public static class FeeApi
    {
        public static decimal GetRecommendedFee(int transactionSizeInBytes, FeeType feeType = FeeType.Fastest)
        {
            if (!FeesPerBytes.Set)
            {
                UpdateFeesSync();
                PeriodicUpdate();
            }

            decimal feeInSatoshi;
            switch (feeType)
            {
                case FeeType.Fastest:
                    feeInSatoshi = FeesPerBytes.Fastest*transactionSizeInBytes;
                    break;
                case FeeType.HalfHour:
                    feeInSatoshi = FeesPerBytes.HalfHour*transactionSizeInBytes;
                    break;
                case FeeType.Hour:
                    feeInSatoshi = FeesPerBytes.Hour*transactionSizeInBytes;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(feeType), feeType, null);
            }

            var fee = new Money(feeInSatoshi, MoneyUnit.Satoshi);

            return fee.ToDecimal(MoneyUnit.BTC);
        }

        private static void UpdateFeesSync()
        {
            UpdateFeesAsync().Wait();
        }

        // ReSharper disable once FunctionNeverReturns
        private static async Task UpdateFeesAsync()
        {
            while (true)
            {
                using (var client = new HttpClient())
                {
                    const string request = @"https://bitcoinfees.21.co/api/v1/fees/recommended";

                    var response = await client.GetAsync(request).ConfigureAwait(false);

                    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var json = JObject.Parse(result);
                    FeesPerBytes.Fastest = json.Value<decimal>("fastestFee");
                    FeesPerBytes.HalfHour = json.Value<decimal>("halfHourFee");
                    FeesPerBytes.Hour = json.Value<decimal>("hourFee");
                    FeesPerBytes.Set = true;
                    return;
                }
            }
        }

        // ReSharper disable once FunctionNeverReturns
        private static async void PeriodicUpdate()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(7));
                UpdateFeesSync();
            }
        }
    }
}