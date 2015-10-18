using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contractual.Exchange
{
    /// <summary>
    /// A factory for query handlers.
    /// </summary>
    public interface IQueryProcessor
    {
        Task<TResult> Execute<TResult>(IQuerySpec<TResult> query);
    }
}
