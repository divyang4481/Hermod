﻿/*
 * Copyright (c) 2010-2011, Achim 'ahzf' Friedland <code@ahzf.de>
 * This file is part of Hermod
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
using System.Net;
using System.Net.Sockets;

using de.ahzf.Hermod.HTTP.Common;
using de.ahzf.Hermod.Sockets.TCP;

#endregion

namespace de.ahzf.Hermod.HTTP
{

    /// <summary>
    /// The interface between the HTTP connection and an
    /// upper-layer application.
    /// </summary>
    public interface IHTTPConnection : ITCPConnection
    {

        HTTPRequestHeader   RequestHeader   { get; }
        //ToDo: Change this to a Stream!
        Byte[]              RequestBody     { get; }
        
        String              ServerName      { get; }
        HTTPResponseHeader  ResponseHeader  { get; }
        NetworkStream       ResponseStream  { get; }

        String              ErrorReason     { get; set; }
        Exception           LastException   { get; set; }
        
        void SendErrorpage(HTTPStatusCode    myHTTPStatusCode,
                           HTTPRequestHeader myRequestHeader,
                           Byte[]            myRequestBody,
                           String            ErrorReason   = null,
                           Exception         LastException = null);

    }

}