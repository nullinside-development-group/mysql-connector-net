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
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Transactions;

namespace MySql.EntityFrameworkCore.Migrations.Internal
{
#if NET9_0
  public class MySQLMigrator : IMigrator
  {
    private readonly IMigrationsAssembly _migrationsAssembly;
    private readonly IHistoryRepository _historyRepository;
    private readonly IRelationalDatabaseCreator _databaseCreator;
    private readonly IMigrationsSqlGenerator _migrationsSqlGenerator;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private readonly IMigrationCommandExecutor _migrationCommandExecutor;
    private readonly IRelationalConnection _connection;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;
    private readonly ICurrentDbContext _currentContext;
    private readonly IModelRuntimeInitializer _modelRuntimeInitializer;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Migrations> _logger;
    private readonly IRelationalCommandDiagnosticsLogger _commandLogger;
    private readonly IMigrationsModelDiffer _migrationsModelDiffer;
    private readonly IDesignTimeModel _designTimeModel;
    private readonly string _activeProvider;
    private readonly IDbContextOptions _dbContextOptions;
    private readonly IExecutionStrategy _executionStrategy;

    public MySQLMigrator(
      IMigrationsAssembly migrationsAssembly,
      IHistoryRepository historyRepository,
      IDatabaseCreator databaseCreator,
      IMigrationsSqlGenerator migrationsSqlGenerator,
      IRawSqlCommandBuilder rawSqlCommandBuilder,
      IMigrationCommandExecutor migrationCommandExecutor,
      IRelationalConnection connection,
      ISqlGenerationHelper sqlGenerationHelper,
      ICurrentDbContext currentContext,
      IModelRuntimeInitializer modelRuntimeInitializer,
      IDiagnosticsLogger<DbLoggerCategory.Migrations> logger,
      IRelationalCommandDiagnosticsLogger commandLogger,
      IDatabaseProvider databaseProvider,
      IMigrationsModelDiffer migrationsModelDiffer,
      IDesignTimeModel designTimeModel,
      IDbContextOptions dbContextOptions,
      IExecutionStrategy executionStrategy)
    {
      _migrationsAssembly = migrationsAssembly;
      _historyRepository = historyRepository;
      _databaseCreator = (IRelationalDatabaseCreator)databaseCreator;
      _migrationsSqlGenerator = migrationsSqlGenerator;
      _rawSqlCommandBuilder = rawSqlCommandBuilder;
      _migrationCommandExecutor = migrationCommandExecutor;
      _connection = connection;
      _sqlGenerationHelper = sqlGenerationHelper;
      _currentContext = currentContext;
      _modelRuntimeInitializer = modelRuntimeInitializer;
      _logger = logger;
      _commandLogger = commandLogger;
      _migrationsModelDiffer = migrationsModelDiffer;
      _designTimeModel = designTimeModel;
      _activeProvider = databaseProvider.Name;
      _dbContextOptions = dbContextOptions;
      _executionStrategy = executionStrategy;
    }

    protected virtual System.Data.IsolationLevel? MigrationTransactionIsolationLevel => null;

    public virtual void Migrate(string? targetMigration)
    {
      var useTransaction = _connection.CurrentTransaction is null;
      if (!useTransaction
          && _executionStrategy.RetriesOnFailure)
      {
        throw new NotSupportedException(RelationalStrings.TransactionSuppressedMigrationInUserTransaction);
      }

      if (RelationalResources.LogPendingModelChanges(_logger).WarningBehavior != WarningBehavior.Ignore
          && HasPendingModelChanges())
      {
        _logger.PendingModelChangesWarning(_currentContext.Context.GetType());
      }

      if (!useTransaction)
      {
        _logger.MigrationsUserTransactionWarning();
      }

      _logger.MigrateUsingConnection(this, _connection);

      using var transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);

      if (!_databaseCreator.Exists())
      {
        _databaseCreator.Create();
      }

      _connection.Open();
      try
      {
        var state = new MigrationExecutionState();
        if (_historyRepository.LockReleaseBehavior != LockReleaseBehavior.Transaction
            && useTransaction)
        {
          state.DatabaseLock = _historyRepository.AcquireDatabaseLock();
        }

        _executionStrategy.Execute(
            this,
            static (_, migrator) =>
            {
              migrator._connection.Open();
              try
              {
                return migrator._historyRepository.CreateIfNotExists();
              }
              finally
              {
                migrator._connection.Close();
              }
            },
            verifySucceeded: null);

        _executionStrategy.Execute(
            (Migrator: this,
            TargetMigration: targetMigration,
            State: state,
            UseTransaction: useTransaction),
            static (c, s) => s.Migrator.MigrateImplementation(c, s.TargetMigration, s.State, s.UseTransaction),
            static (_, s) => new ExecutionResult<bool>(
                successful: s.Migrator.VerifyMigrationSucceeded(s.TargetMigration, s.State),
                result: true));
      }
      finally
      {
        _connection.Close();
      }
    }

    private bool MigrateImplementation(DbContext context, string? targetMigration, MigrationExecutionState state, bool useTransaction)
    {
      var connectionOpened = _connection.Open();
      try
      {
        if (useTransaction)
        {
          state.Transaction = MigrationTransactionIsolationLevel == null
              ? _connection.BeginTransaction()
              : _connection.BeginTransaction(MigrationTransactionIsolationLevel.Value);

          state.DatabaseLock = state.DatabaseLock == null
              ? _historyRepository.AcquireDatabaseLock()
              : state.DatabaseLock.ReacquireIfNeeded(connectionOpened, useTransaction);
        }

        PopulateMigrations(
            _historyRepository.GetAppliedMigrations().Select(t => t.MigrationId),
            targetMigration,
            out var migratorData);

        var commandLists = GetMigrationCommandLists(migratorData);
        foreach (var commandList in commandLists)
        {
          var (id, getCommands) = commandList;
          if (id != state.CurrentMigrationId)
          {
            state.CurrentMigrationId = id;
            state.LastCommittedCommandIndex = 0;
          }

          _migrationCommandExecutor.ExecuteNonQuery(getCommands(), _connection, state, commitTransaction: false, MigrationTransactionIsolationLevel);
        }

        var coreOptionsExtension =
            _dbContextOptions.FindExtension<CoreOptionsExtension>()
            ?? new CoreOptionsExtension();

        var seed = coreOptionsExtension.Seeder;
        if (seed != null)
        {
          seed(context, state.AnyOperationPerformed);
        }
        else if (coreOptionsExtension.AsyncSeeder != null)
        {
          throw new InvalidOperationException(CoreStrings.MissingSeeder);
        }

        state.Transaction?.Commit();
        return state.AnyOperationPerformed;
      }
      finally
      {
        state.DatabaseLock?.Dispose();
        state.DatabaseLock = null;
        state.Transaction?.Dispose();
        state.Transaction = null;
        _connection.Close();
      }
    }

    public virtual async Task MigrateAsync(
        string? targetMigration,
        CancellationToken cancellationToken = default)
    {
      var useTransaction = _connection.CurrentTransaction is null;
      if (!useTransaction
          && _executionStrategy.RetriesOnFailure)
      {
        throw new NotSupportedException(RelationalStrings.TransactionSuppressedMigrationInUserTransaction);
      }

      if (RelationalResources.LogPendingModelChanges(_logger).WarningBehavior != WarningBehavior.Ignore
          && HasPendingModelChanges())
      {
        _logger.PendingModelChangesWarning(_currentContext.Context.GetType());
      }

      if (!useTransaction)
      {
        _logger.MigrationsUserTransactionWarning();
      }

      _logger.MigrateUsingConnection(this, _connection);

      using var transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);

      if (!await _databaseCreator.ExistsAsync(cancellationToken).ConfigureAwait(false))
      {
        await _databaseCreator.CreateAsync(cancellationToken).ConfigureAwait(false);
      }

      await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
      try
      {
        var state = new MigrationExecutionState();
        if (_historyRepository.LockReleaseBehavior != LockReleaseBehavior.Transaction
            && useTransaction)
        {
          state.DatabaseLock = await _historyRepository.AcquireDatabaseLockAsync(cancellationToken).ConfigureAwait(false);
        }

        await _executionStrategy.ExecuteAsync(
            this,
            static async (_, migrator, ct) =>
            {
              await migrator._connection.OpenAsync(ct).ConfigureAwait(false);
              try
              {
                return await migrator._historyRepository.CreateIfNotExistsAsync(ct).ConfigureAwait(false);
              }
              finally
              {
                await migrator._connection.CloseAsync().ConfigureAwait(false);
              }
            },
            verifySucceeded: null,
            cancellationToken).ConfigureAwait(false);

        await _executionStrategy.ExecuteAsync(
            (Migrator: this,
            TargetMigration: targetMigration,
            State: state,
            UseTransaction: useTransaction),
            async static (c, s, ct) => await s.Migrator.MigrateImplementationAsync(
                c, s.TargetMigration, s.State, s.UseTransaction, ct).ConfigureAwait(false),
            async static (_, s, ct) => new ExecutionResult<bool>(
                successful: await s.Migrator.VerifyMigrationSucceededAsync(s.TargetMigration, s.State, ct).ConfigureAwait(false),
                result: true),
            cancellationToken)
            .ConfigureAwait(false);
      }
      finally
      {
        await _connection.CloseAsync().ConfigureAwait(false);
      }
    }

    private async Task<bool> MigrateImplementationAsync(
        DbContext context, string? targetMigration, MigrationExecutionState state, bool useTransaction, CancellationToken cancellationToken = default)
    {
      var connectionOpened = await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
      try
      {
        if (useTransaction)
        {
          state.Transaction = await (MigrationTransactionIsolationLevel == null
              ? context.Database.BeginTransactionAsync(cancellationToken)
              : context.Database.BeginTransactionAsync(MigrationTransactionIsolationLevel.Value, cancellationToken))
                  .ConfigureAwait(false);

          state.DatabaseLock = state.DatabaseLock == null
              ? await _historyRepository.AcquireDatabaseLockAsync(cancellationToken).ConfigureAwait(false)
              : await state.DatabaseLock.ReacquireIfNeededAsync(connectionOpened, useTransaction, cancellationToken)
                  .ConfigureAwait(false);
        }

        PopulateMigrations(
            (await _historyRepository.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false)).Select(t => t.MigrationId),
            targetMigration,
            out var migratorData);

        var commandLists = GetMigrationCommandLists(migratorData);
        foreach (var commandList in commandLists)
        {
          var (id, getCommands) = commandList;
          if (id != state.CurrentMigrationId)
          {
            state.CurrentMigrationId = id;
            state.LastCommittedCommandIndex = 0;
          }

          await _migrationCommandExecutor.ExecuteNonQueryAsync(
              getCommands(), _connection, state, commitTransaction: false, MigrationTransactionIsolationLevel, cancellationToken)
              .ConfigureAwait(false);
        }

        var coreOptionsExtension =
            _dbContextOptions.FindExtension<CoreOptionsExtension>()
            ?? new CoreOptionsExtension();

        var seedAsync = coreOptionsExtension.AsyncSeeder;
        if (seedAsync != null)
        {
          await seedAsync(context, state.AnyOperationPerformed, cancellationToken).ConfigureAwait(false);
        }
        else if (coreOptionsExtension.Seeder != null)
        {
          throw new InvalidOperationException(CoreStrings.MissingSeeder);
        }

        if (state.Transaction != null)
        {
          await state.Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        return state.AnyOperationPerformed;
      }
      finally
      {
        if (state.DatabaseLock != null)
        {
          state.DatabaseLock.Dispose();
          state.DatabaseLock = null;
        }
        if (state.Transaction != null)
        {
          await state.Transaction.DisposeAsync().ConfigureAwait(false);
          state.Transaction = null;
        }
        await _connection.CloseAsync().ConfigureAwait(false);
      }
    }

    private IEnumerable<(string, Func<IReadOnlyList<MigrationCommand>>)> GetMigrationCommandLists(MigratorData parameters)
    {
      var migrationsToApply = parameters.AppliedMigrations;
      var migrationsToRevert = parameters.RevertedMigrations;
      var actualTargetMigration = parameters.TargetMigration;

      for (var i = 0; i < migrationsToRevert.Count; i++)
      {
        var migration = migrationsToRevert[i];

        var index = i;
        yield return (migration.GetId(), () =>
        {
          _logger.MigrationReverting(this, migration);

          var commands = GenerateDownSql(
              migration,
              index != migrationsToRevert.Count - 1
                  ? migrationsToRevert[index + 1]
                  : actualTargetMigration);
          if (migration.DownOperations.Count > 1
              && commands.FirstOrDefault(c => c.TransactionSuppressed) is MigrationCommand nonTransactionalCommand)
          {
            _logger.NonTransactionalMigrationOperationWarning(this, migration, nonTransactionalCommand);
          }

          return commands;
        }
        );
      }

      foreach (var migration in migrationsToApply)
      {
        yield return (migration.GetId(), () =>
        {
          _logger.MigrationApplying(this, migration);

          var commands = GenerateUpSql(migration);
          if (migration.UpOperations.Count > 1
              && commands.FirstOrDefault(c => c.TransactionSuppressed) is MigrationCommand nonTransactionalCommand)
          {
            _logger.NonTransactionalMigrationOperationWarning(this, migration, nonTransactionalCommand);
          }

          return commands;
        }
        );
      }

      if (migrationsToRevert.Count + migrationsToApply.Count == 0)
      {
        _logger.MigrationsNotApplied(this);
      }
    }

    protected virtual void PopulateMigrations(
        IEnumerable<string> appliedMigrationEntries,
        string? targetMigration,
        out MigratorData parameters)
    {
      var appliedMigrations = new Dictionary<string, TypeInfo>();
      var unappliedMigrations = new Dictionary<string, TypeInfo>();
      var appliedMigrationEntrySet = new HashSet<string>(appliedMigrationEntries, StringComparer.OrdinalIgnoreCase);
      if (_migrationsAssembly.Migrations.Count == 0)
      {
        _logger.MigrationsNotFound(this, _migrationsAssembly);
      }

      foreach (var (key, typeInfo) in _migrationsAssembly.Migrations)
      {
        if (appliedMigrationEntrySet.Contains(key))
        {
          appliedMigrations.Add(key, typeInfo);
        }
        else
        {
          unappliedMigrations.Add(key, typeInfo);
        }
      }

      IReadOnlyList<Migration> migrationsToApply;
      IReadOnlyList<Migration> migrationsToRevert;
      Migration? actualTargetMigration = null;
      if (string.IsNullOrEmpty(targetMigration))
      {
        migrationsToApply = unappliedMigrations
            .OrderBy(m => m.Key)
            .Select(p => _migrationsAssembly.CreateMigration(p.Value, _activeProvider))
            .ToList();
        migrationsToRevert = [];
      }
      else if (targetMigration == Migration.InitialDatabase)
      {
        migrationsToApply = [];
        migrationsToRevert = appliedMigrations
            .OrderByDescending(m => m.Key)
            .Select(p => _migrationsAssembly.CreateMigration(p.Value, _activeProvider))
            .ToList();
      }
      else
      {
        targetMigration = _migrationsAssembly.GetMigrationId(targetMigration);
        migrationsToApply = unappliedMigrations
            .Where(m => string.Compare(m.Key, targetMigration, StringComparison.OrdinalIgnoreCase) <= 0)
            .OrderBy(m => m.Key)
            .Select(p => _migrationsAssembly.CreateMigration(p.Value, _activeProvider))
            .ToList();
        migrationsToRevert = appliedMigrations
            .Where(m => string.Compare(m.Key, targetMigration, StringComparison.OrdinalIgnoreCase) > 0)
            .OrderByDescending(m => m.Key)
            .Select(p => _migrationsAssembly.CreateMigration(p.Value, _activeProvider))
            .ToList();
        actualTargetMigration = appliedMigrations
            .Where(m => string.Compare(m.Key, targetMigration, StringComparison.OrdinalIgnoreCase) == 0)
            .Select(p => _migrationsAssembly.CreateMigration(p.Value, _activeProvider))
            .SingleOrDefault();
      }

      parameters = new MigratorData(migrationsToApply, migrationsToRevert, actualTargetMigration);
    }

    protected virtual bool VerifyMigrationSucceeded(
        string? targetMigration, MigrationExecutionState state)
        => false;

    protected virtual Task<bool> VerifyMigrationSucceededAsync(
        string? targetMigration, MigrationExecutionState state, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public virtual string GenerateScript(
        string? fromMigration = null,
        string? toMigration = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
      options |= MigrationsSqlGenerationOptions.Script;

      var idempotent = options.HasFlag(MigrationsSqlGenerationOptions.Idempotent);
      var noTransactions = options.HasFlag(MigrationsSqlGenerationOptions.NoTransactions);

      IEnumerable<string> appliedMigrations;
      if (string.IsNullOrEmpty(fromMigration)
          || fromMigration == Migration.InitialDatabase)
      {
        appliedMigrations = Enumerable.Empty<string>();
      }
      else
      {
        var fromMigrationId = _migrationsAssembly.GetMigrationId(fromMigration);
        appliedMigrations = _migrationsAssembly.Migrations
            .Where(t => string.Compare(t.Key, fromMigrationId, StringComparison.OrdinalIgnoreCase) <= 0)
            .Select(t => t.Key);
      }

      PopulateMigrations(appliedMigrations, toMigration, out var migratorData);

      var builder = new IndentedStringBuilder();

      if (fromMigration == Migration.InitialDatabase
          || string.IsNullOrEmpty(fromMigration))
      {
        builder
            .Append(_historyRepository.GetCreateIfNotExistsScript())
            .Append(_sqlGenerationHelper.BatchTerminator);
      }

      var idempotencyEnd = idempotent
          ? _historyRepository.GetEndIfScript()
          : null;
      var migrationsToApply = migratorData.AppliedMigrations;
      var migrationsToRevert = migratorData.RevertedMigrations;
      var actualTargetMigration = migratorData.TargetMigration;
      var transactionStarted = false;
      for (var i = 0; i < migrationsToRevert.Count; i++)
      {
        var migration = migrationsToRevert[i];
        var previousMigration = i != migrationsToRevert.Count - 1
            ? migrationsToRevert[i + 1]
            : actualTargetMigration;

        _logger.MigrationGeneratingDownScript(this, migration, fromMigration, toMigration, idempotent);

        var idempotencyCondition = idempotent
            ? _historyRepository.GetBeginIfExistsScript(migration.GetId())
            : null;

        GenerateSqlScript(
            GenerateDownSql(migration, previousMigration, options),
            builder, _sqlGenerationHelper, ref transactionStarted, noTransactions, idempotencyCondition, idempotencyEnd);
      }

      foreach (var migration in migrationsToApply)
      {
        _logger.MigrationGeneratingUpScript(this, migration, fromMigration, toMigration, idempotent);

        var idempotencyCondition = idempotent
            ? _historyRepository.GetBeginIfNotExistsScript(migration.GetId())
            : null;

        GenerateSqlScript(
            GenerateUpSql(migration, options),
            builder, _sqlGenerationHelper, ref transactionStarted, noTransactions, idempotencyCondition, idempotencyEnd);
      }

      if (transactionStarted)
      {
        builder
            .AppendLine(_sqlGenerationHelper.CommitTransactionStatement)
            .Append(_sqlGenerationHelper.BatchTerminator);
      }

      return builder.ToString();
    }

    private static void GenerateSqlScript(
        IEnumerable<MigrationCommand> commands,
        IndentedStringBuilder builder,
        ISqlGenerationHelper sqlGenerationHelper,
        ref bool transactionStarted,
        bool noTransactions = false,
        string? idempotencyCondition = null,
        string? idempotencyEnd = null)
    {
      foreach (var command in commands)
      {
        if (!noTransactions)
        {
          if (!transactionStarted && !command.TransactionSuppressed)
          {
            builder
                .AppendLine(sqlGenerationHelper.StartTransactionStatement);
            transactionStarted = true;
          }

          if (transactionStarted && command.TransactionSuppressed)
          {
            builder
                .AppendLine(sqlGenerationHelper.CommitTransactionStatement)
                .Append(sqlGenerationHelper.BatchTerminator);
            transactionStarted = false;
          }
        }

        if (idempotencyCondition != null
            && idempotencyEnd != null)
        {
          builder.AppendLine(idempotencyCondition);
          using (builder.Indent())
          {
            builder.AppendLines(command.CommandText);
          }

          builder.Append(idempotencyEnd);
        }
        else
        {
          builder.Append(command.CommandText);
        }

        if (!transactionStarted)
        {
          builder.Append(sqlGenerationHelper.BatchTerminator);
        }
        else
        {
          builder.Append(Environment.NewLine);
        }
      }
    }

    protected virtual IReadOnlyList<MigrationCommand> GenerateUpSql(
        Migration migration,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
      var insertCommand = _rawSqlCommandBuilder.Build(
          _historyRepository.GetInsertScript(new HistoryRow(migration.GetId(), ProductInfo.GetVersion())));

      var operations = _migrationsSqlGenerator
          .Generate(
              migration.UpOperations,
              FinalizeModel(migration.TargetModel),
              options);

      return
      [
          .. operations,
            new MigrationCommand(insertCommand, _currentContext.Context, _commandLogger,
                transactionSuppressed: operations.Any(o => o.TransactionSuppressed)),
        ];
    }

    protected virtual IReadOnlyList<MigrationCommand> GenerateDownSql(
        Migration migration,
        Migration? previousMigration,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
      var deleteCommand = _rawSqlCommandBuilder.Build(
          _historyRepository.GetDeleteScript(migration.GetId()));

      var operations = _migrationsSqlGenerator
          .Generate(
              migration.DownOperations,
              previousMigration == null ? null : FinalizeModel(previousMigration.TargetModel),
              options);

      return [
          .. operations,
            new MigrationCommand(deleteCommand, _currentContext.Context, _commandLogger,
                transactionSuppressed: operations.Any(o => o.TransactionSuppressed))
          ];
    }

    private IModel? FinalizeModel(IModel? model)
        => model == null
            ? null
            : _modelRuntimeInitializer.Initialize(model);

    public bool HasPendingModelChanges()
        => _migrationsModelDiffer.HasDifferences(
            FinalizeModel(_migrationsAssembly.ModelSnapshot?.Model)?.GetRelationalModel(),
            _designTimeModel.Model.GetRelationalModel());
  }
#else

  internal class MySQLMigrator : Migrator
  {
    private static readonly Dictionary<Type, Tuple<string, string>> _customMigrationCommands =
    new Dictionary<Type, Tuple<string, string>>
    {
      {
        typeof(DropPrimaryKeyOperation),
        new Tuple<string, string>(BeforeDropPrimaryKeyMigrationBegin, BeforeDropPrimaryKeyMigrationEnd)
      },
      {
        typeof(AddPrimaryKeyOperation),
        new Tuple<string, string>(AfterAddPrimaryKeyMigrationBegin, AfterAddPrimaryKeyMigrationEnd)
      },
    };

    private readonly IMigrationsAssembly _migrationsAssembly;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private readonly ICurrentDbContext _currentContext;
    private readonly IRelationalCommandDiagnosticsLogger _commandLogger;

    public MySQLMigrator(
      [NotNull] IMigrationsAssembly migrationsAssembly,
      [NotNull] IHistoryRepository historyRepository,
      [NotNull] IDatabaseCreator databaseCreator,
      [NotNull] IMigrationsSqlGenerator migrationsSqlGenerator,
      [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder,
      [NotNull] IMigrationCommandExecutor migrationCommandExecutor,
      [NotNull] IRelationalConnection connection,
      [NotNull] ISqlGenerationHelper sqlGenerationHelper,
      [NotNull] ICurrentDbContext currentContext,
      [NotNull] IModelRuntimeInitializer modelRuntimeInitializer,
      [NotNull] IDiagnosticsLogger<DbLoggerCategory.Migrations> logger,
      [NotNull] IRelationalCommandDiagnosticsLogger commandLogger,
      [NotNull] IDatabaseProvider databaseProvider)
      : base(
          migrationsAssembly,
          historyRepository,
          databaseCreator,
          migrationsSqlGenerator,
          rawSqlCommandBuilder,
          migrationCommandExecutor,
          connection,
          sqlGenerationHelper,
          currentContext,
          modelRuntimeInitializer,
          logger,
          commandLogger,
          databaseProvider)
    {
      _migrationsAssembly = migrationsAssembly;
      _rawSqlCommandBuilder = rawSqlCommandBuilder;
      _currentContext = currentContext;
      _commandLogger = commandLogger;
    }

    protected override IReadOnlyList<MigrationCommand> GenerateUpSql(
      Migration migration,
      MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
      var commands = base.GenerateUpSql(migration, options);

      return options.HasFlag(MigrationsSqlGenerationOptions.Script) &&
         options.HasFlag(MigrationsSqlGenerationOptions.Idempotent)
        ? commands
        : WrapWithCustomCommands(
          migration.UpOperations,
          commands.ToList(),
          options);
    }

    protected override IReadOnlyList<MigrationCommand> GenerateDownSql(
      Migration migration,
      Migration? previousMigration,
      MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
      var commands = base.GenerateDownSql(migration, previousMigration, options);

      return options.HasFlag(MigrationsSqlGenerationOptions.Script) &&
         options.HasFlag(MigrationsSqlGenerationOptions.Idempotent)
        ? commands
        : WrapWithCustomCommands(
          migration.DownOperations,
          commands.ToList(),
          options);
    }

    public override string GenerateScript(
      string? fromMigration = null,
      string? toMigration = null,
      MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
      options |= MigrationsSqlGenerationOptions.Script;

      if (!options.HasFlag(MigrationsSqlGenerationOptions.Idempotent))
      {
        return base.GenerateScript(fromMigration, toMigration, options);
      }

      var operations = GetAllMigrationOperations(fromMigration, toMigration);

      var builder = new StringBuilder();

      builder.AppendJoin(string.Empty, GetMigrationCommandTexts(operations, true, options));
      builder.Append(base.GenerateScript(fromMigration, toMigration, options));
      builder.AppendJoin(string.Empty, GetMigrationCommandTexts(operations, false, options));

      return builder.ToString();
    }

    protected virtual List<MigrationOperation> GetAllMigrationOperations(string? fromMigration, string? toMigration)
    {
      IEnumerable<string> appliedMigrations;
      if (string.IsNullOrEmpty(fromMigration)
        || fromMigration == Migration.InitialDatabase)
      {
        appliedMigrations = Enumerable.Empty<string>();
      }
      else
      {
        var fromMigrationId = _migrationsAssembly.GetMigrationId(fromMigration);
        appliedMigrations = _migrationsAssembly.Migrations
          .Where(t => string.Compare(t.Key, fromMigrationId, StringComparison.OrdinalIgnoreCase) <= 0)
          .Select(t => t.Key);
      }

      PopulateMigrations(
        appliedMigrations,
        toMigration,
        out var migrationsToApply,
        out var migrationsToRevert,
        out var actualTargetMigration);

      return migrationsToApply
        .SelectMany(x => x.UpOperations)
        .Concat(migrationsToRevert.SelectMany(x => x.DownOperations))
        .ToList();
    }

    protected virtual IReadOnlyList<MigrationCommand> WrapWithCustomCommands(
      IReadOnlyList<MigrationOperation> migrationOperations,
      IReadOnlyList<MigrationCommand> migrationCommands,
      MigrationsSqlGenerationOptions options)
    {
      var beginCommandTexts = GetMigrationCommandTexts(migrationOperations, true, options);
      var endCommandTexts = GetMigrationCommandTexts(migrationOperations, false, options);

      return new List<MigrationCommand>(
      beginCommandTexts.Select(t => new MigrationCommand(_rawSqlCommandBuilder.Build(t),
      _currentContext.Context, _commandLogger))
      .Concat(migrationCommands)
      .Concat(endCommandTexts.Select(t => new MigrationCommand(_rawSqlCommandBuilder.Build(t), _currentContext.Context, _commandLogger)))
      );
    }

    protected virtual string[] GetMigrationCommandTexts(
    IReadOnlyList<MigrationOperation> migrationOperations,
    bool beginTexts,
    MigrationsSqlGenerationOptions options)
      => GetCustomCommands(migrationOperations)
      .Select(
        t => PrepareString(
          beginTexts
          ? t.Item1
          : t.Item2,
          options))
      .ToArray();

    protected virtual IReadOnlyList<Tuple<string, string>> GetCustomCommands(IReadOnlyList<MigrationOperation> migrationOperations)
      => _customMigrationCommands
      .Where(c => migrationOperations.Any(o => c.Key.IsInstanceOfType(o)) && c.Value != null)
      .Select(kvp => kvp.Value)
      .ToList();

    protected virtual string CleanUpScriptSpecificPseudoStatements(string commandText)
    {
      const string temporaryDelimiter = @"//";
      const string defaultDelimiter = @";";
      const string delimiterChangeRegexPatternFormatString = @"[\r\n]*[^\S\r\n]*DELIMITER[^\S\r\n]+{0}[^\S\r\n]*";
      const string delimiterUseRegexPatternFormatString = @"\s*{0}\s*$";

      var temporaryDelimiterRegexPattern = string.Format(
        delimiterChangeRegexPatternFormatString,
        $"(?:{Regex.Escape(temporaryDelimiter)}|{Regex.Escape(defaultDelimiter)})");

      var delimiter = Regex.Match(commandText, temporaryDelimiterRegexPattern, RegexOptions.IgnoreCase);
      if (delimiter.Success)
      {
        commandText = Regex.Replace(commandText, temporaryDelimiterRegexPattern, string.Empty, RegexOptions.IgnoreCase);

        commandText = Regex.Replace(
          commandText,
          string.Format(delimiterUseRegexPatternFormatString, temporaryDelimiter),
          defaultDelimiter,
          RegexOptions.IgnoreCase | RegexOptions.Multiline);
      }

      return commandText;
    }

    protected virtual string PrepareString(string str, MigrationsSqlGenerationOptions options)
    {
      str = options.HasFlag(MigrationsSqlGenerationOptions.Script)
        ? str
        : CleanUpScriptSpecificPseudoStatements(str);

      str = str
        .Replace("\r", string.Empty)
        .Replace("\n", Environment.NewLine);

      str += options.HasFlag(MigrationsSqlGenerationOptions.Script)
        ? Environment.NewLine + (
          options.HasFlag(MigrationsSqlGenerationOptions.Idempotent)
            ? Environment.NewLine
            : string.Empty)
        : string.Empty;

      return str;
    }

    #region Custom SQL

    private const string BeforeDropPrimaryKeyMigrationBegin = @"DROP PROCEDURE IF EXISTS `MYSQL_BEFORE_DROP_PRIMARY_KEY`;
  DELIMITER //
  CREATE PROCEDURE `MYSQL_BEFORE_DROP_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255))
  BEGIN
    DECLARE HAS_AUTO_INCREMENT_ID TINYINT(1);
    DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
    DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
    DECLARE SQL_EXP VARCHAR(1000);
    SELECT COUNT(*)
    INTO HAS_AUTO_INCREMENT_ID
    FROM `information_schema`.`COLUMNS`
    WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
      AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
      AND `Extra` = 'auto_increment'
      AND `COLUMN_KEY` = 'PRI'
      LIMIT 1;
    IF HAS_AUTO_INCREMENT_ID THEN
    SELECT `COLUMN_TYPE`
      INTO PRIMARY_KEY_TYPE
      FROM `information_schema`.`COLUMNS`
      WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
      AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
      AND `COLUMN_KEY` = 'PRI'
      LIMIT 1;
    SELECT `COLUMN_NAME`
      INTO PRIMARY_KEY_COLUMN_NAME
      FROM `information_schema`.`COLUMNS`
      WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
      AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
      AND `COLUMN_KEY` = 'PRI'
      LIMIT 1;
    SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL;');
    SET @SQL_EXP = SQL_EXP;
    PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
    EXECUTE SQL_EXP_EXECUTE;
    DEALLOCATE PREPARE SQL_EXP_EXECUTE;
    END IF;
  END //
  DELIMITER ;";

    private const string BeforeDropPrimaryKeyMigrationEnd = @"DROP PROCEDURE `MYSQL_BEFORE_DROP_PRIMARY_KEY`;";

    private const string AfterAddPrimaryKeyMigrationBegin = @"DROP PROCEDURE IF EXISTS `MYSQL_AFTER_ADD_PRIMARY_KEY`;
  DELIMITER //
  CREATE PROCEDURE `MYSQL_AFTER_ADD_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255), IN `COLUMN_NAME_ARGUMENT` VARCHAR(255))
  BEGIN
    DECLARE HAS_AUTO_INCREMENT_ID INT(11);
    DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
    DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
    DECLARE SQL_EXP VARCHAR(1000);
    SELECT COUNT(*)
    INTO HAS_AUTO_INCREMENT_ID
    FROM `information_schema`.`COLUMNS`
    WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
      AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
      AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
      AND `COLUMN_TYPE` LIKE '%int%'
      AND `COLUMN_KEY` = 'PRI';
    IF HAS_AUTO_INCREMENT_ID THEN
    SELECT `COLUMN_TYPE`
      INTO PRIMARY_KEY_TYPE
      FROM `information_schema`.`COLUMNS`
      WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
      AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
      AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
      AND `COLUMN_TYPE` LIKE '%int%'
      AND `COLUMN_KEY` = 'PRI';
    SELECT `COLUMN_NAME`
      INTO PRIMARY_KEY_COLUMN_NAME
      FROM `information_schema`.`COLUMNS`
      WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
      AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
      AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
      AND `COLUMN_TYPE` LIKE '%int%'
      AND `COLUMN_KEY` = 'PRI';
    SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL AUTO_INCREMENT;');
    SET @SQL_EXP = SQL_EXP;
    PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
    EXECUTE SQL_EXP_EXECUTE;
    DEALLOCATE PREPARE SQL_EXP_EXECUTE;
    END IF;
  END //
  DELIMITER ;";

    private const string AfterAddPrimaryKeyMigrationEnd = @"DROP PROCEDURE `MYSQL_AFTER_ADD_PRIMARY_KEY`;";

    #endregion Custom SQL
  }
#endif
}
