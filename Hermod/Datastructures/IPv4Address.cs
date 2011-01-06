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
using System.Text;

#endregion

namespace de.ahzf.Hermod.Datastructures
{

    /// <summary>
    /// A IPv4 address.
    /// </summary>    
    public class IPv4Address : IComparable, IComparable<IPv4Address>, IEquatable<IPv4Address>, IIPAddress
    {

        #region Data

        private readonly Byte[] _IPAddressArray;
        private const    Byte   _Length = 4;

        #endregion

        #region Properties

        #region Length

        public Byte Length
        {
            get
            {
                return _Length;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        #region IPAddress(myIPAddress)

        /// <summary>
        /// Generates a new IPAddress.
        /// </summary>
        public IPv4Address(System.Net.IPAddress myIPAddress)
            : this(myIPAddress.GetAddressBytes())
        { }

        #endregion

        #region IPAddress(myByteArray)

        /// <summary>
        /// Generates a new IPAddress.
        /// </summary>
        public IPv4Address(Byte[] myByteArray)
        {

            _IPAddressArray = new Byte[_Length];

            Array.Copy(myByteArray, _IPAddressArray, Math.Max(myByteArray.Length, _Length));

        }

        #endregion

        #region IPAddress(myAddressString)

        /// <summary>
        /// Generates a new IPAddress.
        /// </summary>
        public IPv4Address(String myAddressString)
        {
            _IPAddressArray = Encoding.UTF8.GetBytes(myAddressString);
        }

        #endregion

        #endregion


        public static IPv4Address Any
        {
            get
            {
                return new IPv4Address(new Byte[_Length]);
            }
        }


        public Byte[] GetBytes()
        {
            return new Byte[_Length];
        }


        #region Operator overloading

        #region Operator == (myIPv4Address1, myIPv4Address2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="myIPv4Address1">A IPv4Address.</param>
        /// <param name="myIPv4Address2">Another IPv4Address.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (IPv4Address myIPv4Address1, IPv4Address myIPv4Address2)
        {

            // If both are null, or both are same instance, return true.
            if (Object.ReferenceEquals(myIPv4Address1, myIPv4Address2))
                return true;

            // If one is null, but not both, return false.
            if (((Object) myIPv4Address1 == null) || ((Object) myIPv4Address2 == null))
                return false;

            return myIPv4Address1.Equals(myIPv4Address2);

        }

        #endregion

        #region Operator != (myIPv4Address1, myIPv4Address2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="myIPv4Address1">A IPv4Address.</param>
        /// <param name="myIPv4Address2">Another IPv4Address.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (IPv4Address myIPv4Address1, IPv4Address myIPv4Address2)
        {
            return !(myIPv4Address1 == myIPv4Address2);
        }

        #endregion

        #endregion


        #region IComparable Members

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="myObject">An object to compare with.</param>
        /// <returns>true|false</returns>
        public Int32 CompareTo(Object myObject)
        {

            // Check if myObject is null
            if (myObject == null)
                throw new ArgumentNullException("myObject must not be null!");

            // Check if myObject can be casted to an ElementId object
            var myIPAddress = myObject as IPv4Address;
            if ((Object) myIPAddress == null)
                throw new ArgumentException("myObject is not of type IPAddress!");

            return CompareTo(myIPAddress);

        }

        #endregion

        #region IComparable<IPAddress> Members

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="myElementId">An object to compare with.</param>
        /// <returns>true|false</returns>
        public Int32 CompareTo(IPv4Address myIPAddress)
        {

            // Check if myIPAddress is null
            if (myIPAddress == null)
                throw new ArgumentNullException("myElementId must not be null!");

            //return _IPAddress.GetAddressBytes() .CompareTo(myIPAddress._IPAddress);
            return 0;

        }

        #endregion

        #region IEquatable<IPAddress> Members

        #region Equals(myObject)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="myObject">An object to compare with.</param>
        /// <returns>true|false</returns>
        public override Boolean Equals(Object myObject)
        {

            // Check if myObject is null
            if (myObject == null)
                throw new ArgumentNullException("Parameter myObject must not be null!");

            // Check if myObject can be cast to IPAddress
            var myIPAddress = myObject as IPv4Address;
            if ((Object) myIPAddress == null)
                throw new ArgumentException("Parameter myObject could not be casted to type IPAddress!");

            return this.Equals(myIPAddress);

        }

        #endregion

        #region Equals(myIPv4Address)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="myElementId">An object to compare with.</param>
        /// <returns>true|false</returns>
        public Boolean Equals(IPv4Address myIPv4Address)
        {

            // Check if myIPAddress is null
            if ((Object) myIPv4Address == null)
                throw new ArgumentNullException("Parameter myIPv4Address must not be null!");

            // If both are null, or both are same instance, return true.
            if (Object.ReferenceEquals(this, myIPv4Address))
                return true;

            var __IPAddress = _IPAddressArray.Equals(myIPv4Address._IPAddressArray);

            return false;

        }

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            return _IPAddressArray.GetHashCode();
        }

        #endregion

        #region ToString()

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public override String ToString()
        {
            return String.Format("{0}.{1}.{2}.{3}", _IPAddressArray[0], _IPAddressArray[1], _IPAddressArray[2], _IPAddressArray[3]);
        }

        #endregion

    }

}
