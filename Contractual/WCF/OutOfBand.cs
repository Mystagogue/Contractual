using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Configuration;
using System.Configuration;
using System.ComponentModel;
using Contractual.Configuration;

namespace Contractual.WCF
{
    public class OutOfBand : Attribute, IContractBehavior, IEndpointBehavior, IServiceBehavior
    {
        internal const string ErrorHeading = "WCF behavior class 'OutOfBand' error";
        internal const string ErrorNoHeaderContext = "{0}: Out-of-band parameter '{1}' not found.";
        internal const string ErrorBadParameter = "{0}: Delegate for 'Context' type must be of type Func<string,Context>.";

        private List<HeaderInfo> contexts;
        private Type context;
        private bool requireContext = true;
        private Delegate getContext;
        private string name;
        private string nameSpace;
        private int order;

        internal OutOfBand(List<HeaderInfo> contexts)
        {
            this.contexts = contexts;
        }

        /// <summary>
        /// Capture and send out-of-band data.  By default, this data is transacted with System.Remoting.CallContext.
        /// </summary>
        /// <param name="context">The DataContract or serializeable type which will be captured and sent out-of-band</param>
        /// <param name="requireContext">Throw an exception if context is missing</param>
        /// <param name="getContext">A Func&lt;string,T&gt; optionally used by client/proxy code to obtain context. T must match first param type.</param>
        public OutOfBand(Type context, bool requireContext, int order = -1)
            : this(context, null)
        {
            this.requireContext = requireContext;
            this.order = order;
        }

        //I simply want the third parameter to default to null, but there is a known bug with the C# 4.0 compiler regarding attributes
        //http://stackoverflow.com/questions/3436848/default-value-for-attribute-constructor
        public OutOfBand(Type context, bool requireContext, Delegate getContext)
            : this(context, getContext)
        {
            this.requireContext = requireContext;
        }

        /// <summary>
        /// Capture and send out-of-band data.  By default, this data is transacted with System.Remoting.CallContext.
        /// </summary>
        /// <param name="context">The DataContract or serializeable type which will be captured and sent out-of-band</param>
        /// <param name="getContext">A Func&lt;string,T&gt; optionally used by client/proxy code to obtain context. T must match first param type.</param>
        public OutOfBand(Type context, Delegate getContext)
            : this(context)
        {
            if (getContext != null)
            {
                var funcType = getContext.GetType();
                Type[] args = funcType.GetGenericArguments();

                //The value-type handled by the delegate must match the 'context' type passed.
                if (args.Length != 2 || args[1] != context)
                {
                    var msg = String.Format(ErrorBadParameter, ErrorHeading);
                    throw new ArgumentException(msg);
                }

                this.getContext = getContext;
            }
        }

        /// <summary>
        /// Capture and send out-of-band data.  By default, this data is transacted with System.Remoting.CallContext.
        /// </summary>
        /// <param name="context">The DataContract or serializeable type which will be captured and sent out-of-band</param>
        public OutOfBand(Type context)
        {
            string namePair = ConfigurationManager.AppSettings["outOfBand:" + context.Name];
            if (!String.IsNullOrWhiteSpace(namePair))
            {
                string[] names = namePair.Split('/');
                nameSpace = names[0];
                name = names[1];
            }
            this.context = context;
        }

        private IClientMessageInspector CreateGenericInspector(bool required = true, Type type = null, string headerKey = null)
        {
            Type[] types = new Type[] { type ?? context };
            Type contextExtensionType = typeof(OutOfBandClient<>);
            Type genericType = contextExtensionType.MakeGenericType(types);

            Object[] args = new Object[] { type == null ? requireContext : required, getContext };

            var extension = (IClientMessageInspector)Activator.CreateInstance(genericType, args);
            AssignHeaderKey(extension, headerKey);
            return extension;
        }

        private void AssignHeaderKey(dynamic outOfBand, string headerKey)
        {
            string itemName = null;
            string itemNameSpace = null;
            if (!String.IsNullOrWhiteSpace(headerKey))
            {
                string[] names = headerKey.Split('/');
                itemNameSpace = names[0];
                itemName = names[1];
            }

            outOfBand.HeaderNamespace = nameSpace ?? itemNameSpace ?? outOfBand.HeaderNamespace;
            outOfBand.HeaderName = name ?? itemName ?? outOfBand.HeaderName;
        }

        private ICallContextInitializer CreateGenericInitializer(bool required = true, Type type = null, string headerKey = null)
        {
            //The following syntax is, unfortunately, not legal:
            //var inspector = new OutOfBandService<this.context>();

            //So instead, creating the generic extension will require reflection.
            Type[] types = new Type[] { type ?? context };
            Type contextExtensionType = typeof(OutOfBandService<>);
            Type genericType = contextExtensionType.MakeGenericType(types);

            Object[] args = new Object[] { type == null ? requireContext : required, null };

            var initializer = (ICallContextInitializer)Activator.CreateInstance(genericType, args);
            AssignHeaderKey(initializer, headerKey);
            return initializer;
        }

        #region IContractBehavior
        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            IClientMessageInspector inspector = CreateGenericInspector();
            clientRuntime.MessageInspectors.Add(inspector);
        }

        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            ICallContextInitializer initializer = CreateGenericInitializer();

            foreach (var operation in dispatchRuntime.Operations)
            {
                if (order > -1)
                {
                    operation.CallContextInitializers.Insert(order, initializer);
                }
                else
                {
                    operation.CallContextInitializers.Add(initializer);
                }
            }
        }

        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }
        #endregion

        #region IEndpointBehavior

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            IClientMessageInspector inspector = null;
            if (context != null)
            {
                inspector = CreateGenericInspector();
                clientRuntime.MessageInspectors.Add(inspector);
            }
            else
            {
                foreach (var header in contexts)
                {
                    inspector = CreateGenericInspector(header.RequireContext, header.HeaderType, header.Headername);
                    clientRuntime.MessageInspectors.Add(inspector);
                }
            }
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            //throw new NotImplementedException();
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
        #endregion

        #region IServiceBehavior
        public void AddBindingParameters(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        /// <summary>
        /// Allows the config file to specify an OutOfBand DataContract for a give service.
        /// </summary>
        /// <remarks>
        /// It is better to explicitly place an OutOfband(typeof(...)) attribute on any ServiceContract that will be using an OutOfBand parameter
        /// so that the requirements of the contract are entirely imperative.  However, a config-based approach can be used to upgrade legacy code
        /// without touching it or allow services to act as an OutOfBand pass-through without knowing it.
        /// </remarks>
        /// <param name="serviceDescription"></param>
        /// <param name="serviceHostBase"></param>
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase)
        {
            ICallContextInitializer initializer = context == null ? null : CreateGenericInitializer();

            foreach (var endpoint in serviceHostBase.ChannelDispatchers.SelectMany(cd => ((ChannelDispatcher)cd).Endpoints).Where(e => !e.IsSystemEndpoint))
            {
                foreach (var operation in endpoint.DispatchRuntime.Operations)
                {
                    if (context != null)
                    {
                        if (order > -1)
                        {
                            operation.CallContextInitializers.Insert(order, initializer);
                        }
                        else
                        {
                            operation.CallContextInitializers.Add(initializer);
                        }
                    }
                    else
                    {
                        foreach (var header in contexts)
                        {
                            initializer = CreateGenericInitializer(header.RequireContext, header.HeaderType, header.Headername);
                            operation.CallContextInitializers.Add(initializer);
                        }
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase)
        {
        }
        #endregion
    }

    public class OutOfBandExtension : BehaviorExtensionElement
    {
        const string configSection = "outOfBand";
        public override Type BehaviorType
        {
            get
            {
                return typeof(OutOfBand);
            }
        }

        protected override object CreateBehavior()
        {
            List<HeaderInfo> contextMap = new List<HeaderInfo>();

            var masterConfig = ConfigurationManager.GetSection(configSection) as CallContextCollection;
            IEnumerable<CallContextCollectionType> contextList = null;
            if (masterConfig != null)
            {
                contextList = masterConfig.CallContexts.Cast<CallContextCollectionType>(); // as IEnumerable<CallContextType>;
            }
            string type = null;
            bool requireContext = true;
            string headerName = null;


            if (HeaderTypes.Count > 0)
            {
                foreach (var context in HeaderTypes)
                {
                    if (String.IsNullOrWhiteSpace(context.Alias))
                    {
                        type = context.HeaderType;
                        bool.TryParse(context.RequireContext, out requireContext);
                        headerName = context.HeaderName;
                    }
                    else
                    {
                        var contextDir = contextList.First(c => c.Alias == context.Alias);
                        type = contextDir.HeaderType;
                        bool.TryParse(contextDir.RequireContext, out requireContext);
                        headerName = contextDir.HeaderName;
                    }
                    Type headerType = Type.GetType(type, true, false);
                    contextMap.Add(new HeaderInfo(headerType, requireContext, headerName));
                }
            }
            else
            {
                foreach (var context in contextList)
                {
                    var contextDir = contextList.First(c => c.Alias == context.Alias);
                    type = contextDir.HeaderType;
                    bool.TryParse(contextDir.RequireContext, out requireContext);

                    Type headerType = Type.GetType(type, true, false);
                    contextMap.Add(new HeaderInfo(headerType, requireContext, contextDir.HeaderName));
                }
            }

            return new OutOfBand(contextMap);
        }

        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(CallContextType), AddItemName = "add",
            CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public ConfigurationElementCollection<CallContextType> HeaderTypes
        {
            get { return base[""] as ConfigurationElementCollection<CallContextType>; }
        }
    }

    public class CallContextCollection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(CallContextCollectionType), AddItemName = "add",
            CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public ConfigurationElementCollection<CallContextCollectionType> CallContexts
        {
            get { return base[""] as ConfigurationElementCollection<CallContextCollectionType>; }
        }
    }

    /// <summary>
    /// Identical to 'CallContextType' in every way, except this has the HeaderType required.
    /// </summary>
    public class CallContextCollectionType : ConfigurationElement
    {
        [ConfigurationProperty("type", IsKey = true, IsRequired = true)]
        public virtual string HeaderType
        {
            get { return (string)base["type"]; }
            set { base["type"] = value; }
        }

        [ConfigurationProperty("alias", IsKey = true, IsRequired = false)]
        public string Alias
        {
            get { return (string)base["alias"]; }
            set { base["alias"] = value; }
        }

        [ConfigurationProperty("requireContext", IsRequired = false)]
        public string RequireContext
        {
            get { return this["requireContext"] as string; }
            set { this["requireContext"] = value; }
        }

        [ConfigurationProperty("headerName", IsKey = false, IsRequired = false)]
        public virtual string HeaderName
        {
            get
            {
                string name = (string)base["headerName"];
                return String.IsNullOrWhiteSpace(name) ? null : name;
            }
            set { base["headerName"] = value; }
        }
    }

    /// <summary>
    /// Exists to override the 'IsRequired' config setting for HeaderType.
    /// </summary>
    public class CallContextType : CallContextCollectionType
    {
        [ConfigurationProperty("type", IsKey = true, IsRequired = false)]
        public override string HeaderType
        {
            get { return (string)base["type"]; }
            set { base["type"] = value; }
        }
    }

    internal class HeaderInfo
    {
        public HeaderInfo(Type header, bool required, string headerKey = null)
        {
            HeaderType = header;
            RequireContext = required;
            Headername = headerKey;
        }

        public Type HeaderType { get; set; }

        public bool RequireContext { get; set; }

        public string Headername { get; set; }
    }
}
