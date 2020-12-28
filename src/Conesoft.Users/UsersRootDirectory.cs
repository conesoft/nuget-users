using Conesoft.Files;

namespace Conesoft.Users
{
    public class UsersRootDirectory : Directory
    {
        private readonly string applicationName;

        public UsersRootDirectory(string applicationName, Directory directory) : base(directory)
        {
            this.applicationName = applicationName;
        }

        public string ApplicationName => applicationName;
    }
}