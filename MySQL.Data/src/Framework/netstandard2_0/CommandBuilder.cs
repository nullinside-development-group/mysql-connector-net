// Copyright © 2004, 2025, Oracle and/or its affiliates.
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
using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  ///  Automatically generates single-table commands used to reconcile changes made to a <see cref="DataSet"/> with the associated MySQL database.
  ///  This class cannot be inherited.
  ///</summary>
  ///<remarks>
  ///  <para>
  ///    The <see cref="MySqlDataAdapter"/> does not automatically generate the SQL statements required to
  ///    reconcile changes made to a <see cref="DataSet"/> with the associated instance of MySQL.
  ///    However, you can create a <see cref="MySqlCommandBuilder"/> object to automatically generate SQL statements for
  ///    single-table updates if you set the <see cref="MySqlDataAdapter.SelectCommand"/> property
  ///    of the <see cref="MySqlDataAdapter"/>. Then, any additional SQL statements that you do not set are generated by the
  ///    <see cref="MySqlCommandBuilder"/>.
  ///  </para>
  ///  <para>
  ///    The <see cref="MySqlCommandBuilder"/> registers itself as a listener for <see cref="MySqlDataAdapter.OnRowUpdating">RowUpdating</see>
  ///    events whenever you set the <see cref="DataAdapter"/> property. You can only associate one
  ///    <see cref="MySqlDataAdapter"/> or <see cref="MySqlCommandBuilder"/> object with each other at one time.
  ///  </para>
  ///  <para>
  ///    To generate INSERT, UPDATE, or DELETE statements, the <see cref="MySqlCommandBuilder"/> uses the
  ///    <see cref="DbDataAdapter.SelectCommand"/> property to retrieve a required set of metadata automatically. If you change
  ///    the <see cref="DbDataAdapter.SelectCommand"/> after the metadata has is retrieved (for example, after the first update), you
  ///    should call the <see cref="RefreshSchema"/> method to update the metadata.
  ///  </para>
  ///  <para>
  ///    The <see cref="DbDataAdapter.SelectCommand"/> must also return at least one primary key or unique
  ///    column. If none are present, an <see cref="InvalidOperationException"/> exception is generated,
  ///    and the commands are not generated.
  ///  </para>
  ///  <para>
  ///    The <see cref="MySqlCommandBuilder"/> also uses the <see cref="MySqlCommand.Connection"/>,
  ///    <see cref="MySqlCommand.CommandTimeout"/>, and <see cref="MySqlCommand.Transaction"/>
  ///    properties referenced by the <see cref="DbDataAdapter.SelectCommand"/>. The user should call
  ///    <see cref="DbCommandBuilder.RefreshSchema"/> if any of these properties are modified, or if the
  ///    <see cref="DbDataAdapter.SelectCommand"/> itself is replaced. Otherwise the <see cref="MySqlDataAdapter.InsertCommand"/>,
  ///    <see cref="MySqlDataAdapter.UpdateCommand"/>, and <see cref="MySqlDataAdapter.DeleteCommand"/> properties retain
  ///    their previous values.
  ///  </para>
  ///  <para>
  ///    If you call <see cref="DbCommandBuilder.Dispose(bool)"/>, the <see cref="MySqlCommandBuilder"/> is disassociated
  ///    from the <see cref="MySqlDataAdapter"/>, and the generated commands are no longer used.
  ///  </para>
  /// </remarks>
  /// <example>
  ///  The	following example uses the <see cref="MySqlCommand"/>, along
  ///  <see cref="MySqlDataAdapter"/> and <see cref="MySqlConnection"/>, to
  ///  select rows from a data source. The example is passed an initialized
  ///  <see cref="DataSet"/>, a connection string, a
  ///  query string that is a SQL SELECT statement, and a string that is the
  ///  name of the database table. The example then creates a <see cref="MySqlCommandBuilder"/>.
  ///  <code >
  ///    public static DataSet SelectRows(string myConnection, string mySelectQuery, string myTableName)
  ///    {
  ///      MySqlConnection myConn = new MySqlConnection(myConnection);
  ///      MySqlDataAdapter myDataAdapter = new MySqlDataAdapter();
  ///      myDataAdapter.SelectCommand = new MySqlCommand(mySelectQuery, myConn);
  ///      MySqlCommandBuilder cb = new MySqlCommandBuilder(myDataAdapter);
  ///      
  ///      myConn.Open();
  ///      
  ///      DataSet ds = new DataSet();
  ///      myDataAdapter.Fill(ds, myTableName);
  ///      
  ///      ///code to modify data in DataSet here
  ///      ///Without the MySqlCommandBuilder this line would fail
  ///      myDataAdapter.Update(ds, myTableName);
  ///      myConn.Close();
  ///      return ds;
  ///    }
  ///  </code>
  ///</example>
  [ToolboxItem(false)]
  [System.ComponentModel.DesignerCategory("Code")]
  public sealed class MySqlCommandBuilder : DbCommandBuilder
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlCommandBuilder"/> class.
    /// </summary>
    public MySqlCommandBuilder()
    {
      QuotePrefix = QuoteSuffix = "`";
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="MySqlCommandBuilder"/> class with the associated <see cref="MySqlDataAdapter"/> object.
    /// </summary>
    /// <param name="adapter">The <see cref="MySqlDataAdapter"/> to use.</param>
    /// <remarks>
    ///  <para>
    ///    The <see cref="MySqlCommandBuilder"/> registers itself as a listener for
    ///    <see cref="MySqlDataAdapter.RowUpdating"/> events that are generated by the
    ///    <see cref="MySqlDataAdapter"/> specified in this property.
    ///  </para>
    ///  <para>
    ///    When you create a new instance <see cref="MySqlCommandBuilder"/>, any existing
    ///    <see cref="MySqlCommandBuilder"/> associated with this <see cref="MySqlDataAdapter"/> is released.
    ///  </para>
    /// </remarks>
    public MySqlCommandBuilder(MySqlDataAdapter adapter)
      : this()
    {
      DataAdapter = adapter;
    }

    /// <summary>
    ///  Gets or sets a <see cref="MySqlDataAdapter"/> object for which SQL statements are automatically generated.
    /// </summary>
    /// <value>
    ///  A <see cref="MySqlDataAdapter"/> object.
    /// </value>
    /// <remarks>
    ///  <para>
    ///    The <see cref="MySqlCommandBuilder"/> registers itself as a listener for
    ///    <see cref="MySqlDataAdapter.RowUpdating"/> events that are generated by the
    ///    <see cref="MySqlDataAdapter"/> specified in this property.
    ///  </para>
    ///  <para>
    ///    When you create a new instance <see cref="MySqlCommandBuilder"/>, any existing
    ///    <see cref="MySqlCommandBuilder"/> associated with this <see cref="MySqlDataAdapter"/>
    ///    is released.
    ///  </para>
    /// </remarks>
    public new MySqlDataAdapter DataAdapter
    {
      get { return (MySqlDataAdapter)base.DataAdapter; }
      set { base.DataAdapter = value; }
    }

    #region Public Methods

    /// <summary>
    /// Retrieves parameter information from the stored procedure specified in the <see cref="MySqlCommand"/> 
    /// and populates the Parameters collection of the specified <see cref="MySqlCommand"/> object.
    /// This method is not currently supported since stored procedures are not available in MySQL.
    /// </summary>
    /// <param name="command">The <see cref="MySqlCommand"/> referencing the stored 
    /// procedure from which the parameter information is to be derived. The derived parameters are added to the Parameters collection of the 
    /// <see cref="MySqlCommand"/>.</param>
    /// <exception cref="InvalidOperationException">The command text is not a valid stored procedure name.</exception>
    public static void DeriveParameters(MySqlCommand command)
    {
      if (command.CommandType != CommandType.StoredProcedure)
        throw new InvalidOperationException(Resources.CanNotDeriveParametersForTextCommands);

      // retrieve the proc definition from the cache.
      string spName = StoredProcedure.FixProcedureName(command.CommandText);

      try
      {
        ProcedureCacheEntry entry = command.Connection.ProcedureCache.GetProcedureAsync(command.Connection, spName, null, false).GetAwaiter().GetResult();
        command.Parameters.Clear();
        foreach (MySqlSchemaRow row in entry.parameters.Rows)
        {
          MySqlParameter p = new MySqlParameter();
          p.ParameterName = String.Format("@{0}", row["PARAMETER_NAME"]);
          if (row["ORDINAL_POSITION"].Equals(0) && p.ParameterName == "@")
            p.ParameterName = "@RETURN_VALUE";
          p.Direction = GetDirection(row);
          bool unsigned = StoredProcedure.GetFlags(row["DTD_IDENTIFIER"].ToString()).IndexOf("UNSIGNED") != -1;
          bool real_as_float = entry.procedure.Rows[0]["SQL_MODE"].ToString().IndexOf("REAL_AS_FLOAT") != -1;
          p.MySqlDbType = MetaData.NameToType(row["DATA_TYPE"].ToString(),
            unsigned, real_as_float, command.Connection);
          if (row["CHARACTER_MAXIMUM_LENGTH"] != null && row["CHARACTER_MAXIMUM_LENGTH"] != System.DBNull.Value)
            p.Size = Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]);
          if (row["NUMERIC_PRECISION"] != null && row["NUMERIC_PRECISION"] != System.DBNull.Value)
            p.Precision = Convert.ToByte(row["NUMERIC_PRECISION"]);
          if (row["NUMERIC_SCALE"] != null && row["NUMERIC_SCALE"] != System.DBNull.Value)
            p.Scale = Convert.ToByte(row["NUMERIC_SCALE"]);
          if (p.MySqlDbType == MySqlDbType.Set || p.MySqlDbType == MySqlDbType.Enum)
            p.PossibleValues = GetPossibleValues(row);
          command.Parameters.Add(p);
        }
      }
      catch (InvalidOperationException ioe)
      {
        throw new MySqlException(Resources.UnableToDeriveParameters, ioe);
      }
    }

    private static List<string> GetPossibleValues(MySqlSchemaRow row)
    {
      string[] types = new string[] { "ENUM", "SET" };
      string dtdIdentifier = row["DTD_IDENTIFIER"].ToString().Trim();

      int index = 0;
      for (; index < 2; index++)
        if (dtdIdentifier.StartsWith(types[index], StringComparison.OrdinalIgnoreCase))
          break;
      if (index == 2) return null;
      dtdIdentifier = dtdIdentifier.Substring(types[index].Length).Trim();
      dtdIdentifier = dtdIdentifier.Trim('(', ')').Trim();

      List<string> values = new List<string>();
      MySqlTokenizer tokenzier = new MySqlTokenizer(dtdIdentifier);
      string token = tokenzier.NextToken();
      int start = tokenzier.StartIndex;
      while (true)
      {
        if (token == null || token == ",")
        {
          int end = dtdIdentifier.Length - 1;
          if (token == ",")
            end = tokenzier.StartIndex;

          string value = dtdIdentifier.Substring(start, end - start).Trim('\'', '\"').Trim();
          values.Add(value);
          start = tokenzier.StopIndex;
        }
        if (token == null) break;
        token = tokenzier.NextToken();
      }
      return values;
    }

    private static ParameterDirection GetDirection(MySqlSchemaRow row)
    {
      string mode = row["PARAMETER_MODE"].ToString();
      int ordinal = Convert.ToInt32(row["ORDINAL_POSITION"]);

      if (0 == ordinal)
        return ParameterDirection.ReturnValue;
      else if (mode == "IN")
        return ParameterDirection.Input;
      else if (mode == "OUT")
        return ParameterDirection.Output;
      return ParameterDirection.InputOutput;
    }

    /// <summary>
    /// Gets the delete command.
    /// </summary>
    /// <returns>The <see cref="MySqlCommand"/> object required to perform deletions.</returns>
    public new MySqlCommand GetDeleteCommand()
    {
      return (MySqlCommand)base.GetDeleteCommand();
    }

    /// <summary>
    /// Gets the update command.
    /// </summary>
    /// <returns>The <see cref="MySqlCommand"/> object required to perform updates.</returns>
    public new MySqlCommand GetUpdateCommand()
    {
      return (MySqlCommand)base.GetUpdateCommand();
    }

    /// <summary>
    /// Gets the insert command.
    /// </summary>
    /// <returns>The <see cref="MySqlCommand"/> object required to perform inserts.</returns>
    public new MySqlCommand GetInsertCommand()
    {
      return (MySqlCommand)GetInsertCommand(false);
    }

    /// <summary>
    /// Given an unquoted identifier in the correct catalog case, returns the correct quoted form of that identifier,
    /// including properly escaping any embedded quotes in the identifier.
    /// </summary>
    /// <param name="unquotedIdentifier">The original unquoted identifier.</param>
    /// <returns>The quoted version of the identifier. Embedded quotes within the identifier are properly escaped.</returns>
    /// <exception cref="ArgumentNullException">If the <i>unquotedIdentifier</i> is null.</exception>
    public override string QuoteIdentifier(string unquotedIdentifier)
    {
      if (unquotedIdentifier == null) throw new
        ArgumentNullException("unquotedIdentifier");

      // don't quote again if it is already quoted
      if (unquotedIdentifier.StartsWith(QuotePrefix) &&
        unquotedIdentifier.EndsWith(QuoteSuffix))
        return unquotedIdentifier;

      unquotedIdentifier = unquotedIdentifier.Replace(QuotePrefix, QuotePrefix + QuotePrefix);

      return String.Format("{0}{1}{2}", QuotePrefix, unquotedIdentifier, QuoteSuffix);
    }

    /// <summary>
    /// Given a quoted identifier, returns the correct unquoted form of that identifier,
    /// including properly un-escaping any embedded quotes in the identifier.
    /// </summary>
    /// <param name="quotedIdentifier">The identifier that will have its embedded quotes removed.</param>
    /// <returns>The unquoted identifier, with embedded quotes properly un-escaped.</returns>
    /// <exception cref="ArgumentNullException">If the <i>quotedIdentifier</i> is null.</exception>
    public override string UnquoteIdentifier(string quotedIdentifier)
    {
      if (quotedIdentifier == null) throw new
        ArgumentNullException("quotedIdentifier");

      // don't unquote again if it is already unquoted
      if (!quotedIdentifier.StartsWith(QuotePrefix) ||
        !quotedIdentifier.EndsWith(QuoteSuffix))
        return quotedIdentifier;

      if (quotedIdentifier.StartsWith(QuotePrefix))
        quotedIdentifier = quotedIdentifier.Substring(1);
      if (quotedIdentifier.EndsWith(QuoteSuffix))
        quotedIdentifier = quotedIdentifier.Substring(0, quotedIdentifier.Length - 1);

      quotedIdentifier = quotedIdentifier.Replace(QuotePrefix + QuotePrefix, QuotePrefix);

      return quotedIdentifier;
    }

    #endregion

    /// <summary>
    /// Returns the schema table for the <see cref="MySqlCommandBuilder"/>
    /// </summary>
    /// <param name="sourceCommand">The <see cref="DbCommand"/> for which to retrieve the corresponding schema table.</param>
    /// <returns>A <see cref="DataTable"/> that represents the schema for the specific <see cref="DbCommand"/>.</returns>
    protected override DataTable GetSchemaTable(DbCommand sourceCommand)
    {
      DataTable schemaTable = base.GetSchemaTable(sourceCommand);

      foreach (DataRow row in schemaTable.Rows)
        if (row["BaseSchemaName"].Equals(sourceCommand.Connection.Database))
          row["BaseSchemaName"] = null;

      return schemaTable;
    }

    /// <summary>
    /// Returns the full parameter name, given the partial parameter name.
    /// </summary>
    /// <param name="parameterName">The partial name of the parameter.</param>
    /// <returns>The full parameter name corresponding to the partial parameter name requested.</returns>
    protected override string GetParameterName(string parameterName)
    {
      StringBuilder sb = new StringBuilder(parameterName);
      sb.Replace(" ", "");
      sb.Replace("/", "_per_");
      sb.Replace("-", "_");
      sb.Replace(")", "_cb_");
      sb.Replace("(", "_ob_");
      sb.Replace("%", "_pct_");
      sb.Replace("<", "_lt_");
      sb.Replace(">", "_gt_");
      sb.Replace(".", "_pt_");
      return String.Format("@{0}", sb.ToString());
    }

    /// <summary>
    /// Allows the provider implementation of the <see cref="DbCommandBuilder"/> class to handle additional parameter properties.
    /// </summary>
    /// <param name="parameter">A <see cref="DbParameter"/> to which the additional modifications are applied.</param>
    /// <param name="row">The <see cref="DataRow"/> from the schema table provided by <see cref="GetSchemaTable(DbCommand)"/>.</param>
    /// <param name="statementType">The type of command being generated; INSERT, UPDATE or DELETE.</param>
    /// <param name="whereClause">true if the parameter is part of the update or delete WHERE clause, 
    /// false if it is part of the insert or update values.</param>
    protected override void ApplyParameterInfo(DbParameter parameter, DataRow row,
      StatementType statementType, bool whereClause)
    {
      ((MySqlParameter)parameter).MySqlDbType = (MySqlDbType)row["ProviderType"];
    }

    /// <summary>
    /// Returns the name of the specified parameter in the format of @p#. Use when building a custom command builder.
    /// </summary>
    /// <param name="parameterOrdinal">The number to be included as part of the parameter's name.</param>
    /// <returns>The name of the parameter with the specified number appended as part of the parameter name.</returns>
    protected override string GetParameterName(int parameterOrdinal)
    {
      return String.Format("@p{0}", parameterOrdinal.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns the placeholder for the parameter in the associated SQL statement.
    /// </summary>
    /// <param name="parameterOrdinal">The number to be included as part of the parameter's name.</param>
    /// <returns>The name of the parameter with the specified number appended.</returns>
    protected override string GetParameterPlaceholder(int parameterOrdinal)
    {
      return String.Format("@p{0}", parameterOrdinal.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Registers the <see cref="MySqlCommandBuilder"/> to handle the <see cref="RowUpdating"/>
    /// event for a <see cref="DbDataAdapter"/>.
    /// </summary>
    /// <param name="adapter"></param>
    protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
    {
      MySqlDataAdapter myAdapter = (adapter as MySqlDataAdapter);
      if (adapter != base.DataAdapter)
        myAdapter.RowUpdating += new MySqlRowUpdatingEventHandler(RowUpdating);
      else
        myAdapter.RowUpdating -= new MySqlRowUpdatingEventHandler(RowUpdating);
    }

    private void RowUpdating(object sender, MySqlRowUpdatingEventArgs args)
    {
      base.RowUpdatingHandler(args);
    }

  }
}
