using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contractual.Exchange
{
    public interface ICommand<TCommand>
    {
        Task Handle(TCommand command);
    }
}
