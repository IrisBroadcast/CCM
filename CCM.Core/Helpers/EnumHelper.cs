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

using CCM.Core.Enums;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Resources;

namespace CCM.Core.Helpers
{
    public static class EnumHelper
    {
        // Tobias tillägg: Denna smäller inte men frågan är vad som ska hända. 
        private static ResourceManager coreResourceManager = new ResourceManager("CCM.Core.Resources.Resources", Assembly.GetExecutingAssembly());

        //private static ResourceManager coreResourceManager = new ResourceManager("Resources.resx", Assembly.GetExecutingAssembly());
        //private static ResourceManager coreResourceManager = new ResourceManager(typeof(Resources));

        public static string DescriptionAsResource(this Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var field = enumType.GetField(enumValue.ToString());
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length == 0)
            {
                return string.Format($"Update your enum with Description field '{field}'.");
            }

            return coreResourceManager.GetString(((DescriptionAttribute)attributes[0]).Description) ?? string.Format($"Update your resource file with resource key in '{enumType.ToString()}'.");
        }

        //public static string DefaultDescription(this Enum enumValue)
        //{
        //    var enumType = enumValue.GetType();
        //    var field = enumType.GetField(enumValue.ToString());
        //    var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
        //    return attributes.Length == 0 ? enumValue.ToString() : ((DescriptionAttribute)attributes[0]).Description;
        //}

        public static (string, string) DefaultValue(this Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var field = enumType.GetField(enumValue.ToString());
            var attributes = field.GetCustomAttributes(typeof(DefaultSettingAttribute), false);

            if (attributes.Length == 0)
            {
                return (enumValue.ToString(), "Unknown description");
            }
            return (((DefaultSettingAttribute)attributes[0]).Value, ((DefaultSettingAttribute)attributes[0]).Description);
        }
    }
}
