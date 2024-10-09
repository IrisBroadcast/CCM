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

using CCM.Core.Helpers.PasswordGeneration;
using FakeItEasy;
using NUnit.Framework;
using System.Linq;

namespace CCM.UnitTests.CCM.Core.Helpers.PasswordGeneration
{
    [TestFixture]
    public class PasswordGeneratorTests
    {
        [Test]
        public void GeneratePassword_Always_ReturnsPasswordsAccordingToConfiguration()
        {
            const string LowerCaseLetters = "abcdefghijklmnopqrstuvwxyz";
            const string UpperCaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string Digits = "0123456789";
            const string Special = @"!#¤%/&";
            const int NumberOfPasswordsToGenerate = 1000;

            var configurationProvider = A.Fake<IPasswordGeneratorConfigurationProvider>();
            var configuration = new PasswordGeneratorConfiguration(
                length: 16,
                numberOfSpecialCharacters: 1,
                numberOfDigits: 1,
                numberOfLowerCaseLetters: 1,
                numberOfUpperCaseLetters: 1,
                allowedSpecialCharacters: Special);

            A.CallTo(() => configurationProvider.GetConfiguration())
             .Returns(configuration);

            var sipPasswordGenerator = new PasswordGenerator(configurationProvider);

            for (int i = 0; i < NumberOfPasswordsToGenerate; i++)
            {
                var password = sipPasswordGenerator.GeneratePassword();
                var specialCount = password.Count(Special.Contains);
                var digitCount = password.Count(Digits.Contains);
                var lowerCount = password.Count(LowerCaseLetters.Contains);
                var upperCount = password.Count(UpperCaseLetters.Contains);
                Assert.True(specialCount >= configuration.NumberOfSpecialCharacters, "Must at least contain the configured number of special characters");
                Assert.True(digitCount >= configuration.NumberOfDigits, "Must at least contain the configured number of digits");
                Assert.True(lowerCount >= configuration.NumberOfLowerCaseLetters, "Must at least contain the configured number of lower case letters");
                Assert.True(upperCount >= configuration.NumberOfUpperCaseLetters, "Must at least contain the configured number of upper case letters");
            }
        }
    }
}
