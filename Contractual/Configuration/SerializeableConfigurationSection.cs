using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml;

namespace Contractual.Configuration
{
    /// <summary>
    /// Allows a custom config section to be stored in an alternate loction.
    /// </summary>
    public abstract class SerializeableConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// The section name that encloses the custom configuration section within the web or app config.
        /// </summary>
        public abstract string ConfigSectionName { get; }


        /// <summary>
        /// Write the current config-section state into the format of a web or app config section.
        /// </summary>
        /// <param name="name">A non-default section name.  Leave as null to use the default name.</param>
        /// <param name="mode"></param>
        /// <returns>XML string</returns>
        public string SerializeToXml(string name = null, ConfigurationSaveMode mode = ConfigurationSaveMode.Full)
        {
            return (this.SerializeSection(null, name ?? ConfigSectionName, mode));
        }

        /// <summary>
        /// Write the current config-section state into the format of a web or app config section.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public string SerializeToXml(ConfigurationSaveMode mode)
        {
            return (this.SerializeSection(null, ConfigSectionName, mode));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        public void DeserializeFromXml(string content)
        {
            using (XmlTextReader reader = new XmlTextReader(new System.IO.StringReader(content)))
            {
                this.DeserializeSection(reader);
            };
        }
    }
}
