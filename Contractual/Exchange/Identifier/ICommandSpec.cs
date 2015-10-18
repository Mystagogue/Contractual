using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contractual.Exchange;

namespace Contractual.Exchange.Identifier
{
    /// <summary>
    /// Defines a property through which database generated Ids can be accessed in command-spec objects.
    /// </summary>
    /// <remarks>
    /// Wherever possible, application design should avoid using this interface.  Said again,
    /// when possible, command objects and handlers should NOT be required to return
    /// database generated Ids.  A design based on CQRS is a primary way to achieve this goal.
    /// However, too many teams and software designs might be unable or unready to comply with
    /// this strategy.  This interface exists to provide support in such cases.
    /// Using this interface still allows asynchrony via async/await, but negates asynchrony
    /// based on a queuing or service bus strategy.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class ICommandSpec<T>
    {
        /// <summary>
        /// A newly created DB identifier is assigned and accessed through this property.
        /// </summary>
        T Identifier { get; set; }
    }

    /// <summary>
    /// Allows transaction management code to work with ICommandSpec&lt;T&gt; />
    /// </summary>
    public interface IPostCommitRegistrar
    {
        event Action Committed;
    }

    /// <summary>
    /// Allows a transaction-supporting command handler to enlist post-commit assignment of DB-generated Identifiers.
    /// </summary>
    public sealed class PostCommitRegistratorImpl : IPostCommitRegistrar
    {
        public event Action Committed = () => { };

        public void ExecuteActions()
        {
            this.Committed();
        }

        public void Reset()
        {
            // Clears the list of actions.
            this.Committed = () => { };
        }
    }

    /// <summary>
    /// Decorates a command handler that coordinates several transactive command handlers.
    /// </summary>
    /// <code>
    /// using SimpleInjector;
    /// using SimpleInjector.Extensions;
    /// 
    /// container.RegisterManyForOpenGeneric(
    ///    typeof(ICommandHandler<>), 
    ///    typeof(ICommandHandler<>).Assembly);
    ///
    /// container.RegisterDecorator(
    ///    typeof(ICommandHandler<>), 
    ///    typeof(TransactionCommandHandlerDecorator<>));
    ///
    /// container.RegisterDecorator(
    ///    typeof(ICommandHandler<>), 
    ///    typeof(PostCommitCommandHandlerDecorator<>));
    ///
    /// container.RegisterPerWebRequest<PostCommitRegistratorImpl>();
    /// container.Register<IPostCommitRegistrator>(
    ///    () => container.GetInstance<PostCommitRegistratorImpl>());
    /// </code>
    /// <typeparam name="T"></typeparam>
    public sealed class PostCommitCommandDecorator<T> : ICommand<T>
    {
        private readonly ICommand<T> decorated;
        private readonly PostCommitRegistratorImpl registrator;

        public PostCommitCommandDecorator(
            ICommand<T> decorated, PostCommitRegistratorImpl registrator)
        {
            this.decorated = decorated;
            this.registrator = registrator;
        }

        public async Task Handle(T command)
        {
            try
            {
                await this.decorated.Handle(command);

                this.registrator.ExecuteActions();
            }
            finally
            {
                this.registrator.Reset();
            }
        }

        //to support .net 4, consider using the code from here:
        //http://blogs.msdn.com/b/pfxteam/archive/2010/11/21/10094564.aspx
        //Specifically this...
        //public static Task<T2> Then<T1, T2>(this Task<T1> first, Func<T1, Task<T2>> next)
        //{
        //    if (first == null) throw new ArgumentNullException("first");
        //    if (next == null) throw new ArgumentNullException("next");

        //    var tcs = new TaskCompletionSource<T2>();
        //    first.ContinueWith(delegate
        //    {
        //        if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
        //        else if (first.IsCanceled) tcs.TrySetCanceled();
        //        else
        //        {
        //            try
        //            {
        //                var t = next(first.Result);
        //                if (t == null) tcs.TrySetCanceled();
        //                else
        //                    t.ContinueWith(delegate
        //                    {
        //                        if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
        //                        else if (t.IsCanceled) tcs.TrySetCanceled();
        //                        else tcs.TrySetResult(t.Result);
        //                    }, TaskContinuationOptions.ExecuteSynchronously);
        //            }
        //            catch (Exception exc) { tcs.TrySetException(exc); }
        //        }
        //    }, TaskContinuationOptions.ExecuteSynchronously);
        //    return tcs.Task;
        //}
    }
}
