using System;
using System.Threading;
using System.Threading.Tasks;

namespace Contractual
{
    public class AsyncResult<T> : IAsyncResult, IDisposable
    {

        private readonly AsyncCallback callback_;
        private readonly object asyncState_;
        private T result_;
        private Exception e_;

        private bool completed_;
        private bool completedSynchronously_;
        private readonly ManualResetEvent waitHandle_;

        public AsyncResult(AsyncCallback cb, object state, bool completed = false)
        {
            this.callback_ = cb;
            this.asyncState_ = state;
            this.completed_ = completed;
            this.completedSynchronously_ = completed;
            this.waitHandle_ = new ManualResetEvent(false);
        }

        #region IAsyncResult Members

        public object AsyncState
        {
            get { return asyncState_; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return waitHandle_; }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return completedSynchronously_;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return completed_;
            }
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.waitHandle_ != null)
                {
                    ((IDisposable)this.waitHandle_).Dispose();
                }
            }
        }

        //Brent Arias: this property might not be the best idea...
        public Exception Exception
        {
            get
            {
                return this.e_;
            }
        }

        public T Result
        {
            get
            {
                if (!IsCompleted)
                {
                    waitHandle_.WaitOne();
                }

                if (Exception != null)
                {
                    throw Exception;
                }

                return result_;
            }
        }

        public void Complete(T result, bool completedSynchronously = false)
        {
            this.result_ = result;
            Thread.MemoryBarrier();
            this.completedSynchronously_ = completedSynchronously;
            this.completed_ = true;

            this.SignalCompletion();
        }

        public void HandleException(Exception e, bool completedSynchronously = false)
        {
            this.e_ = e;
            Thread.MemoryBarrier();
            this.completedSynchronously_ = completedSynchronously;
            this.completed_ = true;

            this.SignalCompletion();
        }

        public void Complete(Task<T> task, bool aggregateException = true, bool completedSynchronously = false)
        {
            if (task.IsFaulted)
            {
                HandleException(aggregateException ? task.Exception : task.Exception.GetBaseException(), completedSynchronously);
            }
            else if (task.IsCanceled)
            {
                Complete(default(T), completedSynchronously);
            }
            else
            {
                Complete(task.Result, completedSynchronously);
            }
        }

        private void SignalCompletion()
        {
            this.waitHandle_.Set();
            if (callback_ != null)
            {
                callback_(this);
            }
            //Brent Arias: starting a separate thread for this is not necessary,
            //unless attempting to free an I/O thread quickly. For now, we'll do 
            //without.
            //ThreadPool.QueueUserWorkItem(new WaitCallback(this.InvokeCallback));
        }

        //private void InvokeCallback(object state)
        //{
        //    if (this.callback_ != null)
        //    {
        //        this.callback_(this);
        //    }
        //}
    }
}
