using System.Collections.Generic;

namespace CuoiKyDocNet.Models
{
    public class ManageRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<RoleViewModel> Roles { get; set; }
    }

    public class RoleViewModel
    {
        public string RoleName { get; set; }
        public bool IsAssigned { get; set; }
    }
}