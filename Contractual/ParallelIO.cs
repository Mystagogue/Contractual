using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Contractual
{
    public sealed class ParallelIO
    {
        [ThreadStatic]
        private static Retry currentConfig;

        private IEnumerator workIter;
        private int degreeOfParallelism;
        private int defaultParallelism;
        private Dictionary<int, Job> tasks;
        private bool hasMoreRows;
        private TaskCompletionSource<bool> completionSource;
        private Exception fault;
        private bool faulted;
        private Delegate ioStep;
        private object faultLock = new object();
        private CancellationTokenSource cts;
        private bool initialized;

        public ParallelIO()
        {
            this.defaultParallelism = 1;
        }

        public ParallelIO(int numConcurrentOperations)
        {
            if (numConcurrentOperations < 1)
            {
                throw new ArgumentOutOfRangeException("ParallelIO: Requested number of concurrent operations must be great than 0");
            }
            this.defaultParallelism = numConcurrentOperations;
        }

        public static bool WillRetry(Retry retry)
        {
            Retry current = currentConfig;
            if (current != null && current.Equals(retry))
            {
                return current.WillRetry();
            }
            else
            {
                return true;
            }
        }

        public void ForEach<W>(IEnumerable<W> work, Func<W, Retry> operation)
        {
            ForEach(defaultParallelism, work, operation);
        }

        public void ForEach<W>(int numConcurrentOperations, IEnumerable<W> work, Func<W, Retry> operation)
        {
            ioStep = operation;
            this.degreeOfParallelism = numConcurrentOperations;
            cts = new CancellationTokenSource();
            completionSource = new TaskCompletionSource<bool>();
            tasks = new Dictionary<int, Job>();
            hasMoreRows = true;
            fault = null;
            faulted = false;
            workIter = work.GetEnumerator();
            ForEach();
            var task = completionSource.Task;
            task.Wait();
        }

        private void ForEach()
        {
            if (!faulted)
            {
                while (hasMoreRows && tasks.Count < degreeOfParallelism && (hasMoreRows = workIter.MoveNext()))
                {
                    var newTask = Task<Retry>.Factory.StartNew((o) => PerformIO(o, null), workIter.Current);
                    var job = new Job();
                    job.Operation = newTask;
                    job.Work = workIter.Current;
                    tasks[newTask.Id] = job;
                }
            }
            if (tasks.Count > 0)
            {
                var resultHandler = Task.Factory.ContinueWhenAny(tasks.Values.Select(j => j.Operation).ToArray(), HandleResult);
            }
            else
            {
                if (!faulted)
                {
                    completionSource.SetResult(true);
                }
                else
                {
                    completionSource.SetException(fault.GetBaseException());
                }
            }
        }

        /// <summary>
        /// Invoke the action which instigates IO.
        /// </summary>
        /// <param name="work"></param>
        /// <returns></returns>
        private Retry PerformIO(object work, Retry retry)
        {
            //Keep this on Thread Local Storage (TLS) since we are now on using the same thread that will be used by client code.
            currentConfig = retry;
            return (Retry)ioStep.DynamicInvoke(work);
        }

        private void HandleResult(Task basetask)
        {
            var task = (Task<Retry>)basetask;

            if (task.IsFaulted)
            {
                Fault(task);
            }
            else if (task.IsCanceled)
            {
                tasks.Remove(task.Id);
            }
            else
            {
                Retry retry = task.Result;
                if (retry != null)
                {
                    Retry(task, retry);
                }
                else
                {
                    tasks.Remove(task.Id);
                }
            }
            //In all cases, it is necessary to give other still active tasks a chance to finish or terminate.
            ForEach();
        }

        /// <summary>
        /// Requests a retry of the current item.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="delay">wait period in milliseconds</param>
        /// <param name="retries">how many retries before skipping</param>
        /// <returns>true if a retry is occurring, false if all allowed retries have been made.</returns>
        private void Retry(Task<Retry> task, Retry step)
        {
            Job job = tasks[task.Id];
            if (job.Config == null || !job.Config.Equals(step))
            {
                job.Config = step;
            }

            tasks.Remove(task.Id);

            if (job.Config.ShouldRetry && !faulted)
            {
                Task<Retry> retryTask = null;
                if (job.Config.Delay > 0)
                {
                    retryTask = TaskEx.Delay(job.Config.Delay, cts.Token).ContinueWith<Retry>((d) => PerformIO(job.Work, job.Config));
                }
                else
                {
                    retryTask = Task<Retry>.Factory.StartNew((o) => PerformIO(o, job.Config), job.Work);
                }
                job.Operation = retryTask;
                tasks[retryTask.Id] = job;
            }
        }

        private void Fault(Task<Retry> task)
        {
            cts.Cancel();
            //If several uncaught exceptions arrive at the same time, ensure the first to reach this point wins and is
            //recorded as the exception returned by ForEach(...).
            LazyInitializer.EnsureInitialized(ref fault, ref initialized, ref faultLock, () => task.Exception.GetBaseException());
            faulted = true;
            tasks.Remove(task.Id);
        }

        internal class Job
        {
            public Task Operation;
            public object Work;
            public Retry Config;
        }
    }

    public class Retry : IEquatable<Retry>
    {
        private int _retryincrement;
        public int Retries;
        public int Delay;
        //private Action notifyFailed;

        /// <summary>
        /// Provide control over retry dynamics
        /// </summary>
        /// <param name="delay">Retry delay in milliseconds</param>
        /// <param name="retries">Max number of retries to perform</param>
        public Retry(int delay = 0, int retries = int.MaxValue)
        {
            Retries = retries;
            Delay = delay;
            //this.notifyFailed = notifyFailed;
        }

        internal bool WillRetry() { return _retryincrement + 1 < Retries; }

        internal bool ShouldRetry
        {
            get
            {
                if (Retries < int.MaxValue)
                {
                    _retryincrement++;
                }
                bool doRetry = _retryincrement < Retries;
                return doRetry;
            }
        }

        //public override bool Equals(object obj)
        //{
        //    var config = (JobConfiguration)obj;
        //    return Equals(config);
        //}

        public bool Equals(Retry other)
        {
            return Retries == other.Retries && Delay == other.Delay;
        }
    }

    #region Task.Delay()
    public static class TaskEx
    {
        static readonly Task _sPreCompletedTask = GetCompletedTask();
        static readonly Task _sPreCanceledTask = GetPreCanceledTask();

        public static Task Delay(int dueTimeMs, CancellationToken cancellationToken)
        {
            if (dueTimeMs < -1)
                throw new ArgumentOutOfRangeException("dueTimeMs", "Invalid due time");
            if (cancellationToken.IsCancellationRequested)
                return _sPreCanceledTask;
            if (dueTimeMs == 0)
                return _sPreCompletedTask;

            var tcs = new TaskCompletionSource<object>();
            var ctr = new CancellationTokenRegistration();
            var timer = new Timer(delegate (object self)
            {
                ctr.Dispose();
                ((Timer)self).Dispose();
                tcs.TrySetResult(null);
            });
            if (cancellationToken.CanBeCanceled)
                ctr = cancellationToken.Register(delegate
                {
                    timer.Dispose();
                    tcs.TrySetCanceled();
                });

            timer.Change(dueTimeMs, -1);
            return tcs.Task;
        }

        private static Task GetPreCanceledTask()
        {
            var source = new TaskCompletionSource<object>();
            source.TrySetCanceled();
            return source.Task;
        }

        private static Task GetCompletedTask()
        {
            var source = new TaskCompletionSource<object>();
            source.TrySetResult(null);
            return source.Task;
        }
    }
    #endregion
}
