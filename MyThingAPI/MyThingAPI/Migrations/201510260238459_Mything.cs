namespace MyThingAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Mything : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MyThings",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        status = c.String(),
                        rfid = c.String(),
                        image = c.Binary(),
                        name = c.String(),
                        type = c.String(),
                        location = c.String(),
                        createdAt = c.DateTime(nullable: false),
                        lastUpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.MyThings");
        }
    }
}
