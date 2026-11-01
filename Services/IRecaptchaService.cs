namespace PinoyPantry.API.Services
{
    public interface IRecaptchaService
    {
        // True only if Google confirms the token came from a real "I'm not a robot" check.
        Task<bool> VerifyAsync(string? token, string? remoteIp);
    }
}
