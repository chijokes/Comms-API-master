using System.Collections.Generic;

namespace FusionComms.DTOs
{
    public class ClaimRequest
    {
        /// <summary>
        /// Name of the role
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// Claims to be added to role
        /// </summary>
        public List<string> Claims { get; set; }
    }

    public class ClaimRequestForUser
    {
        /// <summary>
        /// entity's username
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Claims to be added to user
        /// </summary>
        public List<string> Claims { get; set; }
    }
}
