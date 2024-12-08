// Copyright © 2021, 2024, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  /// <summary>
  ///   An expression that explicitly specifies the collation of a string value.
  /// </summary>
  internal class MySQLCollateExpression : SqlExpression
  {
    private readonly SqlExpression _valueExpression;
    private readonly string _charset;
    private readonly string _collation;

#if NET9_0
    private static ConstructorInfo? _quotingConstructor;
#endif

#if NET9_0
    public MySQLCollateExpression(SqlExpression operand, string collation)
    : base(operand.Type, operand.TypeMapping)
    {
      Operand = operand;
      Collation = collation;
    }
#else
    public MySQLCollateExpression(
    SqlExpression valueExpression,
    string charset,
    string collation,
    RelationalTypeMapping? typeMapping)
    : base(typeof(string), typeMapping)
    {
      _valueExpression = valueExpression;
      _charset = charset;
      _collation = collation;
    }
#endif


    /// <summary>
    ///   The expression for which a collation is being specified.
    /// </summary>
    public virtual SqlExpression ValueExpression => _valueExpression;

    /// <summary>
    ///   The character set that the string is being converted to.
    /// </summary>
    public virtual string Charset => _charset;

#if NET9_0
    /// <summary>
    ///   The expression on which collation is applied.
    /// </summary>
    public virtual SqlExpression Operand { get; }

    public virtual string Collation { get; }
#else
    /// <summary>
    ///   The collation that the string is being converted to.
    /// </summary>
    public virtual string Collation => _collation;
#endif



    /// <summary>
    ///   Dispatches to the specific visit method for this node type.
    /// </summary>
    protected override Expression Accept(ExpressionVisitor visitor)
      => visitor is MySQLQuerySqlGenerator mySqlQuerySqlGenerator
        ? mySqlQuerySqlGenerator.VisitMySQLCollateExpression(this)
        : base.Accept(visitor);

    /// <summary>
    ///   Reduces the node and then calls the <see cref="ExpressionVisitor.Visit(Expression)" /> method passing the
    ///   reduced expression.
    ///   Throws an exception if the node isn't reducible.
    /// </summary>
    /// <param name="visitor"> An instance of <see cref="ExpressionVisitor" />. </param>
    /// <returns> The expression being visited, or an expression which should replace it in the tree. </returns>
    /// <remarks>
    ///   Override this method to provide logic to walk the node's children.
    ///   A typical implementation will call visitor.Visit on each of its
    ///   children, and if any of them change, should return a new copy of
    ///   itself with the modified children.
    /// </remarks>
    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
      var newValueExpression = visitor.VisitAndConvert(_valueExpression, nameof(VisitChildren));

      return Update(newValueExpression);

    }

#if NET9_0
    public virtual MySQLCollateExpression Update(SqlExpression operand)
        => operand != Operand
            ? new MySQLCollateExpression(operand, Collation)
            : this;
    /// <inheritdoc />
    public override Expression Quote() => New(
        _quotingConstructor ??= typeof(MySQLCollateExpression).GetConstructor([typeof(SqlExpression), typeof(string)])!,
        Operand.Quote(),
        Constant(Collation));

#else
    public virtual MySQLCollateExpression Update(SqlExpression valueExpression)
      => valueExpression != _valueExpression &&
      valueExpression != null
      ? new MySQLCollateExpression(valueExpression, _charset, _collation, TypeMapping)
      : this;
#endif

    /// <summary>
    ///   Tests if this object is considered equal to another.
    /// </summary>
    /// <param name="obj"> The object to compare with the current object. </param>
    /// <returns>
    ///   <see langword="true"/> if the objects are considered equal; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
    {
      if (obj is null)
      {
        return false;
      }

      if (ReferenceEquals(this, obj))
      {
        return true;
      }

      return obj.GetType() == GetType() && Equals((MySQLCollateExpression)obj);
    }

    private bool Equals(MySQLCollateExpression other)
      => string.Equals(_charset, other._charset, StringComparison.OrdinalIgnoreCase)
         && string.Equals(_collation, other._collation, StringComparison.OrdinalIgnoreCase)
         && _valueExpression.Equals(other._valueExpression);

    /// <summary>
    ///   Returns a hash code for this object.
    /// </summary>
    /// <returns>
    ///   A hash code for this object.
    /// </returns>
    public override int GetHashCode()
    {
      unchecked
      {
        var hashCodes =
          new[] {
        _valueExpression.GetHashCode(),
        StringComparer.OrdinalIgnoreCase.GetHashCode(_charset),
        StringComparer.OrdinalIgnoreCase.GetHashCode(_collation)
          };

        return hashCodes.Aggregate(0, (acc, hc) => (acc * 397) ^ hc);
      }
    }

    /// <summary>
    ///   Creates a <see cref="string" /> representation of the Expression.
    /// </summary>
    /// <returns>A <see cref="string" /> representation of the Expression.</returns>
    public override string ToString() =>
      $"{_valueExpression} COLLATE {_collation}";

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
      expressionPrinter.Append(ToString());
    }
  }
}
