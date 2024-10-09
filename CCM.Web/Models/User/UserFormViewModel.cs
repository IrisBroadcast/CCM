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

using CCM.Web.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CCM.Web.Models.User
{
    public class UserFormViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "UserName_Required")]
        [Display(ResourceType = typeof(Resources), Name = "UserName")]
        public string UserName { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "First_Name")]
        public string FirstName { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Last_Name")]
        public string LastName { get; set; }

        [DataType(DataType.Password)]
        [MinLength(4, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Password_To_Short")]
        [Display(ResourceType = typeof(Resources), Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Password_Dont_Match")]
        [Display(ResourceType = typeof(Resources), Name = "Confirm_Password")]
        public string PasswordConfirm { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Comment")]
        public string Comment { get; set; }

        [Display(ResourceType = typeof(Resources), Name = "Role")]
        public string RoleId { get; set; }

        public Dictionary<string, string> Roles { get; set; }
    }
}
