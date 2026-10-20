using System;
using System.Collections.Generic;

namespace GrovsSDK.Internal
{
    /// <summary>
    /// Turns a Unity IAP receipt ({Store, TransactionID, Payload}) into the string the native
    /// bridge expects: the StoreKit transaction id on Apple stores, the Play originalJson on Google Play.
    /// </summary>
    internal static class GrovsUnityIAPReceipt
    {
        public static string FromUnityReceipt(string receiptJson, string transactionId, out string error)
        {
            var receipt = ParseObject(receiptJson);
            if (receipt == null)
            {
                error = "The Unity IAP receipt is not valid JSON.";
                return null;
            }

            var store = Text(receipt, "Store");
            switch (store)
            {
                case "AppleAppStore":
                case "MacAppStore":
                    return AppleTransaction(receipt, transactionId, out error);
                case "GooglePlay":
                    return GoogleOriginalJson(receipt, out error);
                case null:
                    error = "The Unity IAP receipt has no Store.";
                    return null;
                default:
                    error = $"Store {store} is not supported. Only Apple and Google Play purchases are logged.";
                    return null;
            }
        }

        static string AppleTransaction(Dictionary<string, object> receipt, string transactionId, out string error)
        {
            var id = string.IsNullOrEmpty(transactionId) ? Text(receipt, "TransactionID") : transactionId;
            error = string.IsNullOrEmpty(id) ? "The Apple purchase has no transaction id." : null;
            return error == null ? id : null;
        }

        static string GoogleOriginalJson(Dictionary<string, object> receipt, out string error)
        {
            var payload = ParseObject(Text(receipt, "Payload"));
            var originalJson = payload == null ? null : Text(payload, "json");
            error = string.IsNullOrEmpty(originalJson) ? "The Google Play receipt has no purchase json." : null;
            return error == null ? originalJson : null;
        }

        static Dictionary<string, object> ParseObject(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return GrovsJson.Parse(json) as Dictionary<string, object>;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        static string Text(Dictionary<string, object> map, string key) =>
            map.TryGetValue(key, out var value) ? value as string : null;
    }
}
