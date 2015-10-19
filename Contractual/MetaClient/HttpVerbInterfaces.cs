using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Contractual.MetaClient
{
    /// <summary>
    /// Designates a parameter that can be inspected by the HttpClient DelegatingHandler pipeline.
    /// </summary>
    public interface IHandlerParam { }

    /// <summary>
    /// Allows HTTP verbs to be simply aggregated or tested for compliance.
    /// </summary>
    public interface IVerb { }

    /// <summary>
    /// Allows Suave GET access.
    /// </summary>
    public interface IGet : IVerb
    {
        string Uri { get; }
    }
    /// <summary>
    /// Allows Suave GET access.
    /// </summary>
    /// <typeparam name="T">The custom type returned on success.</typeparam>
    public interface IGet<T> : IVerb
    {
        string Uri { get; }
    }
    /// <summary>
    /// Allows Suave GET access
    /// </summary>
    /// <typeparam name="T">The custom type returned on success. Use type 'Empty' if this is not needed.</typeparam>
    /// <typeparam name="M">Custom metadata returned for alternate flows or error reporting.</typeparam>
    public interface IGet<T, M> : IVerb
    {
        string Uri { get; }
    }

    //There is no IDelete<T> interface because, presumably,
    //no info-object is ever returned when DELETE is successful.

    /// <summary>
    /// Allows Suave DELETE access.
    /// </summary>
    public interface IDelete : IVerb
    {
        string Uri { get; }
    }
    /// <summary>
    /// Allows Suave DELETE access.
    /// </summary>
    /// <typeparam name="M">Custom metadata returned for alternate flows or error reporting.</typeparam>
    public interface IDelete<M> : IVerb
    {
        string Uri { get; }
    }

    /// <summary>
    /// Allows Suave POST access.
    /// </summary>
    public interface IPost : IVerb
    {
        string Uri { get; }
    }
    /// <summary>
    /// Allows Suave POST access.
    /// </summary>
    /// <typeparam name="T">The custom type returned on success.</typeparam>
    public interface IPost<T> : IVerb
    {
        string Uri { get; }
    }
    /// <summary>
    /// Allows Suave POST access.
    /// </summary>
    /// <typeparam name="T">The custom type returned on success. Use type 'Empty' if this is not needed.</typeparam>
    /// <typeparam name="M">Custom metadata returned for alternate flows or error reporting.</typeparam>
    public interface IPost<T, M> : IVerb
    {
        string Uri { get; }
    }

    /// <summary>
    /// Allows Suave PUT access.
    /// </summary>
    public interface IPut : IVerb
    {
        string Uri { get; }
    }
    /// <summary>
    /// Allows Suave PUT access.
    /// </summary>
    /// <typeparam name="T">The custom type returned on success.</typeparam>
    public interface IPut<T> : IVerb
    {
        string Uri { get; }
    }
    /// <summary>
    /// Allows Suave PUT access.
    /// </summary>
    /// <typeparam name="T">The custom type returned on success. Use type 'Empty' if this is not needed.</typeparam>
    /// <typeparam name="M">Custom metadata returned for alternate flows or error reporting.</typeparam>
    public interface IPut<T, M> : IVerb
    {
        string Uri { get; }
    }

    /// <summary>
    /// A placedholder for constructions such as 'class Foo : IPut<Empty,MyError>'.
    /// </summary>
    public class Empty { }
}
