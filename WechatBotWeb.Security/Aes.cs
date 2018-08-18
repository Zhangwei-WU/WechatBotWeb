using System;
using System.IO;
using System.Security.Cryptography;

namespace WechatBotWeb.Security
{
    /// <summary>
    /// AES cryptography
    /// </summary>
    public static class Aes
    {
        /// <summary>
        /// Aes key byte length
        /// should always be 32
        /// </summary>
        public const int AesKeyLength = 32;

        /// <summary>
        /// Aes IV byte length
        /// should always be 16
        /// </summary>
        public const int AesIVLength = 16;

        /// <summary>
        /// Read Buffer Length
        /// </summary>
        public const int ReadBufferLength = 1024 * 1024 * 2;

        /// <summary>
        /// aes crypto service provider pool
        /// </summary>
        private static ResourcePool<AesCryptoServiceProvider> aesCryptoServiceProviderPool =
            new ResourcePool<AesCryptoServiceProvider>(
                (aes) =>
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                });

        /// <summary>
        /// Get a new random Aes Key
        /// </summary>
        /// <returns>key array</returns>
        public static byte[] NewKey()
        {
            var key = new byte[AesKeyLength];
            Buffer.BlockCopy(Guid.NewGuid().ToByteArray(), 0, key, 0, AesKeyLength / 2);
            Buffer.BlockCopy(Guid.NewGuid().ToByteArray(), 0, key, AesKeyLength / 2, AesKeyLength / 2);

            return key;
        }

        /// <summary>
        /// Get a new random Aes IV
        /// </summary>
        /// <returns>iv array</returns>
        /// <remarks>
        /// using System.Security.Cryptography.Aes is impact performance a lot
        /// here we use Guid.NewGuid() to generate the random byte array
        /// </remarks>
        public static byte[] NewIV()
        {
            return Guid.NewGuid().ToByteArray();
        }

        /// <summary>
        /// encrypt using key with new initialized iv
        /// </summary>
        /// <param name="dataToEncrypt">raw data</param>
        /// <param name="key">aes key</param>
        /// <returns>encrypted data</returns>
        public static byte[] Encrypt(byte[] dataToEncrypt, byte[] key)
        {
            return Encrypt(dataToEncrypt, key, NewIV());
        }

        /// <summary>
        /// encrypt using key and iv
        /// </summary>
        /// <param name="dataToEncrypt">raw data</param>
        /// <param name="key">aes key</param>
        /// <param name="iv">aes iv</param>
        /// <returns>encrypted data</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "by deisgn")]
        public static byte[] Encrypt(byte[] dataToEncrypt, byte[] key, byte[] iv)
        {
            if (dataToEncrypt == null || dataToEncrypt.Length == 0)
            {
                return null;
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (key.Length != AesKeyLength)
            {
                throw new ArgumentOutOfRangeException("key", "AES Key Length should always be " + AesKeyLength);
            }

            if (iv == null)
            {
                throw new ArgumentNullException("iv");
            }

            if (iv.Length != AesIVLength)
            {
                throw new ArgumentOutOfRangeException("iv", "AES IV Length should always be " + AesIVLength);
            }

            using (var res = aesCryptoServiceProviderPool.Get())
            {
                using (var encrypter = res.Data.CreateEncryptor(key, iv))
                {
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(iv, 0, AesIVLength);
                        using (var cs = new CryptoStream(ms, encrypter, CryptoStreamMode.Write))
                        {
                            cs.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                            cs.FlushFinalBlock();
                        }

                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Encrypt Stream to Stream
        /// </summary>
        /// <param name="input">Input Stream</param>
        /// <param name="output">Output Stream</param>
        /// <param name="key">Aes Key</param>
        /// <param name="iv">Aes IV</param>
        /// <returns></returns>
        public static void Encrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (key.Length != AesKeyLength)
            {
                throw new ArgumentOutOfRangeException("key", "AES Key Length should always be " + AesKeyLength);
            }

            if (iv == null)
            {
                throw new ArgumentNullException("iv");
            }

            if (iv.Length != AesIVLength)
            {
                throw new ArgumentOutOfRangeException("iv", "AES IV Length should always be " + AesIVLength);
            }

            using (var res = aesCryptoServiceProviderPool.Get())
            {
                using (var encrypter = res.Data.CreateEncryptor(key, iv))
                {
                    output.Write(iv, 0, AesIVLength);
                    using (var cs = new CryptoStream(output, encrypter, CryptoStreamMode.Write))
                    {
                        byte[] bytes = new byte[ReadBufferLength];
                        int read;
                        do
                        {
                            read = input.Read(bytes, 0, bytes.Length);
                            cs.Write(bytes, 0, read);
                        }
                        while (read > 0);
                        cs.FlushFinalBlock();
                    }
                }
            }
        }

        /// <summary>
        /// decrypt using key
        /// iv should be embedded as the head 16 bytes in encrypted array
        /// </summary>
        /// <param name="dataToDecrypt">encrypted data</param>
        /// <param name="key">aes key</param>
        /// <returns>decrypted array</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "by design")]
        public static byte[] Decrypt(byte[] dataToDecrypt, byte[] key)
        {
            if (dataToDecrypt == null || dataToDecrypt.Length == 0)
            {
                return null;
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (key.Length != AesKeyLength)
            {
                throw new ArgumentOutOfRangeException("key", "AES Key Length should always be " + AesKeyLength);
            }

            using (var res = aesCryptoServiceProviderPool.Get())
            {
                using (var ms = new MemoryStream(dataToDecrypt))
                {
                    var iv = new byte[AesIVLength];
                    var ivSize = ms.Read(iv, 0, AesIVLength);

                    if (ivSize != AesIVLength)
                    {
                        throw new ArgumentOutOfRangeException("dataToDecrypt", "it looks like an invalid encrypted data");
                    }

                    using (var decryptor = res.Data.CreateDecryptor(key, iv))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            byte[] decrypted = new byte[dataToDecrypt.Length];
                            var dataSize = cs.Read(decrypted, 0, decrypted.Length);
                            var fitSizeBytes = new byte[dataSize];
                            Buffer.BlockCopy(decrypted, 0, fitSizeBytes, 0, dataSize);
                            return fitSizeBytes;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypt Stream to Stream
        /// </summary>
        /// <param name="input">Input Stream</param>
        /// <param name="output">Output Stream</param>
        /// <param name="key">Aes Key</param>
        public static void Decrypt(Stream input, Stream output, byte[] key)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (key.Length != AesKeyLength)
            {
                throw new ArgumentOutOfRangeException("key", "AES Key Length should always be " + AesKeyLength);
            }

            using (var res = aesCryptoServiceProviderPool.Get())
            {
                var iv = new byte[AesIVLength];
                var ivSize = input.Read(iv, 0, AesIVLength);

                if (ivSize != AesIVLength)
                {
                    throw new ArgumentOutOfRangeException("dataToDecrypt", "it looks like an invalid encrypted data");
                }

                using (var decryptor = res.Data.CreateDecryptor(key, iv))
                {
                    using (var cs = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
                    {
                        byte[] bytes = new byte[ReadBufferLength];
                        int read;
                        do
                        {
                            read = cs.Read(bytes, 0, bytes.Length);
                            output.Write(bytes, 0, read);
                        }
                        while (read > 0);
                        output.Flush();
                    }
                }
            }
        }
    }

}
