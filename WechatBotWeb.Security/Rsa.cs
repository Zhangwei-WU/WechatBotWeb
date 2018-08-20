
namespace WechatBotWeb.Security
{
    using System.Security.Cryptography;

    public static class Rsa
    {
        private static ResourcePool<RSACryptoServiceProvider> rsaCryptoServiceProviderPool = new ResourcePool<RSACryptoServiceProvider>(null, (r) => r.Clear());

        public static bool VerifySha256Hash(byte[] hash, byte[] sign, byte[] n, byte[] e)
        {
            using (var rsa = rsaCryptoServiceProviderPool.Get())
            {
                var p = new RSAParameters() { Modulus = n, Exponent = e };
                rsa.Data.ImportParameters(p);

                return rsa.Data.VerifyHash(hash, sign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }
    }
}
