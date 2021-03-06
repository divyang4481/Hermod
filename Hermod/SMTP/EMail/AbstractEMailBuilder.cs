﻿/*
 * Copyright (c) 2010-2015, Achim 'ahzf' Friedland <achim@graphdefined.org>
 * This file is part of Vanaheimr Hermod <http://www.github.com/Vanaheimr/Hermod>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.BouncyCastle;
using Org.BouncyCastle.Bcpg;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.Services.Mail
{

    public class MailBodyString
    {

        private readonly String[] _Lines;

        public IEnumerable<String> Lines
        {
            get
            {
                return _Lines;
            }
        }


        public MailBodyString(String Lines)
        {
            this._Lines = Lines.Replace("\r\n", "\n").Split(new Char[] { '\n' }, StringSplitOptions.None);
        }

        public MailBodyString(IEnumerable<String> Lines)
        {
            this._Lines = Lines.ToArray();
        }

    }


    /// <summary>
    /// An e-mail builder.
    /// </summary>
    public abstract class AbstractEMailBuilder
    {

        #region Data

        protected internal Dictionary<String, String>  _AdditionalHeaders;
        protected readonly List<EMailBodypart>         _Attachments;

        #endregion

        #region Properties

        //ToDo: "resentSender", "resentDate", "resentMessageId", "Resent-From", 
        //      "Resent-Reply-To", "Resent-To", "Resent-Cc", "Resent-Bcc",

        #region From

        private EMailAddress _From;

        /// <summary>
        /// The sender of this e-mail.
        /// </summary>
        public EMailAddress From
        {

            get
            {
                return _From;
            }

            set
            {
                if (value != null)
                    _From = value;
            }

        }

        #endregion

        #region To

        private readonly EMailAddressList _To;

        /// <summary>
        /// The receivers of this e-mail.
        /// </summary>
        public EMailAddressList To
        {

            get
            {
                return _To;
            }

            set
            {
                if (value != null)
                    _To.Add(value);
            }

        }

        #endregion

        #region ReplyTo

        private readonly EMailAddressList _ReplyTo;

        /// <summary>
        /// The receivers of any reply on this e-mail.
        /// </summary>
        public EMailAddressList ReplyTo
        {

            get
            {
                return _ReplyTo;
            }

            set
            {
                if (value != null)
                    _ReplyTo.Add(value);
            }

        }

        #endregion

        #region Cc

        private readonly EMailAddressList _Cc;

        /// <summary>
        /// Additional receivers of this e-mail.
        /// </summary>
        public EMailAddressList Cc
        {

            get
            {
                return _Cc;
            }

            set
            {
                if (value != null)
                    _Cc.Add(value);
            }

        }

        #endregion

        #region Bcc

        private readonly EMailAddressList _Bcc;

        /// <summary>
        /// Additional but hidden receivers of this e-mail.
        /// </summary>
        public EMailAddressList Bcc
        {

            get
            {
                return _Bcc;
            }

            set
            {
                if (value != null)
                    _Bcc.Add(value);
            }

        }

        #endregion

        #region Subject

        private String _Subject;

        /// <summary>
        /// The subject of this e-mail.
        /// </summary>
        public String Subject
        {

            get
            {
                return _Subject;
            }

            set
            {
                if (value != null && value != String.Empty && value.Trim() != "")
                    _Subject = value.Trim();
            }

        }

        #endregion

        #region Date

        /// <summary>
        /// The sending timestamp of this e-mail.
        /// </summary>
        public DateTime Date { get; set; }

        #endregion

        #region MessageId

        private MessageId _MessageId;

        /// <summary>
        /// The unique message identification of the e-mail.
        /// </summary>
        public MessageId MessageId
        {

            get
            {
                return _MessageId;
            }

            set
            {
                if (value != null)
                    _MessageId = value;
            }

        }

        #endregion

        private readonly MessageId _Reference;

        private readonly List<MessageId> _References;

        #region SecurityLevel

        /// <summary>
        /// The security level of the e-mail.
        /// </summary>
        public EMailSecurity SecurityLevel { get; set; }

        #endregion

        #region Passphrase

        public String Passphrase
        {
            get;
            set;
        }

        #endregion


        #region Body

        protected EMailBodypart _Body;

        /// <summary>
        /// The e-mail body.
        /// </summary>
        public EMailBodypart Body
        {
            get
            {
                return _Body;
            }
        }

        #endregion


        #region AsImmutable

        /// <summary>
        /// Convert this e-mail builder to an immutable e-mail.
        /// </summary>
        public EMail AsImmutable
        {
            get
            {

                if (From.Address.Value.IsNullOrEmpty() ||
                    To.Count() < 1 ||
                    Subject.IsNullOrEmpty())

                    throw new Exception("Invalid email!");

                return new EMail(this);

            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        #region AbstractEMailBuilder()

        /// <summary>
        /// Create a new e-mail builder.
        /// </summary>
        public AbstractEMailBuilder()
        {

            this._To                 = new EMailAddressList();
            this._ReplyTo            = new EMailAddressList();
            this._Cc                 = new EMailAddressList();
            this._Bcc                = new EMailAddressList();
            this._Subject            = "";
            this. Date               = DateTime.Now;
            this._References         = new List<MessageId>();
            this._AdditionalHeaders  = new Dictionary<String, String>();
            this._Attachments        = new List<EMailBodypart>();

            this. SecurityLevel      = EMailSecurity.autosign;

        }

        #endregion

        #region AbstractEMailBuilder(TextLines)

        /// <summary>
        /// Parse the given text lines.
        /// </summary>
        /// <param name="TextLines">An enumeration of strings.</param>
        public AbstractEMailBuilder(IEnumerable<String> TextLines)
        {

            var Body         = new List<String>();
            var ReadBody     = false;

            String Property  = null;
            String Value     = null;

            foreach (var Line in TextLines)
            {

                if (ReadBody)
                    Body.Add(Line);

                else if (Line.IsNullOrEmpty())
                {

                    ReadBody = true;

                    if (Property != null)
                        AddHeaderValues(Property, Value);

                    Property = null;
                    Value    = null;

                }

                // The current line is part of a previous line
                else if (Line.StartsWith(" ") ||
                         Line.StartsWith("\t"))
                {

                    // Only if this is the first line ever read!
                    if (Property.IsNullOrEmpty())
                        throw new Exception("Invalid headers found!");

                    Value += " " + Line.Trim();

                }

                else
                {

                    if (Property != null)
                        AddHeaderValues(Property, Value);

                    var Splitted = Line.Split(new Char[] { ':' }, 2, StringSplitOptions.None);

                    Property = Splitted[0].Trim();
                    Value    = Splitted[1].Trim();

                }

            }

        }

        #endregion

        #endregion


        #region AddHeaderValues(Key, Value)

        public AbstractEMailBuilder AddHeaderValues(String Key, String Value)
        {

            //FixMe!
            switch (Key.ToLower())
            {

                case "from":
                    this._From = new EMailAddress(SimpleEMailAddress.Parse(Value));
                    break;

                case "to":
                    this._To.Add(new EMailAddress(SimpleEMailAddress.Parse(Value)));
                    break;

                case "cc":
                    this._To.Add(new EMailAddress(SimpleEMailAddress.Parse(Value)));
                    break;

                case "subject":
                    this._Subject = Value;
                    break;

                default: _AdditionalHeaders.Add(Key, Value);
                    break;

            }

            return this;

        }

        #endregion

        #region AddAttachment(EMailBodypart)

        /// <summary>
        /// Add an attachment to this e-mail.
        /// </summary>
        /// <param name="EMailBodypart">An attachment.</param>
        public AbstractEMailBuilder AddAttachment(EMailBodypart EMailBodypart)
        {
            _Attachments.Add(EMailBodypart);
            return this;
        }

        #endregion


        #region (protected, abstract) EncodeBodyparts()

        /// <summary>
        /// Encode all nested e-mail body parts.
        /// </summary>
        protected abstract EMailBodypart _EncodeBodyparts();

        #endregion

        #region (internal) EncodeBodyparts()

        /// <summary>
        /// Encode this and all nested e-mail body parts.
        /// </summary>
        internal void EncodeBodyparts()
        {

            var SignTheMail     = false;
            var EncryptTheMail  = false;

            EMailBodypart BodypartToBeSigned = null;

            #region Add attachments, if available...

            if (_Attachments.Count == 0)
                BodypartToBeSigned  = _EncodeBodyparts();

            else
                BodypartToBeSigned  = new EMailBodypart(ContentType:              MailContentTypes.multipart_mixed,
                                                        ContentTransferEncoding:  "8bit",
                                                        Charset:                  "utf-8",
                                                        NestedBodyparts:          new EMailBodypart[] { _EncodeBodyparts() }.
                                                                                      Concat(_Attachments)
                                                       );

            #endregion

            #region Check security settings

            switch (SecurityLevel)
            {

                case EMailSecurity.autosign:

                    if (From.SecretKey != null & Passphrase.IsNotNullOrEmpty())
                        SignTheMail = true;

                    break;


                case EMailSecurity.sign:

                    if (From.SecretKey == null | Passphrase.IsNullOrEmpty())
                        throw new ApplicationException("Can not sign the e-mail!");

                    SignTheMail = true;

                    break;


                case EMailSecurity.auto:

                    if (From.SecretKey != null & Passphrase.IsNotNullOrEmpty())
                        SignTheMail = true;

                    if (SignTheMail                      &&
                        To.Any(v => v.PublicKey != null) &&
                        Cc.Any(v => v.PublicKey != null))
                        EncryptTheMail = true;

                    break;


                case EMailSecurity.encrypt:

                    if (From.SecretKey          == null  |
                        Passphrase.IsNullOrEmpty()       |
                        To.Any(v => v.PublicKey == null) |
                        Cc.Any(v => v.PublicKey == null))
                        throw new ApplicationException("Can not sign and encrypt the e-mail!");

                    EncryptTheMail = true;

                    break;

            }

            #endregion

            #region Sign the e-mail

            if (SignTheMail & !EncryptTheMail)
            {

                var DataToBeSigned      = BodypartToBeSigned.

                                              // Include headers of this MIME body
                                              // https://tools.ietf.org/html/rfc1847 Security Multiparts for MIME:
                                              ToText().

                                              // Any trailing whitespace MUST then be removed from the signed material
                                              Select(line => line.TrimEnd()).

                                              // Canonical text format with <CR><LF> line endings
                                              // https://tools.ietf.org/html/rfc3156 5. OpenPGP signed data
                                              Aggregate((a, b) => a + "\r\n" + b)

                                              // Apply Content-Transfer-Encoding

                                              // Additional new line
                                              + "\r\n";

                // MIME Security with OpenPGP (rfc3156, https://tools.ietf.org/html/rfc3156)
                // OpenPGP Message Format     (rfc4880, https://tools.ietf.org/html/rfc4880)
                _Body = new EMailBodypart(ContentType:                 MailContentTypes.multipart_signed,
                                          AdditionalContentTypeInfos:  new List<KeyValuePair<String, String>>() {
                                                                           new KeyValuePair<String, String>("micalg",   "pgp-sha512"),
                                                                           new KeyValuePair<String, String>("protocol", "application/pgp-signature"),
                                                                       },
                                          ContentTransferEncoding:     "8bit",
                                          Charset:                     "utf-8",
                                          NestedBodyparts:             new EMailBodypart[] {

                                                                           BodypartToBeSigned,

                                                                           new EMailBodypart(ContentType:              MailContentTypes.application_pgp__signature,
                                                                                         //    ContentTransferEncoding:  "8bit",
                                                                                             Charset:                  "utf-8",
                                                                                             ContentDescription:       "OpenPGP digital signature",
                                                                                             ContentDisposition:       ContentDispositions.attachment.ToString() + "; filename=\"signature.asc\"",
                                                                                             Content:                  new MailBodyString(

                                                                                                                           OpenPGP.CreateSignature(new MemoryStream(DataToBeSigned.ToUTF8Bytes()),
                                                                                                                                                   From.SecretKey,
                                                                                                                                                   Passphrase,
                                                                                                                                                   HashAlgorithm: HashAlgorithms.Sha512).

                                                                                                                                   WriteTo(new MemoryStream(),
                                                                                                                                           CloseOutputStream: false).
                                                                                                                                       ToUTF8String())

                                                                                                                       )

                                                                       }
                                         );

            }

            #endregion

            #region Encrypt the e-mail

            else if (SignTheMail & EncryptTheMail)
            {

                // MIME Security with OpenPGP (rfc3156, https://tools.ietf.org/html/rfc3156)
                // OpenPGP Message Format     (rfc4880, https://tools.ietf.org/html/rfc4880)
                _Body = new EMailBodypart(ContentType:                 MailContentTypes.multipart_encrypted,
                                          AdditionalContentTypeInfos:  new List<KeyValuePair<String, String>>() {
                                                                           new KeyValuePair<String, String>("protocol", "application/pgp-encrypted"),
                                                                       },
                                          ContentTransferEncoding:     "8bit",
                                          Charset:                     "utf-8",
                                          NestedBodyparts:             new EMailBodypart[] {

                                                                           new EMailBodypart(ContentType:          MailContentTypes.application_pgp__encrypted,
                                                                                             Charset:              "utf-8",
                                                                                             ContentDescription:   "PGP/MIME version identification",
                                                                                             ContentDisposition:   ContentDispositions.attachment.ToString() + "; filename=\"signature.asc\"",
                                                                                             Content:              new MailBodyString("Version: 1")),

                                                                           new EMailBodypart(ContentType:          MailContentTypes.application_octet__stream,
                                                                                             Charset:              "utf-8",
                                                                                             ContentDescription:   "OpenPGP encrypted message",
                                                                                             ContentDisposition:   ContentDispositions.inline.ToString() + "; filename=\"encrypted.asc\"",
                                                                                             Content:              new MailBodyString(BodypartToBeSigned.ToString())),

                                                                       }
                                         );

            }

            #endregion


            else
                this._Body = _EncodeBodyparts();

        }

        #endregion

    }

}
