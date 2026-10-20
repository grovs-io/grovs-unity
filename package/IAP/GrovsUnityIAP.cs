using System;
using System.Threading.Tasks;
using GrovsSDK.Internal;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GrovsSDK
{
    /// <summary>Logs Unity IAP purchases to Grovs. Supports the Apple stores and Google Play.</summary>
    public static class GrovsUnityIAP
    {
        /// <summary>Logs a purchased product from its receipt. Resolves false if the receipt can't be used.</summary>
        public static Task<bool> LogPurchaseAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            // IAP 5 marks these members obsolete but still fills them.
#pragma warning disable 618
            return Log(product.receipt, product.transactionID);
#pragma warning restore 618
        }

#if GROVS_UNITY_IAP5
        /// <summary>Logs an IAP 5 order. Resolves false if the receipt can't be used.</summary>
        public static Task<bool> LogOrderAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.Info == null) return Fail("The order has no info.");
            return Log(order.Info.Receipt, order.Info.TransactionID);
        }
#endif

        static Task<bool> Log(string receipt, string transactionId)
        {
            var storeTransaction = GrovsUnityIAPReceipt.FromUnityReceipt(receipt, transactionId, out var error);
            return storeTransaction == null ? Fail(error) : Grovs.LogInAppPurchaseAsync(storeTransaction);
        }

        static Task<bool> Fail(string error)
        {
            Debug.LogWarning("[Grovs] Purchase not logged. " + error);
            return Task.FromResult(false);
        }
    }
}
