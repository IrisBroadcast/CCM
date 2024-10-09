/*
 * Copyright (c) 2018 Sveriges Radio AB, Stockholm, Sweden
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using CCM.Core.Helpers;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CCM.UnitTests.CCM.Core
{
    [TestFixture]
    public class CryptoHelperTests
    {

        [Test]
        public void CheckPassword()
        {
            var hash = "yeWiadH0QT6kc/tz6QQjtJF9aBGXATnUXJNxnwa2HeV6B4Ti";
            byte[] bytes = Convert.FromBase64String(hash);
            Console.WriteLine("Hash:    " + AsHexString(bytes));

            var pwdHash = bytes.Take(16).ToArray();
            Console.WriteLine("PwdHash: " + AsHexString(pwdHash));

            var saltBytes = bytes.Skip(16).ToArray();
            Console.WriteLine("Salt:    " + AsHexString(saltBytes));
            var salt64 = Convert.ToBase64String(saltBytes);
            Console.WriteLine("Salt64:  " + salt64);

            var md5 = MD5.Create();
            var password = "helpdesk";
            var passwordArray = Encoding.UTF8.GetBytes(password);
            var passwordArrayWithSalt = passwordArray.Concat(saltBytes).ToArray();
            var hashBytes = md5.ComputeHash(passwordArrayWithSalt);
            var hashBytesWithSalt = hashBytes.Concat(saltBytes).ToArray();
            var hash2 = Convert.ToBase64String(hashBytesWithSalt);
            Console.WriteLine(hash2);

            Assert.AreEqual(hash, hash2);
        }

        [Test]
        public void DecryptBase64String()
        {
            var password = "test";
            var s = CryptoHelper.Md5HashSaltedPassword(password);

            //var s = "QUVgNJAwPAIg2t+p/oK8XlzMCtuCpyHiY/+GWR4UGlJA1KPt";
            //var s = "Fuydi2bLFQq09riETC+pqUcbApUaRI/UvRbN5cYX6yFYXfnm";
            //var s = "hvdfJktj2Dcqmh5e1gcmTjXoD9IHGx1meHMTeE/N6gy8qw+8";

            byte[] bytes = Convert.FromBase64String(s);


            //StringBuilder hex = new StringBuilder();
            //foreach (byte b in bytes)
            //{
            //    hex.AppendFormat("{0:x2}", b);
            //}

            //var s2 = hex.ToString();

            Console.WriteLine(AsHexString(bytes));
            Console.WriteLine(@"Length = " + bytes.Length);

            var pwdHash = bytes.Take(16).ToArray();
            var salt = bytes.Skip(16).ToArray();

            Console.WriteLine(AsHexString(pwdHash));
            Console.WriteLine(AsHexString(salt));

        }

        private static string AsHexString(byte[] bytes)
        {
            return String.Concat(Array.ConvertAll(bytes, x => x.ToString("X2")));
        }
    }
}
