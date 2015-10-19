using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Diagnostics.CodeAnalysis;

namespace Contractual.Configuration
{
    /// <summary>
    /// Eliminates the need to make a seperate collection class definition for every collection in a custom config section.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <remarks>In the example, assume a ConfigurationElement called "ServiceElement".  Assume the xml tag that adds each element is "Service".
    /// </remarks>
    /// <example>
    /// [ConfigurationProperty("", IsDefaultCollection = true)]
    /// [ConfigurationCollection(typeof(ServiceElement), AddItemName = "Service",
    ///    CollectionType = ConfigurationElementCollectionType.BasicMap)]
    /// public ConfigurationElementCollection<ServiceElement> ServiceList
    /// {
    ///     get
    ///     {
    ///         return base[""] as ConfigurationElementCollection<ServiceElement>;
    ///     }
    /// }
    /// </example>
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Justification = "Must be ConfigurationElementCollection for the config file.")]
    public class ConfigurationElementCollection<TElement> : ConfigurationElementCollection, IEnumerable<TElement> where TElement : ConfigurationElement, new()
    {
        #region Overriden Methods
        /// <summary>
        /// Creates a new instance of the child element.
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new TElement();
        }
        /// <summary>
        /// Gets the child element key using meta-data.  
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            object key = null;

            var keys = element.ElementInformation.Properties.Cast<PropertyInformation>().Where(p => p.IsKey).Select(p => p.Value.ToString());
            key = String.Join(":", keys);

            if (String.IsNullOrWhiteSpace(key as string))
            {
                string msg = String.Format("The ConfigurationCollection element {0} must define a key.", element.ElementInformation.Type.FullName);
                throw new InvalidOperationException(msg);
            }

            return key;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a configuration element to the System.Configuration.ConfigurationElementCollection.
        /// </summary>
        /// <param name="path"></param>
        public virtual void Add(TElement element)
        {
            BaseAdd(element);
        }
        /// <summary>
        /// Removes a System.Configuration.ConfigurationElement from the collection.
        /// </summary>
        /// <param name="key"></param>
        public virtual void Remove(TElement element)
        {
            if (BaseIndexOf(element) >= 0)
            {
                object key = GetElementKey(element);
                BaseRemove(key);
            }
        }
        /// <summary>
        /// Gets the child element by the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new TElement this[string name]
        {
            get { return base.BaseGet(name) as TElement; }
        }
        /// <summary>
        /// Gets the child element by the index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TElement this[int index]
        {
            get
            {
                return (TElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }
        #endregion

        #region IEnumerable<TElement>
        /// <summary>
        /// Walks the elements in the collection.
        /// </summary>
        /// <returns></returns>
        public new IEnumerator<TElement> GetEnumerator()
        {
            int j = base.Count;
            for (int i = 0; i < j; ++i)
            {
                yield return this[i];
            }
        }
        #endregion
    }

    //See the following SO post to understand why this interface exists.
    //http://stackoverflow.com/questions/32514439/invalid-cast-of-type-constrained-c-sharp-generic
    internal interface IConfigurationElementCollection<TParentedConfig, TParent>
        where TParentedConfig : ConfigurationElement<TParent>
        where TParent : class
    {
        TParent ParentElement { get; }
    }


    /// <summary>
    /// Eliminates the need to make a seperate collection class definition for every collection in a custom config section.
    /// Allows items in the collection to have a reference to the parent config element or config section.
    /// </summary>
    /// <typeparam name="TElement">A subclass of ConfigurationElement&lt;TParent&gt;.</typeparam>
    /// <typeparam name="TParent">The parent type that the children have access to.</typeparam>
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Justification = "Must be ConfigurationElementCollection for the config file.")]
    public class ConfigurationElementCollection<TElement, TParent> : ConfigurationElementCollection<TElement>, IConfigurationElementCollection<ConfigurationElement<TParent>, TParent>
        where TElement : ConfigurationElement<TParent>, new()
        where TParent : class
    {
        public TParent ParentElement { get; set; }

        /// <summary>
        /// Creates a new instance of the child element.
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new TElement { ParentCollection = this };
        }
    }

    //The custom configuration tools the BCL provides does not have the ability to allow a child
    //element to access its parent element.  That is unfortunate, because sometimes it is useful
    //to have the parent element define some kind of "default" value that all the children obtain.
    //The children cannot obtain that default info, unless they can access the parent. 
    //So to solve this problem, this generic class...along with the ConfigurationElementCollection<TElement, TParent>
    //class, were devised to provide a generic way to make this work.
    public class ConfigurationElement<TParent> : ConfigurationElement where TParent : class
    {
        internal IConfigurationElementCollection<ConfigurationElement<TParent>, TParent> ParentCollection { get; set; }

        protected TParent Parent
        {
            get
            {
                return ParentCollection != null ? ParentCollection.ParentElement : null;
            }
        }
    }
}