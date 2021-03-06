﻿using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace Contractual.WCF
{
    /// <summary>
    /// This provides additional IDisposable and dependency-injection capabilities to the client produced by svcutil.exe.  
    /// More precisely, it uses either the interface generated by svcutil.exe or it can use the server-side contract interface itself (if that interface is in its own assembly).
    /// </summary>
    /// <remarks>
    /// Given a service called IAccountService, the "AccountServiceClient" proxy generated by svcutil.exe has two problems.  First, it cannot easily be mocked.  Second, 
    /// it is not compatable with a dependency injection (DI) paradigm.   The standard proxy, an IDisposable, generally must be disposed after each invocation.  Once disposed,
    /// the only way to obtain another instance in a DI environment is to use an abstract factory.  This in turn causes a code explosion as every service would need to have
    /// abstract factory scaffolding written (or interception code would need to be applied everywhere).  To remove the need to write dozens of abstract factories, and to make
    /// interception not necessary...this generic Proxy class ensures that a ClientBase&lt;T&gt; is created and disposed with each and every invocation.  In other words, it
    /// is a self-contained factory.  Furthermore, because its methods are virtual - it is easy to replace with test classes.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class Proxy<T> where T : class
    {
        protected string endpoint;
        protected Binding binding;
        protected EndpointAddress address;
        protected bool removeCustomBehaviors;
        protected FakeClient<T> Fake;

        /// <summary>
        /// Create a Proxy based on configuration.
        /// </summary>
        /// <param name="endpointConfigurationName">If there is more than one endpoint, supply an endpoint name to pick which configuration entry to read.</param>
        public Proxy() : this("*")
        {
            //The "*" is undocumented.  See here: http://stackoverflow.com/questions/426041/why-doesnt-my-channelfactory-see-my-endpoint-configuration
        }

        /// <summary>
        /// Convenience constructor for stubbing a service with a fake.
        /// </summary>
        /// <param name="fake">A stub used to facilitate testing.</param>
        public Proxy(T fake)
        {
            Fake = new FakeClient<T>(fake);
        }


        /// <summary>
        /// Convenience constructor for stubbing a service with a fake.
        /// </summary>
        /// <param name="fake">A stub used to facilitate testing.</param>
        /// <param name="endpoint">A fake, for rare cases it may be needed.</param>
        /// <param name="creds">A fake, for rare cases it may be needed.</param>
        /// <param name="channel">A fake, for rare cases it may be needed.</param>
        public Proxy(T fake, ServiceEndpoint endpoint, ClientCredentials creds, IClientChannel channel)
        {
            Fake = new FakeClient<T>(fake, endpoint, creds, channel);
        }

        /// <summary>
        /// Create a Proxy based on configuration.
        /// </summary>
        /// <param name="endpointConfigurationName">If there is more than one endpoint, supply an endpoint name to pick which configuration entry to read.</param>
        public Proxy(string endpointConfigurationName)
        {
            endpoint = endpointConfigurationName;
        }

        /// <summary>
        /// Create a Proxy based on explicit endpoint information.  This constructor is primarily intended for self-hosted unit test scenarios.
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="address"></param>
        /// <param name="removeCustomContractBehaviors"></param>
        public Proxy(Binding binding, EndpointAddress address, bool removeCustomContractBehaviors = false)
        {
            this.removeCustomBehaviors = removeCustomContractBehaviors;
            this.binding = binding;
            this.address = address;
        }

        /// <summary>
        /// Creates a proxy, makes a call, and closes the proxy.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="invoke">Delegate that invokes the proxy.</param>
        /// <returns></returns>
        public virtual TResult Using<TResult>(Func<T, TResult> invoke)
        {
            TResult result = default(TResult);

            using (var proxy = CreateClient())
            {
                result = proxy.Call(invoke);
            }
            return result;
        }

        /// <summary>
        /// Creates a proxy, makes a call, and closes the proxy.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="invoke">Delegate that invokes the proxy.</param>
        public virtual void Using(Action<T> invoke)
        {
            using (var proxy = CreateClient())
            {
                proxy.Call(invoke);
            }
        }

        /// <summary>
        /// This wraps the invocation of WCF proxy BeginXXX and EndXXX asynchronous methods.  It ensures that CloseOrAbort() is called for you when
        /// either the callback is fired or an exception occurs.
        /// </summary>
        /// <typeparam name="TResult">The value returned from the service call.</typeparam>
        /// <param name="invoke">A function that calls Task.Factory.FromAsync() or similar mechanism to produce an async WCF proxy invocation.</param>
        /// <returns></returns>
        public virtual Task<TResult> Invoke<TResult>(Func<T, Task<TResult>> invoke)
        {
            IClient<T> proxy = CreateClient();

            Task<TResult> response = proxy.Invoke(invoke);
            var callback = response.ContinueWith(result =>
            {
                proxy.Close();
                return result;
            }
            );

            return callback.Unwrap();
        }

        /// <summary>
        /// This wraps the invocation of WCF proxy BeginXXX and EndXXX asynchronous methods.  It ensures that CloseOrAbort() is called for you when
        /// either the callback is fired or an exception occurs.
        /// </summary>
        /// <typeparam name="TResult">The value returned from the service call.</typeparam>
        /// <param name="invoke">A function that calls Task.Factory.FromAsync() or similar mechanism to produce an async WCF proxy invocation.</param>
        /// <returns>A Task to be used with "ContinueWith" operations.</returns>
        public virtual Task Invoke(Func<T, Task> invoke)
        {
            IClient<T> proxy = CreateClient();

            Task response = proxy.Invoke(invoke);
            var callback = response.ContinueWith(result =>
            {
                proxy.Close();
                return result;
            }
            );

            return callback.Unwrap();
        }

        /// <summary>
        /// Provides a Client&lt;T&gt; proxy that will open and remain open until explicitly disposed.
        /// </summary>
        /// <returns></returns>
        public virtual IClient<T> CreateClient()
        {
            IClient<T> result = Fake ?? NewClient();
            ConfigureClient(result);

            return result;
        }

        protected virtual IClient<T> NewClient()
        {
            IClient<T> result = null;
            if (!String.IsNullOrEmpty(endpoint))
            {
                result = new Client<T>(endpoint);
            }
            else
            {
                result = new Client<T>(binding, address);
            }
            return result;
        }

        protected virtual void ConfigureClient(IClient<T> client)
        {
            if (removeCustomBehaviors)
            {
                //If this Proxy<T> is pointing at a service contract with a server-side-only behavior attribute, then it won't work.
                //In that case, it is necessary to strip off the custom contract behavior(s).
                var customBehaviors = client.Endpoint.Contract.Behaviors.Where(b => !b.GetType().Assembly.GlobalAssemblyCache).ToArray();
                foreach (var behavior in customBehaviors)
                {
                    client.Endpoint.Contract.Behaviors.Remove(behavior);
                }
            }
        }
    }

}
