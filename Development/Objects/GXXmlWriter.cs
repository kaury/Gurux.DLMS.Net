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

using Gurux.DLMS.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Gurux.DLMS.Objects
{
    /// <summary>
    /// Save COSEM object to the file.
    /// </summary>
    public class GXXmlWriter : IDisposable
    {
        XmlWriter writer = null;
        Stream stream = null;
        bool skipDefaults;

        public void Dispose()
        {
            if (writer != null)
            {
#if !WINDOWS_UWP
                writer.Close();
#else
                writer.Dispose();
#endif
                writer = null;
            }
            if (stream != null)
            {
#if !WINDOWS_UWP
                stream.Close();
#endif
                stream.Dispose();
                stream = null;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">File name.</param>
        /// <param name="filename">Skip default values.</param>
        public GXXmlWriter(string filename, bool skipDefaultValues)
        {
            skipDefaults = skipDefaultValues;
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = false
            };
            if (File.Exists(filename))
            {
                stream = File.Open(filename, FileMode.Truncate);
            }
            else
            {
                stream = File.Open(filename, FileMode.CreateNew);
            }
            writer = XmlWriter.Create(stream, settings);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="s">Stream.</param>
        public GXXmlWriter(Stream s)
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = false
            };
            writer = XmlWriter.Create(s, settings);
        }

        public void WriteStartDocument()
        {
            writer.WriteStartDocument();
        }

        public void WriteStartElement(string name)
        {
            writer.WriteStartElement(name);
        }
        public void WriteAttributeString(string name, string value)
        {
            writer.WriteAttributeString(name, value);
        }

        public void WriteElementString(string name, UInt64 value)
        {
            if (!skipDefaults || value != 0)
            {
                writer.WriteElementString(name, value.ToString());
            }
        }

        public void WriteElementString(string name, double value)
        {
            WriteElementString(name, value, 0);
        }

        public void WriteElementString(string name, double value, double defaultValue)
        {
            if (!skipDefaults || value != defaultValue)
            {
                writer.WriteElementString(name, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void WriteElementString(string name, int value)
        {
            if (!skipDefaults || value != 0)
            {
                writer.WriteElementString(name, value.ToString());
            }
        }

        public void WriteElementString(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                int eof = value.IndexOf('\0');
                if (eof != -1)
                {
                    value = value.Substring(0, eof);
                }
                writer.WriteElementString(name, value);
            }
            else if (!skipDefaults)
            {
                writer.WriteElementString(name, "");
            }
        }

        public void WriteElementString(string name, bool value)
        {
            if (!skipDefaults || value)
            {
                writer.WriteElementString(name, value ? "1" : "0");
            }
        }

        public void WriteElementString(string name, GXDateTime value)
        {
            if (value != null && value != DateTime.MinValue)
            {
                writer.WriteElementString(name, value.ToFormatString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (!skipDefaults)
            {
                writer.WriteElementString(name, "");
            }
        }

        void WriteArray(object data)
        {
            if (data is List<object>)
            {
                foreach (object tmp in (List<object>)data)
                {
                    if (tmp is byte[])
                    {
                        WriteElementObject("Item", tmp);
                    }
                    else if (tmp is GXArray)
                    {
                        writer.WriteStartElement("Item");
                        writer.WriteAttributeString("Type", ((int)DataType.Array).ToString());
                        WriteArray(tmp);
                        writer.WriteEndElement();
                    }
                    else if (tmp is GXStructure)
                    {
                        writer.WriteStartElement("Item");
                        writer.WriteAttributeString("Type", ((int)DataType.Structure).ToString());
                        WriteArray(tmp);
                        writer.WriteEndElement();
                    }
                    else if (tmp is Enum)
                    {
                        WriteElementObject("Item", Convert.ToInt32(tmp));
                    }
                    else
                    {
                        WriteElementObject("Item", tmp);
                    }
                }
            }
        }

        public void WriteElementObject(string name, object value)
        {
            if (value != null || !skipDefaults)
            {
                DataType dt = GXDLMSConverter.GetDLMSDataType(value);
                WriteElementObject(name, value, dt, DataType.None);
            }
        }

        /// <summary>
        /// Write object value to file.
        /// </summary>
        /// <param name="name">Object name.</param>
        /// <param name="value">Object value.</param>
        /// <param name="skipDefaultValue">Is default value serialized.</param>
        public void WriteElementObject(string name, object value, DataType dt, DataType uiType)
        {
            if (value != null)
            {
                if (skipDefaults && value is DateTime && ((DateTime)value == DateTime.MinValue || (DateTime)value == DateTime.MaxValue))
                {
                    return;
                }

                writer.WriteStartElement(name);
                writer.WriteAttributeString("Type", ((int)dt).ToString());
                if (uiType != DataType.None && dt != uiType && (uiType != DataType.String || dt == DataType.OctetString))
                {
                    writer.WriteAttributeString("UIType", ((int)uiType).ToString());
                }
                else if (value is float || value is double)
                {
                    if (value is double)
                    {
                        writer.WriteAttributeString("UIType", ((int)DataType.Float64).ToString());
                    }
                    else
                    {
                        writer.WriteAttributeString("UIType", ((int)DataType.Float32).ToString());
                    }
                }
                if (dt == DataType.Array || dt == DataType.Structure)
                {
                    WriteArray(value);
                }
                else
                {
                    if (value is GXDateTime)
                    {
                        writer.WriteString(((GXDateTime)value).ToFormatString(CultureInfo.InvariantCulture));
                    }
                    else if (value is DateTime)
                    {
                        writer.WriteString(((DateTime)value).ToString(CultureInfo.InvariantCulture));
                    }
                    else if (value is byte[])
                    {
                        writer.WriteString(GXDLMSTranslator.ToHex((byte[])value));
                    }
                    else
                    {
                        writer.WriteString(Convert.ToString(value, CultureInfo.InvariantCulture));
                    }
                }
                writer.WriteEndElement();
            }
            else if (!skipDefaults)
            {
                writer.WriteStartElement(name);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Write End Element tag.
        /// </summary>
        public void WriteEndElement()
        {
            writer.WriteEndElement();
        }

        /// <summary>
        /// Write End document tag.
        /// </summary>
        public void WriteEndDocument()
        {
            writer.WriteEndDocument();
        }

        public void Flush()
        {
            writer.Flush();
        }
    }
}
