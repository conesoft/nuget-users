namespace Conesoft.Users
{
    public class UsersRootPath
    {
        string path;
        public UsersRootPath(string path)
        {
            this.path = path;
        }

        public string Get() => path;
    }
}