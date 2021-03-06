﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SqlChic.PerfTests
{
	public class TestSuiteLogger
		: IDisposable
	{
		private readonly int _iterations;
		private readonly TextWriter _RawCsvOutput;
		private readonly TextWriter _ConcurrencyCsvOutput;
		private readonly List<Tuple<string, int, int, TestStats>> _Entries = new List<Tuple<string, int, int, TestStats>>();

		public TestSuiteLogger(string rawCsvFilePath, string concurrencyCsvFilePath, int iterations)
		{
			_iterations = iterations;
			if (!String.IsNullOrWhiteSpace(rawCsvFilePath))
			{
				if(System.IO.File.Exists(rawCsvFilePath))
					System.IO.File.Delete(rawCsvFilePath);
				_RawCsvOutput = new StreamWriter(rawCsvFilePath);
				_RawCsvOutput.WriteLine("Test,Iterations,Concurrency,\"Median Time (ms)\",\"Mean Time (ms)\",\"StdDev (ms)\"");
			}
			else
			{
				_RawCsvOutput = TextWriter.Null;
			}
			if (!String.IsNullOrWhiteSpace(concurrencyCsvFilePath))
			{
				if(System.IO.File.Exists(concurrencyCsvFilePath))
					System.IO.File.Delete(concurrencyCsvFilePath);
				_ConcurrencyCsvOutput = new StreamWriter(concurrencyCsvFilePath);
			}
			else
			{
				_ConcurrencyCsvOutput = TextWriter.Null;
			}
		}

		public TestRunLogger BeginLoggingRun(int concurrency)
		{
			return new TestRunLogger(LogTestResult, _iterations, concurrency);
		}

		public void Dispose()
		{
			if (_RawCsvOutput != TextWriter.Null)
			{
				FinishRawCsvOutput();
			}
			if (_ConcurrencyCsvOutput != TextWriter.Null)
			{
				FinishConcurrencyCsvOutput();
			}
		}

		private void FinishConcurrencyCsvOutput()
		{
			try
			{
				if (_Entries.Count > 0)
				{
					var byTest = _Entries.ToLookup(x => x.Item1, x => Tuple.Create(x.Item2, x.Item3, x.Item4));
					var testNames =
						byTest.Select(x => Tuple.Create(x.Key, x.Select(y => y.Item3.Mean.TotalMilliseconds).GeoAverage()))
							  .OrderBy(x => x.Item2)
							  .Select(x => x.Item1)
							  .ToList();
					_ConcurrencyCsvOutput.Write("Concurrencies");
					testNames.ForEach(x => _ConcurrencyCsvOutput.Write(",\"{0}\"", x));
					_ConcurrencyCsvOutput.WriteLine();
					var concurrencies = _Entries.Select(x => x.Item3).Distinct().OrderBy(x => x).ToArray();
					foreach (var concurrency in concurrencies)
					{
						_ConcurrencyCsvOutput.Write(concurrency);
						foreach (var test in testNames)
						{
							var dp = byTest[test].SingleOrDefault(x => x.Item2 == concurrency);
							if (dp == null)
							{
								_ConcurrencyCsvOutput.Write(",");
							}
							else
							{
								_ConcurrencyCsvOutput.Write(",{0}", dp.Item3.Mean.TotalMilliseconds);
							}
						}
						_ConcurrencyCsvOutput.WriteLine();
						_ConcurrencyCsvOutput.Flush();
					}					
				}
			}
			finally
			{
				_ConcurrencyCsvOutput.Dispose();
			}
		}

		private void FinishRawCsvOutput()
		{
			_RawCsvOutput.Flush();
			_RawCsvOutput.Dispose();
		}

		private void LogTestResult(string testName, int iterations, int concurrency, TestStats stats)
		{
			Program.LogTestToConsole(testName, stats);
			if (_RawCsvOutput != TextWriter.Null)
			{
				_RawCsvOutput.WriteLine("\"{0}\",{1},{2},{3},{4},{5}", testName, iterations, concurrency, stats.Median.TotalMilliseconds, stats.Mean.TotalMilliseconds, stats.StdDev.TotalMilliseconds);
			}
			_Entries.Add(Tuple.Create(testName, iterations, concurrency, stats));
		}

		public class TestRunLogger
			: IDisposable
		{
			private readonly Action<string, int, int, TestStats> _logTestResult;
			private readonly int _iterations;
			private readonly int _concurrency;

			public TestRunLogger(Action<string, int, int, TestStats> logTestResult, int iterations, int concurrency)
			{
				_logTestResult = logTestResult;
				_iterations = iterations;
				_concurrency = concurrency;
			}

			public void LogTestResult(string testName, TestStats stats)
			{
				if (_logTestResult != null)
				{
					_logTestResult(testName, _iterations, _concurrency, stats);
				}
			}

			void IDisposable.Dispose()
			{
				// noop
			}
		}
	}
}