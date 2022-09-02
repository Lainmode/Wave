namespace Wave.Models
{
    public enum ResponseCode
    {
        // Successes starting with 1x
        OTPRequestSuccessful = 10,
        OTPVerificationSuccessful = 11,

        RequestFulfilled = 12,

        // Loyalty concerns
        LoyaltyPointAdded = 15,
        LoyaltyPointsModified = 16,
        LoyaltyPointsReset = 17,

        // idk
        Banned = 80,

        // Errors starting with 2x
        GeneralError = 20,
        ExceededMaximumOTPRequests = 23,
        IncorrectOTP = 24,
        ExceededMaximumOTPSubmissions = 25,

        // Informational
        Information = 45,
        Timeout = 81
    }
}