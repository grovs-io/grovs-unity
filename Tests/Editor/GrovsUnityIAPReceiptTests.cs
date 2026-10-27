using NUnit.Framework;
using GrovsSDK.Internal;

namespace GrovsSDK.Tests
{
    public class GrovsUnityIAPReceiptTests
    {
        [Test]
        public void AppleStore_UsesTransactionId()
        {
            const string receipt = "{\"Store\":\"AppleAppStore\",\"TransactionID\":\"2000000123\",\"Payload\":\"...\"}";
            var result = GrovsUnityIAPReceipt.FromUnityReceipt(receipt, "2000000123", out var error);
            Assert.AreEqual("2000000123", result);
            Assert.IsNull(error);
        }

        [Test]
        public void AppleStore_FallsBackToReceiptTransactionId()
        {
            const string receipt = "{\"Store\":\"AppleAppStore\",\"TransactionID\":\"2000000123\",\"Payload\":\"...\"}";
            Assert.AreEqual("2000000123", GrovsUnityIAPReceipt.FromUnityReceipt(receipt, null, out _));
        }

        [Test]
        public void MacStore_UsesTransactionId()
        {
            const string receipt = "{\"Store\":\"MacAppStore\",\"TransactionID\":\"42\",\"Payload\":\"...\"}";
            Assert.AreEqual("42", GrovsUnityIAPReceipt.FromUnityReceipt(receipt, "42", out _));
        }

        [Test]
        public void GooglePlay_UsesInnerJson()
        {
            const string receipt = "{\"Store\":\"GooglePlay\",\"TransactionID\":\"GPA.1\",\"Payload\":\"{\\\"json\\\":\\\"{\\\\\\\"orderId\\\\\\\":\\\\\\\"GPA.1\\\\\\\"}\\\",\\\"signature\\\":\\\"s\\\"}\"}";
            var result = GrovsUnityIAPReceipt.FromUnityReceipt(receipt, "GPA.1", out var error);
            Assert.AreEqual("{\"orderId\":\"GPA.1\"}", result);
            Assert.IsNull(error);
        }

        [Test]
        public void GooglePlay_MissingInnerJson_ReturnsNullWithError()
        {
            const string receipt = "{\"Store\":\"GooglePlay\",\"TransactionID\":\"GPA.1\",\"Payload\":\"{\\\"signature\\\":\\\"s\\\"}\"}";
            Assert.IsNull(GrovsUnityIAPReceipt.FromUnityReceipt(receipt, "GPA.1", out var error));
            Assert.IsNotNull(error);
        }

        [Test]
        public void OtherStore_ReturnsNullWithError()
        {
            const string receipt = "{\"Store\":\"Amazon\",\"TransactionID\":\"a1\",\"Payload\":\"{}\"}";
            Assert.IsNull(GrovsUnityIAPReceipt.FromUnityReceipt(receipt, "a1", out var error));
            StringAssert.Contains("Amazon", error);
        }

        [Test]
        public void MalformedReceipt_ReturnsNullWithError()
        {
            foreach (var receipt in new[] { null, "", "not json", "[1,2]", "{\"Payload\":\"x\"}" })
            {
                Assert.IsNull(GrovsUnityIAPReceipt.FromUnityReceipt(receipt, "1", out var error), receipt);
                Assert.IsNotNull(error, receipt);
            }
        }
    }
}
