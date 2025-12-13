namespace GameController.FBService.MiddleWares
{
    public interface IFacebookSignatureValidator
    {
        bool IsSignatureValid(string requestBody, string signatureHeader);

    }
}
