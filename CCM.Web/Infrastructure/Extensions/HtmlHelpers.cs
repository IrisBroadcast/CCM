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

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq.Expressions;

namespace CCM.Web.Infrastructure.Extensions
{
    public static class HtmlHelpers
    {
        /// <summary>
        /// Usage:
        /// @Html.DescriptionFor(m => m.PropertyName)
        ///
        /// supply cssclass name, and override span with div tag
        /// @Html.DescriptionFor(m => m.PropertyName, "desc", "div")
        ///
        /// using the named param
        /// @Html.DescriptionFor(m => m.PropertyName, tagName: "div")
        /// </summary>
        public static IHtmlContent DescriptionFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string cssClassName = "", string tagName = "span")
        {
            // if (html == null) throw new ArgumentNullException(nameof(html));
            // if (expression == null) throw new ArgumentNullException(nameof(expression));

            var expressionProvider = html.ViewContext?.HttpContext?.RequestServices?.GetService<ModelExpressionProvider>()
                ?? new ModelExpressionProvider(html.MetadataProvider);
            var modelExpression = expressionProvider.CreateModelExpression(html.ViewData, expression);
            var description = modelExpression.Metadata.Description;

            if (!string.IsNullOrEmpty(description))
            {
                using (var writer = new StringWriter())
                {
                    var tag = new TagBuilder(tagName);
                    tag.InnerHtml.AppendHtml(description);

                    if (!string.IsNullOrEmpty(cssClassName))
                    {
                        tag.AddCssClass(cssClassName);
                    }
                    tag.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                    var htmlOutput = writer.ToString();

                    return new HtmlString(htmlOutput);
                }
            }

            return new HtmlString("");
        }
    }
}
