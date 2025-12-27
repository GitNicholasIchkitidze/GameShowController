using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerController.Server.Tests.Fakes
{
    public interface IAllowRateLimitingService
    {
        Task<bool> IsAllowedAsync(string key);
    }
}
