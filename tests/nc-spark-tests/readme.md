

To run Apache Spark:

```bash
docker run -d --name spark-master -p 7077:7077 -p 8080:8080 apache/spark:latest /opt/spark/bin/spark-class org.apache.spark.deploy.master.Master
docker run -d --name spark-worker-1 -p 8081:8081 apache/spark:latest /opt/spark/bin/spark-class org.apache.spark.deploy.worker.Worker spark://172.17.0.2:7077
```

Notes:
- -p 7077:7077: Exposes port 7077 for the Spark master.
- -p 8080:8080: Exposes port 8080 for the web UI.

