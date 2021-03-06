﻿using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using NUnit.Framework;
using ReliableSqlConnection = GlebTeterin.ReliableSql.ReliableSqlConnection;

namespace IntegrationTests
{
	[TestFixture]
	public class SqlConnectionTests
	{
		private const string EmptyConnectionString = "";
		private const string FakeConnectionString = "Data Source=test\test;Initial Catalog=Test; Connect Timeout=1; Connection Timeout=1; Pooling=false; Persist Security Info=True;User ID=Test; password=test ";

		[Test]
		public void InvalidConnectionString()
		{
			var executor = InitExecutor(EmptyConnectionString, 0, 2);

			var testResult = executor.Execute(Guid.NewGuid(), 2, 50001, "custom error");

			Assert.That(testResult.RetryCount, Is.EqualTo(0));
			Assert.That(testResult.ThrownException, Is.Not.Null);
			Assert.That(testResult.ThrownException, Is.TypeOf<InvalidOperationException>());
		}

		[Test]
		public void MockConnectionString()
		{
			var executor = InitExecutor(FakeConnectionString, 0, 2);

			var testResult = executor.Execute(Guid.NewGuid(), 2, 50001, "custom error");

			Assert.That(testResult.RetryCount, Is.EqualTo(0));
			Assert.That(testResult.ThrownException, Is.Not.Null);
			Assert.That(testResult.ThrownException, Is.TypeOf<SqlException>());
		}

		[Test]
		public void SqlException_NotUsable()
		{
			var executor = InitExecutor(Config.ConnectionString, 0, 1);

			var testResult = executor.Execute(Guid.NewGuid(), 2, 50002, "custom error");

			Assert.That(testResult.RetryCount, Is.EqualTo(1));
			Assert.That(testResult.ThrownException, Is.Not.Null);
			Assert.That(testResult.ThrownException, Is.TypeOf<SqlException>());
			Assert.That(((SqlException)testResult.ThrownException).Message, Is.EqualTo("custom error"));
		}
		
		[Test]
		public void NotOpenedConnection_ShouldBeOpened()
		{
			var cnn = new ReliableSqlConnection(
				Config.ConnectionString,
				new MockRetryStrategy(),
				new FixedInterval(1, TimeSpan.FromMilliseconds(0)));

			var command = cnn.CreateCommand();
			command.CommandText = "SELECT 1";

			if (cnn.State != ConnectionState.Closed)
				throw new Exception("It's necessary for the test to keep connection closed.");

			int resul = (int)command.ExecuteScalar();

			Assert.That(resul, Is.EqualTo(1));
		}

		private static TestExecutor InitExecutor(string connectionString, int delayMs, int retries)
		{
			var executor = new TestExecutor(connectionString, new MockRetryStrategy(), new FixedInterval(retries, TimeSpan.FromMilliseconds(delayMs)));

			return executor;
		}
	}
}
