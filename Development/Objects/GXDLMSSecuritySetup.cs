//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux products: https://www.gurux.org
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects.Enums;
using Gurux.DLMS.Internal;
using Gurux.DLMS.Secure;
#if !WINDOWS_UWP
using System.Security.Cryptography;
#endif

namespace Gurux.DLMS.Objects
{
    /// <summary>
    /// Online help:
    /// https://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSSecuritySetup
    /// </summary>
    public class GXDLMSSecuritySetup : GXDLMSObject, IGXDLMSBase
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSSecuritySetup()
        : this("0.0.43.0.0.255")
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ln">Logical Name of the object.</param>
        public GXDLMSSecuritySetup(string ln)
        : this(ln, 0)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ln">Logical Name of the object.</param>
        /// <param name="sn">Short Name of the object.</param>
        public GXDLMSSecuritySetup(string ln, ushort sn)
        : base(ObjectType.SecuritySetup, ln, sn)
        {
            Certificates = new List<GXDLMSCertificateInfo>();
            Version = 1;
        }

        /// <summary>
        /// Security policy.
        /// </summary>
        [XmlIgnore()]
        public SecurityPolicy SecurityPolicy
        {
            get;
            set;
        }

        /// <summary>
        /// Security policy.
        /// </summary>
        [XmlIgnore()]
        public SecurityPolicy0 SecurityPolicy0
        {
            get;
            set;
        }

        /// <summary>
        /// Security suite.
        /// </summary>
        [XmlIgnore()]
        public SecuritySuite SecuritySuite
        {
            get;
            set;
        }

        /// <summary>
        /// Client system title.
        /// </summary>
        [XmlIgnore()]
        public byte[] ClientSystemTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Server system title.
        /// </summary>
        [XmlIgnore()]
        public byte[] ServerSystemTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Available certificates.
        /// </summary>
        [XmlIgnore()]
        public List<GXDLMSCertificateInfo> Certificates
        {
            get;
            set;
        }

        /// <inheritdoc cref="GXDLMSObject.GetValues"/>
        public override object[] GetValues()
        {
            if (Version == 0)
            {
                return new object[] { LogicalName, SecurityPolicy0, SecuritySuite,
                              ClientSystemTitle, ServerSystemTitle, Certificates};
            }
            return new object[] { LogicalName, SecurityPolicy, SecuritySuite,
                              ClientSystemTitle, ServerSystemTitle, Certificates};
        }

        /// <summary>
        /// Get security enum as integer value.
        /// </summary>
        /// <param name="security">Security level.</param>
        /// <returns>Integer value of security level.</returns>
        private static int GetSecurityValue(Gurux.DLMS.Enums.Security security)
        {
            int value;
            switch (security)
            {
                case Gurux.DLMS.Enums.Security.None:
                    value = 0;
                    break;
                case Gurux.DLMS.Enums.Security.Authentication:
                    value = 1;
                    break;
                case Gurux.DLMS.Enums.Security.Encryption:
                    value = 2;
                    break;
                case Gurux.DLMS.Enums.Security.AuthenticationEncryption:
                    value = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid Security enum.");
            }
            return value;
        }

        /// <summary>
        /// Activates and strengthens the security policy.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="security">New security level.</param>
        /// <returns>Generated action.</returns>
        public byte[][] Activate(GXDLMSClient client, Gurux.DLMS.Enums.Security security)
        {
            return client.Method(this, 1, GetSecurityValue(security), DataType.Enum);
        }

        /// <summary>
        /// Updates one or more global keys.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="kek">Master key, also known as Key Encrypting Key.</param>
        /// <param name="list">List of Global key types and keys.</param>
        /// <returns>Generated action.</returns>
        public byte[][] GlobalKeyTransfer(GXDLMSClient client, byte[] kek, List<KeyValuePair<GlobalKeyType, byte[]>> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("Invalid list. It is empty.");
            }
            GXByteBuffer bb = new GXByteBuffer();
            bb.SetUInt8(DataType.Array);
            bb.SetUInt8((byte)list.Count);
            byte[] tmp;
            foreach (KeyValuePair<GlobalKeyType, byte[]> it in list)
            {
                bb.SetUInt8(DataType.Structure);
                bb.SetUInt8(2);
                GXCommon.SetData(client.Settings, bb, DataType.Enum, it.Key);
                tmp = GXDLMSSecureClient.Encrypt(kek, it.Value);
                GXCommon.SetData(client.Settings, bb, DataType.OctetString, tmp);
            }
            return client.Method(this, 2, bb.Array(), DataType.Array);
        }

        /// <summary>
        /// Agree on one or more symmetric keys using the key agreement algorithm.
        /// </summary>
        /// <param name="client"> DLMS client that is used to generate action.</param>
        /// <param name="list"> List of keys.</param>
        /// <returns>Generated action.</returns>
        public byte[][] keyAgreement(GXDLMSClient client, List<KeyValuePair<GlobalKeyType, byte[]>> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("Invalid list. It is empty.");
            }
            GXByteBuffer bb = new GXByteBuffer();
            bb.SetUInt8(DataType.Array);
            bb.SetUInt8((byte)list.Count);
            foreach (KeyValuePair<GlobalKeyType, byte[]> it in list)
            {
                bb.SetUInt8(DataType.Structure);
                bb.SetUInt8(2);
                GXCommon.SetData(client.Settings, bb, DataType.Enum, it.Key);
                GXCommon.SetData(client.Settings, bb, DataType.OctetString, it.Value);
            }
            return client.Method(this, 3, bb.Array(), DataType.Array);
        }

        /// <summary>
        ///  Generates an asymmetric key pair as required by the security suite.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="type">New certificate type.</param>
        /// <returns>Generated action.</returns>
        public byte[][] GenerateKeyPair(GXDLMSClient client, CertificateType type)
        {
            return client.Method(this, 4, type, DataType.Enum);
        }

        /// <summary>
        ///  Ask Server sends the Certificate Signing Request (CSR) data.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="type">identifies the key pair for which the certificate will be requested.</param>
        /// <returns>Generated action.</returns>
        public byte[][] GenerateCertificate(GXDLMSClient client, CertificateType type)
        {
            return client.Method(this, 5, type, DataType.Enum);
        }

#if !__MOBILE__ && !WINDOWS_UWP && !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETSTANDARD2_0 && !NETCOREAPP3_0 && !NETCOREAPP3_1
        /// <summary>
        ///  Imports an X.509 v3 certificate of a public key.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="key">Public key.</param>
        /// <returns>Generated action.</returns>
        public byte[][] Import(GXDLMSClient client, CngKey key)
        {
            return ImportCertificate(client, key.Export(CngKeyBlobFormat.EccPublicBlob));
        }
#endif

        /// <summary>
        ///  Imports an X.509 v3 certificate of a public key.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="key">Public key.</param>
        /// <returns>Generated action.</returns>
        public byte[][] ImportCertificate(GXDLMSClient client, byte[] key)
        {
            return client.Method(this, 6, key, DataType.OctetString);
        }

        /// <summary>
        /// Exports an X.509 v3 certificate from the server using entity information.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="entity">Certificate entity.</param>
        /// <param name="type">Certificate type.</param>
        /// <param name="systemTitle">System title.</param>
        /// <returns>Generated action.</returns>
        public byte[][] ExportCertificateByEntity(GXDLMSClient client, CertificateEntity entity, CertificateType type, byte[] systemTitle)
        {
            GXByteBuffer bb = new GXByteBuffer();
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(2);
            //Add enum
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(0);
            //Add certificate_identification_by_entity
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(3);
            //Add certificate_entity
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(entity);
            //Add certificate_type
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(type);
            //system_title
            GXCommon.SetData(client.Settings, bb, DataType.OctetString, systemTitle);
            return client.Method(this, 7, bb.Array(), DataType.Structure);
        }

        /// <summary>
        /// Exports an X.509 v3 certificate from the server using serial information.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="serialNumber">Serial number.</param>
        /// <param name="issuer">Issuer</param>
        /// <returns>Generated action.</returns>
        public byte[][] ExportCertificateBySerial(GXDLMSClient client, byte[] serialNumber, byte[] issuer)
        {
            GXByteBuffer bb = new GXByteBuffer();
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(2);
            //Add enum
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(1);
            //Add certificate_identification_by_entity
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(2);
            //serialNumber
            GXCommon.SetData(client.Settings, bb, DataType.OctetString, serialNumber);
            //issuer
            GXCommon.SetData(client.Settings, bb, DataType.OctetString, issuer);
            return client.Method(this, 7, bb.Array(), DataType.Structure);
        }

        /// <summary>
        /// Removes X.509 v3 certificate from the server using entity.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="entity">Certificate entity type.</param>
        /// <param name="type">Certificate type.</param>
        /// <param name="systemTitle">System title.</param>
        /// <returns>Generated action.</returns>
        public byte[][] RemoveCertificateByEntity(GXDLMSClient client, CertificateEntity entity, CertificateType type, byte[] systemTitle)
        {
            GXByteBuffer bb = new GXByteBuffer();
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(2);
            //Add enum
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(0);
            //Add certificate_identification_by_entity
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(3);
            //Add certificate_entity
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(entity);
            //Add certificate_type
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(type);
            //system_title
            GXCommon.SetData(client.Settings, bb, DataType.OctetString, systemTitle);
            return client.Method(this, 8, bb.Array(), DataType.Structure);
        }

        /// <summary>
        /// Removes X.509 v3 certificate from the server using serial number.
        /// </summary>
        /// <param name="client">DLMS client that is used to generate action.</param>
        /// <param name="serialNumber">Serial number.</param>
        /// <param name="issuer">Issuer.</param>
        /// <returns>Generated action.</returns>
        public byte[][] RemoveCertificateBySerial(GXDLMSClient client, byte[] serialNumber, byte[] issuer)
        {
            GXByteBuffer bb = new GXByteBuffer();
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(2);
            //Add enum
            bb.SetUInt8(DataType.Enum);
            bb.SetUInt8(1);
            //Add certificate_identification_by_entity
            bb.SetUInt8(DataType.Structure);
            bb.SetUInt8(2);
            //serialNumber
            GXCommon.SetData(client.Settings, bb, DataType.OctetString, serialNumber);
            //issuer
            GXCommon.SetData(client.Settings, bb, DataType.OctetString, issuer);
            return client.Method(this, 8, bb.Array(), DataType.Structure);
        }

        #region IGXDLMSBase Members

        byte[] IGXDLMSBase.Invoke(GXDLMSSettings settings, ValueEventArgs e)
        {
            if (e.Index == 1)
            {
                if (Version == 0)
                {
                    SecurityPolicy0 = (SecurityPolicy0)e.Parameters;
                }
                else
                {
                    SecurityPolicy = (SecurityPolicy)e.Parameters;
                }
            }
            else if (e.Index == 2)
            {
                try
                {
                    foreach (List<object> item in e.Parameters as List<object>)
                    {
                        GlobalKeyType type = (GlobalKeyType)Convert.ToInt32(item[0]);
                        byte[] data = (byte[])item[1];
                        //if settings.Cipher is null non secure server is used.
                        //Keys are take in action after reply is generated.
                        switch (type)
                        {
                            case GlobalKeyType.UnicastEncryption:
                                GXDLMSSecureClient.Decrypt(settings.Kek, data);
                                break;
                            case GlobalKeyType.BroadcastEncryption:
                                //Invalid type
                                e.Error = ErrorCode.ReadWriteDenied;
                                break;
                            case GlobalKeyType.Authentication:
                                GXDLMSSecureClient.Decrypt(settings.Kek, data);
                                break;
                            case GlobalKeyType.Kek:
                                GXDLMSSecureClient.Decrypt(settings.Kek, data);
                                break;
                            default:
                                //Invalid type
                                e.Error = ErrorCode.ReadWriteDenied;
                                break;
                        }
                    }
                }
                catch(Exception)
                {
                    e.Error = ErrorCode.ReadWriteDenied;
                }
            }
            else
            {
                e.Error = ErrorCode.ReadWriteDenied;
            }
            //Return standard reply.
            return null;
        }

        /// <summary>
        /// Start to use new keys after reply is generated.
        /// </summary>
        /// <param name="settings">DLMS settings.</param>
        /// <param name="e"></param>
        internal void ApplyKeys(GXDLMSSettings settings, ValueEventArgs e)
        {
            try
            {
                foreach (List<object> item in e.Parameters as List<object>)
                {
                    GlobalKeyType type = (GlobalKeyType)Convert.ToInt32(item[0]);
                    byte[] data = (byte[])item[1];
                    switch (type)
                    {
                        case GlobalKeyType.UnicastEncryption:
                            settings.Cipher.BlockCipherKey = GXDLMSSecureClient.Decrypt(settings.Kek, data);
                            break;
                        case GlobalKeyType.BroadcastEncryption:
                            //Invalid type
                            e.Error = ErrorCode.ReadWriteDenied;
                            break;
                        case GlobalKeyType.Authentication:
                            //if settings.Cipher is null non secure server is used.
                            settings.Cipher.AuthenticationKey = GXDLMSSecureClient.Decrypt(settings.Kek, data);
                            break;
                        case GlobalKeyType.Kek:
                            settings.Kek = GXDLMSSecureClient.Decrypt(settings.Kek, data);
                            break;
                        default:
                            //Invalid type
                            e.Error = ErrorCode.ReadWriteDenied;
                            break;
                    }
                }
            }
            catch (Exception)
            {
                e.Error = ErrorCode.ReadWriteDenied;
            }
        }

        int[] IGXDLMSBase.GetAttributeIndexToRead(bool all)
        {
            List<int> attributes = new List<int>();
            //LN is static and read only once.
            if (all || string.IsNullOrEmpty(LogicalName))
            {
                attributes.Add(1);
            }
            //SecurityPolicy
            if (all || CanRead(2))
            {
                attributes.Add(2);
            }
            //SecuritySuite
            if (all || CanRead(3))
            {
                attributes.Add(3);
            }
            //ClientSystemTitle
            if (all || CanRead(4))
            {
                attributes.Add(4);
            }
            //ServerSystemTitle
            if (all || CanRead(5))
            {
                attributes.Add(5);
            }
            if (this.Version != 0)
            {
                //Certificates
                if (all || CanRead(6))
                {
                    attributes.Add(6);
                }
            }
            return attributes.ToArray();
        }

        /// <inheritdoc cref="IGXDLMSBase.GetNames"/>
        string[] IGXDLMSBase.GetNames()
        {
            if (this.Version == 0)
            {
                return new string[] { Internal.GXCommon.GetLogicalNameString(), "Security Policy",
                                  "Security Suite", "Client System Title", "Server System Title"
                                };
            }
            return new string[] { Internal.GXCommon.GetLogicalNameString(), "Security Policy",
                              "Security Suite", "Client System Title", "Server System Title" , "Certificates"
                            };
        }

        int IGXDLMSBase.GetAttributeCount()
        {
            if (Version == 0)
            {
                return 5;
            }
            return 6;
        }

        int IGXDLMSBase.GetMethodCount()
        {
            if (Version == 0)
            {
                return 2;
            }
            return 8;
        }

        /// <inheritdoc cref="IGXDLMSBase.GetDataType"/>
        public override DataType GetDataType(int index)
        {
            if (index == 1)
            {
                return DataType.OctetString;
            }
            if (index == 2)
            {
                return DataType.Enum;
            }
            if (index == 3)
            {
                return DataType.Enum;
            }
            if (index == 4)
            {
                return DataType.OctetString;
            }
            if (index == 5)
            {
                return DataType.OctetString;
            }
            if (this.Version > 0)
            {
                if (index == 6)
                {
                    return DataType.Array;
                }
                else
                {
                    throw new ArgumentException("GetDataType failed. Invalid attribute index.");
                }
            }
            else
            {
                throw new ArgumentException("GetDataType failed. Invalid attribute index.");
            }
        }

        /// <summary>
        /// Get sertificates as byte buffer.
        /// </summary>
        /// <returns></returns>
        private byte[] GetSertificates()
        {
            GXByteBuffer bb = new GXByteBuffer();
            bb.SetUInt8((byte)DataType.Array);
            GXCommon.SetObjectCount(Certificates.Count, bb);
            foreach (GXDLMSCertificateInfo it in Certificates)
            {
                bb.SetUInt8((byte)DataType.Structure);
                GXCommon.SetObjectCount(6, bb);
                bb.SetUInt8((byte)DataType.Enum);
                bb.SetUInt8((byte)it.Entity);
                bb.SetUInt8((byte)DataType.Enum);
                bb.SetUInt8((byte)it.Type);
                GXCommon.AddString(it.SerialNumber, bb);
                GXCommon.AddString(it.Issuer, bb);
                GXCommon.AddString(it.Subject, bb);
                GXCommon.AddString(it.SubjectAltName, bb);
            }
            return bb.Array();
        }

        object IGXDLMSBase.GetValue(GXDLMSSettings settings, ValueEventArgs e)
        {
            if (e.Index == 1)
            {
                return GXCommon.LogicalNameToBytes(LogicalName);
            }
            if (e.Index == 2)
            {
                if (Version == 0)
                {
                    return SecurityPolicy0;
                }
                return SecurityPolicy;
            }
            if (e.Index == 3)
            {
                return SecuritySuite;
            }
            if (e.Index == 4)
            {
                return ClientSystemTitle;
            }
            if (e.Index == 5)
            {
                return ServerSystemTitle;
            }
            if (this.Version > 0)
            {
                if (e.Index == 6)
                {
                    return GetSertificates();
                }
            }
            e.Error = ErrorCode.ReadWriteDenied;
            return null;
        }

        private void UpdateSertificates(IEnumerable<object> list)
        {
            Certificates.Clear();
            if (list != null)
            {
                foreach (object tmp in list)
                {
                    List<object> it;
                    if (tmp is List<object>)
                    {
                        it = (List<object>)tmp;
                    }
                    else
                    {
                        it = new List<object>((object[])tmp);
                    }
                    GXDLMSCertificateInfo info = new GXDLMSCertificateInfo();
                    info.Entity = (CertificateEntity)Convert.ToInt32(it[0]);
                    info.Type = (CertificateType)Convert.ToInt32(it[1]);
                    info.SerialNumber = ASCIIEncoding.ASCII.GetString((byte[])it[2]);
                    info.Issuer = ASCIIEncoding.ASCII.GetString((byte[])it[3]);
                    info.Subject = ASCIIEncoding.ASCII.GetString((byte[])it[4]);
                    info.SubjectAltName = ASCIIEncoding.ASCII.GetString((byte[])it[5]);
                    Certificates.Add(info);
                }
            }
        }

        void IGXDLMSBase.SetValue(GXDLMSSettings settings, ValueEventArgs e)
        {
            if (e.Index == 1)
            {
                LogicalName = GXCommon.ToLogicalName(e.Value);
            }
            else if (e.Index == 2)
            {
                if (Version == 0)
                {
                    SecurityPolicy0 = (SecurityPolicy0)Convert.ToInt32(e.Value);
                }
                else
                {
                    SecurityPolicy = (SecurityPolicy)Convert.ToInt32(e.Value);
                }
            }
            else if (e.Index == 3)
            {
                SecuritySuite = (SecuritySuite)Convert.ToInt32(e.Value);
            }
            else if (e.Index == 4)
            {
                ClientSystemTitle = (byte[])e.Value;
            }
            else if (e.Index == 5)
            {
                ServerSystemTitle = (byte[])e.Value;
            }
            else if (e.Index == 6)
            {
                UpdateSertificates((IEnumerable<object>)e.Value);
            }
            else
            {
                e.Error = ErrorCode.ReadWriteDenied;
            }
        }

        void IGXDLMSBase.Load(GXXmlReader reader)
        {
            SecurityPolicy = (SecurityPolicy)reader.ReadElementContentAsInt("SecurityPolicy");
            SecurityPolicy0 = (SecurityPolicy0)reader.ReadElementContentAsInt("SecurityPolicy0");
            SecuritySuite = (SecuritySuite)reader.ReadElementContentAsInt("SecuritySuite");
            string str = reader.ReadElementContentAsString("ClientSystemTitle");
            if (str == null)
            {
                ClientSystemTitle = null;
            }
            else
            {
                ClientSystemTitle = GXDLMSTranslator.HexToBytes(str);
            }
            str = reader.ReadElementContentAsString("ServerSystemTitle");
            if (str == null)
            {
                ServerSystemTitle = null;
            }
            else
            {
                ServerSystemTitle = GXDLMSTranslator.HexToBytes(str);
            }
            Certificates.Clear();
            if (reader.IsStartElement("Certificates", true))
            {
                while (reader.IsStartElement("Item", true))
                {
                    GXDLMSCertificateInfo it = new GXDLMSCertificateInfo();
                    Certificates.Add(it);
                    it.Entity = (CertificateEntity)reader.ReadElementContentAsInt("Entity");
                    it.Type = (CertificateType)reader.ReadElementContentAsInt("Type");
                    it.SerialNumber = reader.ReadElementContentAsString("SerialNumber");
                    it.Issuer = reader.ReadElementContentAsString("Issuer");
                    it.Subject = reader.ReadElementContentAsString("Subject");
                    it.SubjectAltName = reader.ReadElementContentAsString("SubjectAltName");
                }
                reader.ReadEndElement("Certificates");
            }
        }

        void IGXDLMSBase.Save(GXXmlWriter writer)
        {
            writer.WriteElementString("SecurityPolicy", (int)SecurityPolicy);
            writer.WriteElementString("SecurityPolicy0", (int)SecurityPolicy0);
            writer.WriteElementString("SecuritySuite", (int)SecuritySuite);
            writer.WriteElementString("ClientSystemTitle", GXDLMSTranslator.ToHex(ClientSystemTitle));
            writer.WriteElementString("ServerSystemTitle", GXDLMSTranslator.ToHex(ServerSystemTitle));
            if (Certificates != null)
            {
                writer.WriteStartElement("Certificates");
                foreach (GXDLMSCertificateInfo it in Certificates)
                {
                    writer.WriteStartElement("Item");
                    writer.WriteElementString("Entity", (int)it.Entity);
                    writer.WriteElementString("Type", (int)it.Type);
                    writer.WriteElementString("SerialNumber", it.SerialNumber);
                    writer.WriteElementString("Issuer", it.Issuer);
                    writer.WriteElementString("Subject", it.Subject);
                    writer.WriteElementString("SubjectAltName", it.SubjectAltName);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        void IGXDLMSBase.PostLoad(GXXmlReader reader)
        {
        }

        #endregion
    }
}