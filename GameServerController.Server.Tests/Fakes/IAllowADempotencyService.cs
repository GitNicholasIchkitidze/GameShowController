using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerController.Server.Tests.Fakes
{
    public interface IAllowADempotencyService
    {

        Task<bool> IsDuplicateAsync(string key);

        Task MarkAsProcessedAsync(string key);
    }
}

