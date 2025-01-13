// Copyright © 2013, 2025, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.MySqlClient;
using MySql.Web.Common;
using MySql.Web.Security;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Data;
using System.Web.Security;

namespace MySql.Web.Tests
{
  public class SchemaManagerTests : WebTestBase
  {
    protected override void InitSchema()
    {
      // we override this and leave it empty because we don't want
      // to init the schema for this test.
    }

    [OneTimeSetUp]
    public void Setup()
    {
      LoadData();
    }


    /// <summary>
    /// Bug #37469 autogenerateschema optimizing
    /// </summary>
    [Test]
    public void SchemaCheck()
    {
      for (int i = 0; i <= SchemaManager.Version; i++)
      {
        MySQLMembershipProvider provider = new MySQLMembershipProvider();
        NameValueCollection config = new NameValueCollection();
        config.Add("connectionStringName", "LocalMySqlServer");
        config.Add("applicationName", "/");
        config.Add("passwordFormat", "Clear");

        if (i > 0)
          for (int x = 1; x <= i; x++)
            LoadSchema(x);

        try
        {
          provider.Initialize(null, config);
          if (i < SchemaManager.Version)
            Assert.That(false, Is.False, "Should have failed");
        }
        catch (ProviderException)
        {
          if (i == SchemaManager.Version)
            Assert.That(false, Is.False, "This should not have failed");
        }
      }
    }

    /// <summary>
    /// Bug #36444 'autogenerateschema' produces tables with 'random' collations
    /// </summary>
    [Test]
    public void CurrentSchema()
    {
      execSQL(@"set character_set_database=utf8;
              ALTER TABLE my_aspnet_membership CONVERT TO CHARACTER SET DEFAULT;
              UPDATE my_aspnet_schemaversion SET version=4;");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM my_aspnet_schemaversion", Connection);
      object ver = cmd.ExecuteScalar();
      Assert.That(ver, Is.EqualTo(4));

      cmd.CommandText = "SHOW CREATE TABLE my_aspnet_membership";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createSql = reader.GetString(1);
        Assert.That(createSql.IndexOf("CHARSET=utf8") != -1);
      }
    }

    [Test]
    public void UpgradeV1ToV2()
    {
      execSQL(@"CREATE TABLE if not exists  mysql_Membership(`PKID` varchar(36) NOT NULL,
              Username varchar(255) NOT NULL,
              ApplicationName varchar(255) NOT NULL,
              Email varchar(128) NOT NULL,
              Comment varchar(255) default NULL,
              Password varchar(128) NOT NULL,
              PasswordQuestion varchar(255) default NULL,
              PasswordAnswer varchar(255) default NULL,
              IsApproved tinyint(1) default NULL,
              LastActivityDate datetime default NULL,
              LastLoginDate datetime default NULL,
              LastPasswordChangedDate datetime default NULL,
              CreationDate datetime default NULL,
              IsOnline tinyint(1) default NULL,
              IsLockedOut tinyint(1) default NULL,
              LastLockedOutDate datetime default NULL,
              FailedPasswordAttemptCount int(10) unsigned default NULL,
              FailedPasswordAttemptWindowStart datetime default NULL,
              FailedPasswordAnswerAttemptCount int(10) unsigned default NULL,
              FailedPasswordAnswerAttemptWindowStart datetime default NULL,
              PRIMARY KEY  (`PKID`)) DEFAULT CHARSET=latin1 COMMENT='1';
              ALTER TABLE mysql_Membership  CHANGE Email Email VARCHAR(128), COMMENT='1';");


      MySqlCommand cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createTable = reader.GetString(1);
        int index = createTable.IndexOf("COMMENT='1'");
        Assert.That(index, Is.Not.EqualTo(-1));
      }

      execSQL(@" ALTER TABLE mysql_Membership 
            CHANGE Email Email VARCHAR(128), COMMENT='2';");
      cmd = new MySqlCommand("SHOW CREATE TABLE mysql_membership", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string createTable = reader.GetString(1);
        int index = createTable.IndexOf("COMMENT='2'");
        Assert.That(index, Is.Not.EqualTo(-1));
      }
    }

    private void LoadData()
    {
      LoadSchema(1);
      LoadSchema(2);
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('1', 'user1', '', 'app1', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('2', 'user2', '', 'app1', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('3', 'user1', '', 'app2', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_membership (pkid, username, password, applicationname, lastactivitydate) 
                VALUES('4', 'user2', '', 'app2', '2007-01-01')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role1', 'app1')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role2', 'app1')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role1', 'app2')");
      execSQL(@"INSERT INTO mysql_roles VALUES ('role2', 'app2')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user1', 'role1', 'app1')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user2', 'role2', 'app1')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user1', 'role1', 'app2')");
      execSQL(@"INSERT INTO mysql_UsersInRoles VALUES ('user2', 'role2', 'app2')");
      LoadSchema(3);
      Assert.That(!TableExists("mysql_membership"));
      Assert.That(!TableExists("mysql_roles"));
      Assert.That(!TableExists("mysql_usersinroles"));
    }

    [Test]
    public void CheckAppsUpgrade()
    {
      DataTable apps = FillTable("SELECT * FROM my_aspnet_applications");
      Assert.That(apps.Rows.Count, Is.EqualTo(2));
      Assert.That(apps.Rows[0]["id"], Is.EqualTo(1));
      Assert.That(apps.Rows[0]["name"], Is.EqualTo("app1"));
      Assert.That(apps.Rows[1]["id"], Is.EqualTo(2));
      Assert.That(apps.Rows[1]["name"], Is.EqualTo("app2"));
    }

    [Test]
    public void CheckUsersUpgrade()
    {
      DataTable dt = FillTable("SELECT * FROM my_aspnet_users");
      Assert.That(dt.Rows.Count, Is.EqualTo(4));
      Assert.That(dt.Rows[0]["id"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["applicationId"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("user1"));
      Assert.That(dt.Rows[1]["id"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["applicationId"], Is.EqualTo(1));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo("user2"));
      Assert.That(dt.Rows[2]["id"], Is.EqualTo(3));
      Assert.That(dt.Rows[2]["applicationId"], Is.EqualTo(2));
      Assert.That(dt.Rows[2]["name"], Is.EqualTo("user1"));
      Assert.That(dt.Rows[3]["id"], Is.EqualTo(4));
      Assert.That(dt.Rows[3]["applicationId"], Is.EqualTo(2));
      Assert.That(dt.Rows[3]["name"], Is.EqualTo("user2"));
    }

    [Test]
    public void CheckRolesUpgrade()
    {
      DataTable dt = FillTable("SELECT * FROM my_aspnet_roles");
      Assert.That(dt.Rows.Count, Is.EqualTo(4));
      Assert.That(dt.Rows[0]["id"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["applicationId"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("role1"));
      Assert.That(dt.Rows[1]["id"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["applicationId"], Is.EqualTo(1));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo("role2"));
      Assert.That(dt.Rows[2]["id"], Is.EqualTo(3));
      Assert.That(dt.Rows[2]["applicationId"], Is.EqualTo(2));
      Assert.That(dt.Rows[2]["name"], Is.EqualTo("role1"));
      Assert.That(dt.Rows[3]["id"], Is.EqualTo(4));
      Assert.That(dt.Rows[3]["applicationId"], Is.EqualTo(2));
      Assert.That(dt.Rows[3]["name"], Is.EqualTo("role2"));
    }

    [Test]
    public void CheckMembershipUpgrade()
    {
      DataTable dt = FillTable("SELECT * FROM my_aspnet_membership");
      Assert.That(dt.Rows.Count, Is.EqualTo(4));
      Assert.That(dt.Rows[0]["userid"], Is.EqualTo(1));
      Assert.That(dt.Rows[1]["userid"], Is.EqualTo(2));
      Assert.That(dt.Rows[2]["userid"], Is.EqualTo(3));
      Assert.That(dt.Rows[3]["userid"], Is.EqualTo(4));
    }

    [Test]
    public void CheckUsersInRolesUpgrade()
    {
      DataTable dt = FillTable("SELECT * FROM my_aspnet_usersinroles");
      Assert.That(dt.Rows.Count, Is.EqualTo(4));
      Assert.That(dt.Rows[0]["userid"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["roleid"], Is.EqualTo(1));
      Assert.That(dt.Rows[1]["userid"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["roleid"], Is.EqualTo(2));
      Assert.That(dt.Rows[2]["userid"], Is.EqualTo(3));
      Assert.That(dt.Rows[2]["roleid"], Is.EqualTo(3));
      Assert.That(dt.Rows[3]["userid"], Is.EqualTo(4));
      Assert.That(dt.Rows[3]["roleid"], Is.EqualTo(4));
    }

    /// <summary>
    /// Bug #39072 Web provider does not work
    /// </summary>
    [Test]
    public void AutoGenerateSchema()
    {
      MySQLMembershipProvider provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("autogenerateschema", "true");
      config.Add("applicationName", "/");
      config.Add("passwordFormat", "Clear");

      provider.Initialize(null, config);

      MembershipCreateStatus status;
      MembershipUser user = provider.CreateUser("boo", "password", "email@email.com",
          "question", "answer", true, null, out status);
    }

    [Test]
    public void SchemaTablesUseSameEngine()
    {
      for (int x = 1; x <= SchemaManager.Version; x++)
        LoadSchema(x);

      string query = string.Format("SELECT TABLE_NAME, ENGINE FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}'", Connection.Database);
      MySqlCommand cmd = new MySqlCommand(query, Connection);
      string lastEngine = null;
      string currentEngine;

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          currentEngine = reader.GetString("ENGINE");

          if (string.IsNullOrEmpty(lastEngine))
          {
            lastEngine = currentEngine;
          }

          Assert.That(currentEngine, Is.EqualTo(lastEngine));
        }
      }
    }

    [Test]
    public void InitializeInvalidConnStringThrowsArgumentException()
    {
      MySQLMembershipProvider provider = new MySQLMembershipProvider();
      NameValueCollection config = new NameValueCollection();
      var badConnectionString = ConnectionString + ";fookey=boo";
      config.Add("connectionString", badConnectionString);

      Exception ex = Assert.Throws<ArgumentException>(() => provider.Initialize(null, config));
      Assert.That(ex.Message, Is.EqualTo("Option not supported\r\nParameter name: fookey"));
    }
  }
}
