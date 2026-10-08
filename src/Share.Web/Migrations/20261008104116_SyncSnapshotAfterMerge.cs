using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Share.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncSnapshotAfterMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. The Notifications and Announcements tables are created by the
            // AddNotifications and AddAnnouncements migrations, which were merged without updating
            // the model snapshot. This migration only brings the snapshot back in line.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. The Notifications and Announcements tables are created by the
            // AddNotifications and AddAnnouncements migrations, which were merged without updating
            // the model snapshot. This migration only brings the snapshot back in line.
        }
    }
}
