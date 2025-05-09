// Copyright © 2021, 2025, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;
using static MySql.EntityFrameworkCore.Utils.Statics;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLMathMethodTranslator : IMethodCallTranslator
  {
    private static readonly Dictionary<MethodInfo, string> _supportedMethodTranslations = new()
  {
    { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), new[] { typeof(decimal) })!, "ABS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), new[] { typeof(double) })!, "ABS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), new[] { typeof(float) })!, "ABS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), new[] { typeof(int) })!, "ABS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), new[] { typeof(long) })!, "ABS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Abs), new[] { typeof(short) })!, "ABS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Acos), new[] { typeof(double) })!, "ACOS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Asin), new[] { typeof(double) })!, "ASIN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Atan), new[] { typeof(double) })!, "ATAN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Atan2), new[] { typeof(double), typeof(double) })!, "ATAN2" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Ceiling), new[] { typeof(decimal) })!, "CEILING" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Ceiling), new[] { typeof(double) })!, "CEILING" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Cos), new[] { typeof(double) })!, "COS" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Exp), new[] { typeof(double) })!, "EXP" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Floor), new[] { typeof(decimal) })!, "FLOOR" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Floor), new[] { typeof(double) })!, "FLOOR" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Log), new[] { typeof(double) })!, "LOG" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Log), new[] { typeof(double), typeof(double) })!, "LOG" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Log10), new[] { typeof(double) })!, "LOG10" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Max), new[] { typeof(decimal), typeof(decimal) })!, "GREATEST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Max), new[] { typeof(double), typeof(double) })!, "GREATEST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Max), new[] { typeof(float), typeof(float) })!, "GREATEST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Max), new[] { typeof(int), typeof(int) })!, "GREATEST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Max), new[] { typeof(long), typeof(long) })!, "GREATEST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Max), new[] { typeof(short), typeof(short) })!, "GREATEST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Min), new[] { typeof(decimal), typeof(decimal) })!, "LEAST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Min), new[] { typeof(double), typeof(double) })!, "LEAST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Min), new[] { typeof(float), typeof(float) })!, "LEAST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Min), new[] { typeof(int), typeof(int) })!, "LEAST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Min), new[] { typeof(long), typeof(long) })!, "LEAST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Min), new[] { typeof(short), typeof(short) })!, "LEAST" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Pow), new[] { typeof(double), typeof(double) })!, "POWER" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Round), new[] { typeof(double) })!, "ROUND" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Round), new[] { typeof(double), typeof(int) })!, "ROUND" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Round), new[] { typeof(decimal) })!, "ROUND" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Round), new[] { typeof(decimal), typeof(int) })!, "ROUND" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), new[] { typeof(decimal) })!, "SIGN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), new[] { typeof(double) })!, "SIGN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), new[] { typeof(float) })!, "SIGN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), new[] { typeof(int) })!, "SIGN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), new[] { typeof(long) })!, "SIGN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), new[] { typeof(sbyte) })!, "SIGN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sign), new[] { typeof(short) })!, "SIGN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sin), new[] { typeof(double) })!, "SIN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Sqrt), new[] { typeof(double) })!, "SQRT" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Tan), new[] { typeof(double) })!, "TAN" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Truncate), new[] { typeof(double) })!, "TRUNCATE" },
    { typeof(Math).GetRuntimeMethod(nameof(Math.Truncate), new[] { typeof(decimal) })!, "TRUNCATE" },
  };

    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    public MySQLMathMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
      => _sqlExpressionFactory = sqlExpressionFactory;

    /// <inheritdoc />
    public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      if (_supportedMethodTranslations.TryGetValue(method, out var sqlFunctionName))
      {
        var targetArgumentsCount = arguments.Count;

        if (sqlFunctionName == "TRUNCATE")
        {
          targetArgumentsCount = 2;
        }

        var newArguments = new SqlExpression[targetArgumentsCount];
        newArguments[0] = arguments[0];

        if (targetArgumentsCount == 2)
        {
          if (arguments.Count == 2)
          {
            newArguments[1] = arguments[1];
          }
          else
          {
            newArguments[1] = _sqlExpressionFactory.Constant(0);
          }
        }

        return _sqlExpressionFactory.Function(
          sqlFunctionName,
          newArguments,
          nullable: true,
          argumentsPropagateNullability: TrueArrays[newArguments.Length],
          method.ReturnType);
      }

      return null;
    }
  }
}
