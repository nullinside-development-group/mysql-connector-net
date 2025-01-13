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
using MySql.Web.Security;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Specialized;
using System.Web.Security;

namespace MySql.Web.Tests
{
  public class SimpleMembership : WebTestBase
  {
    private readonly string _userTable = "UserProfile";
    private readonly string _userIdColumn = "UserId";
    private readonly string _userNameColumn = "UserName";
    private readonly string _userName = "New User";
    private readonly string _pass = "password";
    private MySqlSimpleMembershipProvider _simpleProvider;
    private MySqlSimpleRoleProvider _simpleRoleProvider;

    public SimpleMembership()
    {
      _simpleProvider = new MySqlSimpleMembershipProvider();
      _simpleRoleProvider = new MySqlSimpleRoleProvider();

      var _config = new NameValueCollection();
      _config.Add("connectionStringName", "LocalMySqlServer");
      _config.Add("userTableName", "UserProfile");
      _config.Add("userIdColumn", "UserId");
      _config.Add("userNameColumn", "UserName");

      _simpleProvider.Initialize("Test", _config);
      _simpleRoleProvider.Initialize("TestRoleProvider", _config);

      MySqlWebSecurity.InitializeDatabaseConnection(ConnectionString, "MySqlSimpleMembership", _userTable, _userIdColumn, _userNameColumn, true, true);
    }

    [TearDown]
    public void Cleanup()
    {
      execSQL(@"delete from userprofile;
                delete from webpages_membership;");
    }

    [Test]
    public void CheckIfRoleNotExists()
    {
      var roleExists = _simpleRoleProvider.RoleExists("roleName");
      Assert.That(!roleExists);
    }

    [Test]
    public void CheckIfRoleExists()
    {
      if (!Roles.RoleExists("Administrator"))
      {
        _simpleRoleProvider.CreateRole("Administrator");
        var roleExists = _simpleRoleProvider.RoleExists("Administrator");
        Assert.That(roleExists);
      }
    }

    [Test]
    public void CreateUserAndAccountTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.That(MySqlWebSecurity.UserExists(_userName));
      var user = MySqlHelper.ExecuteDataRow(ConnectionString, string.Format("select * from {0} where {1} = '{2}'", _userTable, _userNameColumn, _userName));
      Assert.That(user, Is.Not.Null);
      Assert.That(user[_userNameColumn], Is.EqualTo(_userName));

      Assert.That(_simpleProvider.ValidateUser(_userName, _pass));
      //We need to mock the login because in that method there is a call to "FormsAuthentication.SetAuthCookie" which causes an "Object reference not set to an instance of an object" exception, because the test doesn't run on web application context
      //Assert.True(MySqlWebSecurity.Login(_userName, _pass));
    }

    //We need to mock this test because there is no data on Membership object, there is no user available because login doesn't add it to the context
    //[Test]
    public void ChangePasswordTest()
    {
      string newPass = "newpassword";
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.That(MySqlWebSecurity.UserExists(_userName));

      //We need to mock the login because in that method there is a call to "FormsAuthentication.SetAuthCookie" which causes an "Object reference not set to an instance of an object" exception, because the test doesn't run on web application context

      Assert.That(_simpleProvider.ValidateUser(_userName, _pass));
      //Assert.True(MySqlWebSecurity.Login(_userName, _pass));

      Assert.That(MySqlWebSecurity.ChangePassword(_userName, _pass, newPass));

      Assert.That(_simpleProvider.ValidateUser(_userName, newPass));
      //Assert.True(MySqlWebSecurity.Login(_userName, newPass));
    }

    [Test]
    public void ConfirmAccountWithTokenTest()
    {
      var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.That(MySqlWebSecurity.UserExists(_userName));
      Assert.That(MySqlWebSecurity.ConfirmAccount(token));
    }

    [Test]
    public void ConfirmAccountWithUserAndTokenTest()
    {
      var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.That(MySqlWebSecurity.UserExists(_userName));
      Assert.That(MySqlWebSecurity.ConfirmAccount(_userName, token));
    }

    [Test]
    public void ConfirmAccountWithoutTokenTest()
    {
      var token = "falsetoken";
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.That(MySqlWebSecurity.UserExists(_userName));
      Assert.That(!MySqlWebSecurity.ConfirmAccount(token));
    }

    [Test]
    public void CreatedDateTest()
    {
      execSQL(@"delete from userprofile;
                delete from webpages_membership;");
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.That(MySqlWebSecurity.GetCreateDate(_userName), Is.Not.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void DeleteTest()
    {
      _simpleProvider.CreateUserAndAccount(_userName, _pass, false, null);
      Assert.That(_simpleProvider.DeleteAccount(_userName));
      _simpleProvider.CreateAccount(_userName, _pass, false);
      Assert.That(_simpleProvider.DeleteUser(_userName, true));
    }

    [Test]
    public void UserIsConfirmedTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.That(!MySqlWebSecurity.IsConfirmed(_userName));
    }

    [Test]
    public void UserIsLockedOutTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      Assert.That(!MySqlWebSecurity.IsAccountLockedOut(_userName, 5, 60));
    }

    [Test]
    public void PasswordTest()
    {
      MySqlWebSecurity.CreateUserAndAccount(_userName, _pass);
      Assert.That(MySqlWebSecurity.GetLastPasswordFailureDate(_userName), Is.EqualTo(DateTime.MinValue));
      Assert.That(MySqlWebSecurity.GetPasswordChangedDate(_userName), Is.Not.EqualTo(DateTime.MinValue));
      Assert.That(MySqlWebSecurity.GetPasswordFailuresSinceLastSuccess(_userName), Is.EqualTo(0));
    }

    //Password reset token must be assigned to the user but that field is not added in any part of the code, so maybe that field must be handled manually by the user
    // should we handle this functionality? WebMatrix.WebData.SimpleMembershipProvider doesn't handle it
    [Ignore("Fix Me")]
    public void PasswordResetTokenTest()
    {
      var token = MySqlWebSecurity.CreateUserAndAccount(_userName, _pass, null, true);
      int userID = MySqlWebSecurity.GetUserId(_userName);
      Assert.That(MySqlWebSecurity.ConfirmAccount(token));
      var newToken = MySqlWebSecurity.GeneratePasswordResetToken(_userName, 1440);
      Assert.That(newToken, Is.Not.EqualTo(null));
      Assert.That(userID, Is.EqualTo(MySqlWebSecurity.GetUserIdFromPasswordResetToken(newToken)));
    }

    #region WL14389 
    [TestCase(1, "Waren", null)]
    [TestCase(2, "Bill", false)]
    [TestCase(3, "Steve", true)]
    public void CreateUserAccountWithAttributes(int id, string userN, bool? token)
    {
      //Bug25046364 
      switch (token)
      {
        case null:
          MySqlWebSecurity.CreateUserAndAccount(userN, _pass, new {UserId = id, UserName = userN}); 
          break;

        default:
          MySqlWebSecurity.CreateUserAndAccount(userN, _pass, new { UserId = id, UserName = userN }, (bool)token);
          break;
      }

      Assert.That(MySqlWebSecurity.UserExists(userN));
      var user = MySqlHelper.ExecuteDataRow(ConnectionString, string.Format("select * from {0} where {1} = '{2}'", _userTable, _userNameColumn, userN));
      Assert.That(user, Is.Not.Null);
      Assert.That(user[_userNameColumn], Is.EqualTo(userN));
    }
    #endregion

  }
}
