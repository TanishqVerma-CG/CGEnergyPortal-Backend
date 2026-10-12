using BP.Models;

namespace BP.IService
{
    public interface IAccountManagementService
    {
        AccountManagementSummary GetSummary();
        AccountUsersResponse GetAllUsers();
        PendingInvitationsResponse GetPendingInvitations();
        AccountManagementPageResponse GetAccountManagementPage();
        InviteUserResponse InviteUser(InviteUserRequest request);
        DeactivateUserResponse DeactivateUser(string userId);
        ChangePermissionResponse ChangePermission(ChangePermissionRequest request);
        ResetAmPasswordResponse ResetPassword(ResetAmPasswordRequest request);
        bool SendInvitationEmail(int userId, string name, string emailId, string client);
    }
}
