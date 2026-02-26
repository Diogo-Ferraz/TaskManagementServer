using TaskManagement.Shared.Models;

namespace TaskManagement.Shared.DemoData
{
    public static class DemoIdentityBlueprint
    {
        public static IReadOnlyList<DemoIdentityUser> Users { get; } = BuildUsers();

        public static IReadOnlyList<DemoIdentityUser> Administrators =>
            Users.Where(user => user.Role == Roles.Administrator).ToList();

        public static IReadOnlyList<DemoIdentityUser> ProjectManagers =>
            Users.Where(user => user.Role == Roles.ProjectManager).ToList();

        public static IReadOnlyList<DemoIdentityUser> StandardUsers =>
            Users.Where(user => user.Role == Roles.User).ToList();

        private static IReadOnlyList<DemoIdentityUser> BuildUsers()
        {
            var users = new List<DemoIdentityUser>();

            // 3 administrators
            users.Add(new DemoIdentityUser("demo-admin-01", "demo-admin@example.com", "Demo Admin", Roles.Administrator));
            users.Add(new DemoIdentityUser("demo-admin-02", "admin.alex@example.com", "Alex Administrator", Roles.Administrator));
            users.Add(new DemoIdentityUser("demo-admin-03", "admin.jordan@example.com", "Jordan Administrator", Roles.Administrator));

            // 6 project managers
            users.Add(new DemoIdentityUser("demo-pm-01", "demo-manager@example.com", "Demo Manager", Roles.ProjectManager));
            users.Add(new DemoIdentityUser("demo-pm-02", "pm.olivia@example.com", "Olivia Project Manager", Roles.ProjectManager));
            users.Add(new DemoIdentityUser("demo-pm-03", "pm.noah@example.com", "Noah Project Manager", Roles.ProjectManager));
            users.Add(new DemoIdentityUser("demo-pm-04", "pm.ava@example.com", "Ava Project Manager", Roles.ProjectManager));
            users.Add(new DemoIdentityUser("demo-pm-05", "pm.ethan@example.com", "Ethan Project Manager", Roles.ProjectManager));
            users.Add(new DemoIdentityUser("demo-pm-06", "pm.sophia@example.com", "Sophia Project Manager", Roles.ProjectManager));

            // 20 contributors
            users.Add(new DemoIdentityUser("demo-user-01", "demo-user@example.com", "Demo User", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-02", "user.mia@example.com", "Mia Carter", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-03", "user.liam@example.com", "Liam Brooks", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-04", "user.emma@example.com", "Emma Walker", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-05", "user.james@example.com", "James Turner", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-06", "user.amelia@example.com", "Amelia Reed", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-07", "user.lucas@example.com", "Lucas Hayes", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-08", "user.charlotte@example.com", "Charlotte Ross", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-09", "user.benjamin@example.com", "Benjamin Price", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-10", "user.evelyn@example.com", "Evelyn Foster", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-11", "user.henry@example.com", "Henry Morris", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-12", "user.harper@example.com", "Harper Bell", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-13", "user.daniel@example.com", "Daniel Cooper", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-14", "user.abigail@example.com", "Abigail Ward", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-15", "user.matthew@example.com", "Matthew Diaz", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-16", "user.ella@example.com", "Ella Griffin", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-17", "user.sebastian@example.com", "Sebastian Kim", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-18", "user.scarlett@example.com", "Scarlett Moore", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-19", "user.logan@example.com", "Logan Young", Roles.User));
            users.Add(new DemoIdentityUser("demo-user-20", "user.chloe@example.com", "Chloe Adams", Roles.User));

            return users;
        }
    }
}
