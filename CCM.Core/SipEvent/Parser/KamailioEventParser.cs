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

using System;
using System.Linq;
using System.Text.RegularExpressions;
using CCM.Core.Interfaces.Managers;
using CCM.Core.Interfaces.Parser;
using CCM.Core.SipEvent.Event;
using CCM.Core.SipEvent.Messages;
using CCM.Core.SipEvent.Models;
using Microsoft.Extensions.Logging;

namespace CCM.Core.SipEvent.Parser
{
    /// <summary>
    /// Parses incoming SIP registrar messages in string format.
    /// Separated with '::', this is the first and oldest format.
    /// We are now trying to move to JSON.
    /// The format on the incoming messages is custom made
    /// and the details can be found in the 'connect' project
    /// on Github. Be sure to check that you are using the right
    /// version of 'connect' that formats the messages with string
    /// separation. Or try to see if you can implement the JSON version.
    /// </summary>
    public class KamailioEventParser : IKamailioEventParser
    {
        private readonly ILogger<KamailioEventParser> _logger;
        private readonly ISettingsManager _settingsManager;

        public KamailioEventParser(ISettingsManager settingsManager, ILogger<KamailioEventParser> logger)
        {
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public SipMessageBase Parse(string message)
        {
            var kamailioData = ParseRawKamailioData(message);
            if (kamailioData == null)
            {
                return null;
            }

            switch (kamailioData.MessageType)
            {
                case SipEventMessageType.Request:
                    return ParseRegistration(kamailioData);
                case SipEventMessageType.Dialog:
                    return ParseDialog(kamailioData);
                case SipEventMessageType.RegExpire:
                    return ParseExpiredRegistration(kamailioData);
            }
            return null;
        }

        private SipRegistrationMessage ParseRegistration(KamailioMessageData kamailioData)
        {
            var maxRegistrationAge = _settingsManager.MaxRegistrationAge;

            var registration = new SipRegistrationMessage()
            {
                Ip = kamailioData.GetField("si"),
                Port = ParseInt(kamailioData.GetField("sp")),
                UnixTimeStamp = ParseLong(kamailioData.GetField("TS")),
                Sip = new SipUri(kamailioData.GetField("fu")),
                FromDisplayName = ParseDisplayName(kamailioData.GetField("fn")),
                UserAgent = kamailioData.GetField("ua"),
                ToDisplayName = ParseDisplayName(kamailioData.GetField("tn")),
                Expires = ParseInt(kamailioData.GetField("Expires"), maxRegistrationAge),

                // Not in use
                //Username = kamailioData.GetField("Au"),
                //ToUsername = kamailioData.GetField("rU"),
                //RequestedSip = new SipUri(kamailioData.GetField("ru")),
                //ReceivedIp = kamailioData.GetField("Ri"),
                //ReceivedPort = ParseInt(kamailioData.GetField("Rp")),
                //CallId = kamailioData.GetField("ci"),
            };

            return registration;
        }

        private SipRegistrationExpireMessage ParseExpiredRegistration(KamailioMessageData kamailioData)
        {
            var expire = new SipRegistrationExpireMessage()
            {
                SipAddress = new SipUri(kamailioData.GetField("aor")),
                ReceivedIp = kamailioData.GetField("ip"),
            };

            return expire;
        }

        private SipDialogMessage ParseDialog(KamailioMessageData kamailioData)
        {
            SipDialogStatus sipDialogStatus;
            if (!Enum.TryParse(kamailioData.GetField("dstat"), true, out sipDialogStatus))
            {
                _logger.LogWarning("Unable to parse dstat field of Kamailio dialog message");
                return null;
            }

            var dialog = new SipDialogMessage
            {
                Status = sipDialogStatus,
                CallId = kamailioData.GetField("ci"),
                HashId = kamailioData.GetField("hashid"),
                HashEntry = kamailioData.GetField("hashent"),
                FromDisplayName = ParseDisplayName(kamailioData.GetField("fn")),
                ToDisplayName = ParseDisplayName(kamailioData.GetField("tn")),
                FromSipUri = new SipUri(kamailioData.GetField("fu")),
                ToSipUri = new SipUri(kamailioData.GetField("ru")),
                FromTag = kamailioData.GetField("fot"),
                ToTag = kamailioData.GetField("tot"),
                Sdp = kamailioData.GetField("sdp"),
                HangupReason = kamailioData.GetField("hr")
            };

            // Fix for empty ru-field in Kamailio data
            if (dialog.ToSipUri == null || string.IsNullOrEmpty(dialog.ToSipUri.User))
            {
                dialog.ToSipUri = new SipUri(kamailioData.GetField("tu"));
            }

            return dialog;
        }

        /// <summary>
        /// Parses Kamailio data with a '::'-separated string
        /// </summary>
        private KamailioMessageData ParseRawKamailioData(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Message body empty");
                return null;
            }

            string[] dataFields = message.Split('|');

            if (dataFields.Length == 0)
            {
                return null;
            }

            SipEventMessageType msgType;
            if (!Enum.TryParse(dataFields[0], true, out msgType))
            {
                _logger.LogWarning("Unable to get message type from {0}", dataFields[0]);
                return null;
            }

            var fieldsDictionary = dataFields
                .Select(x => x.Split(new[] { "::" }, StringSplitOptions.None))
                .Where(x => x.Length == 2 && !String.IsNullOrEmpty(x[0].Trim()))
                .ToDictionary(x => x[0].Trim(), x => x[1] == "<null>" ? string.Empty : x[1].Trim());

            return new KamailioMessageData { MessageType = msgType, Fields = fieldsDictionary };
        }

        private static string ParseDisplayName(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : Regex.Unescape(s).Trim(' ', '"');
        }

        private static int ParseInt(string s, int defaultValue = 0)
        {
            int i;
            if (int.TryParse(s, out i))
            {
                return i;
            }
            return defaultValue;
        }

        private static long ParseLong(string s)
        {
            long i;
            long.TryParse(s, out i);
            return i;
        }
    }
}
