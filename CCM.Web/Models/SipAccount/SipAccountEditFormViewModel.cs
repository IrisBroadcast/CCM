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

using CCM.Web.Infrastructure.PasswordGeneration;
using CCM.Web.Infrastructure.ValidationAttributes;
using CCM.Web.Properties;
using System;
using System.ComponentModel.DataAnnotations;

namespace CCM.Web.Models.SipAccount
{
    public class SipAccountEditFormViewModel : SipAccountFormViewModel
    {
        public Guid Id { get; set; }

        [DataType(DataType.Text)]
        [Display(ResourceType = typeof(Resources), Name = nameof(Resources.Change_Password))]
        public bool ChangePassword { get; set; }

        [DataType(DataType.Password)]
        [Display(ResourceType = typeof(Resources), Name = nameof(Resources.Password))]
        [RequiredIf(nameof(ChangePassword), ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = nameof(Resources.Password_Required))]
        [MinLength(PasswordComplexityConfiguration.MinLength, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = nameof(Resources.Password_Must_Be_At_Least_X_Characters_Long))]
        [MaxLength(PasswordComplexityConfiguration.MaxLength, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = nameof(Resources.Password_Can_Not_Be_Longer_Than_X_Characters))]
        [MustContainCharacters(PasswordComplexityConfiguration.MinNumberOfSpecial, PasswordComplexityConfiguration.AllowedSpecialCharacters)]
        [MustContainDigits(PasswordComplexityConfiguration.MinNumberOfDigits)]
        [MustContainLowerCaseLetters(PasswordComplexityConfiguration.MinNumberOfLower)]
        [MustContainUpperCaseLetters(PasswordComplexityConfiguration.MinNumberOfUpper)]
        public string PasswordDefault { get; set; }

        [DataType(DataType.Password)]
        [Display(ResourceType = typeof(Resources), Name = nameof(Resources.Confirm_Password))]
        [Compare(nameof(PasswordDefault), ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = nameof(Resources.Password_Dont_Match))]
        public string PasswordConfirm { get; set; }
    }
}
