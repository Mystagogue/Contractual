﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contractual.Exchange
{
    public interface IQuery<TQuery, TResult> where TQuery : IQuerySpec<TResult>
    {
        Task<TResult> Handle(TQuery query);
    }
}
