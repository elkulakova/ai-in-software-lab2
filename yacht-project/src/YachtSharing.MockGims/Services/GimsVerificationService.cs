using YachtSharing.MockGims.Contracts;

namespace YachtSharing.MockGims.Services;

public class GimsVerificationService
{
    public VerifyLicenseResponse VerifyLicense(string series, string number)
    {
        string normalizedPassport = series + number;
        bool isValid = IsPassportInRegistry(normalizedPassport);

        return new VerifyLicenseResponse(isValid);
    }

    private static bool IsPassportInRegistry(string passport)
    {
        string[] validPassports = GetValidPassports();
        return System.Array.Exists(validPassports, currentPassport => currentPassport == passport);
    }

    private static string[] GetValidPassports()
    {
        return
        [
            "4512123456",
            "4512987654",
            "4500111122",
        ];
    }
}
