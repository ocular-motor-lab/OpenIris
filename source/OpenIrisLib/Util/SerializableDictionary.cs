//-----------------------------------------------------------------------
// <copyright file="SerializableDictionary.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley.
//     Licensed under the GNU Affero General Public License v3.0. See LICENSE in the repository root.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
#nullable enable

    using System;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Xml.Schema;
    using System.Collections.Generic;
    

    /// <summary>
    /// This is a dictionary that can be serialized into xml. Necessary for saving the settings of
    /// the plugins.
    /// </summary>
    /// <typeparam name="TValue">Value of the dictionary entry.</typeparam>
    [XmlRoot("dictionary")]

    public class EyeTrackerSettingsDictionary<TValue>
        : Dictionary<string, TValue>, IXmlSerializable
    {
        /// <summary>
        /// Initializes
        /// </summary>
        public EyeTrackerSettingsDictionary()
        {

        }

        #region IXmlSerializable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return new XmlSchema();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        public void ReadXml(XmlReader reader)
        {
            if (reader is null) return;

            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue), EyeTrackerPluginManager.ExtraSettingsTypesForXML?.ToArray());

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");

                reader.ReadStartElement("key");
                string key = (string)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                reader.MoveToContent();

                // Read the value on its own, so that one that can't be read (the settings of a plugin
                // that is no longer installed) is skipped instead of failing the whole file. Closing
                // the subtree leaves the reader on the value's last node.
                try
                {
                    using var valueReader = reader.ReadSubtree();
                    this.Add(key, (TValue)valueSerializer.Deserialize(valueReader));
                }
                catch (InvalidOperationException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"WARNING: skipping the saved settings of '{key}', probably a plugin that is no longer installed. {ex.InnerException?.Message ?? ex.Message}");
                }

                reader.Read();
                reader.MoveToContent();
                reader.ReadEndElement();

                reader.ReadEndElement();
                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlWriter writer)
        {
            if (writer is null) return;

            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue), EyeTrackerPluginManager.ExtraSettingsTypesForXML?.ToArray());

            foreach (string key in this.Keys)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        #endregion IXmlSerializable Members

        /// <summary>
        /// Necessary for serialization.
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected EyeTrackerSettingsDictionary(SerializationInfo serializationInfo, StreamingContext streamingContext)
            :base(serializationInfo, streamingContext)
        {
        }

    }
}
