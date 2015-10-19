using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Contractual.DomainModel
{
    /// <summary>
    /// Allow thread-safe, inter-thread logical stack access.
    /// </summary>
    /// <remarks>
    /// This code takes advantage of a .net 4.5 "copy on write"
    /// behavior which ensures that each thread inherits its own
    /// copy of CallContext values from the prior thread.  To
    /// receive the benefit of this .net 4.5 capability, it is
    /// necessary to have the stack be cloned with each and
    /// every Push() and Pop().  See this article:
    /// http://blog.stephencleary.com/2013/04/implicit-async-context-asynclocal.html 
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class LogicalContextStack<T>
    {
        private static readonly string guiName = Guid.NewGuid().ToString("N");
        private readonly string name;

        protected LogicalContextStack()
        {
            name = this.GetType().Name;
        }

        public LogicalContextStack(string name = null)
        {
            this.name = name ?? guiName;
        }

        private sealed class Wrapper : MarshalByRefObject
        {
            public Stack<T> Value { get; set; }
        }

        private Stack<T> CurrentContext
        {
            get
            {
                //var ret = CallContext.LogicalGetData(name) as Wrapper;
                var ret = LogicalContext<Wrapper>.Get(name);
                return ret == null ? new Stack<T>() : ret.Value;
            }

            set
            {
                //CallContext.LogicalSetData(name, new Wrapper { Value = value });
                LogicalContext<Wrapper>.Set(new Wrapper { Value = value }, name);
            }
        }

        public virtual IDisposable Push(T context)
        {
            //Using immutable collections would be the elegent
            //way to solve this problem.  However, a brute-force
            //stack copy instead is used because (1) it is important
            //that this project have no dependencies on other
            //non-GAC libraries and (2) the depth of these Stack instances
            //is expected to range from one to six levels - making
            //it too small to be worth the benefit of an immutable
            //collection.
            var stack = new Stack<T>(CurrentContext);
            stack.Push(context);
            CurrentContext = stack;
            return new PopWhenDisposed(this);
        }

        private void Pop()
        {
            var stack = new Stack<T>(CurrentContext);
            stack.Pop();
            if (stack.Count == 0)
            {
                //CallContext.FreeNamedDataSlot(name);
                LogicalContext<Wrapper>.Free(name);
            }
            else
            {
                CurrentContext = stack;
            }
        }

        private sealed class PopWhenDisposed : IDisposable
        {
            private bool disposed;
            LogicalContextStack<T> context;

            public PopWhenDisposed(LogicalContextStack<T> context)
            {
                this.context = context;
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                context.Pop();
                disposed = true;
            }
        }

        public IEnumerable<T> CurrentStack
        {
            get
            {
                return CurrentContext.Reverse();
            }
        }
    }
}
