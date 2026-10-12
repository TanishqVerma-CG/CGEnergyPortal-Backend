using BP.Models;

namespace BP.IService
{
    public interface IUserService
    {
        User? ValidateUser(string emailId, string password, string role);
        User? GetUserById(int userId);
        User? GetUserByEmail(string emailId);
        UpdateUser? UpdateUserDetail(UpdateUser user);
        UserAddress? UpdateUserAddress(UserAddress user);
        bool ChangePassword(ChangePassword request);
        bool ResetPassword(int userId, string newPassword);
        User? CreateUser(CreateAccountRequest request);
        User CreateUserFromInvite(InviteUserRequest request, string accountType);
        string GeneratePasswordResetToken(int userId);
        bool ResetPasswordByToken(string token, string newPassword);
        string GenerateVerificationCode(int userId);
        bool VerifyAndConsumeVerificationCode(int userId, string code);
    }
}
