using GameController.FBService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerController.Server.Tests.Fakes
{
    public class AllowAllRateLimitingService : IAllowRateLimitingService
    {
        public Task<bool> IsAllowedAsync(string key)
            => Task.FromResult(true);
    }
}
