
namespace WechatBotWeb.Security
{
    using System.Security.Cryptography;

    public static class SHA256Hashing
    {
        public static byte[] Hash(byte[] data)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                return sha256Hash.ComputeHash(data);
            }
        }
    }
}
