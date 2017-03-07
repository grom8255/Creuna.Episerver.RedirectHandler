namespace Creuna.Episerver.RedirectHandler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CustomRedirects",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        NewUrl = c.String(),
                        OldUrl = c.String(nullable: false),
                        AppendMatchToNewUrl = c.Boolean(nullable: false),
                        ExactMatch = c.Boolean(nullable: false),
                        IncludeQueryString = c.Boolean(nullable: false),
                        State = c.Int(nullable: false),
                        NotfoundErrorCount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.CustomRedirects");
        }
    }
}
