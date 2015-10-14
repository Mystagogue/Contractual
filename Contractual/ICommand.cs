using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contractual
{
    public interface ICommand<TCommand>
    {
        void Handle(TCommand command);
    }
}
