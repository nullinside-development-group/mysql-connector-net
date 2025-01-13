// Copyright © 2018, 2025, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySqlX.XDevAPI;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MySql.Data.MySqlClient.Tests
{
  public class SslTests : TestBase
  {
    private string _sslCa;
    private string _sslCert;
    private string _sslKey;

    public SslTests()
    {
      string cPath = TestContext.CurrentContext.TestDirectory + Path.DirectorySeparatorChar;

      _sslCa = cPath + "ca.pem";
      _sslCert = cPath + "client-cert.pem";
      _sslKey = cPath + "client-key.pem";
    }

    #region General

    [Test]
    [Property("Category", "Security")]
    public void SslModePreferredByDefault()
    {
      MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", Connection);
      using (MySqlDataReader reader = command.ExecuteReader())
      {
        Assert.That(reader.Read());
        Assert.That(reader.GetString(1), Does.StartWith("TLSv1"));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslModeOverriden()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Disabled;
      builder.AllowPublicKeyRetrieval = true;
      builder.Database = "";
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1), Is.EqualTo(string.Empty));
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void RepeatedSslConnectionOptions()
    {
      string repeatedOption = "foo";

      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      var conn = new MySqlConnection($"{builder.ConnectionString};sslca={repeatedOption}");
      Assert.That(conn.Settings.SslCa, Is.EqualTo(repeatedOption));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.CertificateFile = _sslCa;
      conn = new MySqlConnection($"{builder.ConnectionString};certificatefile={repeatedOption}");
      Assert.That(conn.Settings.CertificateFile, Is.EqualTo(repeatedOption));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      conn = new MySqlConnection($"{builder.ConnectionString};certificatefile={repeatedOption}");
      Assert.That(conn.Settings.CertificateFile, Is.EqualTo(repeatedOption));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.CertificateFile = _sslCa;
      conn = new MySqlConnection($"{builder.ConnectionString};sslca={repeatedOption}");
      Assert.That(conn.Settings.SslCa, Is.EqualTo(repeatedOption));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      conn = new MySqlConnection($"{builder.ConnectionString};sslcert={_sslCert};sslkey={_sslKey};sslca={repeatedOption}");
      Assert.That(conn.Settings.SslCa, Is.EqualTo(repeatedOption));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.CertificatePassword = "pass";
      conn = new MySqlConnection($"{builder.ConnectionString};certificatepassword={repeatedOption}");
      Assert.That(conn.Settings.CertificatePassword, Is.EqualTo(repeatedOption));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCert = _sslCert;
      conn = new MySqlConnection($"{builder.ConnectionString};sslcert={repeatedOption}");
      Assert.That(conn.Settings.SslCert, Is.EqualTo(repeatedOption));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslKey = _sslKey;
      conn = new MySqlConnection($"{builder.ConnectionString};sslkey={repeatedOption}");
      Assert.That(conn.Settings.SslKey, Is.EqualTo(repeatedOption));
    }

    [Test]
    [Property("Category", "Security")]
    public void SslOptionsWhenSslDisabled()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Disabled;
      builder.SslCa = "value";
      Assert.DoesNotThrow(() => new MySqlConnection(builder.ConnectionString));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Disabled;
      builder.SslCert = "value";
      Assert.DoesNotThrow(() => new MySqlConnection(builder.ConnectionString));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Disabled;
      builder.SslKey = "value";
      Assert.DoesNotThrow(() => new MySqlConnection(builder.ConnectionString));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Disabled;
      builder.CertificateFile = "value";
      Assert.DoesNotThrow(() => new MySqlConnection(builder.ConnectionString));

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Disabled;
      builder.CertificatePassword = "value";
      Assert.DoesNotThrow(() => new MySqlConnection(builder.ConnectionString));
    }

    [Test]
    [Property("Category", "Security")]
    public void SslPreferredByDefault()
    {
      MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", Connection);
      using (MySqlDataReader reader = command.ExecuteReader())
      {
        Assert.That(reader.Read());
        Assert.That(reader.GetString(1), Does.StartWith("TLSv1"));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslOverrided()
    {
      var cstrBuilder = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      cstrBuilder.SslMode = MySqlSslMode.Disabled;
      cstrBuilder.AllowPublicKeyRetrieval = true;
      cstrBuilder.Database = "";
      using (MySqlConnection connection = new MySqlConnection(cstrBuilder.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1), Is.EqualTo(string.Empty));
        }
      }
    }

    /// <summary>
    /// WL14811 - Remove support for TLS 1.0 and 1.1
    /// WL16176 - Add support for TLS 1.3
    /// </summary>
    [TestCase("[]", 1)]
    [TestCase("Tlsv1, Tlsv1.1", 2)]
    [TestCase("Tlsv1, foo", 2)]
    [TestCase("Tlsv1", 2)]
    [TestCase("Tlsv1.1", 2)]
    [TestCase("foo, bar", 3)]
    [TestCase("Tlsv1.0, Tlsv1.2", 0)]
    [TestCase("foo, Tlsv1.2", 0)]
    [TestCase("Tlsv1.3", 4)]
    [Property("Category", "Security")]
    public void TlsVersionTest(string tlsVersion, int error)
    {
      Assume.That(error != 4 || !Platform.IsMacOSX());

      var builder = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      void SetTlsVersion() { builder.TlsVersion = tlsVersion; }
      string ex;
      string tlsdefault = "TLSv1.2";

      switch (error)
      {
        case 1:
          ex = Assert.Throws<ArgumentException>(SetTlsVersion).Message;
          Assert.That(ex, Is.EqualTo(Resources.TlsVersionsEmpty).IgnoreCase);
          break;
        case 2:
          ex = Assert.Throws<ArgumentException>(SetTlsVersion).Message;
          Assert.That(ex, Is.EqualTo(Resources.TlsUnsupportedVersions).IgnoreCase);
          break;
        case 3:
          ex = Assert.Throws<ArgumentException>(SetTlsVersion).Message;
          Assert.That(ex, Is.EqualTo(Resources.TlsNonValidProtocols).IgnoreCase);
          break;
        default:
          SetTlsVersion();
          var conn = new MySqlConnection(builder.ConnectionString);

          using (conn)
          {
            conn.Open();
            Assert.That(conn.State, Is.EqualTo(ConnectionState.Open));

            MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SHOW SESSION STATUS LIKE 'ssl_version'";

            using MySqlDataReader dr = cmd.ExecuteReader();
            Assert.That(dr.Read());
            if (error==4)
              Assert.That(dr[1].ToString(), Is.EqualTo(tlsVersion).IgnoreCase);
            else
              Assert.That(dr[1].ToString(), Is.EqualTo(tlsdefault).IgnoreCase);
          }
          break;
      }
    }

    /// <summary>
    /// WL14811 - Remove support for TLS 1.0 and 1.1
    /// </summary>
    [TestCase("Tlsv1.0, Tlsv1.2")]
    [TestCase("foo, Tlsv1.2")]
    public void TlsVersionNoSslTest(string tlsVersion)
    {
      var builder = new MySqlConnectionStringBuilder(Connection.ConnectionString)
      {
        TlsVersion = tlsVersion,
        SslMode = MySqlSslMode.Disabled
      };

      using (var conn = new MySqlConnection(builder.ConnectionString))
      {
        conn.Open();
        Assert.That(conn.State, Is.EqualTo(ConnectionState.Open));

        MySqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SHOW SESSION STATUS LIKE 'ssl_version'";

        using MySqlDataReader dr = cmd.ExecuteReader();
        Assert.That(dr.Read());
        Assert.That(dr[1].ToString(), Is.Empty);
      }
    }
    #endregion

    #region PFX

    /// <summary>
    /// A client can connect to MySQL server using SSL and a pfx file.
    /// <remarks>
    /// This test requires starting the server with SSL support.
    /// For instance, the following command line enables SSL in the server:
    /// mysqld --no-defaults --standalone --console --ssl-ca='MySQLServerDir'\mysql-test\std_data\cacert.pem --ssl-cert='MySQLServerDir'\mysql-test\std_data\server-cert.pem --ssl-key='MySQLServerDir'\mysql-test\std_data\server-key.pem
    /// </remarks>
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void CanConnectUsingFileBasedCertificate()
    {
      string connstr = Connection.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1), Does.StartWith("TLSv1"));
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingPFXCertificates()
    {
      string connstr = Settings.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1), Does.StartWith("TLSv1"));
        }

        command = new MySqlCommand("show variables like 'tls_version'", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1), Does.Contain("TLS"));
        }
      }

      connstr = Settings.ConnectionString;
      connstr += ";CertificateFile=client-incorrect.pfx;CertificatePassword=pass;SSL Mode=" + MySqlSslMode.VerifyCA;
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        Assert.Catch(() => c.Open());
      }

      connstr = Settings.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=WRONGPASSWORD;SSL Mode=" + MySqlSslMode.VerifyCA;
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        Assert.Catch(() => c.Open());
      }
    }

    /// <summary>
    /// Bug#31954655 - NET CONNECTOR - THUMBPRINT OPTION DOES NOT CHECK THUMBPRINT
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void InvalidCertificateThumbprint()
    {
#if !NETFRAMEWORK
      Assume.That(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows));
#endif

      // Create a mock of certificate store
      string assemblyPath = TestContext.CurrentContext.TestDirectory;
      var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
      store.Open(OpenFlags.ReadWrite);
      var certificate = new X509Certificate2(assemblyPath + "\\client.pfx", "pass");
      store.Add(certificate);

      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      csb.CertificateStoreLocation = MySqlCertificateStoreLocation.CurrentUser;
      csb.CertificateThumbprint = "spaghetti";

      // Throws an exception with and invalid/incorrect Thumbprint
      var ex = Assert.Throws<MySqlException>(() => new MySqlConnection(csb.ConnectionString).Open());
      Assert.That(ex.Message, Is.EqualTo(string.Format(Resources.InvalidCertificateThumbprint, csb.CertificateThumbprint)));

      csb.CertificateThumbprint = certificate.Thumbprint;
      using (var conn = new MySqlConnection(csb.ConnectionString))
      {
        conn.Open();
        Assert.That(conn.connectionState, Is.EqualTo(ConnectionState.Open));
      }
    }

    #endregion

    #region PEM

    [Test]
    [Property("Category", "Security")]
    public void SslCertificateConnectionOptionsExistAndDefaultToNull()
    {
      var builder = new MySqlConnectionStringBuilder();

      // Options exist.
      Assert.That(builder.values.ContainsKey("sslca"));
      Assert.That(builder.values.ContainsKey("sslcert"));
      Assert.That(builder.values.ContainsKey("sslkey"));

      // Options default to null.
      Assert.That(builder["sslca"], Is.Null);
      Assert.That(builder["sslcert"], Is.Null);
      Assert.That(builder["sslkey"], Is.Null);
      Assert.That(builder["ssl-ca"], Is.Null);
      Assert.That(builder["ssl-cert"], Is.Null);
      Assert.That(builder["ssl-key"], Is.Null);
      Assert.That(builder.SslCa, Is.Null);
      Assert.That(builder.SslCert, Is.Null);
      Assert.That(builder.SslKey, Is.Null);

      // Null or whitespace options are ignored.
      var connectionString = $"host={Settings.Server};user={Settings.UserID};port={Settings.Port};password={Settings.Password};";
      builder = new MySqlConnectionStringBuilder(connectionString);
      builder.SslCa = null;
      builder.SslCert = string.Empty;
      builder.SslKey = "  ";
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        Assert.That(connection.Settings.SslCa, Is.Null);
        Assert.That(connection.Settings.SslCert, Is.Null);
        Assert.That(connection.Settings.SslKey, Is.EqualTo("  "));
        connection.Open();
        connection.Close();
      }

      // Failing to provide a value defaults to null.
      connectionString = $"{connectionString}sslca=;sslcert=;sslkey=";
      using (var connection = new MySqlConnection(connectionString))
      {
        Assert.That(connection.Settings.SslCa, Is.Null);
        Assert.That(connection.Settings.SslCert, Is.Null);
        Assert.That(connection.Settings.SslKey, Is.Null);
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslCaConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(Resources.FilePathNotSet, nameof(Settings.SslCa))));
      }

      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(Resources.FilePathNotSet, nameof(Settings.SslCa))));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslCertConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = string.Empty;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(Resources.FilePathNotSet, nameof(Settings.SslCert))));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslKeyConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(string.Format(Resources.FilePathNotSet, nameof(Settings.SslKey))));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslCaConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = "C:\\certs\\ca.pema";
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileNotFound));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslCertConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = "C:\\certs\\client-cert";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileNotFound));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslKeyConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = "file";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileNotFound));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingCaPemCertificate()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingAllCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyFull;
      builder.TlsVersion = "Tlsv1.2";
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_altered.pem");
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        try { connection.Open(); }
        catch (Exception ex)
        {
          Assert.That(ex is MySqlException ||
          ex is AuthenticationException ||
          ex is FormatException, ex.Message);
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslCaConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Preferred causign SslCa to be ignored.
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslCertandKeyConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslCert = null;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Required causing SslCa, SslCert and SslKey to be ignored.
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void AttemptConnectionWithDummyPemCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa.Replace("ca.pem", "ca_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileIsNotACertificate), $"Cert. path: {_sslCa}");
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert.Replace("client-cert.pem", "client-cert_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileIsNotACertificate));
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileIsNotAKey));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void AttemptConnectionWithSwitchedPemCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCert;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.SslCertificateIsNotCA));
      }

      builder.SslCa = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileIsNotACertificate));
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.InvalidSslCertificate));
      }

      builder.SslCert = _sslCert;
      builder.SslKey = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.That(exception.Message, Is.EqualTo(Resources.SslConnectionError));
        Assert.That(exception.InnerException.Message, Is.EqualTo(Resources.FileIsNotAKey));
      }
    }

    #endregion

    [Test, Description("Test session variables specific to clients ")]
    [Ignore("This test needs to be executed individually because server setup affects to other tests")]
    public void MaxConnectionsWithPersistAndGlobalVariables()
    {
      var connStr = $"server={Settings.Server};user={Settings.UserID};port={Settings.Port};password={Settings.Password};ssl-mode=required;ConnectionTimeout=5";
      var maxIterations = 7;
      ExecuteSQL("SET GLOBAL max_connections = 10;");
      List<MySqlConnection> connectionList = new();
      using (var conn = new MySqlConnection(connStr))
      {
        while (connectionList.Count() < maxIterations)
        {
          var c1 = new MySqlConnection(connStr);
          c1.Open();
          connectionList.Add(c1);
        }
        Exception ex = Assert.Throws<MySqlException>(() => conn.Open());
        Assert.That(ex.Message, Does.Contain("Too many connections"));
      }
      foreach (var item in connectionList)
      {
        item.Close();
        item.Dispose();
      }
      connectionList.Clear();

      ExecuteSQL("SET PERSIST max_connections = 10;");

      using (var conn = new MySqlConnection(connStr))
      {
        while (connectionList.Count() < maxIterations)
        {
          var c1 = new MySqlConnection(connStr);
          c1.Open();
          connectionList.Add(c1);
        }
        Exception ex = Assert.Throws<MySqlException>(() => conn.Open());
        Assert.That(ex.Message, Does.Contain("Too many connections"));
      }
      foreach (var item in connectionList)
      {
        item.Close();
        item.Dispose();
      }
      connectionList.Clear();

      ExecuteSQL("SET GLOBAL max_connections = 151", true);
    }

    [Test, Description("CONNECTOR/NET DOESN'T NEED TO RUN SHOW VARIABLES")]
    public void ShowVariablesRemoved()
    {
      using (var dbConn = new MySqlConnection(Settings.ConnectionString))
      {
        dbConn.Open();//Manually verify in server log that show variables is not present
        Assert.That(dbConn.State, Is.EqualTo(ConnectionState.Open));
      }
    }

    [Test]
    public void ConnectUsingCertificateFileAndTlsVersion()
    {
      Assume.That(Version >= new Version(8, 0, 16), "This test for MySql server 8.0.16 or higher");
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Required;
      builder.CertificateFile = "client.pfx";
      builder.CertificatePassword = "pass";
      builder.TlsVersion = "Tlsv1.2";
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand("show variables like '%tls_version%'", connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1), Does.Contain("TLSv1"));
        }

        cmd = new MySqlCommand("show status like 'Ssl_cipher'", connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1).ToString().Length > 0);
        }

        cmd = new MySqlCommand("show status like 'Ssl_version'", connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.That(reader.Read());
          Assert.That(reader.GetString(1), Is.EqualTo("TLSv1.2").IgnoreCase);
        }
      }
    }

    [Test, Description("Classic-Scenario(correct ssl-ca,wrong ssl-key/ssl-cert,ssl-mode required and default)")]
    public void CorrectSslcaWrongSslkeySslcertRequiredMode()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS");
      Assume.That(Version >= new Version(8, 0, 16), "This test for MySql server 8.0.16 or higher");
      string[] sslcertlist = new string[] { "", " ", null, "file", "file.pem" };
      string[] sslkeylist = new string[] { "", " ", null, "file", "file.pem" };
      for (int i = 0; i < sslcertlist.Length; i++)
      {

        var connClassic = new MySqlConnectionStringBuilder();
        connClassic.Server = Host;
        connClassic.Port = Convert.ToUInt32(Port);
        connClassic.UserID = Settings.UserID;
        connClassic.Password = Settings.Password;
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = _sslCa;
        connClassic.SslMode = MySqlSslMode.Required;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        }

        connClassic = new MySqlConnectionStringBuilder();
        connClassic.Server = Host;
        connClassic.Port = Convert.ToUInt32(Port);
        connClassic.UserID = Settings.UserID;
        connClassic.Password = Settings.Password;
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = _sslCa;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        }

        connClassic = new MySqlConnectionStringBuilder();
        connClassic.Server = Host;
        connClassic.Port = Convert.ToUInt32(Port);
        connClassic.UserID = Settings.UserID;
        connClassic.Password = Settings.Password;
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = _sslCa;
        connClassic.SslMode = MySqlSslMode.Prefered;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        }

        var conn = new MySqlXConnectionStringBuilder();
        conn.Server = Host;
        conn.Port = Convert.ToUInt32(Port);
        conn.UserID = Settings.UserID;
        conn.Password = Settings.Password;
        conn.SslCert = sslcertlist[i];
        conn.SslKey = sslkeylist[i];
        conn.SslCa = _sslCa;
        conn.SslMode = MySqlSslMode.Required;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        }

        conn = new MySqlXConnectionStringBuilder();
        conn.Server = Host;
        conn.Port = Convert.ToUInt32(Port);
        conn.UserID = Settings.UserID;
        conn.Password = Settings.Password;
        conn.SslCert = sslcertlist[i];
        conn.SslKey = sslkeylist[i];
        conn.SslCa = _sslCa;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        }
      }
    }

    [Test, Description("Clasic-Scenario(correct ssl-ca,no ssl-key/ssl-cert,ssl-mode required and default)")]
    public void CorrectSslcaNoSslkeyorCertRequiredMode()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS");
      Assume.That(Version >= new Version(8, 0, 16), "This test for MySql server 8.0.16 or higher");
      var connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      connClassic.SslMode = MySqlSslMode.Required;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
      }

      connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
      }

      connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      connClassic.SslMode = MySqlSslMode.Prefered;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
      }

      connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      connClassic.SslMode = MySqlSslMode.Preferred;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
      }

      var conn = new MySqlXConnectionStringBuilder();
      conn.Server = Host;
      conn.Port = Convert.ToUInt32(Port);
      conn.UserID = Settings.UserID;
      conn.Password = Settings.Password;
      conn.SslCa = _sslCa;
      conn.SslMode = MySqlSslMode.Required;
      using (var c = new MySqlConnection(conn.ConnectionString))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
      }

      conn = new MySqlXConnectionStringBuilder();
      conn.Server = Host;
      conn.Port = Convert.ToUInt32(Port);
      conn.UserID = Settings.UserID;
      conn.Password = Settings.Password;
      conn.SslCa = _sslCa;
      using (var c = new MySqlConnection(conn.ConnectionString))
      {
        c.Open();
        Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
      }
    }

    [Test, Description("checking different versions of TLS versions")]
    public void SecurityTlsCheck()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS");
      Assume.That(Version >= new Version(8, 0, 16), "This test for MySql server 8.0.16 or higher");
      Assume.That(ServerHaveSsl(), "Server doesn't have Ssl support");

      MySqlSslMode[] modes = { MySqlSslMode.Required, MySqlSslMode.VerifyCA, MySqlSslMode.VerifyFull };
      String[] version;
      var conStr = $"server={Host};port={Port};userid={Settings.UserID};password={Settings.Password};SslCa={_sslCa};SslCert={_sslCert};SslKey={_sslKey};ssl-ca-pwd=pass";

      foreach (MySqlSslMode mode in modes)
      {
        version = new string[] { "TLSv1.2" };
        foreach (string tlsVersion in version)
        {
          using (var conn = new MySqlConnection(conStr + ";ssl-mode=" + mode + ";tls-version=" + tlsVersion))
          {
            conn.Open();
            MySqlCommand cmd = new MySqlCommand("SELECT variable_value FROM performance_schema.session_status WHERE VARIABLE_NAME='Ssl_version'", conn);
            object result = cmd.ExecuteScalar();
            Assert.That(result, Is.EqualTo(tlsVersion));
          }
        }

        version = new string[] { "[TLSv1.1,TLSv1.2]", "[TLSv1,TLSv1.2]" };
        for (int i = 0; i < 2; i++)
        {
          using (var conn = new MySqlConnection(conStr + ";ssl-mode=" + mode + ";tls-version=" + version[i]))
          {
            // TLSv1.0 and TLSv1.1 has been deprecated in Ubuntu 20.04 so an exception is thrown
            try { conn.Open(); }
            catch (Exception ex) { Assert.That(ex is AuthenticationException); return; }
            MySqlCommand cmd = new MySqlCommand("SELECT variable_value FROM performance_schema.session_status WHERE VARIABLE_NAME='Ssl_version'", conn);
            object result = cmd.ExecuteScalar();
            Assert.That(result.ToString().StartsWith("TLSv1"));
          }
        }
      }
    }

    [Test, Description("checking errors when invalid values are used ")]
    public void InvalidTlsversionValues()
    {
      Assume.That(Version >= new Version(8, 0, 16), "This test for MySql server 8.0.16 or higher");
      Assume.That(ServerHaveSsl(), "Server doesn't have Ssl support");

      string[] version = new string[] { "null", "v1", "[ ]", "[TLSv1.9]", "[TLSv1.1,TLSv1.7]", "ui9" };//blank space is considered as default value
      var conStr = $"server={Host};port={Port};userid={Settings.UserID};password={Settings.Password};SslCa={_sslCa};SslCert={_sslCert};SslKey={_sslKey};ssl-ca-pwd=pass";
      MySqlConnection conn;
      foreach (string tlsVersion in version)
      {
        Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version={tlsVersion}"));
      }

      // Not supported protocols
      var ex = Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version=[TLSv1];tls-version=[TLSv1]"));
      Assert.That(ex.Message, Is.EqualTo("TLS protocols TLSv1 and TLSv1.1 are no longer supported. Accepted values are TLSv1.2 and TLSv1.3").IgnoreCase);

      // Repeated options are allowed
      Assert.DoesNotThrow(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version=[TLSv1.1,TLSv1.2];tls-version=[TLSv1.2]"));
    }

    [Test, Description("Default SSL user with SSL but without SSL Parameters")]
    public void SslUserWithoutSslParams()
    {
      Assume.That(ServerHaveSsl(), "Server doesn't have Ssl support");
      MySqlCommand cmd = new MySqlCommand();
      string connstr = $"server={Host};user={Settings.UserID};port={Port};password={Settings.Password};sslmode={MySqlSslMode.Disabled}";
      using (var c = new MySqlConnection(connstr))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "SHOW STATUS like 'ssl_cipher'";
        cmd.ExecuteNonQuery();
        var rdr1 = cmd.ExecuteReader();
        while (rdr1.Read())
          Assert.That(rdr1.GetValue(1).ToString().Trim() == "");
      }

      connstr = $"server={Host};user={Settings.UserID};port={Port};password=;sslmode={MySqlSslMode.Disabled}";
      using (var c = new MySqlConnection(connstr))
      {
        Assert.Throws<MySqlException>(() => c.Open());
      }
    }

    [Test]
    public void PositiveSslConnectionWithCertificates()
    {
      Assume.That(Version >= new Version(5, 7, 0), "This test for MySql server 5.7 or higher");
      Assume.That(ServerHaveSsl(), "Server doesn't have Ssl support");
      MySqlCommand cmd = new MySqlCommand();

      var connStr = $"server={Host};port={Port};user={Settings.UserID};password={Settings.Password};CertificateFile={_sslCa};CertificatePassword=pass;SSL Mode=Required;";
      using (var c = new MySqlConnection(connStr))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "show variables like 'tls_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.That(rdr.GetValue(1).ToString().Trim().Contains("TLSv1"));
          }
        }

        cmd.CommandText = "show status like 'Ssl_cipher'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.That(rdr.GetValue(1).ToString().Trim().Length > 0);
          }
        }

        cmd.CommandText = "show status like 'Ssl_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.That(rdr.GetValue(1).ToString().StartsWith("TLSv1"));
          }
        }
      }

      connStr = $"server={Host};port={Port};user={Settings.UserID};password={Settings.Password};sslcert={_sslCert};sslkey={_sslKey};sslca={_sslCa};SSL Mode=VerifyCA;";
      using (var c = new MySqlConnection(connStr))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "show status like 'Ssl_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.That(rdr.GetValue(1).ToString().Trim().Contains("TLSv1"));
          }
        }

        cmd.Connection = c;
        cmd.CommandText = "show status like 'Ssl_cipher'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.That(rdr.GetValue(1).ToString().Trim().Length > 0);
          }
        }
        cmd.Connection = c;
        cmd.CommandText = "show status like 'Ssl_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.That(rdr.GetValue(1).ToString().StartsWith("TLSv1"));
          }
        }
      }
    }

    /// <summary>
    /// WL14828 - Align TLS option checking across connectors
    /// </summary>
    [TestCase(MySqlSslMode.Disabled)]
    [TestCase(MySqlSslMode.Disabled, "ssl-ca=foo")]
    [TestCase(MySqlSslMode.Disabled, "ssl-cert=foo")]
    [TestCase(MySqlSslMode.Disabled, "ssl-key=foo")]
    [TestCase(MySqlSslMode.Disabled, "tls-version=TLSv1.2")]
    [TestCase(MySqlSslMode.Required, "ssl-mode=disabled")]
    [Theory]
    public void SslOptionsCombinedWhenDisabled(MySqlSslMode sslMode, string sslOption = "")
    {
      var connStr = Connection.ConnectionString + $";ssl-mode={sslMode};{sslOption}";

      using var conn = new MySqlConnection(connStr);
      conn.Open();
      var cmd = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_cipher'", conn);
      using var reader = cmd.ExecuteReader();
      reader.Read();
      string encryption = reader.GetString(1);

      Assert.That(encryption, Is.Empty);
      Assert.That(conn.State, Is.EqualTo(ConnectionState.Open));
    }

    [Test]
    public void SslRequiredOverDisabled()
    {
      var connStr = Connection.ConnectionString + $";ssl-mode={MySqlSslMode.Disabled};ssl-mode={MySqlSslMode.Required}";

      using var conn = new MySqlConnection(connStr);
      conn.Open();
      var cmd = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_cipher'", conn);
      using var reader = cmd.ExecuteReader();
      reader.Read();
      string encryption = reader.GetString(1);

      Assert.That(encryption, Is.Not.Empty);
      Assert.That(conn.State, Is.EqualTo(ConnectionState.Open));
    }

    /// <summary>
    /// Bug #33179908 [SSL CONNECTION FAILS WITH CHAINED CERTIFICATES]
    /// In order to execute this tests, it is needed to start the server with next configuration:
    /// --ssl-ca=<path>/ca/root.crt --ssl-key=<path>/server/server.key --ssl-cert=<path>/server/server.cachain
    /// The chained certificates (certificates.zip) are attached in the bug report.
    /// </summary>
    [Ignore("MySQL Server needs to start with special configuration.")]
    [TestCase(MySqlSslMode.VerifyCA)]
    [TestCase(MySqlSslMode.VerifyFull)]
    public void SslChainedCertificates(MySqlSslMode sslMode)
    {
      MySqlConnectionStringBuilder stringBuilder = new()
      {
        Server = "localhost",
        Port = 3306,
        UserID = "root",
        SslCa = @"<path_to_chained_certificates>\root.crt",
        SslCert = @"<path_to_chained_certificates>\client.cachain",
        SslKey = @"<path_to_chained_certificates>\client.key",
        SslMode = sslMode
      };

      using var conn = new MySqlConnection(stringBuilder.ConnectionString);
      conn.Open();

      Assert.That(conn.State == ConnectionState.Open);
    }

    #region Methods
    public bool ServerHaveSsl()
    {
      Dictionary<string, string> strValues = new();

      var commandList = new string[] { "show  status like '%Ssl_version'", "show variables like 'tls_version'" };
      foreach (var item in commandList)
      {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = item;
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            strValues[rdr.GetString(0)] = rdr.GetString(1);
          }
        }
      }
      return (strValues["Ssl_version"].StartsWith("TLS") && !string.IsNullOrEmpty(strValues["tls_version"])) ? true : false;
    }
    #endregion Methods
  }
}
