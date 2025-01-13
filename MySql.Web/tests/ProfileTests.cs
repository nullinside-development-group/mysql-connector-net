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

using MySql.Web.Profile;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Web.Profile;

namespace MySql.Web.Tests
{
  public class ProfileTests : WebTestBase
  {
    private MySQLProfileProvider InitProfileProvider()
    {
      MySQLProfileProvider p = new MySQLProfileProvider();
      NameValueCollection config = new NameValueCollection();
      config.Add("connectionStringName", "LocalMySqlServer");
      config.Add("applicationName", "/");
      p.Initialize(null, config);
      return p;
    }
    [TearDown]
    public void Cleanup()
    {
      execSQL(@"delete from my_aspnet_applications;
                delete from my_aspnet_paths;
                delete from my_aspnet_users;
                 delete from my_aspnet_profiles;
                delete from my_aspnet_personalizationallusers;");
    }

    [Test]
    public void SettingValuesCreatesAnAppAndUserId()
    {

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", false);
      ctx.Add("UserName", "user1");

      SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
      SettingsProperty property1 = new SettingsProperty("color");
      property1.PropertyType = typeof(string);
      property1.Attributes["AllowAnonymous"] = true;
      SettingsPropertyValue value = new SettingsPropertyValue(property1);
      value.PropertyValue = "blue";
      values.Add(value);

      provider.SetPropertyValues(ctx, values);

      DataTable dt = FillTable("SELECT * FROM my_aspnet_applications");
      Assert.That(1 == dt.Rows.Count, "Rows count on table my_aspnet_applications is not 1");

      dt = FillTable("SELECT * FROM my_aspnet_users");
      Assert.That(1 == dt.Rows.Count, "Rows count on table my_aspnet_users is not 1");


      dt = FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.That(1 == dt.Rows.Count, "Rows count on table my_aspnet_profiles is not 1");

      values["color"].PropertyValue = "green";
      provider.SetPropertyValues(ctx, values);

      dt = FillTable("SELECT * FROM my_aspnet_applications");
      Assert.That(1 == dt.Rows.Count, "Rows count on table my_aspnet_applications is not 1 after setting property");

      dt = FillTable("SELECT * FROM my_aspnet_users");
      Assert.That(1 == dt.Rows.Count, "Rows count on table my_aspnet_users is not 1 after setting property");

      dt = FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.That(1 == dt.Rows.Count, "Rows count on table my_aspnet_profiles is not 1 after setting property");
    }

    [Test]
    public void AnonymousUserSettingNonAnonymousProperties()
    {
      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", false);
      ctx.Add("UserName", "user1");

      SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
      SettingsProperty property1 = new SettingsProperty("color");
      property1.PropertyType = typeof(string);
      property1.Attributes["AllowAnonymous"] = false;
      SettingsPropertyValue value = new SettingsPropertyValue(property1);
      value.PropertyValue = "blue";
      values.Add(value);

      provider.SetPropertyValues(ctx, values);

      DataTable dt = FillTable("SELECT * FROM my_aspnet_applications");
      Assert.That(0 == dt.Rows.Count, "Table my_aspnet_applications Rows is not 0");

      dt = FillTable("SELECT * FROM my_aspnet_users");
      Assert.That(0 == dt.Rows.Count, "Table my_aspnet_users Rows is not 0");

      dt = FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.That(0 == dt.Rows.Count, "Table my_aspnet_profiles Rows is not 0");

    }

    [Test]
    public void StringCollectionAsProperty()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      StringCollection colors = new StringCollection();
      colors.Add("red");
      colors.Add("green");
      colors.Add("blue");
      profile["FavoriteColors"] = colors;
      profile.Save();

      DataTable dt = FillTable("SELECT * FROM my_aspnet_applications");
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      dt = FillTable("SELECT * FROM my_aspnet_users");
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      dt = FillTable("SELECT * FROM my_aspnet_profiles");
      Assert.That(dt.Rows.Count, Is.EqualTo(1));

      // now retrieve them
      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("FavoriteColors");
      getProp1.PropertyType = typeof(StringCollection);
      getProp1.SerializeAs = SettingsSerializeAs.Xml;
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");
      SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
      Assert.That(getValues.Count, Is.EqualTo(1));
      SettingsPropertyValue getValue1 = getValues["FavoriteColors"];
      StringCollection outValue = (StringCollection)getValue1.PropertyValue;
      Assert.That(outValue.Count, Is.EqualTo(3));
      Assert.That(outValue[0], Is.EqualTo("red"));
      Assert.That(outValue[1], Is.EqualTo("green"));
      Assert.That(outValue[2], Is.EqualTo("blue"));
    }

    [Test]
    public void AuthenticatedDateTime()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      DateTime date = DateTime.Now;
      profile["BirthDate"] = date;
      profile.Save();

      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("BirthDate");
      getProp1.PropertyType = typeof(DateTime);
      getProp1.SerializeAs = SettingsSerializeAs.Xml;
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");

      SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
      Assert.That(getValues.Count, Is.EqualTo(1));
      SettingsPropertyValue getValue1 = getValues["BirthDate"];
      Assert.That(getValue1.PropertyValue, Is.EqualTo(date));
    }

    /// <summary>
    /// We have to manually reset the app id because our profile provider is loaded from
    /// previous tests but we are destroying our database between tests.  This means that
    /// our provider thinks we have an application in our database when we really don't.
    /// Doing this will force the provider to generate a new app id.
    /// Note that this is not really a problem in a normal app that is not destroying
    /// the database behind the back of the provider.
    /// </summary>
    /// <param name="p"></param>
    private void ResetAppId(MySQLProfileProvider p)
    {
      Type t = p.GetType();
      FieldInfo fi = t.GetField("app",
          BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.GetField);
      object appObject = fi.GetValue(p);
      Type appType = appObject.GetType();
      PropertyInfo pi = appType.GetProperty("Id");
      pi.SetValue(appObject, -1, null);
    }

    [Test, Order(4)]
    public void AuthenticatedStringProperty()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      profile["Name"] = "Fred Flintstone";
      profile.Save();

      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("Name");
      getProp1.PropertyType = typeof(String);
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");

      SettingsPropertyValueCollection getValues = provider.GetPropertyValues(ctx, getProps);
      Assert.That(getValues.Count, Is.EqualTo(1));
      SettingsPropertyValue getValue1 = getValues["Name"];
      Assert.That(getValue1.PropertyValue, Is.EqualTo("Fred Flintstone"));
    }

    /// <summary>
    /// Bug #41654	FindProfilesByUserName error into Connector .NET
    /// </summary>
    [Test]
    public void GetAllProfiles()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      ResetAppId(profile.Providers["MySqlProfileProvider"] as MySQLProfileProvider);
      profile["Name"] = "Fred Flintstone";
      profile.Save();

      SettingsPropertyCollection getProps = new SettingsPropertyCollection();
      SettingsProperty getProp1 = new SettingsProperty("Name");
      getProp1.PropertyType = typeof(String);
      getProps.Add(getProp1);

      MySQLProfileProvider provider = InitProfileProvider();
      SettingsContext ctx = new SettingsContext();
      ctx.Add("IsAuthenticated", true);
      ctx.Add("UserName", "foo");

      int total;
      ProfileInfoCollection profiles = provider.GetAllProfiles(
          ProfileAuthenticationOption.All, 0, 10, out total);
      Assert.That(total, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests deleting a user profile
    /// </summary>
    [Test]
    public void DeleteProfiles()
    {
      ProfileBase profile = ProfileBase.Create("foo", true);
      profile.SetPropertyValue("Name", "this is my name");
      profile.Save();
      profile = ProfileBase.Create("foo", true); // refresh profile from database
      Assert.That(profile.GetPropertyValue("Name"), Is.EqualTo("this is my name"));

      Assert.That(ProfileManager.DeleteProfiles(new string[] { "foo" }), Is.EqualTo(1));
      profile = ProfileBase.Create("foo", true); // refresh profile from database
      Assert.That(profile.GetPropertyValue("Name"), Is.Empty);
    }

  }
}
