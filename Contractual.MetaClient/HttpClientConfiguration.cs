using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contractual.Configuration;

namespace Contractual.MetaClient
{
    public class RestResourcesConfiguration : SerializeableConfigurationSection
    {
        public const string SectionName = "restResources";

        public override string ConfigSectionName { get { return SectionName; } }

        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(Host), AddItemName = "host")]
        public ConfigurationElementCollection<Host, RestResourcesConfiguration> Hosts
        {
            get
            {
                var collection = (ConfigurationElementCollection<Host, RestResourcesConfiguration>)base[""];
                collection.ParentElement = this;
                return collection;
            }
            //set { base[""] = value; }
        }

        [ConfigurationProperty("defaultAddress")]
        public string DefaultAddress
        {
            get { return (string)base["defaultAddress"]; }
            set { base["defaultAddress"] = value; }
        }

        [ConfigurationProperty("disableClientCertificates")]
        public bool DisableClientCertificates
        {
            get { return (bool)base["disableClientCertificates"]; }
            set { base["disableClientCertificates"] = value; }
        }
    }

    public class Host : ConfigurationElement<RestResourcesConfiguration>
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("address", IsKey = true, IsRequired = true)]
        public string Address
        {
            get { return (string)base["address"]; }
            set { base["address"] = value; }
        }

        [ConfigurationProperty("certificate", IsKey = true, IsRequired = false)]
        public string Certificate
        {
            get
            {
                return Parent.DisableClientCertificates ? "" : (string)base["certificate"];
            }
            set { base["certificate"] = value; }
        }

        [ConfigurationProperty("flags", IsRequired = false)]
        public string Flags
        {
            get { return (string)base["flags"]; }
            set { base["flags"] = value; }
        }

        [ConfigurationProperty("jsonFormatter", IsRequired = false)]
        public string JsonFormatter
        {
            get { return (string)base["jsonFormatter"]; }
            set { base["jsonFormatter"] = value; }
        }

        /// <summary>
        /// A convenience flag tester used by DelegatingHandlers.
        /// </summary>
        /// <param name="flag">A keyword recognized by a DelegatingHandler</param>
        /// <returns></returns>
        public bool HasFlag(string flag)
        {
            return !String.IsNullOrWhiteSpace(Flags) && Flags.Split(',').Any(f => f == flag);
        }

        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(Resource), AddItemName = "resource")]
        public ConfigurationElementCollection<Resource> Mappings
        {
            get { return base[""] as ConfigurationElementCollection<Resource>; }
            //set { base[""] = value; }
        }

        public override bool IsReadOnly()
        {
            return false;
        }
    }

    public class Resource : ConfigurationElement
    {
        [ConfigurationProperty("map", IsKey = true, IsRequired = true)]
        public string Map
        {
            get { return (string)base["map"]; }
            //set { base["serialNum"] = value; }
        }
    }


}
