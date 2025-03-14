using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsLetterBanan.Migrations
{
    /// <inheritdoc />
    public partial class UserNavigationLinkToReplyCommentsFieldAddedINUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserReplyComment_AspNetUsers_UserId",
                table: "UserReplyComment");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReplyComment_Comments_CommentId",
                table: "UserReplyComment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserReplyComment",
                table: "UserReplyComment");

            migrationBuilder.RenameTable(
                name: "UserReplyComment",
                newName: "UserReplyComments");

            migrationBuilder.RenameIndex(
                name: "IX_UserReplyComment_UserId",
                table: "UserReplyComments",
                newName: "IX_UserReplyComments_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserReplyComment_CommentId",
                table: "UserReplyComments",
                newName: "IX_UserReplyComments_CommentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserReplyComments",
                table: "UserReplyComments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserReplyComments_AspNetUsers_UserId",
                table: "UserReplyComments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReplyComments_Comments_CommentId",
                table: "UserReplyComments",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserReplyComments_AspNetUsers_UserId",
                table: "UserReplyComments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReplyComments_Comments_CommentId",
                table: "UserReplyComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserReplyComments",
                table: "UserReplyComments");

            migrationBuilder.RenameTable(
                name: "UserReplyComments",
                newName: "UserReplyComment");

            migrationBuilder.RenameIndex(
                name: "IX_UserReplyComments_UserId",
                table: "UserReplyComment",
                newName: "IX_UserReplyComment_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserReplyComments_CommentId",
                table: "UserReplyComment",
                newName: "IX_UserReplyComment_CommentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserReplyComment",
                table: "UserReplyComment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserReplyComment_AspNetUsers_UserId",
                table: "UserReplyComment",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReplyComment_Comments_CommentId",
                table: "UserReplyComment",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
