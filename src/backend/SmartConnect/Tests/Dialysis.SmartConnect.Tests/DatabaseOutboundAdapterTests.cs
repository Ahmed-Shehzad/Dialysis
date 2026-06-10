using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class DatabaseOutboundAdapterTests
{
    private static IntegrationMessage Build(string parametersJson, byte[] payload, params (string k, string v)[] metadata)
    {
        var meta = ImmutableDictionary<string, string>.Empty
            .Add(HttpOutboundAdapter.ParametersMetadataKey, parametersJson);
        foreach (var (k, v) in metadata)
        {
            meta = meta.Add(k, v);
        }

        return new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "corr-1",
            Payload = payload,
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = meta,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    [Fact]
    public async Task Sends_Parameterized_Command_And_Binds_Payload_String_Async()
    {
        var connection = new RecordingConnection();
        var factory = new RecordingFactory(connection);
        var adapter = new DatabaseOutboundAdapter(factory);

        var json = """
            {
              "Provider": 0,
              "ConnectionStringName": "SmartConnect",
              "Sql": "INSERT INTO inbox(body) VALUES (@body)",
              "Parameters": [
                { "Name": "@body", "Source": 1 }
              ]
            }
            """;
        var msg = Build(json, "hello"u8.ToArray());

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorDetail);
        Assert.Single(connection.Commands);
        Assert.Equal("INSERT INTO inbox(body) VALUES (@body)", connection.Commands[0].CommandText);
        Assert.Equal("hello", connection.Commands[0].BoundParameters["@body"]);
        Assert.Equal(DatabaseProvider.SqlServer, factory.LastProvider);
        Assert.Equal("SmartConnect", factory.LastConnectionStringName);
    }

    [Fact]
    public async Task Binds_Metadata_Correlation_And_Message_Ids_Async()
    {
        var connection = new RecordingConnection();
        var adapter = new DatabaseOutboundAdapter(new RecordingFactory(connection));

        var json = """
            {
              "Provider": 1,
              "ConnectionStringName": "Pg",
              "Sql": "INSERT INTO m(corr, msg, mt) VALUES (@c, @m, @mt)",
              "Parameters": [
                { "Name": "@c", "Source": 3 },
                { "Name": "@m", "Source": 5 },
                { "Name": "@mt", "Source": 2, "Path": "tenant" }
              ]
            }
            """;
        var msg = Build(json, "x"u8.ToArray(), ("tenant", "acme"));

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorDetail);
        var bound = connection.Commands[0].BoundParameters;
        Assert.Equal(msg.CorrelationId, bound["@c"]);
        Assert.Equal(msg.Id.ToString(), bound["@m"]?.ToString());
        Assert.Equal("acme", bound["@mt"]);
    }

    [Fact]
    public async Task Missing_Metadata_Value_Binds_Dbnull_Async()
    {
        var connection = new RecordingConnection();
        var adapter = new DatabaseOutboundAdapter(new RecordingFactory(connection));

        var json = """
            {
              "Provider": 0,
              "ConnectionStringName": "S",
              "Sql": "x",
              "Parameters": [ { "Name": "@v", "Source": 2, "Path": "absent" } ]
            }
            """;
        var msg = Build(json, "x"u8.ToArray());

        await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.Equal(DBNull.Value, connection.Commands[0].BoundParameters["@v"]);
    }

    /// <summary>
    /// Regression: a payload containing classic SQL-injection text must be passed verbatim as a
    /// bound parameter value — never substituted into the SQL command text.
    /// </summary>
    [Fact]
    public async Task Sql_Injection_Payload_Is_Bound_Not_Concatenated_Async()
    {
        var connection = new RecordingConnection();
        var adapter = new DatabaseOutboundAdapter(new RecordingFactory(connection));

        var json = """
            {
              "Provider": 0,
              "ConnectionStringName": "S",
              "Sql": "INSERT INTO t(body) VALUES (@p0)",
              "Parameters": [ { "Name": "@p0", "Source": 1 } ]
            }
            """;
        var attack = "'; DROP TABLE users; --";
        var msg = Build(json, Encoding.UTF8.GetBytes(attack));

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.True(result.Succeeded, result.ErrorDetail);
        var cmd = connection.Commands[0];
        Assert.DoesNotContain("DROP TABLE", cmd.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(attack, cmd.BoundParameters["@p0"]);
    }

    [Fact]
    public async Task Missing_Connection_String_Or_Sql_Returns_Failure_Async()
    {
        var adapter = new DatabaseOutboundAdapter(new RecordingFactory(new RecordingConnection()));
        var msg = Build("""{"Provider":0}""", "x"u8.ToArray());

        var result = await adapter.SendAsync(msg, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    private sealed class RecordingFactory : IDatabaseOutboundConnectionFactory
    {
        private readonly RecordingConnection _connection;
        public RecordingFactory(RecordingConnection connection) => _connection = connection;
        public DatabaseProvider LastProvider { get; private set; }

        public string? LastConnectionStringName { get; private set; }

        public Task<DbConnection> OpenAsync(DatabaseProvider provider, string connectionStringName, CancellationToken cancellationToken)
        {
            LastProvider = provider;
            LastConnectionStringName = connectionStringName;
            _connection.OpenedTimes++;
            return Task.FromResult<DbConnection>(_connection);
        }
    }

    private sealed class RecordingConnection : DbConnection
    {
        public List<RecordingCommand> Commands { get; } = [];

        public int OpenedTimes;

        [AllowNull]
        public override string ConnectionString { get; set; } = "fake";

        public override string Database => "fake";

        public override string DataSource => "fake";

        public override string ServerVersion => "0";

        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close() { }

        public override void Open() { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

        protected override DbCommand CreateDbCommand()
        {
            var cmd = new RecordingCommand { Connection = this };
            Commands.Add(cmd);
            return cmd;
        }
    }

    private sealed class RecordingCommand : DbCommand
    {
        public Dictionary<string, object?> BoundParameters { get; } = new(StringComparer.Ordinal);

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection { get; } = new RecordingParameterCollection();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }

        public override int ExecuteNonQuery()
        {
            CapturePending();
            return 1;
        }

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            CapturePending();
            return Task.FromResult(1);
        }

        public override object? ExecuteScalar() => null;

        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new RecordingParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

        private void CapturePending()
        {
            foreach (DbParameter p in DbParameterCollection)
            {
                BoundParameters[p.ParameterName] = p.Value;
            }
        }
    }

    private sealed class RecordingParameter : DbParameter
    {
        public override DbType DbType { get; set; }

        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

        public override bool IsNullable { get; set; }

        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;

        public override int Size { get; set; }

        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;

        public override bool SourceColumnNullMapping { get; set; }

        public override object? Value { get; set; }

        public override void ResetDbType() { }
    }

    private sealed class RecordingParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];

        public override int Count => _items.Count;

        public override object SyncRoot { get; } = new();

        public override int Add(object value)
        {
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var v in values)
                _items.Add((DbParameter)v);
        }

        public override void Clear() => _items.Clear();

        public override bool Contains(object value) => _items.Contains((DbParameter)value);

        public override bool Contains(string value) => IndexOf(value) >= 0;

        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

        public override IEnumerator GetEnumerator() => _items.GetEnumerator();

        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

        public override int IndexOf(string parameterName)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].ParameterName, parameterName, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);

        public override void Remove(object value) => _items.Remove((DbParameter)value);

        public override void RemoveAt(int index) => _items.RemoveAt(index);

        public override void RemoveAt(string parameterName)
        {
            var i = IndexOf(parameterName);
            if (i >= 0)
                _items.RemoveAt(i);
        }

        protected override DbParameter GetParameter(int index) => _items[index];

        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];

        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }
}
