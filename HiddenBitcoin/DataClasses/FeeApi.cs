using System;
using System.Net.Http;
using System.Threading;
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

    internal static class FeesPerBytes
    {
        internal static DateTimeOffset LastUpdated;

        internal static decimal Fastest;
        internal static decimal HalfHour;
        internal static decimal Hour;
    }

    public static class FeeApi
    {
        public static decimal GetRecommendedFee(int transactionSizeInBytes, FeeType feeType = FeeType.Fastest)
        {
            if (FeesPerBytes.LastUpdated == default(DateTimeOffset)
                || DateTimeOffset.Now - FeesPerBytes.LastUpdated > TimeSpan.FromSeconds(7) )
            {
                UpdateFeesSync();
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
                    FeesPerBytes.LastUpdated = DateTimeOffset.Now;
                    return;
                }
            }
        }
    }
}