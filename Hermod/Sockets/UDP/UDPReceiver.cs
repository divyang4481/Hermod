﻿/*
 * Copyright (c) 2010-2013, Achim 'ahzf' Friedland <achim@graph-database.org>
 * This file is part of Hermod <http://www.github.com/Vanaheimr/Hermod>
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
using System.Net;
using System.Threading;
using System.Reflection;
using System.Net.Sockets;
using System.Threading.Tasks;

using eu.Vanaheimr.Styx;
using eu.Vanaheimr.Hermod;

#endregion

namespace eu.Vanaheimr.Hermod.Sockets.UDP
{

    #region UDPReceiver<TOut>

    /// <summary>
    /// A Styx arrow sender that listens on an UDP socket
    /// and notifies about incoming UDP packets or messages.
    /// </summary>
    /// <typeparam name="TOut">The type of the Styx arrows to send.</typeparam>
    public class UDPReceiver<TOut> : INotification<TOut>,
                                     INotification<UDPPacket<TOut>>,
                                     IServer
    {

        #region Data

        private          Task                     ListenerTask;
        private readonly Socket                   LocalDotNetSocket;
        private readonly IPEndPoint               LocalIPEndPoint;
        private readonly MapperDelegate           Mapper; 
        private          CancellationTokenSource  CancellationTokenSource;
        private          CancellationToken        CancellationToken;
        public  readonly IPSocket                 LocalSocket;

        #endregion

        #region Properties

        // readonly

        #region IPAddress

        private readonly IIPAddress _IPAddress;

        /// <summary>
        /// Gets the IPAddress on which the TCPServer listens.
        /// </summary>
        public IIPAddress IPAddress
        {
            get
            {
                return _IPAddress;
            }
        }

        #endregion

        #region Port

        private readonly IPPort _Port;

        /// <summary>
        /// Gets the port on which the TCPServer listens.
        /// </summary>
        public IPPort Port
        {
            get
            {
                return _Port;
            }
        }

        #endregion

        #region ThreadName

        private readonly String _ThreadName;

        /// <summary>
        /// The name of the UDP receiver thread.
        /// </summary>
        public String ThreadName
        {
            get
            {
                return _ThreadName;
            }
        }

        #endregion

        #region ThreadPrio

        private readonly ThreadPriority _ThreadPrio;

        /// <summary>
        /// The priority of the UDP receiver thread.
        /// </summary>
        public ThreadPriority ThreadPrio
        {
            get
            {
                return _ThreadPrio;
            }
        }

        #endregion

        #region IsBackground

        private readonly Boolean _IsBackground;

        /// <summary>
        /// Whether the UDP receiver thread is a background thread or not.
        /// </summary>
        public Boolean IsBackground
        {
            get
            {
                return _IsBackground;
            }
        }

        #endregion


        // mutatable

        #region BufferSize

        /// <summary>
        /// The size of the receive buffer.
        /// </summary>
        public UInt32 BufferSize { get; set; }

        #endregion

        #region ReceiveTimeout

        /// <summary>
        /// Gets or sets a value that specifies the amount of time in milliseconds
        /// after which a synchronous Socket.Receive(...) call will time out.
        /// </summary>
        public UInt32 ReceiveTimeout
        {

            get
            {

                if (LocalDotNetSocket != null)
                    return (UInt32) LocalDotNetSocket.ReceiveTimeout;

                return 0;

            }

            set
            {

                if (value > Int32.MaxValue)
                    throw new ArgumentException("The value for the ReceiveTimeout must be smaller than " + Int32.MaxValue + "!");

                if (LocalDotNetSocket != null)
                    LocalDotNetSocket.ReceiveTimeout = (Int32) value;

            }

        }

        #endregion

        #region IsRunning

        private Int32 _IsRunning = 0;

        /// <summary>
        /// True while the server is listening for new clients
        /// </summary>
        public Boolean IsRunning
        {
            get
            {
                return _IsRunning == 1;
            }
        }

        #endregion

        #region StopRequested

        /// <summary>
        /// The server was requested to stop and will no
        /// longer accept new client connections
        /// </summary>
        public Boolean StopRequested
        {
            get
            {
                return this.CancellationToken.IsCancellationRequested;
            }
        }

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// A delegate to transform the incoming UDP packets into a custom data structure.
        /// </summary>
        /// <param name="Timestamp">The server timestamp of the UDP packet.</param>
        /// <param name="LocalSocket">The local UDP socket.</param>
        /// <param name="RemoteSocket">The remote UDP socket.</param>
        /// <param name="Message">The payload of the UDP packet.</param>
        /// <returns>The payload/message of the UDP packet transformed into a custom data structure.</returns>
        public delegate TOut MapperDelegate(DateTime Timestamp, IPSocket LocalSocket, IPSocket RemoteSocket, Byte[] Message);



        private event NotificationEventHandler<TOut>            OnNotification_Message;
        private event NotificationEventHandler<UDPPacket<TOut>> OnNotification_UDPPacket;



        // INotification
        event NotificationEventHandler<TOut> INotification<TOut>.OnNotification
        {
            add    { OnNotification_Message   += value; }
            remove { OnNotification_Message   -= value; }
        }

        event NotificationEventHandler<UDPPacket<TOut>> INotification<UDPPacket<TOut>>.OnNotification
        {
            add    { OnNotification_UDPPacket += value; }
            remove { OnNotification_UDPPacket -= value; }
        }



        public event ExceptionEventHandler OnError;
        public event CompletedEventHandler OnCompleted;

        #endregion

        #region Constructor(s)

        #region UDPReceiver(Port, Mapper, ThreadName = "...", ThreadPrio = AboveNormal, IsBackground = true, Autostart = false)

        /// <summary>
        /// Create a new UDP receiver using IPAddress.Any and the given parameters.
        /// </summary>
        /// <param name="Port">The port to listen.</param>
        /// <param name="Mapper">A delegate to transform the incoming UDP packets into a custom data structure.</param>
        /// <param name="ThreadName">The optional name of the UDP receiver thread.</param>
        /// <param name="ThreadPrio">The optional priority of the UDP receiver thread.</param>
        /// <param name="IsBackground">Whether the UDP receiver thread is a background thread or not.</param>
        /// <param name="Autostart">Start the UDP receiver thread immediately.</param>
        public UDPReceiver(IPPort          Port,
                           MapperDelegate  Mapper,
                           String          ThreadName    = "UDPReceiver thread",
                           ThreadPriority  ThreadPrio    = ThreadPriority.AboveNormal,
                           Boolean         IsBackground  = true,
                           Boolean         Autostart     = false)

            : this(IPv4Address.Any, Port, Mapper, ThreadName, ThreadPrio, IsBackground, Autostart)

        { }

        #endregion

        #region UDPReceiver(IPAddress, Port, Mapper, ThreadName = "...", ThreadPrio = AboveNormal, IsBackground = true, Autostart = false)

        /// <summary>
        /// Create a new UDP receiver listening on the given IP address and port.
        /// </summary>
        /// <param name="IPAddress">The IP address to listen.</param>
        /// <param name="Port">The port to listen.</param>
        /// <param name="Mapper">A delegate to transform the incoming UDP packets into a custom data structure.</param>
        /// <param name="ThreadName">The optional name of the UDP receiver thread.</param>
        /// <param name="ThreadPrio">The optional priority of the UDP receiver thread.</param>
        /// <param name="IsBackground">Whether the UDP receiver thread is a background thread or not.</param>
        /// <param name="Autostart">Start the UDP receiver thread immediately.</param>
        public UDPReceiver(IIPAddress      IPAddress,
                           IPPort          Port,
                           MapperDelegate  Mapper,
                           String          ThreadName    = "UDPReceiver thread",
                           ThreadPriority  ThreadPrio    = ThreadPriority.AboveNormal,
                           Boolean         IsBackground  = true,
                           Boolean         Autostart     = false)

        {

            if (Mapper == null)
                throw new ArgumentNullException("The mapper delegate must not be null!");

            this._IPAddress               = IPAddress;
            this._Port                    = Port;
            this.Mapper                   = Mapper;
            this.BufferSize               = BufferSize;

            this.LocalIPEndPoint          = new IPEndPoint(new System.Net.IPAddress(_IPAddress.GetBytes()), _Port.ToInt32());
            this.LocalSocket              = new IPSocket(LocalIPEndPoint);
            this.LocalDotNetSocket        = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.LocalDotNetSocket.Bind(LocalIPEndPoint);
            this.CancellationTokenSource  = new CancellationTokenSource();
            this.CancellationToken        = CancellationTokenSource.Token;

            // Timeout will throw an exception which is a little bit stupid!
            ReceiveTimeout                = 5000;

            this._ThreadName              = ThreadName;
            this._ThreadPrio              = ThreadPrio;
            this._IsBackground            = IsBackground;
            this.BufferSize               = 65536;

            if (Autostart)
                Start();

        }

        #endregion

        #region UDPReceiver(IPSocket, Mapper, ThreadName = "...", ThreadPrio = AboveNormal, IsBackground = true, Autostart = false)

        /// <summary>
        /// Create a new UDP receiver listening on the given IP socket.
        /// </summary>
        /// <param name="IPAddress">The IP address to listen.</param>
        /// <param name="Port">The port to listen.</param>
        /// <param name="Mapper">A delegate to transform the incoming UDP packets into a custom data structure.</param>
        /// <param name="ThreadName">The optional name of the UDP receiver thread.</param>
        /// <param name="ThreadPrio">The optional priority of the UDP receiver thread.</param>
        /// <param name="IsBackground">Whether the UDP receiver thread is a background thread or not.</param>
        /// <param name="Autostart">Start the UDP receiver thread immediately.</param>
        public UDPReceiver(IPSocket        IPSocket,
                           MapperDelegate  Mapper,
                           String          ThreadName    = "UDPReceiver thread",
                           ThreadPriority  ThreadPrio    = ThreadPriority.AboveNormal,
                           Boolean         IsBackground  = true,
                           Boolean         Autostart     = false)

            : this(IPSocket.IPAddress, IPSocket.Port, Mapper, ThreadName, ThreadPrio, IsBackground, Autostart)

        { }

        #endregion

        #endregion


        #region Start()

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public void Start()
        {

            if (_IsRunning == 1)
                return;

            try
            {

                this.ListenerTask = Task.Factory.StartNew(() =>
                {

                    Thread.CurrentThread.Name          = ThreadName;
                    Thread.CurrentThread.Priority      = ThreadPrio;
                    Thread.CurrentThread.IsBackground  = IsBackground;

                    EndPoint RemoteEndPoint = null;
                    Byte[]   UDPPacket;
                    Int32    NumberOfReceivedBytes;
                    DateTime Timestamp;
                    Int32    WaitForChildTaskCreation = 0;

                    Interlocked.Exchange(ref _IsRunning, 1);

                    #region ReceiverLoop

                    while (!CancellationToken.IsCancellationRequested)
                    {

                        UDPPacket       = new Byte[this.BufferSize];
                        RemoteEndPoint  = new IPEndPoint(0, 0);

                        try
                        {

                            // Wait for the next packet...
                            // Will throw an exception every ReceiveTimeout when no packet was received!
                            NumberOfReceivedBytes  = LocalDotNetSocket.ReceiveFrom(UDPPacket, ref RemoteEndPoint);

                            if (CancellationToken.IsCancellationRequested)
                                break;

                            Timestamp              = DateTime.Now;

                            if (NumberOfReceivedBytes > 0)
                            {

                                Interlocked.Exchange(ref WaitForChildTaskCreation, 1);

                                #region Inner task

                                Task.Factory.StartNew(() =>
                                {

                                    var RemoteSocketLocal = new IPSocket((IPEndPoint) RemoteEndPoint);
                                    Thread.CurrentThread.Name = "UDPPacket from " + RemoteSocketLocal.IPAddress + ":" + RemoteSocketLocal.Port;

                                    // Create a local copy of the UDPPacket and RemoteEndPoint as we
                                    // do not want to wait till the new thread has accepted the packet

                                    Array.Resize(ref UDPPacket, NumberOfReceivedBytes);

                                    var TimestampLocal                  = Timestamp;
                                    var UDPPacketLocal                  = UDPPacket;
                                    var OnNotificationLocal             = OnNotification_UDPPacket;
                                    var OnNotification_Message_Local    = OnNotification_Message;
                                    var OnNotification_UDPPacket_Local  = OnNotification_UDPPacket;

                                    Interlocked.Exchange(ref WaitForChildTaskCreation, 0);

                                    // Start upper-layer protocol processing
                                    if (OnNotification_Message_Local != null)
                                        OnNotification_Message_Local(Mapper(TimestampLocal,
                                                                            this.LocalSocket,
                                                                            RemoteSocketLocal,
                                                                            UDPPacketLocal));

                                    if (OnNotification_UDPPacket_Local != null)
                                        OnNotification_UDPPacket_Local(new UDPPacket<TOut>(
                                                                           TimestampLocal,
                                                                           this.LocalSocket,
                                                                           RemoteSocketLocal,
                                                                           Mapper(TimestampLocal,
                                                                                  this.LocalSocket,
                                                                                  RemoteSocketLocal,
                                                                                  UDPPacketLocal)
                                                                      ));



                                }, CancellationTokenSource.Token,
                                   TaskCreationOptions.AttachedToParent,
                                   TaskScheduler.Default);

                                #endregion

                                // Wait till the new Task had used some of its time to
                                // make a copy of the given references.
                                while (WaitForChildTaskCreation > 0)
                                    Thread.Sleep(1);

                            }

                        }
                        catch (SocketException e)
                        { }
                        catch (Exception e)
                        {
                            var OnErrorLocal = OnError;
                            if (OnErrorLocal != null)
                                OnErrorLocal(this, e);
                        }

                    }

                    #endregion

                    Interlocked.Exchange(ref _IsRunning, 0);

                }, CancellationTokenSource.Token,
                   TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent,
                   TaskScheduler.Default);

            }
            catch (Exception ex)
            {
                var OnErrorLocal = OnError;
                if (OnErrorLocal != null)
                    OnErrorLocal(this, ex);
            }

        }

        #endregion

        #region Shutdown(Wait = true)

        /// <summary>
        /// Shutdown the UDP listener.
        /// </summary>
        /// <param name="Wait">Wait until the server finally shutted down.</param>
        public void Shutdown(Boolean Wait = true)
        {

            if (ListenerTask == null)
                throw new Exception("You can not stop the listener if it wasn't started before!");

            this.CancellationTokenSource.Cancel();

            if (Wait)
                while (_IsRunning > 0)
                    Thread.Sleep(10);

        }

        #endregion


        #region IDisposable Members

        public void Dispose()
        {

            //StopAndWait();

            //if (_TCPListener != null)
            //    _TCPListener.Stop();

        }

        #endregion

        #region ToString()

        public override String ToString()
        {

            var _TypeName    = this.GetType().Name;
            var _GenericType = this.GetType().GetGenericArguments()[0].Name;

            var _Running = "";
            if (IsRunning) _Running = " (running)";

            return String.Concat(_TypeName.Remove(_TypeName.Length - 2), "<", _GenericType, "> ", _IPAddress.ToString(), ":", _Port, _Running);

        }

        #endregion

    }

    #endregion

    #region UDPReceiver

    public class UDPReceiver : UDPReceiver<Byte[]>
    {

        #region UDPReceiver(Port, Mapper = null, ThreadName = "...", ThreadPrio = AboveNormal, IsBackground = true, Autostart = false)

        /// <summary>
        /// Create a new UDP receiver using IPAddress.Any and the given parameters.
        /// </summary>
        /// <param name="Port">The port to listen.</param>
        /// <param name="Mapper">An optional delegate to transform the incoming UDP packets into a custom data structure.</param>
        /// <param name="ThreadName">The optional name of the UDP receiver thread.</param>
        /// <param name="ThreadPrio">The optional priority of the UDP receiver thread.</param>
        /// <param name="IsBackground">Whether the UDP receiver thread is a background thread or not.</param>
        /// <param name="Autostart">Start the UDP receiver thread immediately.</param>
        public UDPReceiver(IPPort          Port,
                           MapperDelegate  Mapper        = null,
                           String          ThreadName    = "UDPReceiver thread",
                           ThreadPriority  ThreadPrio    = ThreadPriority.AboveNormal,
                           Boolean         IsBackground  = true,
                           Boolean         Autostart     = false)

            : base(Port,
                   (Mapper == null) ? (Timestamp, LocalSocket, RemoteSocket, Message) => Message : Mapper,
                   ThreadName,
                   ThreadPrio,
                   IsBackground,
                   Autostart)

        { }

        #endregion

        #region UDPReceiver(IPAddress, Port, Mapper = null, ThreadName = "...", ThreadPrio = AboveNormal, IsBackground = true, Autostart = false)

        /// <summary>
        /// Create a new UDP receiver listening on the given IP address and port.
        /// </summary>
        /// <param name="IPAddress">The IP address to listen.</param>
        /// <param name="Port">The port to listen.</param>
        /// <param name="Mapper">An optional delegate to transform the incoming UDP packets into a custom data structure.</param>
        /// <param name="ThreadName">The optional name of the UDP receiver thread.</param>
        /// <param name="ThreadPrio">The optional priority of the UDP receiver thread.</param>
        /// <param name="IsBackground">Whether the UDP receiver thread is a background thread or not.</param>
        /// <param name="Autostart">Start the UDP receiver thread immediately.</param>
        public UDPReceiver(IIPAddress      IPAddress,
                           IPPort          Port,
                           MapperDelegate  Mapper        = null,
                           String          ThreadName    = "UDPReceiver thread",
                           ThreadPriority  ThreadPrio    = ThreadPriority.AboveNormal,
                           Boolean         IsBackground  = true,
                           Boolean         Autostart     = false)

            : base(IPAddress,
                   Port,
                   (Mapper == null) ? (Timestamp, LocalSocket, RemoteSocket, Message) => Message : Mapper,
                   ThreadName,
                   ThreadPrio,
                   IsBackground,
                   Autostart)

        { }

        #endregion

        #region UDPReceiver(IPSocket, Mapper = null, ThreadName = "...", ThreadPrio = AboveNormal, IsBackground = true, Autostart = false)

        /// <summary>
        /// Create a new UDP receiver listening on the given IP socket.
        /// </summary>
        /// <param name="IPAddress">The IP address to listen.</param>
        /// <param name="Port">The port to listen.</param>
        /// <param name="Mapper">An optional delegate to transform the incoming UDP packets into a custom data structure.</param>
        /// <param name="ThreadName">The optional name of the UDP receiver thread.</param>
        /// <param name="ThreadPrio">The optional priority of the UDP receiver thread.</param>
        /// <param name="IsBackground">Whether the UDP receiver thread is a background thread or not.</param>
        /// <param name="Autostart">Start the UDP receiver thread immediately.</param>
        public UDPReceiver(IPSocket        IPSocket,
                           MapperDelegate  Mapper        = null,
                           String          ThreadName    = "UDPReceiver thread",
                           ThreadPriority  ThreadPrio    = ThreadPriority.AboveNormal,
                           Boolean         IsBackground  = true,
                           Boolean         Autostart     = false)

            : base(IPSocket,
                   (Mapper == null) ? (Timestamp, LocalSocket, RemoteSocket, Message) => Message : Mapper,
                   ThreadName,
                   ThreadPrio,
                   IsBackground,
                   Autostart)

        { }

        #endregion

    }

    #endregion

}

