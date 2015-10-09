# Contractual
Infrastructure utilities and interfaces exposed to your business logic and domain model.

## Overview

Provide hyper-thin support for application frameworks:

  * WCF
  * WCF REST
  * ASP.NET MVC
  * ASP.NET Web API
  * System.Configuration

Provide hyper-thin support for patterns:

  * Repository
  * CQRS
  * Event Brokering

The core `Contractual` library promises never to take dependencies on other NuGet packages; it will always be a single, stand-alone assembly and NuGet package.  Other supporting libraries, such as `Contractual.WebApi` might take dependencies on external NuGet packages...but those are dependencies always kept to a bare minimum.

## Index of Tools

  * WCF
    * **Proxy<T>**: This is a wrapper around the WCF ClientBase<T>.
      * Gaurantees proper disposal of ClientBase<T>, which circumvents the long-standing Indigo Disposable bug.
      * Allows hyper-simple testing, merely by implementing (stubbing / mocking) a ServiceContract interface.
      * Supports ClientBase<T> asynchronous invocations, which are based on IO completion ports,while still ensuring that each ClientBase<T> is properly disposed.
  * ASP.NET Web API
    * **MetaClient**: This is a subclass of ASP.NET Web API `HttpClient`.
      * Uses generic methods to add meta-data capabilities such as: type-safe return values and encapsulated resource paths.
      * Provides a custom configuration section which holds endpoint addresses, TLS certificate info, serialization parameters and more.
  * System.Configuration
    * Collection ElementList<T>: Generic support for creating custom configuration section collections.

...this document is incomplete.  Stay tuned.
