using GameController.FBService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerController.Server.Tests.Fakes
{
    public class AllowAllIdempotencyService : IAllowADempotencyService
    {
        public Task<bool> IsDuplicateAsync(string key)
            => Task.FromResult(false); // არასდროს არის დუბლიკატი

        public Task MarkAsProcessedAsync(string key)
            => Task.CompletedTask;
    }
}
