using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using System;
using Xunit;


namespace nc_spark_tests
{
    public class SparkIntegrationTests
    {
        [Fact]
        public void TestSparkJob()
        {
            // Define the Spark master URI running in Docker.
            var sparkMasterUri = "spark://spark-master:7077"; // Ensure Spark master is listening on 7077

            // Initialize SparkSession.
            SparkSession spark = SparkSession
                .Builder()
                .AppName("xUnit Spark Integration Test")
                .Master(sparkMasterUri) // Connect to the Spark master in Docker.
                .GetOrCreate();



            // Create a DataFrame from a sample data collection.
            var rows = new List<GenericRow>
            {
                new GenericRow(new object[] { "Alice", 30 }),
                new GenericRow(new object[] { "Bob", 25 }),
                new GenericRow(new object[] { "Charlie", 35 })
            };

            var schema = new StructType(new[]
            {
                new StructField("Name", new StringType(), isNullable: false),
                new StructField("Age", new IntegerType(), isNullable: false)
            });

            // Create a DataFrame from the collection.
            DataFrame dataFrame = spark.CreateDataFrame(rows, schema);

            // Perform some transformations (simple count).
            long count = dataFrame.Count();

            // Assert that the count matches the expected value.
            Assert.Equal(3, count);

            // Stop the Spark session.
            spark.Stop();
        }
    }
}
