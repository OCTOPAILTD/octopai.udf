# Spark 2.4.8 with Scala 2.11 Container

This directory contains your Spark applications and scripts.

## 🚀 Quick Start

### 1. Start the Spark Container

```bash
cd docker
docker-compose up -d spark
```

### 2. Access Spark Shell

```bash
docker exec -it nifi-metadata-spark spark-shell
```

### 3. Access Spark Master Web UI

Open in browser: http://localhost:8080

### 4. Access Spark Application UI

When running Spark jobs: http://localhost:4040

## 📝 Running Spark Applications

### Interactive Spark Shell (Scala)

```bash
docker exec -it nifi-metadata-spark spark-shell
```

### PySpark Shell (Python)

```bash
docker exec -it nifi-metadata-spark pyspark
```

### Submit a Spark Application

```bash
docker exec -it nifi-metadata-spark spark-submit \
  --class com.example.MainClass \
  --master local[*] \
  /workspace/your-app.jar
```

### Run Scala Script

```bash
docker exec -it nifi-metadata-spark scala /workspace/your-script.scala
```

## 📂 Directory Structure

Place your Spark applications in this directory:
- `*.scala` - Scala scripts
- `*.py` - Python scripts
- `*.jar` - Compiled Spark applications
- `data/` - Input/output data files

## 🔧 Configuration

### Versions
- **Spark:** 2.4.8
- **Scala:** 2.11.12
- **Hadoop:** 2.7
- **Java:** OpenJDK 8

### Ports
- **4040:** Spark UI (application)
- **7077:** Spark Master
- **8080:** Spark Master Web UI
- **8081:** Spark Worker Web UI

## 💡 Example: Simple Word Count

Create a file `wordcount.scala` in this directory:

```scala
val textFile = sc.textFile("/workspace/data/input.txt")
val counts = textFile.flatMap(line => line.split(" "))
                     .map(word => (word, 1))
                     .reduceByKey(_ + _)
counts.saveAsTextFile("/workspace/data/output")
```

Run it:

```bash
docker exec -it nifi-metadata-spark spark-shell -i /workspace/wordcount.scala
```

## 🔗 Integration with Other Services

The Spark container is connected to the same network as your other services:
- **ArangoDB:** `arangodb:8529`
- **OpenSearch:** `opensearch:9200`
- **Redis:** `redis:6379`
- **C# API:** `csharp-api:5000`

You can access these services from your Spark applications using the container names as hostnames.

## 📊 Example: Connect to OpenSearch

```scala
import org.apache.spark.sql.SparkSession

val spark = SparkSession.builder()
  .appName("OpenSearch Integration")
  .master("local[*]")
  .getOrCreate()

// Add OpenSearch Spark connector to your dependencies
// Then read/write data from/to OpenSearch
```

## 🛠️ Management Commands

### View Logs
```bash
docker logs nifi-metadata-spark -f
```

### Restart Container
```bash
docker-compose -f docker/docker-compose.yml restart spark
```

### Stop Container
```bash
docker-compose -f docker/docker-compose.yml stop spark
```

### Access Container Shell
```bash
docker exec -it nifi-metadata-spark /bin/bash
```

## 📚 Resources

- [Spark 2.4.8 Documentation](https://spark.apache.org/docs/2.4.8/)
- [Scala 2.11 Documentation](https://docs.scala-lang.org/overviews/scala-book/introduction.html)
- [Spark SQL Guide](https://spark.apache.org/docs/2.4.8/sql-programming-guide.html)
