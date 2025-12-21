using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerController.Server.Tests.Fakes
{
    public class AllowAllSignatureValidator: GameController.FBService.MiddleWares.IFacebookSignatureValidator
    {
        public bool IsValid(string? signatureHeader, byte[] bodyBytes)
            => true;
        public bool IsSignatureValid(string requestBody, string signatureHeader) => true;
    }
}
