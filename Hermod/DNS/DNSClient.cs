﻿/*
 * Copyright (c) 2010-2014, Achim 'ahzf' Friedland <achim@graphdefined.org>
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
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Generic;
using System.Net.NetworkInformation;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.Services.DNS
{

    /// <summary>
    /// A DNS resolver client.
    /// </summary>
    public class DNSClient
    {

        #region Data

        private readonly Dictionary<UInt16, ConstructorInfo> RRLookup;

        #endregion

        #region Properties

        #region DNSServers

        private readonly List<IPSocket> _DNSServers;

        public IEnumerable<IPSocket> DNSServers
        {
            get
            {
                return _DNSServers;
            }
        }

        #endregion

        #region QueryTimeout

        public TimeSpan  QueryTimeout       { get; set; }

        #endregion

        #region RecursionDesired

        public Boolean   RecursionDesired   { get; set; }

        #endregion

        #endregion

        #region Constructor(s)

        #region DNSClient(DNSServer)

        /// <summary>
        /// Create a new DNS resolver client.
        /// </summary>
        /// <param name="DNSServer">The DNS server to query.</param>
        public DNSClient(IIPAddress DNSServer)

            : this(new IPSocket[1] { new IPSocket(DNSServer, new IPPort(53)) })

        { }

        #endregion

        #region DNSClient(DNSServer, Port)

        /// <summary>
        /// Create a new DNS resolver client.
        /// </summary>
        /// <param name="DNSServer">The DNS server to query.</param>
        /// <param name="Port">The IP port of the DNS server to query.</param>
        public DNSClient(IIPAddress DNSServer, IPPort Port)

            : this(new IPSocket[1] { new IPSocket(DNSServer, Port) })

        { }

        #endregion

        #region DNSClient(DNSServers)

        /// <summary>
        /// Create a new DNS resolver client.
        /// </summary>
        /// <param name="DNSServers">A list of DNS servers to query.</param>
        public DNSClient(IEnumerable<IIPAddress> DNSServers)

            : this(DNSServers.Select(IPAddress => new IPSocket(IPAddress, new IPPort(53))))

        { }

        #endregion

        #region DNSClient(DNSServers, Port)

        /// <summary>
        /// Create a new DNS resolver client.
        /// </summary>
        /// <param name="DNSServers">A list of DNS servers to query.</param></param>
        /// <param name="Port">The common IP port of the DNS servers to query.</param>
        public DNSClient(IEnumerable<IIPAddress> DNSServers, IPPort Port)

            : this(DNSServers.Select(IPAddress => new IPSocket(IPAddress, Port)))

        { }

        #endregion

        #region DNSClient(SearchForIPv4DNSServers = true, SearchForIPv6DNSServers = true)

        /// <summary>
        /// Create a new DNS resolver client.
        /// </summary>
        /// <param name="SearchForIPv4DNSServers">If yes, the DNS client will query a list of DNS servers from the IPv4 network configuration.</param>
        /// <param name="SearchForIPv6DNSServers">If yes, the DNS client will query a list of DNS servers from the IPv6 network configuration.</param>
        public DNSClient(Boolean SearchForIPv4DNSServers = true,
                         Boolean SearchForIPv6DNSServers = true)

            : this(new IPSocket[0], SearchForIPv4DNSServers, SearchForIPv6DNSServers)

        { }

        #endregion

        #region DNSClient(ManualDNSServers, SearchForIPv4DNSServers = true, SearchForIPv6DNSServers = true)

        /// <summary>
        /// Create a new DNS resolver client.
        /// </summary>
        /// <param name="ManualDNSServers">A list of manually configured DNS servers to query.</param>
        /// <param name="SearchForIPv4DNSServers">If yes, the DNS client will query a list of DNS servers from the IPv4 network configuration.</param>
        /// <param name="SearchForIPv6DNSServers">If yes, the DNS client will query a list of DNS servers from the IPv6 network configuration.</param>
        public DNSClient(IEnumerable<IPSocket>  ManualDNSServers,
                         Boolean                SearchForIPv4DNSServers = true,
                         Boolean                SearchForIPv6DNSServers = true)

        {

            this.RecursionDesired  = true;
            this.QueryTimeout      = TimeSpan.FromSeconds(23.5);

            _DNSServers = new List<IPSocket>(ManualDNSServers);

            #region Search for IPv4/IPv6 DNS Servers...

            if (SearchForIPv4DNSServers)
                _DNSServers.AddRange(NetworkInterface.
                                         GetAllNetworkInterfaces().
                                         Where     (NI        => NI.OperationalStatus == OperationalStatus.Up).
                                         SelectMany(NI        => NI.GetIPProperties().DnsAddresses).
                                         Where     (IPAddress => IPAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).
                                         Select    (IPAddress => new IPSocket(new IPv4Address(IPAddress), new IPPort(53))));

            if (SearchForIPv6DNSServers)
                _DNSServers.AddRange(NetworkInterface.
                                         GetAllNetworkInterfaces().
                                         Where     (NI        => NI.OperationalStatus == OperationalStatus.Up).
                                         SelectMany(NI        => NI.GetIPProperties().DnsAddresses).
                                         Where     (IPAddress => IPAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6).
                                         Select    (IPAddress => new IPSocket(new IPv6Address(IPAddress), new IPPort(53))));

            #endregion

            #region Reflect ResourceRecordTypes

            this.RRLookup          = new Dictionary<UInt16, ConstructorInfo>();

            FieldInfo        TypeIdField;
            ConstructorInfo  Constructor;

            foreach (var _ActualType in typeof(ADNSResourceRecord).
                                            Assembly.GetTypes().
                                            Where(type => type.IsClass &&
                                                 !type.IsAbstract &&
                                                  type.IsSubclassOf(typeof(ADNSResourceRecord))))
            {

                TypeIdField = _ActualType.GetField("TypeId");

                if (TypeIdField == null)
                    throw new ArgumentException("Constant field 'TypeId' of type '" + _ActualType.Name + "' was not found!");

                Constructor = _ActualType.GetConstructor(new Type[2] { typeof(String), typeof(Stream) });

                if (Constructor == null)
                    throw new ArgumentException("Constructor<String, Stream> of type '" + _ActualType.Name + "' was not found!");

                RRLookup.Add((UInt16) TypeIdField.GetValue(_ActualType), Constructor);

            }

            #endregion

        }

        #endregion

        #endregion


        #region Query(DomainName, params ResourceRecordTypes)

        public DNSInfo Query(String           DomainName,
                             params UInt16[]  ResourceRecordTypes)
        {

            if (ResourceRecordTypes.Length == 0)
                ResourceRecordTypes = new UInt16[1] { 255 };

            // Preparing the DNS query packet
            var QueryPacket = new DNSQuery(DomainName, ResourceRecordTypes) {
                                      RecursionDesired = RecursionDesired
                                  };

            // Query DNS server(s)...
            var serverAddress  = IPAddress.Parse(DNSServers.First().IPAddress.ToString());
            var endPoint       = (EndPoint) new IPEndPoint(serverAddress, DNSServers.First().Port.ToInt32());
            var socket         = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout,    (Int32) QueryTimeout.TotalMilliseconds);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, (Int32) QueryTimeout.TotalMilliseconds);
            socket.Connect(endPoint);
            socket.SendTo(QueryPacket.Serialize(), endPoint);

            var data    = new Byte[512];
            var length  = socket.ReceiveFrom(data, ref endPoint);

            socket.Shutdown(SocketShutdown.Both);

            return ReadResponse(new MemoryStream(data));

        }

        #endregion

        #region Query<T>(DomainName)

        public IEnumerable<T> Query<T>(String DomainName)
            where T : ADNSResourceRecord
        {

            var TypeIdField = typeof(T).GetField("TypeId");

            if (TypeIdField == null)
                throw new ArgumentException("Constant field 'TypeId' of type '" + typeof(T).Name + "' was not found!");

            var TypeId = (UInt16) TypeIdField.GetValue(typeof(T));

            return Query(DomainName, new UInt16[1] { TypeId }).
                       Answers.
                       Where(v => v.GetType() == typeof(T)).
                       Cast<T>();

        }

        #endregion

        #region QueryFirst<T>(DomainName)

        public T QueryFirst<T>(String DomainName)
            where T : ADNSResourceRecord
        {
            return Query<T>(DomainName).FirstOrDefault();
        }

        #endregion

        #region Query<T1, T2>(DomainName, Mapper)

        public IEnumerable<T2> Query<T1, T2>(String DomainName, Func<T1, T2> Mapper)
            where T1 : ADNSResourceRecord
        {
            return Query<T1>(DomainName).Select(v => Mapper(v));
        }

        #endregion


        #region (private) ReadResponse(DNSBuffer)

        private DNSInfo ReadResponse(Stream DNSBuffer)
        {

            #region DNS Header

            var RequestId       = ((DNSBuffer.ReadByte() & Byte.MaxValue) << 8) + (DNSBuffer.ReadByte() & Byte.MaxValue);

            var Byte2           = DNSBuffer.ReadByte();
            var IS              = (Byte2 & 128) == 128;
            var OpCode          = (Byte2 >> 3 & 15);
            var AA              = (Byte2 & 4) == 4;
            var TC              = (Byte2 & 2) == 2;
            var RD              = (Byte2 & 1) == 1;

            var Byte3           = DNSBuffer.ReadByte();
            var RA              = (Byte3 & 128) == 128;
            var Z               = (Byte3 & 1);    //reserved, not used
            var ResponseCode    = (DNSResponseCodes) (Byte3 & 15);

            var QuestionCount   = ((DNSBuffer.ReadByte() & Byte.MaxValue) << 8) | (DNSBuffer.ReadByte() & Byte.MaxValue);
            var AnswerCount     = ((DNSBuffer.ReadByte() & Byte.MaxValue) << 8) | (DNSBuffer.ReadByte() & Byte.MaxValue);
            var AuthorityCount  = ((DNSBuffer.ReadByte() & Byte.MaxValue) << 8) | (DNSBuffer.ReadByte() & Byte.MaxValue);
            var AdditionalCount = ((DNSBuffer.ReadByte() & Byte.MaxValue) << 8) | (DNSBuffer.ReadByte() & Byte.MaxValue);

            #endregion

            #region Process Questions

            DNSBuffer.Seek(12, SeekOrigin.Begin);

            for (var i = 0; i < QuestionCount; ++i) {
                var QuestionName  = DNSTools.ExtractName(DNSBuffer);
                var TypeId        = (UInt16)          ((DNSBuffer.ReadByte() & Byte.MaxValue) << 8 | DNSBuffer.ReadByte() & Byte.MaxValue);
                var ClassId       = (DNSQueryClasses) ((DNSBuffer.ReadByte() & Byte.MaxValue) << 8 | DNSBuffer.ReadByte() & Byte.MaxValue);
            }

            #endregion

            var Answers            = new List<ADNSResourceRecord>();
            var Authorities        = new List<ADNSResourceRecord>();
            var AdditionalRecords  = new List<ADNSResourceRecord>();

            for (var i = 0; i < AnswerCount; ++i)
                Answers.Add(ReadResourceRecord(DNSBuffer));

            for (var i = 0; i < AuthorityCount; ++i)
                Authorities.Add(ReadResourceRecord(DNSBuffer));

            for (var i = 0; i < AdditionalCount; ++i)
                AdditionalRecords.Add(ReadResourceRecord(DNSBuffer));

            return new DNSInfo(RequestId, AA, TC, RD, RA, ResponseCode, Answers, Authorities, AdditionalRecords);

        }

        #endregion

        #region (private) ReadResourceRecord(DNSStream)

        private ADNSResourceRecord ReadResourceRecord(Stream DNSStream)
        {

            var ResourceName  = DNSTools.ExtractName(DNSStream);
            var TypeId        = (UInt16) ((DNSStream.ReadByte() & Byte.MaxValue) << 8 | DNSStream.ReadByte() & Byte.MaxValue);

            ConstructorInfo Constructor;

            if (RRLookup.TryGetValue(TypeId, out Constructor))
                return (ADNSResourceRecord) Constructor.Invoke(new Object[2] {
                                                                   ResourceName,
                                                                   DNSStream
                                                               });

            return null;

        }

        #endregion

    }

}
