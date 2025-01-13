// Copyright © 2014, 2025, Oracle and/or its affiliates.
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
using MySql.Web.Personalization;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls.WebParts;

namespace MySql.Web.Tests
{
  public class PersonalizationTests : WebTestBase
  {
    private long applicationId;

    private void CreateDataForSharedScope()
    {
      execSQL(@"delete from my_aspnet_applications;
                delete from my_aspnet_paths;
                delete from my_aspnet_personalizationallusers;");

      var cmd = new MySqlCommand();
      cmd.CommandText = @"insert into my_aspnet_applications(name,description) values('\\', '\\')";
      cmd.Connection = Connection;
      cmd.ExecuteNonQuery();
      applicationId = cmd.LastInsertedId;

      // Add my_aspnet_paths
      var pathId = new Guid();
      cmd.CommandText = @"insert into my_aspnet_paths(applicationId, pathid, path, loweredpath) values(" + applicationId +
                          ",'" + pathId.ToString() + @"', '~/default.aspx', '~/default.aspx')";
      cmd.Connection = Connection;
      cmd.ExecuteNonQuery();


      // personalization all users      
      byte[] settings = CreateBlob(1000);

      cmd.CommandText = @"insert into my_aspnet_personalizationallusers(pathid, pagesettings, lastUpdatedDate) values(" +
                        "'" + pathId.ToString() + "',   @pageSettings, @LastUpdatedDate)";
      cmd.Parameters.AddWithValue("@pathId", pathId);
      cmd.Parameters.AddWithValue("@pageSettings", settings);
      cmd.Parameters.AddWithValue("@LastUpdatedDate", DateTime.UtcNow);
      cmd.Connection = Connection;
      cmd.ExecuteNonQuery();
    }


    public static byte[] CreateBlob(int size)
    {
      byte[] buf = new byte[size];

      Random r = new Random();
      r.NextBytes(buf);
      return buf;
    }


    private void CreateDataForUserScope()
    {
      execSQL(@"delete from my_aspnet_applications;
                delete from my_aspnet_paths;
                delete from my_aspnet_users;
                delete from my_aspnet_personalizationallusers;");
      var cmd = new MySqlCommand();
      cmd.CommandText = @"insert into my_aspnet_applications(name,description) values('\\', '\\')";
      cmd.Connection = Connection;
      cmd.ExecuteNonQuery();
      applicationId = cmd.LastInsertedId;

      // Add my_aspnet_paths
      var pathId = new Guid();
      cmd.CommandText = @"insert into my_aspnet_paths(applicationId, pathid, path, loweredpath) values(" + applicationId +
                          ",'" + pathId.ToString() + @"', '~/default.aspx', '~/default.aspx')";
      cmd.Connection = Connection;
      cmd.ExecuteNonQuery();

      // add user
      cmd.CommandText = @"insert into my_aspnet_users(applicationId, name, isAnonymous, lastActivityDate) values(" + applicationId +
                        @",'GabPC\\Gab', 0, @LastActivityDate)";
      cmd.Connection = Connection;
      cmd.Parameters.AddWithValue("@LastActivityDate", DateTime.UtcNow);
      cmd.ExecuteNonQuery();
      var userId = cmd.LastInsertedId;

      // personalization per user      
      byte[] settings = CreateBlob(1000);

      cmd.CommandText = @"insert into my_aspnet_personalizationperuser(applicationId, pathid, userId, pagesettings, lastUpdatedDate) values(" +
                        applicationId + ", '" + pathId.ToString() + "', " + userId + ", @pageSettings, @LastUpdatedDate)";
      cmd.Parameters.AddWithValue("@pageSettings", settings);
      cmd.Parameters.AddWithValue("@LastUpdatedDate", DateTime.UtcNow);
      cmd.Connection = Connection;
      cmd.ExecuteNonQuery();
    }

    private MySqlPersonalizationProvider InitPersonalizationProvider()
    {
      MySqlPersonalizationProvider p = new MySqlPersonalizationProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", @"\");
      config.Add("description", @"\");
      config.Add("autogenerateschema", "true");
      p.Initialize(null, config);
      return p;
    }

    [Test]
    public void CanFindState()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;
      var psq = new PersonalizationStateQuery();
      psq.UsernameToMatch = @"GabPC\\Gab";
      psq.PathToMatch = "~/default.aspx";
      psq.UserInactiveSinceDate = DateTime.UtcNow.AddMinutes(1);
      var collection = p.FindState(PersonalizationScope.User, psq, 1, 1, out totalRecords);
      Assert.That(totalRecords, Is.EqualTo(1));
    }

    [Test]
    public void CanGetCountofStateForUser()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;
      var psq = new PersonalizationStateQuery();
      psq.UsernameToMatch = @"GabPC\\Gab";
      psq.PathToMatch = "~/default.aspx";
      psq.UserInactiveSinceDate = DateTime.UtcNow.AddMinutes(1);
      //System.Threading.Thread.Sleep(1000);
      totalRecords = p.GetCountOfState(PersonalizationScope.User, psq);
      Assert.That(totalRecords, Is.EqualTo(1));
    }

    [Test]
    public void CanGetCountofStateForAllUsers()
    {
      CreateDataForSharedScope();
      var p = InitPersonalizationProvider();
      int totalRecords;
      var psq = new PersonalizationStateQuery();
      psq.PathToMatch = "~/default.aspx";
      psq.UserInactiveSinceDate = DateTime.UtcNow;
      totalRecords = p.GetCountOfState(PersonalizationScope.Shared, psq);
      Assert.That(totalRecords, Is.EqualTo(1));
    }

    [Test]
    public void CanResetStateForUser()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;
      string[] paths = new string[1];
      paths[0] = "~/default.aspx";

      string[] users = new string[1];
      users[0] = @"GabPC\Gab";

      totalRecords = p.ResetState(PersonalizationScope.User, paths, users);
      Assert.That(totalRecords, Is.EqualTo(1));
    }

    [Test]
    public void CanResetStateForAllUsers()
    {
      CreateDataForSharedScope();
      var p = InitPersonalizationProvider();

      string[] paths = new string[1];
      paths[0] = "~/default.aspx";

      string[] users = new string[1];
      users[0] = @"GabPC\Gab";

      int totalRecords;
      totalRecords = p.ResetState(PersonalizationScope.Shared, paths, users);
      Assert.That(totalRecords, Is.EqualTo(1));
    }

    [Test]
    public void CanResetAllState()
    {
      CreateDataForSharedScope();
      var p = InitPersonalizationProvider();

      int totalRecords;
      totalRecords = p.ResetState(PersonalizationScope.Shared, null, null);
      Assert.That(totalRecords, Is.EqualTo(1));
    }


    [Test]
    public void CanResetUsertState()
    {
      CreateDataForUserScope();
      var p = InitPersonalizationProvider();
      int totalRecords;

      totalRecords = p.ResetUserState("~/default.aspx", Convert.ToDateTime("2038-01-19 03:14:07.999999")); // TimeStamp MaxValue
      Assert.That(totalRecords, Is.EqualTo(1));
    }

  }
}
